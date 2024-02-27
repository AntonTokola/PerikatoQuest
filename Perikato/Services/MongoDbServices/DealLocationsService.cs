using MongoDB.Driver;
using Perikato.Data.MongoDb;

namespace Perikato.Services.MongoDbServices
{
    public class DealLocationsService
    {
        private readonly IMongoCollection<DealLocations> _DealLocationsCollection;

        public DealLocationsService(IMongoClient mongoClient, string databaseName, string collectionName)
        {
            IMongoDatabase database = mongoClient.GetDatabase(databaseName);
            _DealLocationsCollection = database.GetCollection<DealLocations>(collectionName);
        }

        public IMongoCollection<DealLocations> GetDealLocationsCollection()
        {
            return _DealLocationsCollection;
        }

        public async Task AddDealLocationsAsync(DealLocations dealLocations)
        {
            await _DealLocationsCollection.InsertOneAsync(dealLocations);
        }

        public async Task<DealLocations> GetDealLocationsAsync(Guid dealId)
        {
            return await _DealLocationsCollection.Find(leg => leg.DealId == dealId).FirstOrDefaultAsync();
        }

        public async Task<List<DealLocations>> GetActiveDealLocationsAsync()
        {
            var builder = Builders<DealLocations>.Filter;
            var filter = builder.And(
                builder.Eq("IsActive", true),
                builder.Eq("Status", "open")
            );

            return await _DealLocationsCollection.Find(filter).ToListAsync();
        }

        public async Task UpdateDealLocationsAsync(Guid dealId, DealLocations dealLocations)
        {
            var filter = Builders<DealLocations>.Filter.Eq("DealId", dealId);
            var update = Builders<DealLocations>.Update
                .Set("StartLocation", dealLocations.StartLocation)
                .Set("EndLocation", dealLocations.EndLocation);

            var result = await _DealLocationsCollection.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
            {
                Console.WriteLine("Ongelma Mongon Deal-lokaatiota päivitettäess. Id:llä ei löydy dokumenttia.");
            }
        }


    }
}
