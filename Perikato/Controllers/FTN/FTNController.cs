using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Perikato.Controllers.FTN.FTOControllerDTO;
using Perikato.Data;
using Perikato.Services;

namespace Perikato.Controllers


{
    [ApiController]
    [Route("eident")]
    public class FTNController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FTNController> _logger;
        private readonly OIDC_Service _oidcService;
        private readonly IUserClaimsService _userClaimsService;
        private readonly UserDataHandler _userDataHandler;
        private readonly ApplicationDbContext _dbContext;

        public FTNController(ILogger<FTNController> logger, IHttpClientFactory httpClientFactory, OIDC_Service oidcService, IUserClaimsService userClaimsService, UserDataHandler userDataHandler, ApplicationDbContext dbContext)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _oidcService = oidcService;
            _userClaimsService = userClaimsService;
            _userDataHandler = userDataHandler;
            _dbContext = dbContext;
        }

        // Tunnistautumisprosessi
        [HttpGet("OpenIdP")]
        public IActionResult OpenIDP()
        {
            // OIDC-tunnistautumisen jälkeen käyttäjä ohjataan tämän päätepisteen kautta
            string redirectUriAfterAuth = Url.Action("CustomData", "eident", null, Request.Scheme);
            return Challenge(new AuthenticationProperties { RedirectUri = redirectUriAfterAuth }, "oidc");
        }
         

        // Tämä on päätepiste, johon OIDC-tunnistautumisen jälkeen ohjataan.
        [HttpGet("CustomData")]
        public IActionResult CustomData()
        {
            var claims = _userClaimsService.CurrentUser?.Claims;
            string loginToken;

            if (claims == null)
            {
                // Käyttäjä ei ole tunnistautunut
                var unauthorizedUri = $"com.muulipalvelu.muuli://customdata?data=unauthorized";
                return Redirect(unauthorizedUri);
            }
            else
            {
                loginToken = _userDataHandler.CheckIfUserExist(claims);
            }          
                       

            var reactNativeAppRedirectUri = $"com.muulipalvelu.muuli://customdata?data={loginToken}";

            return Redirect(reactNativeAppRedirectUri);
        }

        [Authorize]
        [HttpGet("GetUserInfo")]
        public IActionResult GetUserInfo()
        {
            // Claimista saatua tupasPID:tä käytetään käyttäjän etsimiseen tietokannasta
            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User id claim is missing");
            }

            var user = _dbContext.Users.FirstOrDefault(u => u.Id.ToString() == userId);

            // Tarkistaa, onko käyttäjä olemassa
            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var userInfo = new
            {
                user.Username,
                user.GivenName,
                user.FamilyName,
                user.Email,
                user.Phonenumber,
                user.DefaultVehicle,
                user.CreatedDate
            };

            return Ok(userInfo);
        }

        [Authorize]
        [HttpPost("UpdateUserInfo")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UpdateUserInfoDTO request)
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
            try
            {
                // Päivittää käyttäjän tiedot, jos pyynnössä annetut arvot eivät ole null tai tyhjiä
                user.Email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : user.Email;
                user.Phonenumber = !string.IsNullOrWhiteSpace(request.Phonenumber) ? request.Phonenumber : user.Phonenumber;
                user.DefaultVehicle = !string.IsNullOrWhiteSpace(request.DefaultVehicle) ? request.DefaultVehicle : user.DefaultVehicle;

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while processing your request.");
            }
            

            return Ok("User information updated successfully");
        }

    }
}
