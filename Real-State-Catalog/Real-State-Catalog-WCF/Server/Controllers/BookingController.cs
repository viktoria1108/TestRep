using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Real_State_Catalog_WCF.Models;
using Real_State_Catalog_WCF.Data;

namespace Real_State_Catalog_WCF.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class BookingController : Controller
    {
        private readonly AppContextDb _context;
        private readonly UserManager<User> _userManager;

        public BookingController(AppContextDb context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Booking
        [HttpGet]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            User user = await _userManager.GetUserAsync(User);

            if (user == null) { return NotFound(); }

            ViewBag.ReturnAction = "Index";

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return View(await _context.Booking
                    .Include(b => b.Offer).Include(b => b.User).Include(b => b.Offer.Accommodation).ToListAsync());
            }
            else
            {
                return View(await _context.Booking
                    .Where(b => b.UserId == user.Id)
                    .Include(b => b.Offer).Include(b => b.User).Include(b => b.Offer.Accommodation).ToListAsync());
            }
        }

        // GET: Booking/HostIndex
        [HttpGet]
        [Authorize(Roles = "Host, Admin")]
        [Route("HostIndex")]
        public async Task<IActionResult> HostIndex()
        {
            User user = await _userManager.GetUserAsync(User);

            if (user == null) { return NotFound(); }

            ViewBag.ReturnAction = "HostIndex";

            return View("Index", await _context.Booking
                .Include(b => b.Offer).Include(b => b.User).Include(b => b.Offer.Accommodation)
                .Where(b => b.Offer.Accommodation.UserId == user.Id).ToListAsync());
        }
        

        // POST: Booking/Details
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Details")]
        public async Task<IActionResult> Details(Guid id, string returnAction)
        {
            var booking = await _context.Booking
                .Include(b => b.Offer)
                .Include(b => b.User)
                .Include(b => b.Offer.Accommodation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.ReturnAction = returnAction;

            return View(booking);
        }
        
        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(
            [Bind("OfferId, ArrivalDate, ArrivalTime, DepartureDate, DepartureTime, NbPerson")] Booking booking)
        {
            
            int nbNight = (booking.DepartureDate - booking.ArrivalDate).Days;
            double pricePerNight = await _context.Offers.Where(o => o.Id == booking.OfferId).Select(o => o.PricePerNight).SingleOrDefaultAsync();
            double cleaningFee = await _context.Offers.Where(o => o.Id == booking.OfferId).Select(o => o.CleaningFee).SingleOrDefaultAsync();

            User senderUser = await _userManager.GetUserAsync(User);
            User? receiverUser = await _context.Offers.Where(o => o.Id == booking.OfferId).Select(o => o.Accommodation.User).SingleOrDefaultAsync();

            double totalPrice = pricePerNight * (double)nbNight + cleaningFee;
            booking.TotalPrice = totalPrice;
            booking.UserId = (await _userManager.GetUserAsync(User)).Id;
            _context.Add(booking);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Index));
        }
    }
}
