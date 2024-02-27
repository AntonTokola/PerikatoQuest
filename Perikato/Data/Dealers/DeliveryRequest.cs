using Perikato.Data.Carriers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Perikato.Data.Dealers
{
    public class DeliveryRequest
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public string UserName { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsActive { get; set; }
        public string? Status { get; set; }

        public float StartLatitude { get; set; }
        public float StartLongitude { get; set; }
        public float EndLatitude { get; set; }
        public float EndLongitude { get; set; }
        public string? PickUpAddress { get; set; }
        public string? DeliveryAddress { get; set; }
        public virtual ICollection<PreferredPickUpDates> PickUpDates { get; set; }
        public virtual ICollection<Package> Packages { get; set; }

        public string? Description { get; set; }
        public string? CustomerNotes { get; set; }
        public string? VehicleRecommendation { get; set; }
        public float? Price { get; set; }
    }

    public class Package
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("DeliveryRequest")]
        public Guid DeliveryRequestId { get; set; }
        public virtual DeliveryRequest DeliveryRequest { get; set; }

        public string? Size { get; set; }
        public float? Weight { get; set; }
    }

    public class PreferredPickUpDates
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("DeliveryRequest")]
        public Guid DeliveryRequestId { get; set; }
        public virtual DeliveryRequest DeliveryRequest { get; set; }

        public DateTime? PickUpDate { get; set; }
        public virtual ICollection<TimeRange>? PreferredTimeRanges { get; set; }
    }

    public class TimeRange
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("PreferredPickUpDates")]
        public Guid PreferredPickUpDatesId { get; set; }
        public virtual PreferredPickUpDates PreferredPickUpDates { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
    }

}
