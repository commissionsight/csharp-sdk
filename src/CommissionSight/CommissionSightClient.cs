using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommissionSight.Json;

namespace CommissionSight;

/// <summary>
/// A typed client for the CommissionSight API. Thread-safe; create one and reuse it
/// (it wraps a single <see cref="HttpClient"/>). Pass your own <see cref="HttpClient"/>
/// to integrate with <c>IHttpClientFactory</c> / dependency injection.
/// </summary>
public sealed partial class CommissionSightClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly string _baseUrl;
    private string? _token;

    /// <summary>Admin endpoints (require an allowlisted admin session token).</summary>
    public AdminApi Admin { get; }

    /// <summary>Create a client that manages its own <see cref="HttpClient"/>.</summary>
    public CommissionSightClient(CommissionSightClientOptions options)
        : this(options, new HttpClient(), ownsHttpClient: true)
    {
    }

    /// <summary>Create a client over a caller-supplied <see cref="HttpClient"/> (DI friendly).</summary>
    public CommissionSightClient(CommissionSightClientOptions options, HttpClient httpClient)
        : this(options, httpClient, ownsHttpClient: false)
    {
    }

    private CommissionSightClient(CommissionSightClientOptions options, HttpClient httpClient, bool ownsHttpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.BaseUrl);
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _token = options.Token;
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttpClient = ownsHttpClient;
        Admin = new AdminApi(this);
    }

    /// <summary>Set or clear the bearer token used for subsequent requests.</summary>
    public void SetToken(string? token) => _token = token;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    // ─── HTTP core ────────────────────────────────────────────────────────────

    internal async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        HttpContent? content = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(method, path, content, headers);
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw await BuildErrorAsync(response, cancellationToken).ConfigureAwait(false);
        }

        var result = await response.Content
            .ReadFromJsonAsync<T>(CommissionSightJson.Options, cancellationToken)
            .ConfigureAwait(false);
        return result!;
    }

    internal async Task SendNoContentAsync(
        HttpMethod method,
        string path,
        HttpContent? content = null,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(method, path, content, headers);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildErrorAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<string> SendForTextAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(method, path, content: null, headers: null);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildErrorAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private HttpRequestMessage BuildRequest(
        HttpMethod method,
        string path,
        HttpContent? content,
        IReadOnlyDictionary<string, string>? headers)
    {
        var request = new HttpRequestMessage(method, _baseUrl + path);
        if (_token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        if (content is not null)
        {
            request.Content = content;
        }

        return request;
    }

    private static async Task<CommissionSightApiException> BuildErrorAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var title = response.ReasonPhrase ?? response.StatusCode.ToString();
        string? detail = null;
        JsonElement? body = null;

        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                body = doc.RootElement.Clone();
                if (body.Value.ValueKind == JsonValueKind.Object)
                {
                    if (body.Value.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                    {
                        title = t.GetString() ?? title;
                    }

                    if (body.Value.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                    {
                        detail = d.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Non-JSON error body — keep the status reason as the title.
            }
        }

        return new CommissionSightApiException(response.StatusCode, title, detail, body);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static StringContent JsonBody(object body) => JsonBodyOf(body);

    /// <summary>Serialize a request body (omitting null members). Shared with <see cref="AdminApi"/>.</summary>
    internal static StringContent JsonBodyOf(object body) =>
        new(JsonSerializer.Serialize(body, CommissionSightJson.Options), Encoding.UTF8, "application/json");

    /// <summary>Serialize a request body, writing explicit nulls (for clear-to-null fields).</summary>
    internal static StringContent JsonBodyVerbatim(object body) =>
        new(JsonSerializer.Serialize(body, CommissionSightJson.OptionsIncludingNulls), Encoding.UTF8, "application/json");

    private static string Query(params (string Key, string? Value)[] parameters)
    {
        var sb = new StringBuilder();
        foreach (var (key, value) in parameters)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            sb.Append(sb.Length == 0 ? '?' : '&');
            sb.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
        }

        return sb.ToString();
    }

    private static string? Num(int? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static string? Num(long? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static string? Flag(bool value) => value ? "true" : null;

    private static StreamContent FileContent(FileUpload file)
    {
        var content = new StreamContent(file.Content);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        return content;
    }

    // ─── Carriers / configs ─────────────────────────────────────────────────────

    /// <summary>List the carriers available to this account.</summary>
    public Task<Page<CarrierSummary>> ListCarriersAsync(bool withConfig = false, CancellationToken cancellationToken = default) =>
        SendAsync<Page<CarrierSummary>>(HttpMethod.Get, "/carriers" + Query(("withConfig", Flag(withConfig))), cancellationToken: cancellationToken);

    /// <summary>Get a single carrier.</summary>
    public Task<CarrierSummary> GetCarrierAsync(string carrierId, CancellationToken cancellationToken = default) =>
        SendAsync<CarrierSummary>(HttpMethod.Get, $"/carriers/{carrierId}", cancellationToken: cancellationToken);

    /// <summary>List a carrier's config versions (global + your overrides).</summary>
    public Task<Page<JsonElement>> ListConfigsAsync(string carrierId, CancellationToken cancellationToken = default) =>
        SendAsync<Page<JsonElement>>(HttpMethod.Get, $"/carriers/{carrierId}/configs", cancellationToken: cancellationToken);

    /// <summary>Fetch a specific carrier config version.</summary>
    public Task<JsonElement> GetConfigVersionAsync(string carrierId, int version, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, $"/carriers/{carrierId}/configs/{version}", cancellationToken: cancellationToken);

    /// <summary>Create an account-scoped carrier config override.</summary>
    public Task<CreateConfigResult> CreateConfigAsync(string carrierId, object config, CancellationToken cancellationToken = default) =>
        SendAsync<CreateConfigResult>(HttpMethod.Post, $"/carriers/{carrierId}/configs", JsonBody(config), cancellationToken: cancellationToken);

    /// <summary>Dry-run a config against a sample file (maps + previews, persists nothing).</summary>
    public Task<ConfigPreview> TestConfigAsync(string carrierId, object config, FileUpload file, CancellationToken cancellationToken = default)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(JsonSerializer.Serialize(config, CommissionSightJson.Options)), "config" },
            { FileContent(file), "file", file.FileName },
        };
        return SendAsync<ConfigPreview>(HttpMethod.Post, $"/carriers/{carrierId}/configs/test", form, cancellationToken: cancellationToken);
    }

    /// <summary>Infer a draft config from a sample file.</summary>
    public Task<InferredConfig> InferConfigAsync(string carrierId, FileUpload file, string? sheetName = null, CancellationToken cancellationToken = default)
    {
        var form = new MultipartFormDataContent { { FileContent(file), "file", file.FileName } };
        if (!string.IsNullOrEmpty(sheetName))
        {
            form.Add(new StringContent(sheetName), "sheetName");
        }

        return SendAsync<InferredConfig>(HttpMethod.Post, $"/carriers/{carrierId}/configs/infer", form, cancellationToken: cancellationToken);
    }

    // ─── Files ────────────────────────────────────────────────────────────────

    /// <summary>Upload a statement for a carrier + period (async ingest; returns the queued job).</summary>
    /// <param name="replace">Replace an existing statement for this carrier+period (otherwise a duplicate returns 409 <c>period_exists</c>).</param>
    /// <param name="workspaceId">Required when the account has multiple workspaces enabled.</param>
    public Task<UploadResult> UploadFileAsync(
        FileUpload file,
        string carrierId,
        int periodYear,
        int periodMonth,
        string? webhookUrl = null,
        string? idempotencyKey = null,
        bool replace = false,
        string? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        var form = new MultipartFormDataContent
        {
            { FileContent(file), "file", file.FileName },
            { new StringContent(carrierId), "carrierId" },
            { new StringContent(periodYear.ToString(CultureInfo.InvariantCulture)), "periodYear" },
            { new StringContent(periodMonth.ToString(CultureInfo.InvariantCulture)), "periodMonth" },
        };
        if (!string.IsNullOrEmpty(webhookUrl))
        {
            form.Add(new StringContent(webhookUrl), "webhookUrl");
        }

        if (replace)
        {
            form.Add(new StringContent("true"), "replace");
        }

        if (!string.IsNullOrEmpty(workspaceId))
        {
            form.Add(new StringContent(workspaceId), "workspaceId");
        }

        var headers = string.IsNullOrEmpty(idempotencyKey)
            ? null
            : new Dictionary<string, string> { ["idempotency-key"] = idempotencyKey };
        return SendAsync<UploadResult>(HttpMethod.Post, "/files", form, headers, cancellationToken);
    }

    /// <summary>List uploaded files (cursor-paginated by upload time).</summary>
    public Task<Page<FileSummary>> ListFilesAsync(string? carrierId = null, int? limit = null, long? cursor = null, CancellationToken cancellationToken = default) =>
        SendAsync<Page<FileSummary>>(HttpMethod.Get, "/files" + Query(("carrierId", carrierId), ("limit", Num(limit)), ("cursor", Num(cursor))), cancellationToken: cancellationToken);

    /// <summary>Get a single file's metadata.</summary>
    public Task<FileSummary> GetFileAsync(string fileId, CancellationToken cancellationToken = default) =>
        SendAsync<FileSummary>(HttpMethod.Get, $"/files/{fileId}", cancellationToken: cancellationToken);

    /// <summary>Re-score a file's period without re-uploading (after an out-of-order baseline upload).</summary>
    public Task<JobRef> RescoreFileAsync(string fileId, CancellationToken cancellationToken = default) =>
        SendAsync<JobRef>(HttpMethod.Post, $"/files/{fileId}/rescore", cancellationToken: cancellationToken);

    /// <summary>Retract (unapply) a file's carrier+period and re-score the following month.</summary>
    public Task<JobRef> RetractFileAsync(string fileId, CancellationToken cancellationToken = default) =>
        SendAsync<JobRef>(HttpMethod.Delete, $"/files/{fileId}", cancellationToken: cancellationToken);

    /// <summary>Purge the raw statement bytes from object storage (data retention). Idempotent.</summary>
    public Task<PurgeResult> PurgeFileAsync(string fileId, CancellationToken cancellationToken = default) =>
        SendAsync<PurgeResult>(HttpMethod.Post, $"/files/{fileId}/purge", cancellationToken: cancellationToken);

    // ─── Jobs ─────────────────────────────────────────────────────────────────

    /// <summary>List ingest jobs (offset-paginated).</summary>
    public Task<Page<JobSummary>> ListJobsAsync(
        string? status = null,
        string? carrierId = null,
        int? periodYear = null,
        int? periodMonth = null,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<Page<JobSummary>>(HttpMethod.Get, "/jobs" + Query(
            ("status", status), ("carrierId", carrierId), ("periodYear", Num(periodYear)),
            ("periodMonth", Num(periodMonth)), ("limit", Num(limit)), ("offset", Num(offset))),
            cancellationToken: cancellationToken);

    /// <summary>Get a single job.</summary>
    public Task<JobSummary> GetJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        SendAsync<JobSummary>(HttpMethod.Get, $"/jobs/{jobId}", cancellationToken: cancellationToken);

    /// <summary>Download the exception file (rejected rows + their errors) for a job as CSV text.</summary>
    public Task<string> DownloadExceptionsAsync(string jobId, CancellationToken cancellationToken = default) =>
        SendForTextAsync(HttpMethod.Get, $"/jobs/{jobId}/exceptions", cancellationToken);

    /// <summary>Get a job's scored results grid (green/yellow/red), filterable.</summary>
    public Task<JobResultsPage> GetJobResultsAsync(
        string jobId,
        string? status = null,
        bool owedOnly = false,
        bool chargeback = false,
        int? limit = null,
        int? offset = null,
        CancellationToken cancellationToken = default) =>
        SendAsync<JobResultsPage>(HttpMethod.Get, $"/jobs/{jobId}/results" + Query(
            ("status", status), ("owedOnly", Flag(owedOnly)), ("chargeback", Flag(chargeback)),
            ("limit", Num(limit)), ("offset", Num(offset))),
            cancellationToken: cancellationToken);

    /// <summary>Get a job's field-level deltas.</summary>
    public Task<Page<JsonElement>> GetJobDeltasAsync(string jobId, string? memberRefId = null, string? changeType = null, CancellationToken cancellationToken = default) =>
        SendAsync<Page<JsonElement>>(HttpMethod.Get, $"/jobs/{jobId}/deltas" + Query(("memberRefId", memberRefId), ("changeType", changeType)), cancellationToken: cancellationToken);

    /// <summary>Retry a failed job.</summary>
    public Task<JobStatusRef> RetryJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        SendAsync<JobStatusRef>(HttpMethod.Post, $"/jobs/{jobId}/retry", cancellationToken: cancellationToken);

    // ─── Members ──────────────────────────────────────────────────────────────

    /// <summary>List members for the latest (or a given) period.</summary>
    public Task<Page<JsonElement>> ListMembersAsync(string? carrierId = null, string? status = null, int? periodYear = null, int? periodMonth = null, CancellationToken cancellationToken = default) =>
        SendAsync<Page<JsonElement>>(HttpMethod.Get, "/members" + Query(
            ("carrierId", carrierId), ("status", status), ("periodYear", Num(periodYear)), ("periodMonth", Num(periodMonth))),
            cancellationToken: cancellationToken);

    /// <summary>Get a single member.</summary>
    public Task<JsonElement> GetMemberAsync(string memberRefId, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, $"/members/{memberRefId}", cancellationToken: cancellationToken);

    /// <summary>Get a member's rolling MoM status history.</summary>
    public Task<Page<JsonElement>> GetMemberTimelineAsync(string memberRefId, CancellationToken cancellationToken = default) =>
        SendAsync<Page<JsonElement>>(HttpMethod.Get, $"/members/{memberRefId}/timeline", cancellationToken: cancellationToken);

    /// <summary>Full audit journey of a member: every period, source file, status + flags, and changes.</summary>
    public Task<Journey> GetMemberJourneyAsync(string memberRefId, CancellationToken cancellationToken = default) =>
        SendAsync<Journey>(HttpMethod.Get, $"/members/{memberRefId}/journey", cancellationToken: cancellationToken);

    /// <summary>Full audit journey of a single policy (member-scoped).</summary>
    public Task<Journey> GetPolicyJourneyAsync(string policyRefId, CancellationToken cancellationToken = default) =>
        SendAsync<Journey>(HttpMethod.Get, $"/policies/{policyRefId}/journey", cancellationToken: cancellationToken);

    /// <summary>Where/when a member was last seen (period + originating file).</summary>
    public Task<JsonElement> GetMemberLastSeenAsync(string memberRefId, CancellationToken cancellationToken = default) =>
        SendAsync<JsonElement>(HttpMethod.Get, $"/members/{memberRefId}/last-seen", cancellationToken: cancellationToken);

    // ─── Team ─────────────────────────────────────────────────────────────────

    /// <summary>List the account's members.</summary>
    public Task<DataList<TeamMember>> ListTeamAsync(CancellationToken cancellationToken = default) =>
        SendAsync<DataList<TeamMember>>(HttpMethod.Get, "/team", cancellationToken: cancellationToken);

    /// <summary>Invite a teammate by email (passwordless — they sign in with a one-time code).</summary>
    public Task<InviteResult> InviteTeammateAsync(string email, CancellationToken cancellationToken = default) =>
        SendAsync<InviteResult>(HttpMethod.Post, "/team/invites", JsonBody(new { email }), cancellationToken: cancellationToken);

    /// <summary>Remove a teammate and revoke their sessions.</summary>
    public Task RemoveTeammateAsync(string userId, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"/team/{userId}", cancellationToken: cancellationToken);

    // ─── Audit ────────────────────────────────────────────────────────────────

    /// <summary>Read the account's audit trail (newest first). Filter by <paramref name="action"/>.</summary>
    public Task<Page<AuditEvent>> ListAuditAsync(string? action = null, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) =>
        SendAsync<Page<AuditEvent>>(HttpMethod.Get, "/audit" + Query(("action", action), ("limit", Num(limit)), ("offset", Num(offset))), cancellationToken: cancellationToken);

    // ─── Comparisons / reports ────────────────────────────────────────────────

    /// <summary>Compare any two periods (set <paramref name="granularity"/> to quarter/year for QoQ/YoY).</summary>
    public Task<ComparisonResult> CompareAsync(string from, string to, string? carrierId = null, string? workspaceId = null, string? granularity = null, CancellationToken cancellationToken = default) =>
        SendAsync<ComparisonResult>(HttpMethod.Get, "/comparisons" + Query(
            ("from", from), ("to", to), ("carrierId", carrierId), ("workspaceId", workspaceId), ("granularity", granularity)),
            cancellationToken: cancellationToken);

    /// <summary>Green/yellow/red rollup for a period (defaults to the latest period with data).</summary>
    public Task<RollupResult> RollupAsync(string? period = null, string? carrierId = null, string? workspaceId = null, CancellationToken cancellationToken = default) =>
        SendAsync<RollupResult>(HttpMethod.Get, "/reports/rollup" + Query(("period", period), ("carrierId", carrierId), ("workspaceId", workspaceId)), cancellationToken: cancellationToken);

    /// <summary>Chargebacks for a period, each enriched with the policy's original payout.</summary>
    public Task<ChargebacksResult> ListChargebacksAsync(string? period = null, string? carrierId = null, string? workspaceId = null, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) =>
        SendAsync<ChargebacksResult>(HttpMethod.Get, "/chargebacks" + Query(
            ("period", period), ("carrierId", carrierId), ("workspaceId", workspaceId), ("limit", Num(limit)), ("offset", Num(offset))),
            cancellationToken: cancellationToken);

    /// <summary>Attrition for a period (red ÷ members present in the prior period), overall + per carrier.</summary>
    public Task<AttritionResult> AttritionAsync(string period, string? carrierId = null, string? workspaceId = null, CancellationToken cancellationToken = default) =>
        SendAsync<AttritionResult>(HttpMethod.Get, "/reports/attrition" + Query(("period", period), ("carrierId", carrierId), ("workspaceId", workspaceId)), cancellationToken: cancellationToken);

    /// <summary>Month-over-month attrition trend (1–36 periods, default 12), oldest → newest.</summary>
    public Task<AttritionSeriesResult> AttritionSeriesAsync(int? months = null, string? carrierId = null, string? workspaceId = null, CancellationToken cancellationToken = default) =>
        SendAsync<AttritionSeriesResult>(HttpMethod.Get, "/reports/attrition-series" + Query(("months", Num(months)), ("carrierId", carrierId), ("workspaceId", workspaceId)), cancellationToken: cancellationToken);

    /// <summary>
    /// Cumulative audit totals over a period range — summed commission owed / at-risk /
    /// chargebacks (+ counts) with per-period and per-carrier breakdowns, plus cumulative
    /// owed coverage. <paramref name="from"/>/<paramref name="to"/> (YYYY-MM) are inclusive
    /// and both optional (omit → all periods). The figures an agency takes to a carrier audit.
    /// </summary>
    public Task<CumulativeReport> CumulativeAsync(string? from = null, string? to = null, string? carrierId = null, string? workspaceId = null, CancellationToken cancellationToken = default) =>
        SendAsync<CumulativeReport>(HttpMethod.Get, "/reports/cumulative" + Query(("from", from), ("to", to), ("carrierId", carrierId), ("workspaceId", workspaceId)), cancellationToken: cancellationToken);

    /// <summary>Statement-quality signal per carrier for a period (incomplete / wrong-period file detection).</summary>
    public Task<DataQualityReport> DataQualityAsync(string? period = null, string? workspaceId = null, CancellationToken cancellationToken = default) =>
        SendAsync<DataQualityReport>(HttpMethod.Get, "/reports/data-quality" + Query(("period", period), ("workspaceId", workspaceId)), cancellationToken: cancellationToken);

    // ─── Expected commission rates (the "owed" model inputs) ─────────────────────

    /// <summary>List the account's contracted (expected) commission rates.</summary>
    public Task<DataList<ExpectedCommissionRate>> ListExpectedRatesAsync(string? carrierId = null, CancellationToken cancellationToken = default) =>
        SendAsync<DataList<ExpectedCommissionRate>>(HttpMethod.Get, "/expected-rates" + Query(("carrierId", carrierId)), cancellationToken: cancellationToken);

    /// <summary>Upsert the contracted rate for a carrier (+ optional plan). Re-scores affected periods.</summary>
    public Task<UpsertedRate> UpsertExpectedRateAsync(string carrierId, RateType rateType, double rateValue, string? planCode = null, CancellationToken cancellationToken = default) =>
        SendAsync<UpsertedRate>(HttpMethod.Post, "/expected-rates", JsonBody(new
        {
            carrierId,
            planCode,
            rateType,
            rateValue,
        }), cancellationToken: cancellationToken);

    /// <summary>Delete an expected rate by id.</summary>
    public Task DeleteExpectedRateAsync(string id, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"/expected-rates/{id}", cancellationToken: cancellationToken);

    // ─── Webhooks ─────────────────────────────────────────────────────────────

    /// <summary>List webhook subscriptions.</summary>
    public Task<DataList<Webhook>> ListWebhooksAsync(CancellationToken cancellationToken = default) =>
        SendAsync<DataList<Webhook>>(HttpMethod.Get, "/webhooks", cancellationToken: cancellationToken);

    /// <summary>Subscribe to job events. The signing <c>secret</c> is returned ONCE on creation.</summary>
    public Task<CreatedWebhook> CreateWebhookAsync(string url, IEnumerable<WebhookEvent> events, CancellationToken cancellationToken = default) =>
        SendAsync<CreatedWebhook>(HttpMethod.Post, "/webhooks", JsonBody(new { url, events }), cancellationToken: cancellationToken);

    /// <summary>Delete a webhook subscription.</summary>
    public Task DeleteWebhookAsync(string id, CancellationToken cancellationToken = default) =>
        SendNoContentAsync(HttpMethod.Delete, $"/webhooks/{id}", cancellationToken: cancellationToken);

    // ─── Session / service ──────────────────────────────────────────────────────

    /// <summary>The account behind the current token.</summary>
    public Task<AccountInfo> MeAsync(CancellationToken cancellationToken = default) =>
        SendAsync<AccountInfo>(HttpMethod.Get, "/me", cancellationToken: cancellationToken);

    /// <summary>The account's workspaces (and whether multi-workspace is enabled).</summary>
    public Task<WorkspacesInfo> ListWorkspacesAsync(CancellationToken cancellationToken = default) =>
        SendAsync<WorkspacesInfo>(HttpMethod.Get, "/workspaces", cancellationToken: cancellationToken);

    /// <summary>
    /// Create an additional workspace (requires the multi-workspace feature on the account;
    /// returns <c>403 feature_not_enabled</c> otherwise). The new workspace is non-default;
    /// pass its id as <c>workspaceId</c> to <see cref="UploadFileAsync"/> and reports.
    /// </summary>
    public Task<WorkspaceInfo> CreateWorkspaceAsync(string name, CancellationToken cancellationToken = default) =>
        SendAsync<WorkspaceInfo>(HttpMethod.Post, "/workspaces", JsonBody(new { name }), cancellationToken: cancellationToken);

    /// <summary>Liveness probe (no auth required).</summary>
    public Task<HealthInfo> HealthAsync(CancellationToken cancellationToken = default) =>
        SendAsync<HealthInfo>(HttpMethod.Get, "/health", cancellationToken: cancellationToken);

    // ─── Billing / profile ────────────────────────────────────────────────────

    /// <summary>Get the account's billing profile.</summary>
    public Task<BillingProfile> GetBillingAsync(CancellationToken cancellationToken = default) =>
        SendAsync<BillingProfile>(HttpMethod.Get, "/billing", cancellationToken: cancellationToken);

    /// <summary>Update the account's billing contact details.</summary>
    public Task<BillingProfile> UpdateBillingAsync(BillingDetails details, CancellationToken cancellationToken = default) =>
        SendAsync<BillingProfile>(HttpMethod.Put, "/billing", JsonBody(details), cancellationToken: cancellationToken);

    /// <summary>Preview the next invoice for the account.</summary>
    public Task<BillingPreview> BillingPreviewAsync(CancellationToken cancellationToken = default) =>
        SendAsync<BillingPreview>(HttpMethod.Get, "/billing/preview", cancellationToken: cancellationToken);

    /// <summary>Create a Stripe SetupIntent to capture a card/ACH payment method.</summary>
    public Task<SetupIntentResult> CreateSetupIntentAsync(CancellationToken cancellationToken = default) =>
        SendAsync<SetupIntentResult>(HttpMethod.Post, "/billing/setup-intent", cancellationToken: cancellationToken);

    /// <summary>Save a confirmed Stripe payment method to the account.</summary>
    public Task<SavedPaymentMethod> SavePaymentMethodAsync(string paymentMethodId, CancellationToken cancellationToken = default) =>
        SendAsync<SavedPaymentMethod>(HttpMethod.Post, "/billing/payment-method", JsonBody(new { paymentMethodId }), cancellationToken: cancellationToken);
}
