namespace Perikato.Controllers.UserControllers.CarrierControllerDTO
{
    public class UpdateRouteDTO
    {
        public Guid Id { get; set; }
        public string Vehicle { get; set; }
        public int Range { get; set; }
        public float StartLatitude { get; set; }
        public float StartLongitude { get; set; }
        public float EndLatitude { get; set; }
        public float EndLongitude { get; set; }
        public List<UpdateRouteRouteDateDTO> RouteDates { get; set; }
        public List<UpdateRouteLegDTO>? RouteLegs { get; set; }

        public class UpdateRouteLegDTO
        {
            public float Latitude { get; set; }
            public float Longitude { get; set; }
        }

        public class UpdateRouteRouteDateDTO
        {
            public Guid? Id { get; set; }
            public DateTime RouteDateTime { get; set; }
        }
    }
}
