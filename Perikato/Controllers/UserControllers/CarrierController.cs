using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using Perikato.Controllers.UserControllers.CarrierControllerDTO;
using Perikato.Data;
using Perikato.Data.Carriers;
using Perikato.Data.MongoDb;
using Perikato.Services;
using Perikato.Services.MongoDbServices;
using System.Net.Http;

namespace Perikato.Controllers.UserControllers
{
    public class CarrierController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly GoogleMapsLegsService _routesService;
        private readonly DealLocationsService _dealLocationsService;
        private readonly MapMatchesService _mapMatchesService;
        

        public CarrierController(ApplicationDbContext dbContext, GoogleMapsLegsService routesService, DealLocationsService dealLocationsService, MapMatchesService dealMatchesService)
        {
            _dbContext = dbContext;
            _routesService = routesService;
            _dealLocationsService = dealLocationsService;
            _mapMatchesService = dealMatchesService;
        }

        //Tallentaa tietokantaan uuden reitin, ja siihen liittyvät päivämäärät
        [Authorize]
        [HttpPost("PostRoute")]
        public async Task<IActionResult> PostRoute([FromBody] PostRouteDTO request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data");
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id claim is missing");
            }

            var user = _dbContext.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return Unauthorized("User not found");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Luo uusi Routes-olio
                var newRoute = new Routes
                {
                    UserId = user.Id,
                    UserName = user.Username,
                    Vehicle = request.Vehicle,
                    Range = request.Range,
                    StartLatitude = ValidateLatitude(request.StartLatitude),
                    StartLongitude = ValidateLongitude(request.StartLongitude),
                    EndLatitude = ValidateLatitude(request.EndLatitude),
                    EndLongitude = ValidateLongitude(request.EndLongitude),
                    IsActive = true,
                    Status = "open",
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };

                // Lisää uusi Routes-olio tietokantaan
                await _dbContext.Routes.AddAsync(newRoute);
                // Tallenna muutokset tietokantaan, jotta newRoute.Id arvo luodaan myöhempää käsittelyä varten
                await _dbContext.SaveChangesAsync();

                // Käsittele jokainen RouteDate
                foreach (var routeDate in request.RouteDates)
                {
                    var newRouteDate = new RouteDates
                    {
                        RouteId = newRoute.Id, // Uuden Routes-olion Id
                        RouteDateTime = routeDate.RouteDateTime
                    };

                    // Lisää uusi RouteDates-olio tietokantaan
                    await _dbContext.RouteDates.AddAsync(newRouteDate);
                }

