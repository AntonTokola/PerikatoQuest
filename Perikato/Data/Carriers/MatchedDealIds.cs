using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Perikato.Data.Carriers
{
    public class MatchedDealIds
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Routes")]
        public Guid RouteId { get; set; }
        public Guid MatchedDealId { get; set; }
        public virtual Routes Routes { get; set; }
    }
}
