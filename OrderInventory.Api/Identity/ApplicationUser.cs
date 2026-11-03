using Microsoft.AspNetCore.Identity;

namespace OrderInventory.Api.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}