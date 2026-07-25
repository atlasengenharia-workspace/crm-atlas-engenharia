using CrmAtlas.ApplicationCore.Financeiro;
using Microsoft.Extensions.Hosting;

namespace CrmAtlas.Infrastructure.Files;

public sealed class LocalReceiptStorage(IHostEnvironment? environment = null) : IReceiptStorage
{
    private readonly string _root = Path.Combine(environment?.ContentRootPath ?? AppContext.BaseDirectory, "App_Data", "comprovantes");
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".png", ".jpg", ".jpeg", ".webp" };

    public async Task<StoredReceipt> SaveAsync(
        Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeName);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("O comprovante deve ser PDF, PNG, JPG ou WEBP.");
        Directory.CreateDirectory(_root);
        var key = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var output = new FileStream(Path.Combine(_root, key), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);
        return new(key, safeName, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
    }

    public Task<(Stream Content, string ContentType, string FileName)?> OpenReadAsync(
        string key, CancellationToken cancellationToken = default)
    {
        var safeKey = Path.GetFileName(key);
        if (!string.Equals(key, safeKey, StringComparison.Ordinal) || !File.Exists(Path.Combine(_root, safeKey)))
            return Task.FromResult<(Stream, string, string)?>(null);
        Stream stream = new FileStream(Path.Combine(_root, safeKey), FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream, string, string)?>((stream, ContentType(safeKey), safeKey));
    }

    private static string ContentType(string key) => Path.GetExtension(key).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf", ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp", _ => "application/octet-stream"
    };
}
