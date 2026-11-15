namespace DocumentManagement.Services;

public interface IDocumentStorage
{
    Task<StoredFile> SaveAsync(Stream source, string extension, CancellationToken token);
    Task<Stream?> OpenReadAsync(string key, CancellationToken token);
    Task DeleteAsync(string key, CancellationToken token);
}
public sealed record StoredFile(string Key, string Sha256, long SizeBytes);
