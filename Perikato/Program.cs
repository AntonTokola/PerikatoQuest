using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Perikato.Data;
using Perikato.Services;
using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Design;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Perikato.Services.MongoDbServices;



var builder = WebApplication.CreateBuilder(args);
OIDC_Service OIDC_service = new OIDC_Service();

// Lisätään konttiin tarvittavia palveluja, kuten kontrollerit ja lokitus.
builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Swagger/OpenAPI-konfiguraatio.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Perikato API", Version = "v1" });

    // JWT-tuki Swaggeriin
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please insert JWT with Bearer into field",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }});
});

// Lue konfiguraatiotiedostot 
ConfigurationManager configuration = builder.Configuration;

// MS-SQL Databasen konfigurointi paikallisia asetuksia käyttäen [PAIKALLINEN]
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// MongoDB:n konfigurointi paikallisia asetuksia käyttäen [PAIKALLINEN]
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = MongoClientSettings.FromConnectionString(
        configuration.GetConnectionString("MongoDbConnection")
    );
    settings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
    return new MongoClient(settings);
});

// Rekisteröi RoutesService [PAIKALLINEN]
builder.Services.AddSingleton<GoogleMapsLegsService>(serviceProvider =>
{
    var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
    string databaseName = configuration["MongoDbDatabaseName"]; // Määritelty appsettings.json:ssa
    return new GoogleMapsLegsService(mongoClient, databaseName, "GoogleMapsLegs");
});

builder.Services.AddSingleton<DealLocationsService>(serviceProvider =>
{
    var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
    string databaseName = configuration["MongoDbDatabaseName"]; // Määritelty appsettings.json:ssa
    return new DealLocationsService(mongoClient, databaseName, "DealLocations");
});


//Azure avaimet haetaan ja tallennetaan
var (identityTokenCertificate, OIDC_client_id, OIDC_client_secret, PerikatoDBpass, PerikatoMongoDBpass) = OIDC_Service.ConnectToAzureKeyVault(builder.Configuration);

//// MS-SQL databasen konfigurointi Key Vaultia käyttäen [PALVELIN]
//string connectionString = $"Server=tcp:perikatoserver.database.windows.net,1433;Initial Catalog=PerikatoDB;Persist Security Info=False;User ID=perikatodb;Password={PerikatoDBpass.Value};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

//// MongoDB databasen konfigurointi Key Vaultia käyttäen [PALVELIN]
//string mongoDbConnectionString = $"mongodb://mongogeodb:{PerikatoMongoDBpass.Value}@mongogeodb.mongo.cosmos.azure.com:10255/?ssl=true&retrywrites=false&replicaSet=globaldb&maxIdleTimeMS=120000&appName=@mongogeodb@";
//var mongoClientSettings = MongoClientSettings.FromConnectionString(mongoDbConnectionString);
//mongoClientSettings.SslSettings = new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };

//// Konfiguroi MongoDB palvelukonttiin [PALVELIN]
//builder.Services.AddSingleton<IMongoClient>(serviceProvider => new MongoClient(mongoClientSettings));

//// Rekisteröi GoogleMapsLegsService ja DealLocationsService [PALVELIN]
//string databaseName = configuration["MongoDbDatabaseName"]; // Määritelty appsettings.jsonissa
//builder.Services.AddSingleton(serviceProvider =>
//    new GoogleMapsLegsService(serviceProvider.GetRequiredService<IMongoClient>(), databaseName, "GoogleMapsLegs"));
//builder.Services.AddSingleton(serviceProvider =>
//    new DealLocationsService(serviceProvider.GetRequiredService<IMongoClient>(), databaseName, "DealLocations"));

// [PALVELIN JA PAIKALLINEN]
builder.Services.AddSingleton<MapMatchesService>();

//JWT-Tokenin konfigurointi
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Käytä allekirjoittavan avaimen julkista avainta
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("signingKey")),
            // Validointiasetukset
            ValidateIssuer = false,
            ValidateAudience = false,
            // Konfiguroi kellonpoikkeama
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddScoped<UserDataHandler>();

//Henkilötietojen siirto OIDC-prosessista API-kontrollerille endpointin käsittelyä varten
builder.Services.AddSingleton<IUserClaimsService, UserClaimsService>();


//CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:8080")
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});


// HttpClient
builder.Services.AddHttpClient();

//Oletusarvoisen SSL-sertifikaatin käyttöönotto
builder.WebHost.UseKestrel(options =>
{
    //options.Listen(IPAddress.Loopback, 8080);  // http
    options.Listen(IPAddress.Loopback, 8080, listenOptions =>  // https
    {
        listenOptions.UseHttps();
    });
});



var oidcService = new OIDC_Service
{
    ClientId = OIDC_client_id.Value,
    ClientSecret = OIDC_client_secret.Value,
    RedirectUrl = "http://localhost:8080/eident/return"
};

builder.Services.AddSingleton(oidcService);


