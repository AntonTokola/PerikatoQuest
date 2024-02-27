using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;

namespace Perikato.Data.MongoDb
{
    public class GoogleMapsLegs
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public Guid RouteId { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; }
        public int Range { get; set; }

        // Lista reittipisteistä (legeistä)
        public List<GeoJsonPoint<GeoJson2DGeographicCoordinates>> GoogleMapsLegsList { get; set; }
    }

    public class DealLocations
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public Guid DealId { get; set; }
        public Guid UserId { get; set; }

        public bool IsActive { get; set; }
        public string Status { get; set; }

        // Lähtöpisteen koordinaatit
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> StartLocation { get; set; }

        // Määränpään koordinaatit
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> EndLocation { get; set; }

    }
}
