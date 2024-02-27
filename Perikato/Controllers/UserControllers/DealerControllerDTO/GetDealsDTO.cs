namespace Perikato.Controllers.UserControllers.DealerControllerDTO
{
    public class GetDealsDTO
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public float StartLatitude { get; set; }
        public float StartLongitude { get; set; }
        public float EndLatitude { get; set; }
        public float EndLongitude { get; set; }
        public string? PickUpAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? Description { get; set; }
        public string? CustomerNotes { get; set; }
        public string? VehicleRecommendation { get; set; }
        public float? Price { get; set; }
        public List<GetDealsPackageDTO>? Packages { get; set; }
        public List<GetDealsPickUpDateDTO>? PickUpDates { get; set; }
    }

    public class GetDealsPackageDTO
    {
        public Guid Id { get; set; }
        public string? Size { get; set; }
        public float? Weight { get; set; }
    }

    public class GetDealsPickUpDateDTO
    {
        public Guid Id { get; set; }
        public DateTime? PickUpDate { get; set; }
        public List<string>? PreferredTimeFrames { get; set; }
    }
}