                // Luo ja tallenna MongoDB:hen GoogleMapsLegs-objekti.
                // Tämän reitin routeLegit tulisi syöttää palveluun, joka yrittää löytää DealLocationseja näiden legejen varrelta
                var routeLegs = request.RouteLegs.Select(leg => new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(leg.Longitude, leg.Latitude))).ToList();

                var googleMapsLegs = new GoogleMapsLegs
                {
                    RouteId = newRoute.Id,
                    GoogleMapsLegsList = routeLegs,
                    IsActive = true,
                    Status = "open",
                    Range = newRoute.Range
                };
                // Tallenna reittipisteet MongoDb-tietokantaan
                await _routesService.AddRouteLegsAsync(googleMapsLegs);


                // Tallenna uuden reitin muutokset SQL-tietokantaan
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();


                //Hakee kaikkien aktiivisten Dealien lähtö- ja päätelokaatiopisteet MongoDB:stä
                var DealLocations = await _dealLocationsService.GetActiveDealLocationsAsync();
                var fullyMatchedDeals = _mapMatchesService.FindDealMatches(routeLegs, DealLocations, request.Range);

                if (fullyMatchedDeals.Any())
                {
                    // Haetaan uuteen reittiin tallennetut ajopäivämäärät
                    var newRouteDates = newRoute.RouteDates.Select(rd => rd.RouteDateTime.Date).ToList();

                    //Tietokannasta aikaisemmin haettu userId konvertoidaan Guid-tyyppiseksi, jotta käyttäjän omiin reitteihin ei tallenneta hänen päivittämäänsä Dealia
                    Guid.TryParse(userId, out Guid userIdGuid);

                    // Hae kaikki aktiiviset Dealit, jotka ovat fullyMatchedDeals-listassa.
                    //Jättää käyttäjän mahdolliset omat dealit pois päivitettävän reitin varrelta
                    var matchedDeals = await _dbContext.DeliveryRequest
                        .Where(dr => fullyMatchedDeals.Contains(dr.Id)
                        && dr.IsActive
                        && dr.Status == "open"
                        && dr.UserId != userIdGuid)
                        .Include(dr => dr.PickUpDates)
                        .ToListAsync();

                    foreach (var matchedDeal in matchedDeals)
                    {
                        // Varmista, että jokainen PickUpDate ei ole null ja muunna ne päivämäärälistaksi
                        var dealPickUpDates = matchedDeal.PickUpDates
                            .Where(pud => pud.PickUpDate.HasValue)
                            .Select(pud => pud.PickUpDate.Value.Date)
                            .ToList();

                        // Jos Dealin PickUpDates on tyhjä, oletetaan, että se sopii mille tahansa päivälle
                        bool datesMatch = !dealPickUpDates.Any() || newRouteDates.Any(date => dealPickUpDates.Contains(date));

                        if (datesMatch) // Jos löytyy yhteensopiva päivämäärä tai päivämääriä ei ole määritelty...
                        {
                            var matchedDealId = new MatchedDealIds
                            {
                                RouteId = newRoute.Id,
                                MatchedDealId = matchedDeal.Id
                            };

                            // Lisää luotu MatchedDealIds entiteetti tietokantaan
                            _dbContext.MatchedDealIds.Add(matchedDealId);
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                }

                return Ok("Route data processed successfully");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [Authorize]
        [HttpGet("GetOwnRoutes")]
        public async Task<IActionResult> GetOwnRoutes()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id claim is missing");
            }

            try
            {
                var routes = await _dbContext.Routes
                                    .Where(r => r.UserId == Guid.Parse(userId) && r.IsActive == true)
                                    .Include(r => r.RouteDates)
                                    .Include(r => r.matchedDealIds)
                                    .ToListAsync();

                var routeDTOs = new List<GetRoutesDTO>();
                foreach (var route in routes)
                {
                    var routeDTO = new GetRoutesDTO
                    {
                        Id = route.Id,
                        CreatedDate = route.CreatedDate,
                        LastModifiedDate = route.LastModifiedDate,
                        Vehicle = route.Vehicle,
                        Range = route.Range,
                        StartLatitude = route.StartLatitude,
                        StartLongitude = route.StartLongitude,
                        EndLatitude = route.EndLatitude,
                        EndLongitude = route.EndLongitude,
                        RouteDates = route.RouteDates.Select(rd => new GetRoutesDTO.GetRouteRouteDateDTO
                        {
                            Id = rd.Id,
                            RouteDateTime = rd.RouteDateTime
                        }).ToList(),
                        MatchedDeals = route.matchedDealIds.Select(md => new GetRoutesDTO.GetRouteMatchedDealIdsDTO
                        {
                            Id = md.MatchedDealId
                        }).ToList()
                    };

                    // Hae reittipisteet (legit) MongoDB:stä
                    var legs = await _routesService.GetRouteLegsAsync(route.Id);

                    routeDTO.RouteLegs = legs.GoogleMapsLegsList.Select(leg => new GetRoutesDTO.GetRouteLegDTO
                    {
                        Latitude = (float)leg.Coordinates.Latitude,
                        Longitude = (float)leg.Coordinates.Longitude
                    }).ToList();

                    routeDTOs.Add(routeDTO);
                }

                return Ok(routeDTOs);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        //Päivitä reitti ja sen alaluokkien tiedot
        [Authorize]
        [HttpPost("UpdateRoute")]
        public async Task<IActionResult> UpdateRoute([FromBody] UpdateRouteDTO request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data");
            }

            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id claim is missing");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var existingRoute = await _dbContext.Routes
                .Include(r => r.RouteDates)
                .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == user.Id);

            if (existingRoute == null)
            {
                return NotFound("Route not found");
            }

            // Tallenna alkuperäiset koordinaatit ennen muutosta
            var originalStartLatitude = existingRoute.StartLatitude;
            var originalStartLongitude = existingRoute.StartLongitude;
            var originalEndLatitude = existingRoute.EndLatitude;
            var originalEndLongitude = existingRoute.EndLongitude;
            var originalRange = existingRoute.Range;
            var originalDates = existingRoute.RouteDates
            .Select(rd => new RouteDates { RouteDateTime = rd.RouteDateTime })
            .ToList();

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Päivitä olemassa olevan reitin tiedot
                existingRoute.Vehicle = request.Vehicle;
                existingRoute.Range = request.Range;
                existingRoute.StartLatitude = ValidateLatitude(request.StartLatitude);
                existingRoute.StartLongitude = ValidateLongitude(request.StartLongitude);
                existingRoute.EndLatitude = ValidateLatitude(request.EndLatitude);
                existingRoute.EndLongitude = ValidateLongitude(request.EndLongitude);

                // Poista vanhat RouteDates-entiteetit
                var routeDateIdsInRequest = request.RouteDates.Select(rd => rd.Id).ToList();
                var routeDatesToRemove = existingRoute.RouteDates
                    .Where(rd => !routeDateIdsInRequest.Contains(rd.Id))
                    .ToList();

                foreach (var routeDate in routeDatesToRemove)
                {
                    _dbContext.RouteDates.Remove(routeDate);
                }

                // Päivitä tai lisää RouteDates
                foreach (var routeDateDto in request.RouteDates)
                {
                    if (routeDateDto.Id.HasValue)
                    {
                        var existingRouteDate = existingRoute.RouteDates
                            .FirstOrDefault(rd => rd.Id == routeDateDto.Id.Value);

                        if (existingRouteDate != null)
                        {
                            // Päivitä olemassa oleva RouteDates
                            existingRouteDate.RouteDateTime = routeDateDto.RouteDateTime;
                        }
                    }
                    else
                    {
                        // Luo uusi RouteDates, koska Id on null
                        var newRouteDate = new RouteDates
                        {
                            RouteId = existingRoute.Id,
                            RouteDateTime = routeDateDto.RouteDateTime,
                        };
                        existingRoute.RouteDates.Add(newRouteDate);
                    }
                }

                // Tarkista, onko lähtö- tai päämääräkoordinaatteja muutettu
                if (RouteOrDatesHasBeenChanged(
                    originalStartLatitude,
                    originalStartLongitude,
                    originalEndLatitude,
                    originalEndLongitude,
                    originalRange,
                    originalDates,
                    request))
                    {
                    var routeLegs = request.RouteLegs.Select(leg => new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                        new GeoJson2DGeographicCoordinates(leg.Longitude, leg.Latitude))).ToList();

                    var googleMapsLegsUpdate = new GoogleMapsLegs
                    {
                        RouteId = existingRoute.Id,
                        GoogleMapsLegsList = routeLegs,
                        Range = existingRoute.Range
                    };

                    await _routesService.UpdateRouteLegsAsync(existingRoute.Id, googleMapsLegsUpdate);

                    // Poista kaikki olemassa olevat matchedDealIds kyseiseltä reitiltä
                    var existingMatchedDeals = _dbContext.MatchedDealIds.Where(md => md.RouteId == existingRoute.Id).ToList();
                    _dbContext.MatchedDealIds.RemoveRange(existingMatchedDeals);

                    // Hae uudelleen aktiiviset diilit ja etsi matchaavat diilit päivitetylle reitille
                    var DealLocations = await _dealLocationsService.GetActiveDealLocationsAsync();
                    var fullyMatchedDeals = _mapMatchesService.FindDealMatches(routeLegs, DealLocations, request.Range);

                    if (fullyMatchedDeals.Any())
                    {
                        // Haetaan uuteen reittiin tallennetut ajopäivämäärät
                        var newRouteDates = existingRoute.RouteDates.Select(rd => rd.RouteDateTime.Date).ToList();

                        //Tietokannasta aikaisemmin haettu userId konvertoidaan Guid-tyyppiseksi, jotta käyttäjän omiin reitteihin ei tallenneta hänen päivittämäänsä Dealia
                        Guid.TryParse(userId, out Guid userIdGuid);

                        // Hae kaikki aktiiviset Dealit, jotka ovat fullyMatchedDeals-listassa.
                        //Jättää käyttäjän mahdolliset omat dealit pois päivitettävän reitin varrelta
                        var matchedDeals = await _dbContext.DeliveryRequest
                            .Where(dr => fullyMatchedDeals.Contains(dr.Id) 
                            && dr.IsActive 
                            && dr.Status == "open"
                            && dr.UserId != userIdGuid)
                            .Include(dr => dr.PickUpDates)
                            .ToListAsync();

                        foreach (var matchedDeal in matchedDeals)
                        {
                            // Varmista, että jokainen PickUpDate ei ole null ja muunna ne päivämäärälistaksi
                            var dealPickUpDates = matchedDeal.PickUpDates
                                .Where(pud => pud.PickUpDate.HasValue)
                                .Select(pud => pud.PickUpDate.Value.Date)
                                .ToList();

                            // Jos Dealin PickUpDates on tyhjä, oletetaan, että se sopii mille tahansa päivälle
                            bool datesMatch = !dealPickUpDates.Any() || newRouteDates.Any(date => dealPickUpDates.Contains(date));

                            if (datesMatch) // Jos löytyy yhteensopiva päivämäärä tai päivämääriä ei ole määritelty...
                            {
                                var matchedDealId = new MatchedDealIds
                                {
                                    RouteId = existingRoute.Id,
                                    MatchedDealId = matchedDeal.Id
                                };

                                // Lisää luotu MatchedDealIds entiteetti tietokantaan
                                _dbContext.MatchedDealIds.Add(matchedDealId);
                            }
                        }
                    }
                    
                }
                existingRoute.LastModifiedDate = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok("Route updated successfully");
            }
            catch (ArgumentException ex)
            {
                // Palauta BadRequest (400) mukautetulla viestillä, jos ongelma on väärässä syötteessä
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Palauta InternalServerError (500)
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        private bool RouteOrDatesHasBeenChanged(
       float originalStartLatitude,
       float originalStartLongitude,
       float originalEndLatitude,
       float originalEndLongitude,
       int originalRange,
       List<RouteDates> originalRouteDates,
       UpdateRouteDTO request)
        {
            // Tarkistaa ensin, onko perustiedoissa muutoksia.
            if (originalStartLatitude != request.StartLatitude ||
                originalStartLongitude != request.StartLongitude ||
                originalEndLatitude != request.EndLatitude ||
                originalEndLongitude != request.EndLongitude ||
                originalRange != request.Range)
            {
                return true;
            }

            // Tarkistaa sitten, onko päivämäärissä muutoksia.
            // Muuntaa ensin DTO:n päivämäärät päivämääräarvoiksi.
            var requestDates = request.RouteDates
                .Select(dto => dto.RouteDateTime.Date)
                .OrderBy(d => d)
                .ToList();

            // Vertaa alkuperäisiä päivämääriä päivitettyihin.
            var originalDates = originalRouteDates
                .Select(rd => rd.RouteDateTime.Date)
                .OrderBy(d => d)
                .ToList();

            return !requestDates.SequenceEqual(originalDates);
        }


        private float ValidateLatitude(float latitude)
        {
            if (latitude < -90 || latitude > 90)
            {
                throw new ArgumentException("Invalid latitude value.");
            }
            return latitude;
        }

        private float ValidateLongitude(float longitude)
        {
            if (longitude < -180 || longitude > 180)
            {
                throw new ArgumentException("Invalid longitude value.");
            }
            return longitude;
        }


    }
}
