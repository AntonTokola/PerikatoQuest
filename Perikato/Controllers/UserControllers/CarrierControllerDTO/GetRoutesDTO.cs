using System.ComponentModel.DataAnnotations.Schema;

namespace Perikato.Controllers.UserControllers.CarrierControllerDTO
{
    public class GetRoutesDTO
    {

        public Guid Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public string Vehicle { get; set; }
        public int Range { get; set; }
        public float StartLatitude { get; set; }
        public float StartLongitude { get; set; }
        public float EndLatitude { get; set; }
        public float EndLongitude { get; set; }
        public List<GetRouteRouteDateDTO> RouteDates { get; set; }
        public List<GetRouteMatchedDealIdsDTO> MatchedDeals { get; set; }
        public List<GetRouteLegDTO> RouteLegs { get; set; }

        public class GetRouteLegDTO
        {
            public float Latitude { get; set; }
            public float Longitude { get; set; }
        }

        public class GetRouteRouteDateDTO
        {
            public Guid Id { get; set; }
            public DateTime RouteDateTime { get; set; }
        }
        public class GetRouteMatchedDealIdsDTO
        {
            public Guid Id { get; set; }
        }
    }

}