//OIDC konfigurointi
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
    .AddCookie("Cookies", options => 
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Käytä aina secure cookies
        options.Cookie.SameSite = SameSiteMode.Lax; 
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.SignInScheme = "Cookies";
        options.Authority = "https://www.ident-preprod1.nets.eu/oidc";
        options.ClientId = oidcService.ClientId;
        options.ClientSecret = oidcService.ClientSecret;
        options.ResponseType = "code";
        options.CallbackPath = "/eident/return";
        options.SaveTokens = true;

        //ID Tokenin Dekryptaus ja Validointi
        options.Events = new OpenIdConnectEvents
        {
            OnRemoteFailure = context =>
            {
                if (context.Failure is OpenIdConnectProtocolException oidcFailure)
                {
                    if (string.Equals(oidcFailure.Message, "Message contains error: 'access_denied'", StringComparison.Ordinal))
                    {
                        context.Response.Redirect("/FTN/AuthenticationFailed");
                        context.HandleResponse();
                        context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
                        return Task.FromResult(0);
                    }
                }

                return Task.CompletedTask;
            },

            ////Logitietoja debugausta varten. Authorisointi koodi saadaan onnistuneesti, tämä on varmistettu.
            OnRedirectToIdentityProvider = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OpenIdConnectEvents>>();
                logger.LogInformation("Redirecting to Identity Provider.");
                return Task.CompletedTask;
            },

            //Authorisointi koodi on saatu
            OnAuthorizationCodeReceived = async context =>
            {

                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<OpenIdConnectEvents>>();
                logger.LogInformation("Authorization code received.");

                // Luo uusi HttpClient
                var client = new HttpClient();

                // Määrittele TokenRequest
                var tokenRequest = new AuthorizationCodeTokenRequest
                {
                    Address = "https://www.ident-preprod1.nets.eu/oidc/token", // Token Endpoint
                    ClientId = options.ClientId,  // Appin client ID
                    ClientSecret = options.ClientSecret,  // Appin client secret
                    Code = context.ProtocolMessage.Code, // Autorisointikoodi
                    RedirectUri = "https://localhost:8080" + options.CallbackPath  // Uudelleenohjaus URI
                };

                // Vaihda authorization code tokeniksi
                var tokenResponse = await client.RequestAuthorizationCodeTokenAsync(tokenRequest);

                //Ilmoitetaan OIDC:lle, että token äsitellään manuaalisesti. Ei anneta varaa tehdä sitä toista kertaa automaattisesti.
                context.HandleCodeRedemption(tokenResponse.AccessToken, tokenResponse.IdentityToken);

                if (tokenResponse.IsError)
                {
                    logger.LogError($"Error while requesting token: {tokenResponse.Error}");
                    context.Fail("Failed to exchange code for token");
                    return;
                }

                var identityToken = tokenResponse.IdentityToken;

                try
                {
                    // Dekryptaa ja validoi token
                    var (claimsInfo, decryptedToken) = OIDC_service.DecryptToken(identityToken, identityTokenCertificate);

                    // Muunna claimsInfo-merkkijonot Claim-olioiksi LINQ:in avulla
                    var claims = claimsInfo.Select(claimInfo =>
                    {
                        var parts = claimInfo.Split(':');
                        return new Claim(parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : string.Empty);
                    }).ToList();

                    // Luo ClaimsIdentity käyttäen saatuja claims-tietoja
                    var claimsIdentity = new ClaimsIdentity(claims, "oidc");

                    // Luo ClaimsPrincipal käyttäen luotua ClaimsIdentity-oliota
                    context.Principal = new ClaimsPrincipal(claimsIdentity);

                    //Käytä IUserClaimsServiceä henkilötietojen siirtoon API-kontrollerin endpointiin
                    var userClaimsService = context.HttpContext.RequestServices.GetRequiredService<IUserClaimsService>();
                    userClaimsService.CurrentUser = context.Principal;

                    // TIETOJEN SIIRTO URLIN KAUTTA KONTROLLERILLE
                    var claimsData = new
                    {
                        GivenName = claimsIdentity?.FindFirst("given_name")?.Value,
                        FamilyName = claimsIdentity?.FindFirst("family_name")?.Value,
                        Birthdate = claimsIdentity?.FindFirst("birthdate")?.Value
                    };

                    string jsonClaimsData = JsonConvert.SerializeObject(claimsData);
                    string encodedJsonClaimsData = WebUtility.UrlEncode(jsonClaimsData);
                    //string redirectUriToCustomData = "/eident/CustomData?claims=" + encodedJsonClaimsData;
                    string redirectUriToCustomData = "/eident/CustomData";


                    context.Success();
                    context.HandleResponse();
                    //context.Response.Redirect("Home/Index");
                    context.Response.Redirect(redirectUriToCustomData);

                }
                catch (Exception ex)
                {
                    context.Fail("Token decryption/validation failed: " + ex.Message);
                }

                return;
            },

            OnTokenValidated = async context =>
            {

            },
            OnAuthenticationFailed = context =>
            {
                context.Response.Redirect("/FTN/AuthenticationFailed");
                context.HandleResponse();

                return Task.CompletedTask;
            }

        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
        };

        //Scope
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");


        //State
        options.ProtocolValidator.RequireState = true;
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            if (string.IsNullOrEmpty(context.ProtocolMessage.State))
            {
                context.ProtocolMessage.State = Guid.NewGuid().ToString();
            }
            return Task.CompletedTask;
        };
    });

//Cookies
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.OnAppendCookie = cookieContext =>
        CheckSameSite(cookieContext.CookieOptions);
    options.OnDeleteCookie = cookieContext =>
        CheckSameSite(cookieContext.CookieOptions);
});



//Edelleenlähetettyjen otsakkeiden käsittely.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Muut mahdolliset asetukset...
});


builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseExceptionHandler("/FTN/AuthenticationFailed");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.UseCookiePolicy();

app.UseForwardedHeaders();



//Redirect-sivu debugausta varten
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=FTN}/{action=Index}/{id?}");
});


app.Run();


void CheckSameSite(CookieOptions options)
{
    if (options.SameSite == SameSiteMode.None)
    {
        options.SameSite = SameSiteMode.Unspecified;
    }
}

