using Microsoft.AspNetCore.Identity;

namespace GFGestao.API.Identity;

public class ApplicationUser : IdentityUser
{
    public string Nome { get; set; } = string.Empty;
}