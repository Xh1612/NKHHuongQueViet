// Controllers/ProductsApiController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;

namespace HuongQueViet.Controllers
{
	[ApiController]
	[Route("api/products")]
	public class ProductsApiController : ControllerBase
	{
		private readonly AppDbContext _context;
		public ProductsApiController(AppDbContext context) { _context = context; }

		[HttpGet]
		public async Task<IActionResult> GetAll() => Ok(await _context.Products.Where(p => p.IsActive).ToListAsync());

		[HttpGet("my-orders")]
		[Authorize]
		public async Task<IActionResult> GetMyOrders()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			return Ok(await _context.Orders.Where(o => o.UserId == userId).ToListAsync());
		}
	}
}