namespace Perikato.Controllers.UserControllers.DealerControllerDTO
{
    public class UpdateDealsDTO
    {
        public Guid Id { get; set; }
        public float? StartLatitude { get; set; }
        public float? StartLongitude { get; set; }
        public float? EndLatitude { get; set; }
        public float? EndLongitude { get; set; }
        public string? PickUpAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? Description { get; set; }
        public string? CustomerNotes { get; set; }
        public string? VehicleRecommendation { get; set; }
        public float? Price { get; set; }
        public List<UpdateDealsPackageDTO>? Packages { get; set; }
        public List<UpdateDealsPickUpDateDTO>? PickUpDates { get; set; }
    }

    public class UpdateDealsPickUpDateDTO
    {
        public Guid? Id { get; set; } // Jätä tyhjäksi, jos kyseessä on uusi tietue
        public DateTime? PickUpDate { get; set; }
        public List<string>? PreferredTimeFrames { get; set; }
    }

    public class UpdateDealsPackageDTO
    {
        public Guid? Id { get; set; }
        public string? Size { get; set; }
        public float? Weight { get; set; }
    }

}
