using Microsoft.AspNetCore.Identity;

namespace EzyTaskin.Data;

public class Account : IdentityUser
{
    [PersonalData]
    public string? FullName { get; set; }

    [PersonalData]
    public string? Address { get; set; }
}
