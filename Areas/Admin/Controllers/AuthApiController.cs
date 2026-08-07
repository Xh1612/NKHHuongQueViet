// Controllers/AuthApiController.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using HuongQueViet.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace HuongQueViet.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public AuthApiController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration config)
        { _signInManager = signInManager; _userManager = userManager; _config = config; }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Unauthorized(new { message = "Sai email hoặc mật khẩu" });
            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            if (!result.Succeeded) return Unauthorized(new { message = "Sai email hoặc mật khẩu" });

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Email, user.Email!) };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var token = new JwtSecurityToken(issuer: _config["Jwt:Issuer"], audience: _config["Jwt:Audience"], claims: claims,
                expires: DateTime.Now.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!)),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }
}