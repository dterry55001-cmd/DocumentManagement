using DocumentManagement.Data;
using DocumentManagement.Models;
using DocumentManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocumentManagement.Controllers;

public sealed class DocumentsController(
    DocumentDbContext db, IDocumentStorage storage, IOptions<StorageOptions> storageOptions,
    ILogger<DocumentsController> logger) : Controller
{
    private static readonly HashSet<string> PreviewTypes = new(StringComparer.OrdinalIgnoreCase)
        { "application/pdf", "image/png", "image/jpeg", "text/plain" };

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? category, CancellationToken token)
    {
        var query = db.Documents.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Title.Contains(q) || x.OriginalFileName.Contains(q) ||
                (x.Description != null && x.Description.Contains(q)));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        ViewBag.Query = q; ViewBag.Category = category;
        return View(await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(token));
    }

    [HttpGet] public IActionResult Upload() => View(new UploadDocumentViewModel());

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Upload(UploadDocumentViewModel model, CancellationToken token)
    {
        if (model.File is null || model.File.Length == 0) ModelState.AddModelError(nameof(model.File), "Choose a file.");
        var max = storageOptions.Value.MaxFileSizeBytes;
        if (model.File is not null && model.File.Length > max) ModelState.AddModelError(nameof(model.File), $"Maximum size is {max / 1024 / 1024} MB.");
        if (!ModelState.IsValid) return View(model);

        await using var input = model.File!.OpenReadStream();
        var check = await DocumentFileValidator.ValidateAsync(input, model.File.FileName, token);
        if (!check.Valid) { ModelState.AddModelError(nameof(model.File), check.Error!); return View(model); }

        StoredFile saved;
        try { saved = await storage.SaveAsync(input, Path.GetExtension(model.File.FileName), token); }
        catch (Exception ex) { logger.LogError(ex, "Document file storage failed."); ModelState.AddModelError("", "Unable to store the uploaded file."); return View(model); }

        var doc = new Document {
            Title = model.Title.Trim(), Description = model.Description?.Trim(), Category = model.Category?.Trim(),
            OriginalFileName = Path.GetFileName(model.File.FileName), StoredFileName = saved.Key,
            ContentType = check.Type, SizeBytes = saved.SizeBytes, Sha256 = saved.Sha256
        };
        try {
            db.Documents.Add(doc);
            await db.SaveChangesAsync(token);
        } catch (Exception ex) {
            logger.LogError(ex, "Document metadata save failed; cleaning stored file.");
            await storage.DeleteAsync(saved.Key, token);
            ModelState.AddModelError("", "Unable to save document metadata.");
            return View(model);
        }
        TempData["Success"] = "Document uploaded.";
        return RedirectToAction(nameof(Details), new { id = doc.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken token)
    {
        var doc = await Find(id, token);
        return doc is null ? NotFound() : View(doc);
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id, CancellationToken token)
    {
        var doc = await Find(id, token);
        if (doc is null) return NotFound();
        var stream = await storage.OpenReadAsync(doc.StoredFileName, token);
        return stream is null ? NotFound("Stored file is missing.") : File(stream, doc.ContentType, doc.OriginalFileName);
    }

    [HttpGet]
    public async Task<IActionResult> Preview(Guid id, CancellationToken token)
    {
        var doc = await Find(id, token);
        if (doc is null) return NotFound();
        if (!PreviewTypes.Contains(doc.ContentType)) return BadRequest("Preview unavailable.");
        var stream = await storage.OpenReadAsync(doc.StoredFileName, token);
        if (stream is null) return NotFound("Stored file is missing.");
        Response.Headers.ContentDisposition = "inline";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(stream, doc.ContentType, enableRangeProcessing: true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken token)
    {
        var doc = await Find(id, token);
        if (doc is null) return NotFound();
        doc.IsDeleted = true; doc.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        try { await storage.DeleteAsync(doc.StoredFileName, token); }
        catch (Exception ex) { logger.LogError(ex, "Physical document cleanup failed for {Id}", id); }
        TempData["Success"] = "Document deleted.";
        return RedirectToAction(nameof(Index));
    }

    private Task<Document?> Find(Guid id, CancellationToken token) =>
        db.Documents.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, token);
}
