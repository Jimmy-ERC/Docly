using System.ComponentModel.DataAnnotations;
using Docly.Application.Common;
using Docly.Application.Documents;
using Microsoft.AspNetCore.Http;

namespace Docly.Web.ViewModels.Documents;

public record CategoryOption(Guid Id, string Name);

public class DocumentIndexViewModel
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public required PagedResult<DocumentListItemDto> Documents { get; set; }
    public IReadOnlyList<CategoryOption> Categories { get; set; } = [];
}

public class DocumentUploadViewModel
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, ErrorMessage = "El título no puede superar {1} caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede superar {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Selecciona un archivo.")]
    [Display(Name = "Archivo")]
    public IFormFile? File { get; set; }

    public IReadOnlyList<CategoryOption> Categories { get; set; } = [];
}

public class DocumentEditViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, ErrorMessage = "El título no puede superar {1} caracteres.")]
    [Display(Name = "Título")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "La descripción no puede superar {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Display(Name = "Categoría")]
    public Guid CategoryId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }

    public IReadOnlyList<CategoryOption> Categories { get; set; } = [];
}

public class DocumentReplaceFileViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Selecciona un archivo.")]
    [Display(Name = "Nuevo archivo")]
    public IFormFile? File { get; set; }
}
