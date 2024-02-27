using MongoDB.Driver;
using Perikato.Data.MongoDb;
using System.Threading.Tasks;

namespace Perikato.Services.MongoDbServices
{
    public class GoogleMapsLegsService
    {
        private readonly IMongoCollection<GoogleMapsLegs> _RouteLegsCollection;

        public GoogleMapsLegsService(IMongoClient mongoClient, string databaseName, string collectionName)
        {
            IMongoDatabase database = mongoClient.GetDatabase(databaseName);
            _RouteLegsCollection = database.GetCollection<GoogleMapsLegs>(collectionName);
        }

        public async Task AddRouteLegsAsync(GoogleMapsLegs routeLegs)
        {
            await _RouteLegsCollection.InsertOneAsync(routeLegs);
        }

        public async Task<GoogleMapsLegs> GetRouteLegsAsync(Guid routeId)
        {
            return await _RouteLegsCollection.Find(leg => leg.RouteId == routeId).FirstOrDefaultAsync();
        }

        public async Task<List<GoogleMapsLegs>> GetActiveLegLocationsAsync()
        {
            var builder = Builders<GoogleMapsLegs>.Filter;
            var filter = builder.And(
                builder.Eq("IsActive", true),
                builder.Eq("Status", "open")
            );

            return await _RouteLegsCollection.Find(filter).ToListAsync();
        }


        public async Task UpdateRouteLegsAsync(Guid routeId, GoogleMapsLegs updatedLegs)
        {
            var filter = Builders<GoogleMapsLegs>.Filter.Eq(leg => leg.RouteId, routeId);
            var update = Builders<GoogleMapsLegs>.Update.Set(leg => leg.GoogleMapsLegsList, updatedLegs.GoogleMapsLegsList);
            await _RouteLegsCollection.UpdateOneAsync(filter, update);
        }


    }
}
