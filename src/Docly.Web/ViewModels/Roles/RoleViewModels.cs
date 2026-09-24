using System.ComponentModel.DataAnnotations;
using Docly.Application.Roles;

namespace Docly.Web.ViewModels.Roles;

public class RoleIndexViewModel
{
    public IReadOnlyList<RoleListItemDto> Roles { get; set; } = [];
}

public record PermissionOption(string Code, string Name, string? Description, bool Selected);

public class RoleFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar {1} caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250, ErrorMessage = "La descripción no puede superar {1} caracteres.")]
    [Display(Name = "Descripción")]
    public string? Description { get; set; }

    public bool IsBuiltIn { get; set; }

    /// <summary>The Admin role always has every permission, so its checkboxes are read-only.</summary>
    public bool IsAdmin { get; set; }

    public IReadOnlyList<PermissionOption> Permissions { get; set; } = [];

    /// <summary>Codes ticked in the form (bound on POST).</summary>
    public string[] SelectedPermissions { get; set; } = [];
}
