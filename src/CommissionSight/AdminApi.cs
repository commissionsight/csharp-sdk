using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using CommissionSight.Json;

namespace CommissionSight;

/// <summary>
/// Admin (back-office) endpoints. Require a session token for an allowlisted admin.
/// Accessed via <see cref="CommissionSightClient.Admin"/>.
/// </summary>
public sealed class AdminApi
{
    private readonly CommissionSightClient _client;

    internal AdminApi(CommissionSightClient client) => _client = client;

    private static string Q(params (string Key, string? Value)[] parameters)
    {
        var sb = new System.Text.StringBuilder();
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

    // ─── Accounts ───────────────────────────────────────────────────────────────

    /// <summary>List accounts, optionally filtered by status.</summary>
    public Task<DataList<AdminAccountListItem>> ListAccountsAsync(string? status = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<DataList<AdminAccountListItem>>(HttpMethod.Get, "/admin/accounts" + Q(("status", status)), cancellationToken: cancellationToken);

    /// <summary>Set an account's custom per-member billing rate (cents), or null to clear it.</summary>
    public Task<AdminBillingRateResult> SetBillingRateAsync(string accountId, long? rateCents, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminBillingRateResult>(HttpMethod.Put, $"/admin/accounts/{accountId}/billing-rate", CommissionSightClient.JsonBodyVerbatim(new { rateCents }), cancellationToken: cancellationToken);

    /// <summary>Get an account's billing record.</summary>
    public Task<AdminAccountBilling> GetAccountBillingAsync(string accountId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminAccountBilling>(HttpMethod.Get, $"/admin/accounts/{accountId}/billing", cancellationToken: cancellationToken);

    /// <summary>Per-account dashboard: counts + latest-period rollup.</summary>
    public Task<AdminAccountOverview> AccountOverviewAsync(string accountId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminAccountOverview>(HttpMethod.Get, $"/admin/accounts/{accountId}/overview", cancellationToken: cancellationToken);

    /// <summary>List an account's files.</summary>
    public Task<DataList<AdminAccountFile>> AccountFilesAsync(string accountId, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<DataList<AdminAccountFile>>(HttpMethod.Get, $"/admin/accounts/{accountId}/files" + Q(("limit", Num(limit)), ("offset", Num(offset))), cancellationToken: cancellationToken);

    /// <summary>List an account's jobs.</summary>
    public Task<DataList<AdminAccountJob>> AccountJobsAsync(string accountId, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<DataList<AdminAccountJob>>(HttpMethod.Get, $"/admin/accounts/{accountId}/jobs" + Q(("limit", Num(limit)), ("offset", Num(offset))), cancellationToken: cancellationToken);

    /// <summary>List an account's users.</summary>
    public Task<DataList<AdminAccountUser>> AccountUsersAsync(string accountId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<DataList<AdminAccountUser>>(HttpMethod.Get, $"/admin/accounts/{accountId}/users", cancellationToken: cancellationToken);

    /// <summary>Set an account's AI-assistant monthly cap (cents) and pass-through billing.</summary>
    public Task<AdminAiSettingsResult> SetAiSettingsAsync(string accountId, long? capCents = null, bool? passthrough = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminAiSettingsResult>(HttpMethod.Put, $"/admin/accounts/{accountId}/ai-settings", CommissionSightClient.JsonBodyOf(new { capCents, passthrough }), cancellationToken: cancellationToken);

    /// <summary>Toggle whether an account is charged the payment-processing surcharge.</summary>
    public Task<AdminSurchargeResult> SetSurchargeAsync(string accountId, bool enabled, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminSurchargeResult>(HttpMethod.Put, $"/admin/accounts/{accountId}/surcharge", CommissionSightClient.JsonBodyOf(new { enabled }), cancellationToken: cancellationToken);

    /// <summary>Approve a pending account (notifies users and provisions its data store).</summary>
    public Task<AdminApproveResult> ApproveAccountAsync(string accountId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminApproveResult>(HttpMethod.Post, $"/admin/accounts/{accountId}/approve", cancellationToken: cancellationToken);

    /// <summary>Provision (or re-provision) an account's data store. Pass a connection string to use a DB created out of band; omit to auto-create a Neon DB.</summary>
    public Task<AdminProvisionResult> ProvisionAccountAsync(string accountId, string? connString = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminProvisionResult>(HttpMethod.Post, $"/admin/accounts/{accountId}/provision",
            CommissionSightClient.JsonBodyOf(connString is null ? new { } : (object)new { connString }), cancellationToken: cancellationToken);

    /// <summary>Create a new account.</summary>
    public Task<NamedEntity> CreateAccountAsync(string name, CancellationToken cancellationToken = default) =>
        _client.SendAsync<NamedEntity>(HttpMethod.Post, "/admin/accounts", CommissionSightClient.JsonBodyOf(new { name }), cancellationToken: cancellationToken);

    /// <summary>Purge ALL raw statement bytes for an account from object storage (retention).</summary>
    public Task<AdminPurgeFilesResult> PurgeAccountFilesAsync(string accountId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminPurgeFilesResult>(HttpMethod.Post, $"/admin/accounts/{accountId}/purge-files", cancellationToken: cancellationToken);

    // ─── Tokens & credentials ─────────────────────────────────────────────────────

    /// <summary>Issue a new API token for an account (the secret is returned ONCE).</summary>
    public Task<AdminIssuedToken> IssueTokenAsync(string accountId, string? label = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminIssuedToken>(HttpMethod.Post, $"/admin/accounts/{accountId}/tokens", CommissionSightClient.JsonBodyOf(new { label }), cancellationToken: cancellationToken);

    /// <summary>List an account's API tokens — metadata only (never the secret).</summary>
    public Task<DataList<AdminTokenInfo>> ListTokensAsync(string accountId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<DataList<AdminTokenInfo>>(HttpMethod.Get, $"/admin/accounts/{accountId}/tokens", cancellationToken: cancellationToken);

    /// <summary>Revoke an API token.</summary>
    public Task<AdminRevokeTokenResult> RevokeTokenAsync(string tokenId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminRevokeTokenResult>(HttpMethod.Post, $"/admin/tokens/{tokenId}/revoke", cancellationToken: cancellationToken);

    /// <summary>Store (encrypt) an account's data-plane connection string.</summary>
    public Task<AdminStoreCredentialsResult> StoreCredentialsAsync(string accountId, string connString, string? region = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminStoreCredentialsResult>(HttpMethod.Put, $"/admin/accounts/{accountId}/credentials", CommissionSightClient.JsonBodyOf(new { connString, region }), cancellationToken: cancellationToken);

    // ─── Carriers & configs ───────────────────────────────────────────────────────

    /// <summary>Create a carrier.</summary>
    public Task<NamedEntity> CreateCarrierAsync(string name, string slug, CancellationToken cancellationToken = default) =>
        _client.SendAsync<NamedEntity>(HttpMethod.Post, "/admin/carriers", CommissionSightClient.JsonBodyOf(new { name, slug }), cancellationToken: cancellationToken);

    /// <summary>Rename a carrier (and optionally change its slug).</summary>
    public Task<NamedEntity> RenameCarrierAsync(string id, string name, string? slug = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<NamedEntity>(HttpMethod.Put, $"/admin/carriers/{id}", CommissionSightClient.JsonBodyOf(slug is null ? (object)new { name } : new { name, slug }), cancellationToken: cancellationToken);

    /// <summary>Onboarding: infer a draft config from a sample statement (CSV/XLSX).</summary>
    public Task<InferredConfig> InferConfigAsync(string carrierId, FileUpload file, string? sheetName = null, string? fileType = null, CancellationToken cancellationToken = default)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.Content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);
        if (!string.IsNullOrEmpty(sheetName))
        {
            form.Add(new StringContent(sheetName), "sheetName");
        }

        if (!string.IsNullOrEmpty(fileType))
        {
            form.Add(new StringContent(fileType), "fileType");
        }

        return _client.SendAsync<InferredConfig>(HttpMethod.Post, $"/admin/carriers/{carrierId}/configs/infer", form, cancellationToken: cancellationToken);
    }

    /// <summary>Create a global (carrier-default) config.</summary>
    public Task<CreateConfigResult> CreateGlobalConfigAsync(string carrierId, object config, CancellationToken cancellationToken = default) =>
        _client.SendAsync<CreateConfigResult>(HttpMethod.Post, $"/admin/carriers/{carrierId}/configs", CommissionSightClient.JsonBodyOf(config), cancellationToken: cancellationToken);

    /// <summary>List a carrier's configs (admin view).</summary>
    public Task<DataList<CarrierConfigEntry>> ListCarrierConfigsAsync(string carrierId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<DataList<CarrierConfigEntry>>(HttpMethod.Get, $"/admin/carriers/{carrierId}/configs", cancellationToken: cancellationToken);

    /// <summary>Update a carrier config (creates a new version).</summary>
    public Task<AdminUpdateConfigResult> UpdateCarrierConfigAsync(string carrierId, string configId, object config, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminUpdateConfigResult>(HttpMethod.Put, $"/admin/carriers/{carrierId}/configs/{configId}", CommissionSightClient.JsonBodyOf(config), cancellationToken: cancellationToken);

    // ─── Users & allowlist ────────────────────────────────────────────────────────

    /// <summary>List all platform users.</summary>
    public Task<DataList<AdminUser>> ListUsersAsync(CancellationToken cancellationToken = default) =>
        _client.SendAsync<DataList<AdminUser>>(HttpMethod.Get, "/admin/users", cancellationToken: cancellationToken);

    /// <summary>Create a user (optionally linked to an account, with a role).</summary>
    public Task<AdminUser> CreateUserAsync(string email, string? accountId = null, TeamRole? role = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminUser>(HttpMethod.Post, "/admin/users", CommissionSightClient.JsonBodyOf(new { email, accountId, role }), cancellationToken: cancellationToken);

    /// <summary>Update a user's role. The account link is immutable (set at invite).</summary>
    public Task<AdminUpdateUserResult> UpdateUserAsync(string id, TeamRole role, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminUpdateUserResult>(HttpMethod.Put, $"/admin/users/{id}", CommissionSightClient.JsonBodyOf(new { role }), cancellationToken: cancellationToken);

    /// <summary>Delete a user.</summary>
    public Task<AdminDeleteUserResult> DeleteUserAsync(string id, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminDeleteUserResult>(HttpMethod.Delete, $"/admin/users/{id}", cancellationToken: cancellationToken);

    /// <summary>Add an email to the admin allowlist.</summary>
    public Task<AdminAllowlistResult> AddAllowlistAsync(string email, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminAllowlistResult>(HttpMethod.Post, "/admin/allowlist", CommissionSightClient.JsonBodyOf(new { email }), cancellationToken: cancellationToken);

    // ─── Jobs / system / metrics ──────────────────────────────────────────────────

    /// <summary>List jobs across all accounts.</summary>
    public Task<Page<AdminJobListItem>> ListJobsAsync(string? status = null, int? limit = null, int? offset = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<Page<AdminJobListItem>>(HttpMethod.Get, "/admin/jobs" + Q(("status", status), ("limit", Num(limit)), ("offset", Num(offset))), cancellationToken: cancellationToken);

    /// <summary>Inspect a single job (with account, file, and timing detail).</summary>
    public Task<AdminJobDetail> JobDetailAsync(string jobId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminJobDetail>(HttpMethod.Get, $"/admin/jobs/{jobId}", cancellationToken: cancellationToken);

    /// <summary>Retry a job (admin).</summary>
    public Task<JobStatusRef> RetryJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<JobStatusRef>(HttpMethod.Post, $"/admin/jobs/{jobId}/retry", cancellationToken: cancellationToken);

    /// <summary>Re-score a job's period (admin).</summary>
    public Task<AdminRescoreJobResult> RescoreJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminRescoreJobResult>(HttpMethod.Post, $"/admin/jobs/{jobId}/rescore", cancellationToken: cancellationToken);

    /// <summary>Recent scheduled-maintenance (cron) runs + the task schedule.</summary>
    public Task<AdminSystemActivity> CronRunsAsync(int? limit = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminSystemActivity>(HttpMethod.Get, "/admin/system/cron-runs" + Q(("limit", Num(limit))), cancellationToken: cancellationToken);

    /// <summary>Platform billing/revenue summary — heartbeats, projected revenue, A/R.</summary>
    public Task<AdminRevenueSummary> RevenueAsync(CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminRevenueSummary>(HttpMethod.Get, "/admin/revenue", cancellationToken: cancellationToken);

    /// <summary>Platform-wide metrics.</summary>
    public Task<AdminMetrics> MetricsAsync(CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminMetrics>(HttpMethod.Get, "/admin/metrics", cancellationToken: cancellationToken);

    /// <summary>Recent platform logs + alerts.</summary>
    public Task<AdminLogs> LogsAsync(int? limit = null, int? offset = null, CancellationToken cancellationToken = default) =>
        _client.SendAsync<AdminLogs>(HttpMethod.Get, "/admin/logs" + Q(("limit", Num(limit)), ("offset", Num(offset))), cancellationToken: cancellationToken);
}
