using System.ComponentModel.DataAnnotations;
using Docly.Application.Categories;

namespace Docly.Web.ViewModels.Categories;

public class CategoryIndexViewModel
{
    public IReadOnlyList<CategoryListItemDto> Categories { get; set; } = [];
}

public class CategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }
}
