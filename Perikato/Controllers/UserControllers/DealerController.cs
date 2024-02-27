using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Perikato.Controllers.UserControllers.DealerControllerDTO;
using Perikato.Data;
using Perikato.Data.Carriers;
using Perikato.Data.Dealers;
using Perikato.Data.MongoDb;
using Perikato.Services.MongoDbServices;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Perikato.Controllers.UserControllers
{
    public class DealerController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly GoogleMapsLegsService _routesService;
        private readonly DealLocationsService _dealLocationsService;
        private readonly MapMatchesService _mapMatchesService;


        public DealerController(ApplicationDbContext dbContext, GoogleMapsLegsService routesService, DealLocationsService dealLocationsService, MapMatchesService dealMatchesService)
        {
            _dbContext = dbContext;
            _routesService = routesService;
            _dealLocationsService = dealLocationsService;
            _mapMatchesService = dealMatchesService;
        }

        //Tallentaa tietokantaan uuden kaupan, ja siihen liittyvät reitit, päivämäärät yms.
        [Authorize]
        [HttpPost("PostDeal")]
        public async Task<IActionResult> PostDeal([FromBody] PostDealDTO request)
        {
            // Tarkistetaan, onko pyyntö validi
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            //userId konvertoidaan Guid-tyyppiseksi tietokanta vertailuja varten
            Guid.TryParse(userId, out Guid userIdGuid);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id claim is missing");
            }

            var user = _dbContext.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return Unauthorized("User not found");
            }



            try
            {
                var newDeal = new DeliveryRequest
                {
                    UserId = user.Id,
                    UserName = user.Username,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    IsActive = true,
                    Status = "open",
                    StartLatitude = request.StartLatitude,
                    StartLongitude = request.StartLongitude,
                    EndLatitude = request.EndLatitude,
                    EndLongitude = request.EndLongitude,
                    PickUpAddress = request.PickUpAddress,
                    DeliveryAddress = request.DeliveryAddress,
                    Description = request.Description,
                    CustomerNotes = request.CustomerNotes,
                    VehicleRecommendation = request.VehicleRecommendation,
                    Price = request.Price,
                    // Tarkistetaan, onko pickUpDates-lista tyhjä ja asetetaan se nulliksi, jos on.
                    PickUpDates = request.PickUpDates != null && request.PickUpDates.Count > 0 ? new List<PreferredPickUpDates>() : null,
                    Packages = new List<Package>() // Alustetaan kokoelma tässä
                };

                // Muunna ja lisää PreferredPickUpDates ja Packages
                if (newDeal.PickUpDates != null)
                {
                    foreach (var pickUpDate in request.PickUpDates)
                    {
                        var newPickUpDate = new PreferredPickUpDates
                        {
                            PickUpDate = pickUpDate.PickUpDate,
                            PreferredTimeRanges = new List<TimeRange>()
                        };

                        foreach (var timeFrame in pickUpDate.PreferredTimeFrames)
                        {
                            var timeRange = ConvertStringToTimeRange(timeFrame);
                            if (timeRange != null)
                            {
                                newPickUpDate.PreferredTimeRanges.Add(timeRange);
                            }
                        }

                        newDeal.PickUpDates.Add(newPickUpDate);
                    }
                }

                foreach (var package in request.Packages)
                {
                    var newPackage = new Package
                    {
                        Size = package.Size,
                        Weight = package.Weight,
                    };
                    newDeal.Packages.Add(newPackage);
                }                

                //Objekti tallennetaan, jotta sille saadaan generoitua ID tietokantaan mm. Mongon dokumenttia varten
                await _dbContext.DeliveryRequest.AddAsync(newDeal);
                await _dbContext.SaveChangesAsync();

                // Luo DealLocations objekti MongoDB:n dokumenttia varten
                var dealLocations = new DealLocations
                {
                    DealId = newDeal.Id,
                    UserId = userIdGuid,
                    IsActive = newDeal.IsActive,
                    Status = newDeal.Status,
                    StartLocation = GeoJson.Point(GeoJson.Geographic(request.StartLongitude, request.StartLatitude)),
                    EndLocation = GeoJson.Point(GeoJson.Geographic(request.EndLongitude, request.EndLatitude))
                };

                //Tallentaa dokumentin Mongoon
                await _dealLocationsService.AddDealLocationsAsync(dealLocations);

                //Hakee kaikkien aktiivisten (status=open) reittien leg-karttapisteet MongoDB:stä
                var RouteLocations = await _routesService.GetActiveLegLocationsAsync();
                var fullyMatchedRouteIds = _mapMatchesService.FindRouteMatches(dealLocations, RouteLocations);

                if (fullyMatchedRouteIds.Any())
                {
                    List<DateTime> newDealPickUpDates = newDeal.PickUpDates
                        .Where(pud => pud.PickUpDate.HasValue) // Varmistaa, että PickUpDate ei ole null
                        .Select(pud => pud.PickUpDate.Value.Date) // Ottaa vain päivämääräosan DateTime-objektista
                        .ToList();                    

                    //Haetaan id-listan perusteella reitit joiden varrelle uusi deali osuu
                    var matchedRoutesQuery = _dbContext.Routes
                        .Where(r => fullyMatchedRouteIds.Contains(r.Id) 
                        && r.IsActive 
                        && r.Status == "open"
                        && r.UserId != userIdGuid);


                    // Lisää päivämäärävertailu vain, jos newDeal.PickUpDates ei ole tyhjä ja ei sisällä null-arvoja.
                    //Jos dealin noutopäivä on null, niin silloin se hyväksytään jos sen lokaatiot osuvat reitin varrelle
                    if (newDealPickUpDates.Any())
                    {
                        matchedRoutesQuery = matchedRoutesQuery
                            .Where(r => r.RouteDates.Any(rd => newDealPickUpDates.Contains(rd.RouteDateTime.Date)));
                    }
                    
                    var matchedRoutes = await matchedRoutesQuery
                        .Include(r => r.matchedDealIds)
                        .ToListAsync();

                    foreach (var route in matchedRoutes)
                    {
                        // Luodaan uusi MatchedDealIds-entiteetti jokaista vastaavaa reittiä kohden
                        var matchedDealId = new MatchedDealIds
                        {
                            RouteId = route.Id,
                            MatchedDealId = newDeal.Id // Tässä käytetään uutta Deal Id:tä
                        };

                        // Lisätään luotu entiteetti DbContextiin
                        _dbContext.MatchedDealIds.Add(matchedDealId);
                    }

                    await _dbContext.SaveChangesAsync();
                }


                return Ok(new { Message = "Deal created successfully", DealId = newDeal.Id });

            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
            
        }

        [Authorize]
        [HttpGet("GetOwnDeals")]
        public async Task<IActionResult> GetOwnDeals()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id claim is missing");
            }

            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return Unauthorized("Invalid user ID format");
            }

            var user = await _dbContext.Users.FindAsync(userGuid);

            if (user == null)
            {
                return Unauthorized("User not found");
            }

            try
            {
                var dealsQuery = _dbContext.DeliveryRequest
               .Where(r => r.UserId == userGuid)
               .Include(r => r.Packages)
               .Include(r => r.PickUpDates)
                   .ThenInclude(pud => pud.PreferredTimeRanges)
               .AsEnumerable(); // Tämä siirtää datan palvelimelle ennen TimeRangeDescription-kutsua

                var deals = dealsQuery.Select(d => new GetDealsDTO
                    {
                        Id = d.Id,
                        UserName = d.UserName,
                        CreatedDate = d.CreatedDate,
                        LastModifiedDate = d.LastModifiedDate,
                        IsActive = d.IsActive,
                        Status = d.Status,
                        StartLatitude = d.StartLatitude,
                        StartLongitude = d.StartLongitude,
                        EndLatitude = d.EndLatitude,
                        EndLongitude = d.EndLongitude,
                        PickUpAddress = d.PickUpAddress,
                        DeliveryAddress = d.DeliveryAddress,
                        Description = d.Description,
                        CustomerNotes = d.CustomerNotes,
                        VehicleRecommendation = d.VehicleRecommendation,
                        Price = d.Price,
                        Packages = d.Packages.Select(p => new GetDealsPackageDTO
                        {
                            Id = p.Id,
                            Size = p.Size,
                            Weight = p.Weight
                        }).ToList(),
                        PickUpDates = d.PickUpDates.Select(pud => new GetDealsPickUpDateDTO
                        {
                            Id = pud.Id,
                            PickUpDate = pud.PickUpDate,
                            PreferredTimeFrames = pud.PreferredTimeRanges
                            .Select(ptr => TimeRangeDescription(ptr))
                            .ToList()
                        }).ToList()
                }).ToList();

                return Ok(deals);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        //Hakee muiden dealit, jotka ovat käyttäjän sijainnin lähellä. Syötteenä voi antaa säteen kilometreinä, kuinka kaukaa etsitään.
        [Authorize]
        [HttpPost("GetOthersDealsNearYou")]
        public async Task<IActionResult> GetOthersDealsNearYou([FromBody] GetOthersDealsNearYouDTO request)
        {

            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            Guid.TryParse(userId, out Guid userIdGuid);

            if (request == null)
            {
                return Unauthorized("Wrong request format");
            }

            try
            {
                var geoNearOptions = new BsonDocument
        {
            { "near", new BsonDocument
                {
                    { "type", "Point" },
                    { "coordinates", new BsonArray { request.CurrentLocationLongitude, request.CurrentLocationLatitude } }
                }
            },
            { "distanceField", "distance" },
            { "maxDistance", request.Range * 1000 },
            { "spherical", true }
        };

                var match = new BsonDocument
        {
            { "$match", new BsonDocument
                {
                    { "UserId", new BsonDocument { { "$ne", userId } } }, // Oletus että UserId on tallennettu MongoDB-dokumenttiin
                    { "IsActive", true },
                    { "Status", "open" }
                }
            }
        };

                var pipeline = new BsonDocument[]
                {
            new BsonDocument { { "$geoNear", geoNearOptions } },
            match
                };

                var dealLocations = await _dealLocationsService.GetDealLocationsCollection().Aggregate<DealLocations>(pipeline).ToListAsync();

                // Kerää DealId:t MongoDB-haun tuloksista
                var dealIds = dealLocations.Select(dl => dl.DealId).ToList();

                var deals = await _dbContext.DeliveryRequest
                    .Where(dr => dealIds.Contains(dr.Id) && dr.UserId != userIdGuid) // userId on käyttäjän tunniste
                    .Include(r => r.Packages)
                    .Include(r => r.PickUpDates)
                        .ThenInclude(pud => pud.PreferredTimeRanges)
                    .ToListAsync();

                // Säilytä MongoDB-haun mukainen järjestys
                deals = deals.OrderBy(d => dealIds.IndexOf(d.Id)).ToList();

                return Ok(deals);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [Authorize]
        [HttpGet("GetDealsById/{id}")]
        public async Task<IActionResult> GetDealsById(Guid id)
        {
            if (id == null)
            {
                return BadRequest("Invalid ID");
            }

            var deal = await _dbContext.DeliveryRequest
                .Where(d => d.Id == id)
                .Include(d => d.Packages)
                .Include(d => d.PickUpDates)
                    .ThenInclude(pud => pud.PreferredTimeRanges)
                .SingleOrDefaultAsync();

            if (deal == null)
            {
                return NotFound("Deal not found");
            }

            try
            {
                var dealDto = new GetDealsDTO
                {
                    Id = deal.Id,
                    UserName = deal.UserName,
                    CreatedDate = deal.CreatedDate,
                    LastModifiedDate = deal.LastModifiedDate,
                    IsActive = deal.IsActive,
                    Status = deal.Status,
                    StartLatitude = deal.StartLatitude,
                    StartLongitude = deal.StartLongitude,
                    EndLatitude = deal.EndLatitude,
                    EndLongitude = deal.EndLongitude,
                    PickUpAddress = deal.PickUpAddress,
                    DeliveryAddress = deal.DeliveryAddress,
                    Description = deal.Description,
                    CustomerNotes = deal.CustomerNotes,
                    VehicleRecommendation = deal.VehicleRecommendation,
                    Price = deal.Price,
                    Packages = deal.Packages.Select(p => new GetDealsPackageDTO
                    {
                        Id = p.Id,
                        Size = p.Size,
                        Weight = p.Weight
                    }).ToList(),
                    PickUpDates = deal.PickUpDates.Select(pud => new GetDealsPickUpDateDTO
                    {
                        Id = pud.Id,
                        PickUpDate = pud.PickUpDate,
                        PreferredTimeFrames = pud.PreferredTimeRanges
                        .Select(ptr => TimeRangeDescription(ptr))
                        .ToList()
                    }).ToList()
                };

                return Ok(dealDto);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }


        [Authorize]
        [HttpPut("UpdateDeal")]
        public async Task<IActionResult> UpdateDeal([FromBody] UpdateDealsDTO updateDealDto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var deal = await _dbContext.DeliveryRequest
                    .Include(r => r.Packages)
                    .Include(r => r.PickUpDates)
                        .ThenInclude(pud => pud.PreferredTimeRanges)
                    .FirstOrDefaultAsync(d => d.Id == updateDealDto.Id);

                if (deal == null)
                {
                    return NotFound($"Deal with id {updateDealDto.Id} not found.");
                }

                //Haettu userId konvertoidaan Guid-tyyppiseksi tietokanta vertailuja varten
                var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
                Guid.TryParse(userId, out Guid userIdGuid);

                // Tallenna alkuperäiset sijainti- ja noutopäivämäärät ennen päivitystä vertailua varten
                var originalStartLatitude = deal.StartLatitude;
                var originalStartLongitude = deal.StartLongitude;
                var originalEndLatitude = deal.EndLatitude;
                var originalEndLongitude = deal.EndLongitude;
                var originalPickUpDates = deal.PickUpDates
                    .Select(pud => pud.PickUpDate)
                    .ToList();

                // Päivitä Dealin yksinkertaiset kentät
                deal.StartLatitude = updateDealDto.StartLatitude ?? deal.StartLatitude;
                deal.StartLongitude = updateDealDto.StartLongitude ?? deal.StartLongitude;
                deal.EndLatitude = updateDealDto.EndLatitude ?? deal.EndLatitude;
                deal.EndLongitude = updateDealDto.EndLongitude ?? deal.EndLongitude;
                deal.PickUpAddress = updateDealDto.PickUpAddress ?? deal.PickUpAddress;
                deal.DeliveryAddress = updateDealDto.DeliveryAddress ?? deal.DeliveryAddress;
                deal.Description = updateDealDto.Description ?? deal.Description;
                deal.CustomerNotes = updateDealDto.CustomerNotes ?? deal.CustomerNotes;
                deal.VehicleRecommendation = updateDealDto.VehicleRecommendation ?? deal.VehicleRecommendation;
                deal.Price = updateDealDto.Price ?? deal.Price;


                // Päivitä tai poista Packages
                var packageIdsToUpdate = updateDealDto.Packages.Select(p => p.Id).ToList();
                var packagesToRemove = deal.Packages.Where(p => !packageIdsToUpdate.Contains(p.Id)).ToList();
                _dbContext.Packages.RemoveRange(packagesToRemove);

                // Päivitä Packages
                foreach (var packageDto in updateDealDto.Packages ?? new List<UpdateDealsPackageDTO>())
                {
                    var existingPackage = deal.Packages.FirstOrDefault(p => p.Id == packageDto.Id);
                    if (existingPackage != null)
                    {
                        // Jos Package löytyy, päivitä vain muuttuneet kentät
                        existingPackage.Size = packageDto.Size ?? existingPackage.Size;
                        existingPackage.Weight = packageDto.Weight ?? existingPackage.Weight;
                    }
                    else
                    {
                        // Jos uusi Package, lisää se Deal-olioon
                        deal.Packages.Add(new Package
                        {
                            Size = packageDto.Size,
                            Weight = packageDto.Weight
                        });
                    }
                }

                // Päivitä tai poista PickUpDates ja PreferredTimeRanges
                var pickUpDateIdsToUpdate = updateDealDto.PickUpDates.Select(pud => pud.Id).ToList();
                var pickUpDatesToRemove = deal.PickUpDates.Where(pud => !pickUpDateIdsToUpdate.Contains(pud.Id)).ToList();
                _dbContext.preferredPickUpDates.RemoveRange(pickUpDatesToRemove);

                // Päivitä PickUpDates ja PreferredTimeRanges
                foreach (var pudDto in updateDealDto.PickUpDates ?? new List<UpdateDealsPickUpDateDTO>())
                {
                    var existingPickUpDate = deal.PickUpDates.FirstOrDefault(pud => pud.Id == pudDto.Id);
                    if (existingPickUpDate != null)
                    {
                        // Päivitä olemassaolevan PickUpDate-olion tiedot
                        existingPickUpDate.PickUpDate = pudDto.PickUpDate ?? existingPickUpDate.PickUpDate;

                        // Päivitä PreferredTimeRanges
                        UpdatePreferredTimeRanges(existingPickUpDate.PreferredTimeRanges, pudDto.PreferredTimeFrames);
                    }
                    else
                    {
                        // Jos uusi PickUpDate, lisää se Deal-olioon
                        deal.PickUpDates.Add(new PreferredPickUpDates
                        {
                            PickUpDate = pudDto.PickUpDate,
                            PreferredTimeRanges = pudDto.PreferredTimeFrames
                                .Select(t => ConvertStringToTimeRange(t)).ToList()
                        });
                    }
                }

                //Jos uuden diilin noutopäiviin, nouto- tai toimituspaikkaan on tehty muutoksia, tehdään reittejä vasten uusi "päivityshaku" ja tallennus
                // Tarkista onko sijainti- tai noutopäivämäärissä tapahtunut muutoksia
                var isLocationChanged = originalStartLatitude != updateDealDto.StartLatitude ||
                                        originalStartLongitude != updateDealDto.StartLongitude ||
                                        originalEndLatitude != updateDealDto.EndLatitude ||
                                        originalEndLongitude != updateDealDto.EndLongitude;

                // Oletetaan, että tyhjä PickUpDates tarkoittaa joustavuutta päivämäärissä
                //Jos lista on tyhjä, tai jos sen sisällä on "PickUpDate" joka on null - silloin jatkoon.
                var isPickUpDatesChanged = updateDealDto.PickUpDates != null && !originalPickUpDates.SequenceEqual(updateDealDto.PickUpDates.Select(p => p.PickUpDate));


                if (isLocationChanged || isPickUpDatesChanged)
                {
                    // Päivittää diilin sijainti- ja noutopäivämäärätiedot

                    // Luo uusi DealLocations-objekti MongoDB:tä varten päivitetyillä tiedoilla
                    var dealLocations = new DealLocations
                    {
                        DealId = deal.Id,
                        UserId = userIdGuid,
                        IsActive = deal.IsActive,
                        Status = deal.Status,
                        StartLocation = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                            new GeoJson2DGeographicCoordinates(updateDealDto.StartLongitude ?? deal.StartLongitude, updateDealDto.StartLatitude ?? deal.StartLatitude)),
                        EndLocation = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                            new GeoJson2DGeographicCoordinates(updateDealDto.EndLongitude ?? deal.EndLongitude, updateDealDto.EndLatitude ?? deal.EndLatitude))
                    };

                    // Päivitä DealLocations MongoDB:ssä
                    await _dealLocationsService.UpdateDealLocationsAsync(deal.Id, dealLocations);

                    // Poista kaikki olemassa olevat matchedDealIds, jotka viittaavat kyseiseen diiliin
                    var existingMatchedDeals = await _dbContext.MatchedDealIds
                        .Where(md => md.MatchedDealId == deal.Id)
                        .ToListAsync();
                    _dbContext.MatchedDealIds.RemoveRange(existingMatchedDeals);

                    // Hae uudet matchit reittejä vasten
                    var activeLegLocations = await _routesService.GetActiveLegLocationsAsync();
                    var fullyMatchedRouteIds = _mapMatchesService.FindRouteMatches(dealLocations, activeLegLocations);

                    if (fullyMatchedRouteIds.Any())
                    {
                        // Haetaan reittiobjektit, joiden id on fullyMatchedRouteIds-listalla ja jotka ovat aktiivisia sekä avoimia
                        // (jättäen pois kirjautuneen käyttäjän omat reitit)
                        var matchedRoutes = await _dbContext.Routes
                        .Where(route => fullyMatchedRouteIds.Contains(route.Id)
                                        && route.IsActive
                                        && route.Status == "open"
                                        && route.UserId != userIdGuid) // Tarkistetaan, että reitin käyttäjä-ID ei vastaa kirjautuneen käyttäjän ID:tä
                        .Include(route => route.RouteDates)
                        .ToListAsync();

                        // Vertaa uuden dealin noutopäivämääriä reittien päivämääriin
                        foreach (var route in matchedRoutes)
                        {
                            var routePickUpDates = route.RouteDates
                                .Select(rd => rd.RouteDateTime.Date)
                                .ToList();

                            // Jos RouteDates on tyhjä tai RouteDateTime on null, katsotaan sopivaksi
                            // Tai jos on edes yksi sama päivämäärä
                            var dealPickUpDates = updateDealDto.PickUpDates
                                .Select(pud => pud.PickUpDate?.Date)
                                .ToList();

                            bool isRouteSuitable = !route.RouteDates.Any() || 
                                updateDealDto.PickUpDates == null || 
                                routePickUpDates.Any(date => dealPickUpDates.Contains(date) || 
                                dealPickUpDates.Contains(null));


                            if (isRouteSuitable)
                            {
                                // Reitin varrelle osuneet kaupat tallennetaan MatchedDealsIds-tauluun reitti-idllä varustettuna
                                var matchedDealId = new MatchedDealIds
                                {
                                    RouteId = route.Id,
                                    MatchedDealId = deal.Id
                                };
                                _dbContext.MatchedDealIds.Add(matchedDealId);
                            }
                        }

                        await _dbContext.SaveChangesAsync();
                    }


                }

                deal.LastModifiedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok($"Deal with id {updateDealDto.Id} updated successfully.");
            }
            catch
            {
                // Log error
                await transaction.RollbackAsync();
                throw;
            }

        }

        private void UpdatePreferredTimeRanges(ICollection<TimeRange> existingRanges, List<string> newFrames)
        {
            // Jos uusia aikavälejä ei ole annettu, poista kaikki olemassa olevat aikavälit.
            if (newFrames == null || newFrames.Count == 0)
            {
                existingRanges.Clear();
                return;
            }

            // Muunna kaikki uudet aikavälit TimeRange-olioiksi, ohittaen null-arvot.
            var newTimeRanges = newFrames.Where(frame => frame != "null").Select(frame => ConvertStringToTimeRange(frame)).ToList();

            // Etsi poistettavat aikavälit ja poista ne.
            var rangesToRemove = existingRanges.Where(existing => !newTimeRanges.Any(ntr => ntr != null && ntr.StartTime == existing.StartTime && ntr.EndTime == existing.EndTime)).ToList();
            foreach (var rangeToRemove in rangesToRemove)
            {
                existingRanges.Remove(rangeToRemove);
            }

            // Lisää uudet aikavälit, jotka eivät ole vielä olemassa.
            foreach (var newRange in newTimeRanges)
            {
                if (newRange != null && !existingRanges.Any(existing => existing.StartTime == newRange.StartTime && existing.EndTime == newRange.EndTime))
                {
                    existingRanges.Add(newRange);
                }
            }
        }


        private string TimeRangeDescription(TimeRange? timeRange)
        {
            if (timeRange == null || timeRange.StartTime == null || timeRange.EndTime == null)
            {
                // Jos timeRange on null tai sen StartTime/EndTime on null, palautetaan tyhjä merkkijono.
                return "";
            }

            // Aikavälien määrittely
            var morningStart = new TimeSpan(6, 0, 0);
            var morningEnd = new TimeSpan(11, 59, 59);
            var dayStart = new TimeSpan(12, 0, 0);
            var dayEnd = new TimeSpan(17, 59, 59);
            var eveningStart = new TimeSpan(18, 0, 0);
            var eveningEnd = new TimeSpan(23, 59, 59);
            var nightStart = new TimeSpan(0, 0, 1);
            var nightEnd = new TimeSpan(5, 59, 59);

            // Vuorokaudenaikojen tunnistaminen
            if (timeRange.StartTime.Value == morningStart && timeRange.EndTime.Value == morningEnd)
            {
                return "morning";
            }
            else if (timeRange.StartTime.Value == dayStart && timeRange.EndTime.Value == dayEnd)
            {
                return "day";
            }
            else if (timeRange.StartTime.Value == eveningStart && timeRange.EndTime.Value == eveningEnd)
            {
                return "evening";
            }
            else if (timeRange.StartTime.Value == nightStart && timeRange.EndTime.Value == nightEnd)
            {
                return "night";
            }
            else
            {
                // Jos aikaväli ei vastaa yhtään määriteltyä, palautetaan muotoiltu aikaväli.
                return $"{timeRange.StartTime.Value:hh\\:mm} - {timeRange.EndTime.Value:hh\\:mm}";
            }
        }


        private TimeRange ConvertStringToTimeRange(string timeFrame)
        {
            switch (timeFrame.ToLower())
            {
                case "morning":
                    return new TimeRange { StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(11, 59, 59) };
                case "day":
                    return new TimeRange { StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(17, 59, 59) };
                case "evening":
                    return new TimeRange { StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(23, 59, 59) };
                case "night":
                    return new TimeRange { StartTime = new TimeSpan(0, 0, 1), EndTime = new TimeSpan(5, 59, 59) };
                default:
                    // Palauttaa null, jos aikaväli ei vastaa mitään määriteltyä vuorokaudenaikaa tai on tyhjä
                    return null;
            }
        }


    }


}
