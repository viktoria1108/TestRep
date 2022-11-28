﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Real_State_Catalog_WCF.Models;
using Real_State_Catalog_WCF.Data;

namespace Real_State_Catalog_WCF.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class AmenityController : Controller
    {
        private readonly AppContextDb _context;

        public AmenityController(AppContextDb context)
        {
            _context = context;
        }

        // GET: Accommodation/ManageAmenities
        [Route("ManageAmenities/{roomId:guid?}")]
        [HttpGet]
        public async Task<IActionResult> ManageAmenities(Guid? roomId)
        {
            if (roomId == null) { return NotFound(); }

            var room = await _context.Rooms
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null) { return NotFound(); }

            if (TempData["AlertType"] != null && TempData["AlertMsg"] != null)
            {
                ViewBag.AlertType = TempData["AlertType"];
                ViewBag.AlertMsg = TempData["AlertMsg"];
            }

            string roomType = room.RoomType.ToString();

            ViewBag.AmenityTypes = AmenityTools.AmenitiesForRoom(roomType);

            return View(room);
        }

        // POST: Accommodation/AddAmenity
        [HttpPost]
        [Route("AddAmenity/{roomId:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAmenity(Guid roomId, string amenityType)
        {
            var room = await _context.Rooms
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room == null) { return NotFound(); }

            List<string> amenityTypes = AmenityTools.AmenitiesForRoom(room.RoomType.ToString());

            if (amenityTypes.Contains(amenityType))
            {
                room.Amenities.Add(new Amenity { AmenityType = (AmenityTypes)Enum.Parse(typeof(AmenityTypes), amenityType, true) });
                _context.Update(room);
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["AlertType"] = "warning";
                TempData["AlertMsg"] = "Invalid equipment !";
            }

            return RedirectToAction("ManageAmenities", new { roomId });
        }

        // POST: Accommodation/DeleteAmenity
        [HttpPost]
        [Route("DeleteAmenity/{amenityId:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAmenity(Guid amenityId, Guid roomId)
        {
            var nbAmenities = await _context.Amenity.CountAsync(r => r.RoomId == roomId);

            if (nbAmenities > 1)
            {
                var amenity = await _context.Amenity.FindAsync(amenityId);

                _context.Amenity.Remove(amenity);
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["AlertType"] = "warning";
                TempData["AlertMsg"] = "A room must contain at least 1 piece of equipment !";
            }

            return RedirectToAction("ManageAmenities", new { roomId });
        }
    }
}
