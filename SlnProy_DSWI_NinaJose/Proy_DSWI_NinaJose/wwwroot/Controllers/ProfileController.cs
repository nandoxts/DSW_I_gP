using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proy_DSWI_NinaJose.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Proy_DSWI_NinaJose.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly BDPROYVENTASContex _ctx;
        public ProfileController(BDPROYVENTASContex ctx) => _ctx = ctx;

        // GET /Profile/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _ctx.Usuarios.FindAsync(userId);
            if (user == null) return NotFound();
            return PartialView("~/Views/Profile/Profile.cshtml", user);
        }
    }
}
