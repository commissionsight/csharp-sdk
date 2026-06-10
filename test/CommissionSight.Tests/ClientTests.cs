using System.Net;
using Xunit;

namespace CommissionSight.Tests;

public class ClientTests
{
    private const string BaseUrl = "https://api.test/v1";

    private static (CommissionSightClient Client, StubHttpMessageHandler Handler) Make(
        Func<HttpRequestMessage, HttpResponseMessage> responder, string? token = "test-token")
    {
        var handler = new StubHttpMessageHandler(responder);
        var client = new CommissionSightClient(
            new CommissionSightClientOptions { BaseUrl = BaseUrl, Token = token },
            new HttpClient(handler));
        return (client, handler);
    }

    [Fact]
    public async Task Health_parses_response()
    {
        var (client, _) = Make(_ => StubHttpMessageHandler.Json(
            """{"status":"ok","service":"commissionsight-api","environment":"production"}"""));

        var health = await client.HealthAsync();

        Assert.Equal("ok", health.Status);
        Assert.Equal("production", health.Environment);
    }

    [Fact]
    public async Task Sets_bearer_authorization_header()
    {
        var (client, handler) = Make(_ => StubHttpMessageHandler.Json(
            """{"accountId":"acct_1","name":"Acme","status":"active"}"""));

        await client.MeAsync();

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("test-token", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task ListJobs_builds_path_and_query()
    {
        var (client, handler) = Make(_ => StubHttpMessageHandler.Json("""{"data":[]}"""));

        await client.ListJobsAsync(status: "failed", carrierId: "c1", limit: 10, offset: 20);

        var uri = handler.LastRequest!.RequestUri!.ToString();
        Assert.StartsWith($"{BaseUrl}/jobs?", uri);
        Assert.Contains("status=failed", uri);
        Assert.Contains("carrierId=c1", uri);
        Assert.Contains("limit=10", uri);
        Assert.Contains("offset=20", uri);
    }

    [Fact]
    public async Task JobResults_parses_status_flags_and_money()
    {
        var (client, _) = Make(_ => StubHttpMessageHandler.Json(
            """
            {
              "data": [{
                "memberRefId": "m1", "policyRefId": "p1", "status": "yellow",
                "flags": ["COMMISSION_CHANGED", "CHARGEBACK"],
                "commissionAmount": 120.55, "prevCommissionAmount": 140.00,
                "commissionOwed": 19.45, "comparedAgainstPeriod": "2026-03",
                "memberExternalId": "X1", "memberName": "Ada", "email": null,
                "planName": "Gold", "policyNumber": "POL1", "premiumAmount": 1000.0
              }],
              "pagination": { "limit": 50, "offset": 0, "hasMore": false },
              "period": { "year": 2026, "month": 4 }
            }
            """));

        var page = await client.GetJobResultsAsync("job1");

        var row = Assert.Single(page.Data);
        Assert.Equal(Status.Yellow, row.Status);
        Assert.Equal([Flag.CommissionChanged, Flag.Chargeback], row.Flags);
        Assert.Equal(120.55m, row.CommissionAmount);
        Assert.Equal(19.45m, row.CommissionOwed);
        Assert.Equal(2026, page.Period!.Year);
        Assert.False(page.Pagination!.HasMore);
    }

    [Fact]
    public async Task Unknown_flag_falls_back_to_Unknown()
    {
        var (client, _) = Make(_ => StubHttpMessageHandler.Json(
            """{"data":[{"memberRefId":"m1","status":"green","flags":["A_BRAND_NEW_FLAG"],"commissionOwed":0}]}"""));

        var page = await client.GetJobResultsAsync("job1");

        Assert.Equal(Flag.Unknown, Assert.Single(page.Data).Flags[0]);
    }

    [Fact]
    public async Task Timestamps_accept_epoch_number_and_iso_string()
    {
        // createdAt as epoch ms (number), uploadedAt as ISO string — both must parse.
        var (client, _) = Make(_ => StubHttpMessageHandler.Json(
            """
            {
              "id": "f1", "accountId": "a1", "carrierId": "c1", "periodYear": 2026, "periodMonth": 4,
              "originalFilename": "x.csv", "byteSize": 10, "checksumSha256": "abc",
              "uploadedAt": "2026-04-02T15:10:00.000Z"
            }
            """));

        var file = await client.GetFileAsync("f1");

        Assert.Equal(2026, file.UploadedAt.Year);
        Assert.Equal(4, file.UploadedAt.Month);
    }

    [Fact]
    public async Task Rollup_new_field_maps_to_New()
    {
        var (client, _) = Make(_ => StubHttpMessageHandler.Json(
            """
            {
              "period": "2026-04",
              "totals": { "memberCount": 100, "green": 80, "yellow": 12, "red": 8, "new": 5, "reappeared": 1,
                "commissionAtRisk": 1000.50, "commissionDropped": 600, "commissionReduced": 400.50, "reducedCount": 4,
                "commissionOwed": 250.25, "owedEvaluated": 90, "owedTotal": 100, "chargebackCount": 2, "chargebackAmount": 75.00 },
              "byCarrier": []
            }
            """));

        var rollup = await client.RollupAsync("2026-04");

        Assert.Equal(5, rollup.Totals.New);
        Assert.Equal(1000.50m, rollup.Totals.CommissionAtRisk);
        Assert.Equal(250.25m, rollup.Totals.CommissionOwed);
    }

    [Fact]
    public async Task Error_response_maps_to_exception_with_code()
    {
        var (client, _) = Make(_ => StubHttpMessageHandler.Json(
            """{"title":"Period already exists","detail":"Use replace=true","code":"period_exists"}""",
            HttpStatusCode.Conflict));

        var ex = await Assert.ThrowsAsync<CommissionSightApiException>(
            () => client.GetFileAsync("f1"));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal(409, ex.Status);
        Assert.Equal("Period already exists", ex.Title);
        Assert.Equal("Use replace=true", ex.Detail);
        Assert.Equal("period_exists", ex.Code);
    }

    [Fact]
    public async Task UploadFile_sends_multipart_with_idempotency_header()
    {
        var (client, handler) = Make(_ => StubHttpMessageHandler.Json(
            """{"jobId":"j1","fileId":"f1","status":"queued"}"""));

        var file = FileUpload.FromBytes("a,b\n1,2\n"u8.ToArray(), "statement.csv", "text/csv");
        var result = await client.UploadFileAsync(file, "carrier1", 2026, 4, idempotencyKey: "idem-1");

        Assert.Equal("j1", result.JobId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal($"{BaseUrl}/files", handler.LastRequest.RequestUri!.ToString());
        Assert.StartsWith("multipart/form-data", handler.LastRequest.Content!.Headers.ContentType!.MediaType);
        Assert.True(handler.LastRequest.Headers.Contains("idempotency-key"));
    }

    [Fact]
    public async Task SetBillingRate_transmits_explicit_null_when_clearing()
    {
        var (client, handler) = Make(_ => StubHttpMessageHandler.Json(
            """{"accountId":"a1","customRateCents":null}"""));

        await client.Admin.SetBillingRateAsync("a1", null);

        Assert.Contains("\"rateCents\":null", handler.LastBody);
    }

    [Fact]
    public async Task UpsertExpectedRate_serializes_enum_as_wire_string()
    {
        var (client, handler) = Make(_ => StubHttpMessageHandler.Json(
            """{"id":"r1","carrierId":"c1","planCode":null,"rateType":"percent_of_premium","rateValue":0.2,"rescoredPeriods":3}"""));

        var rate = await client.UpsertExpectedRateAsync("c1", RateType.PercentOfPremium, 0.2);

        Assert.Contains("\"rateType\":\"percent_of_premium\"", handler.LastBody);
        Assert.Equal(RateType.PercentOfPremium, rate.RateType);
        Assert.Equal(3, rate.RescoredPeriods);
    }

    [Fact]
    public async Task DownloadExceptions_returns_raw_csv_text()
    {
        var (client, _) = Make(_ => StubHttpMessageHandler.Text("_row,_errors\n3,bad\n"));

        var csv = await client.DownloadExceptionsAsync("job1");

        Assert.StartsWith("_row,_errors", csv);
    }

    [Fact]
    public async Task Health_works_without_a_token()
    {
        var (client, handler) = Make(
            _ => StubHttpMessageHandler.Json("""{"status":"ok","service":"api","environment":"production"}"""),
            token: null);

        await client.HealthAsync();

        Assert.Null(handler.LastRequest!.Headers.Authorization);
    }
}
