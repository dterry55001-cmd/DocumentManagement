using System.ComponentModel.DataAnnotations;

namespace DocumentManagement.Models;

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    [StringLength(100)] public string? Category { get; set; }
    [Required, StringLength(255)] public string OriginalFileName { get; set; } = "";
    [Required, StringLength(100)] public string StoredFileName { get; set; } = "";
    [Required, StringLength(150)] public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    [Required, StringLength(64)] public string Sha256 { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
