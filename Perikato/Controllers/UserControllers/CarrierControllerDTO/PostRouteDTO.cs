namespace Perikato.Controllers.UserControllers.CarrierControllerDTO
{
    public class PostRouteDTO
    {
        public string Vehicle { get; set; }
        public int Range { get; set; }
        public float StartLatitude { get; set; }
        public float StartLongitude { get; set; }
        public float EndLatitude { get; set; }
        public float EndLongitude { get; set; }
        public List<PostRouteRouteDateDTO> RouteDates { get; set; }
        public List<PostRouteRouteLegDTO> RouteLegs { get; set; }

        public class PostRouteRouteDateDTO
        {
            public DateTime RouteDateTime { get; set; }
        }
        public class PostRouteRouteLegDTO
        {
            public float Latitude { get; set; }
            public float Longitude { get; set; }
        }
    }
}