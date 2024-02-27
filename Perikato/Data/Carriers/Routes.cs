using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Perikato.Data.Carriers
{
    public class Routes
    {
        [Key]
        public Guid Id { get; set; }


        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string? Status { get; set; }
        public bool IsActive { get; set; }
        public string Vehicle { get; set; }
        public int Range { get; set; }
        public float StartLatitude { get; set; }
        public float StartLongitude { get; set; }
        public float EndLatitude { get; set; }
        public float EndLongitude { get; set; }       

        public virtual User User { get; set; }

        public virtual ICollection<RouteDates> RouteDates { get; set; }
        public virtual ICollection<MatchedDealIds>? matchedDealIds { get; set; }

    }
}