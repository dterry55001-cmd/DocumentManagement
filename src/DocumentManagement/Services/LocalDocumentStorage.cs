using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace DocumentManagement.Services;

public sealed class LocalDocumentStorage(IOptions<StorageOptions> options) : IDocumentStorage
{
    private readonly string _root = Path.GetFullPath(Path.Combine(options.Value.RootPath, "documents"));

    public async Task<StoredFile> SaveAsync(Stream source, string extension, CancellationToken token)
    {
        Directory.CreateDirectory(_root);
        var key = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
        var path = Path.Combine(_root, key);
        await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        using var sha = SHA256.Create();
        var buffer = new byte[81920];
        long size = 0;
        int count;
        while ((count = await source.ReadAsync(buffer.AsMemory(), token)) > 0)
        {
            size += count;
            await target.WriteAsync(buffer.AsMemory(0, count), token);
            sha.TransformBlock(buffer, 0, count, buffer, 0);
        }
        sha.TransformFinalBlock([], 0, 0);
        await target.FlushAsync(token);
        return new StoredFile(key, Convert.ToHexString(sha.Hash!).ToLowerInvariant(), size);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken token)
    {
        if (Path.GetFileName(key) != key) return Task.FromResult<Stream?>(null);
        var path = Path.Combine(_root, key);
        Stream? stream = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken token)
    {
        if (Path.GetFileName(key) != key) throw new InvalidOperationException("Invalid storage key.");
        var path = Path.Combine(_root, key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}
