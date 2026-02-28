using System.ComponentModel.DataAnnotations;

namespace Security.Web.Models.Account;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Username or Email")]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
