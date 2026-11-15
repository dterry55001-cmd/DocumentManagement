namespace DocumentManagement.Services;

public static class DocumentFileValidator
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".docx", ".xlsx", ".pptx", ".txt", ".csv", ".png", ".jpg", ".jpeg" };

    public static async Task<(bool Valid, string Type, string? Error)> ValidateAsync(
        Stream stream, string fileName, CancellationToken token)
    {
        var ext = Path.GetExtension(fileName);
        if (!Allowed.Contains(ext)) return (false, "", "Unsupported file type.");
        var header = new byte[16];
        var n = await stream.ReadAsync(header.AsMemory(), token);
        stream.Position = 0;
        var valid = ext.ToLowerInvariant() switch
        {
            ".pdf" => n >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".png" => n >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}),
            ".jpg" or ".jpeg" => n >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            ".docx" or ".xlsx" or ".pptx" => n >= 4 && header[0] == 0x50 && header[1] == 0x4b && header[2] is 0x03 or 0x05 or 0x07,
            ".txt" or ".csv" => n == 0 || !header.AsSpan(0, n).Contains((byte)0),
            _ => false
        };
        if (!valid) return (false, "", "File signature does not match the selected extension.");
        var type = ext.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".csv" => "text/csv",
            _ => "text/plain"
        };
        return (true, type, null);
    }
}
