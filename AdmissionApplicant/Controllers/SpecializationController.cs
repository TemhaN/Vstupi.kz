using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdmissionSystem.Models;

namespace AdmissionSystem.Controllers
{
    public class SpecializationController : Controller
    {
        private readonly AdmissionContext _context;

        public SpecializationController(AdmissionContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var specializations = await _context.Specializations
                .Include(s => s.Faculty)
                .Include(s => s.Grants)
                .ToListAsync();

            var grants = await _context.Grants
                .ToDictionaryAsync(g => g.SpecializationID, g => g);
            ViewBag.Grants = grants;

            return View(specializations);
        }
    }
}