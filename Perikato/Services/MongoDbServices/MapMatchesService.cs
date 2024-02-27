using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perikato.Data.MongoDb;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver;
using Perikato.Data.Carriers;

public class MapMatchesService
{
    private const double EarthRadiusKm = 6371.0;


    public async Task<List<DealLocations>> FindDealsStartLocations(
        IMongoCollection<DealLocations> dealsCollection,
        List<GeoJsonPoint<GeoJson2DGeographicCoordinates>> routeLegs,
        int rangeInKiloMeters)
    {
        var dealsNearRoute = new List<DealLocations>();
        double rangeInMeters = rangeInKiloMeters * 1000;

        // Luodaan filtteri, joka tarkistaa, että IsActive on true.
        var activeFilter = Builders<DealLocations>.Filter.Eq(deal => deal.IsActive, true);

        foreach (var leg in routeLegs)
        {
            var point = leg;
            var nearSphereFilter = Builders<DealLocations>.Filter.NearSphere(
                x => x.StartLocation,
                point,
                rangeInMeters
            );

            var combinedFilter = Builders<DealLocations>.Filter.And(activeFilter, nearSphereFilter);

            var deals = await dealsCollection.Find(combinedFilter).ToListAsync();
            dealsNearRoute.AddRange(deals);
        }

        // Poista mahdolliset duplikaatit, jos sama deal löytyy useammalta legiltä.
        dealsNearRoute = dealsNearRoute.DistinctBy(deal => deal.Id).ToList();

        return dealsNearRoute;
    }



    public List<Guid> FindDealMatches(
       List<GeoJsonPoint<GeoJson2DGeographicCoordinates>> routeLegs,
       List<DealLocations> dealLocations,
       int range)
    {
        List<Guid> matchedDealIds = new List<Guid>();

        foreach (var deal in dealLocations)
        {
            // Etsi ensimmäinen leg, joka on 'range' kilometrin säteellä deal.StartLocation pisteestä.
            var startMatchIndex = routeLegs.FindIndex(leg => IsWithinRadius(leg.Coordinates, deal.StartLocation.Coordinates, range));
            if (startMatchIndex != -1)
            {
                // Jos StartLocation löytyi, aloita seuraavasta pisteestä ja etsi EndLocationia.
                for (int i = startMatchIndex + 1; i < routeLegs.Count; i++)
                {
                    if (IsWithinRadius(routeLegs[i].Coordinates, deal.EndLocation.Coordinates, range))
                    {
                        // Kun molemmat, StartLocation ja EndLocation, ovat löytyneet, lisää diilin ID listalle.
                        matchedDealIds.Add(deal.DealId);
                    }
                }
            }
        }

        return matchedDealIds;
    }

    public List<Guid> FindRouteMatches(DealLocations deal, List<GoogleMapsLegs> routeObject)
    {
        List<Guid> matchedRouteIds = new List<Guid>();

        foreach (var route in routeObject)
        {
            // Etsi ensimmäinen leg, joka on 'rangen' säteellä deal.StartLocation pisteestä.
            var startMatchIndex = route.GoogleMapsLegsList.FindIndex(leg => IsWithinRadius(leg.Coordinates, deal.StartLocation.Coordinates, route.Range));
            if (startMatchIndex != -1)
            {
                // Jos StartLocation löytyi, aloita seuraavasta pisteestä ja etsi EndLocationia.
                for (int i = startMatchIndex + 1; i < route.GoogleMapsLegsList.Count; i++)
                {
                    if (IsWithinRadius(route.GoogleMapsLegsList[i].Coordinates, deal.EndLocation.Coordinates, route.Range))
                    {
                        // Kun molemmat, StartLocation ja EndLocation, ovat löytyneet, lisää diilin ID listalle.
                        matchedRouteIds.Add(route.RouteId);
                        break;
                    }
                }
            }
        }            
        
        return matchedRouteIds;
    }

    //Haversine-kaava karttapisteen lähistöltä muita pisteitä etsittäessä
    private bool IsWithinRadius(GeoJson2DGeographicCoordinates point1, GeoJson2DGeographicCoordinates point2, double radiusKm)
    {
        double dLat = ToRadians(point2.Latitude - point1.Latitude);
        double dLon = ToRadians(point2.Longitude - point1.Longitude);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(point1.Latitude)) * Math.Cos(ToRadians(point2.Latitude)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = EarthRadiusKm * c;
        return distance <= radiusKm;
    }

    private static double ToRadians(double angle)
    {
        return Math.PI * angle / 180.0;
    }


}

