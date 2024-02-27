using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Perikato.Data.Carriers
{
    public class RouteDates
    {
        [Key]
        public Guid Id { get; set; }


        [ForeignKey("Routes")]
        public Guid RouteId { get; set; }
        public DateTime RouteDateTime { get; set; }

        public virtual Routes Routes { get; set; } // Navigointiominaisuus
    }
}