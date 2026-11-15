namespace DocumentManagement.Services;

public sealed class StorageOptions
{
    public string RootPath { get; set; } = "./data";
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024;
}
