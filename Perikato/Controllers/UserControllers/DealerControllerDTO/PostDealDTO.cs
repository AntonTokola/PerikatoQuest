using System;
using System.Collections.Generic;

namespace Perikato.Controllers.UserControllers.DealerControllerDTO
{
    public class PostDealDTO
    {
        public float StartLatitude { get; set; }
        public float StartLongitude { get; set; }
        public float EndLatitude { get; set; }
        public float EndLongitude { get; set; }
        public string? PickUpAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public List<PreferredPickUpDatesDTO> PickUpDates { get; set; }
        public List<PackageDTO> Packages { get; set; }
        public string? Description { get; set; }
        public string? CustomerNotes { get; set; }
        public string? VehicleRecommendation { get; set; }
        public float? Price { get; set; }
    }

    public class PackageDTO
    {
        public string? Size { get; set; }
        public float? Weight { get; set; }
    }

    public class PreferredPickUpDatesDTO
    {
        public DateTime? PickUpDate { get; set; }
        public List<string> PreferredTimeFrames { get; set; } // Muutettu käyttämään stringejä
    }

    public class TimeRangeDTO
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
