using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.Controllers
{
    [Route("doctors")]
    public class DoctorsController : Controller
    {
        // GET /doctors
        [HttpGet("")]
        public IActionResult Index() => View();

        // GET /doctors/{id}
        [HttpGet("{id:int}")]
        public IActionResult Details(int id) => View(model: id);
    }
}
