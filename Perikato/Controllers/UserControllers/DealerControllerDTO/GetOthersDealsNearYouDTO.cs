namespace Perikato.Controllers.UserControllers.DealerControllerDTO
{
    public class GetOthersDealsNearYouDTO
    {
        public float CurrentLocationLatitude { get; set; }
        public float CurrentLocationLongitude { get; set; }
        public int Range { get; set; }
    }
}
