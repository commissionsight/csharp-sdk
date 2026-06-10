namespace CommissionSight;

/// <summary>
/// A file to send in a multipart upload (statement uploads, config test/infer).
/// Construct from a stream, byte array, or a path.
/// </summary>
public sealed class FileUpload
{
    /// <summary>The file contents.</summary>
    public required Stream Content { get; init; }

    /// <summary>The file name sent to the server (used to detect CSV vs XLSX).</summary>
    public required string FileName { get; init; }

    /// <summary>MIME type. Defaults to <c>application/octet-stream</c>.</summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>Create an upload from raw bytes.</summary>
    public static FileUpload FromBytes(byte[] bytes, string fileName, string contentType = "application/octet-stream") =>
        new() { Content = new MemoryStream(bytes, writable: false), FileName = fileName, ContentType = contentType };

    /// <summary>Create an upload from an open stream.</summary>
    public static FileUpload FromStream(Stream stream, string fileName, string contentType = "application/octet-stream") =>
        new() { Content = stream, FileName = fileName, ContentType = contentType };

    /// <summary>Open a file on disk for upload. The caller owns nothing — the client disposes the stream.</summary>
    public static FileUpload FromFile(string path, string? fileName = null) =>
        new()
        {
            Content = File.OpenRead(path),
            FileName = fileName ?? Path.GetFileName(path),
            ContentType = GuessContentType(path),
        };

    private static string GuessContentType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".csv" => "text/csv",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            _ => "application/octet-stream",
        };
}
