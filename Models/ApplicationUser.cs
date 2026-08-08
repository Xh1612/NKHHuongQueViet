using Microsoft.AspNetCore.Identity;
namespace HuongQueViet.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData] //them test
        public string FullName { get; set; } = string.Empty;
    }
}