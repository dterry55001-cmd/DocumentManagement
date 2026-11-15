using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DocumentManagement.Models;

public sealed class UploadDocumentViewModel
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    [StringLength(100)] public string? Category { get; set; }
    [Required] public IFormFile? File { get; set; }
}
