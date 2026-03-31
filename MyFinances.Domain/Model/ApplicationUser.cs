using Microsoft.AspNetCore.Identity;

namespace MyFinances.Domain.Model;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
}