using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HuongQueViet.Data;
using HuongQueViet.Models;

namespace HuongQueViet.Controllers
{
    [Authorize]
    public class AddressesController : Controller
    {
        private readonly AppDbContext _context;
        public AddressesController(AppDbContext context) { _context = context; }
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index() => View(await _context.Addresses.Where(a => a.UserId == CurrentUserId).ToListAsync());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Address address)
        {
            address.UserId = CurrentUserId;
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}