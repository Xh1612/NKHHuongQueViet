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

        public async Task<IActionResult> Edit(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null || address.UserId != CurrentUserId) return NotFound();
            return View(address);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Address model)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null || address.UserId != CurrentUserId) return NotFound();

            address.Street = model.Street;
            address.Ward = model.Ward;
            address.District = model.District;
            address.Province = model.Province;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null || address.UserId != CurrentUserId) return NotFound();

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}