using System.ComponentModel.DataAnnotations;

namespace Security.Web.Models.Admin;

public class CreateUserViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    [Required, MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required]
    [Display(Name = "Role Group")]
    public int RoleGroupId { get; set; }
}

public class AssignRoleGroupViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    [Required]
    [Display(Name = "Role Group")]
    public int RoleGroupId { get; set; }
}
