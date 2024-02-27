using Microsoft.EntityFrameworkCore;
using Perikato.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Globalization;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;


namespace Perikato.Services
{
    public class UserDataHandler
    {
        private readonly ApplicationDbContext _dbContext;
        private const string SecretKey = "salainenAvain";

        public UserDataHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        //Käyttäjätietojen olemassa olon tarkistus OIDC-prosessin jälkeen. Tarvittaessa luodaan uusi käyttäjä.
        public string CheckIfUserExist(IEnumerable<Claim>? claims)
        {
            if (claims == null || !claims.Any())
            {
                return "claimsError:NoClaims";
            }

            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                try
                {
                    var pidClaim = claims.FirstOrDefault(c => c.Type == "pid")?.Value;
                    if (string.IsNullOrWhiteSpace(pidClaim))
                    {
                        throw new Exception("PIDClaimIsMissingOrEmpty.");
                    }

                    var user = _dbContext.Users.FirstOrDefault(u => u.TupasPID == pidClaim);

                    if (user == null)
                    {
                        string birthdateClaim = claims.FirstOrDefault(c => c.Type == "birthdate")?.Value;
                        if (string.IsNullOrWhiteSpace(birthdateClaim))
                        {
                            throw new Exception("birthdateClaimIsMissingOrEmpty.");
                        }

                        DateTime birthdate;
                        bool birthdateParsed = DateTime.TryParse(birthdateClaim, out birthdate);

                        if (!birthdateParsed)
                        {
                            string[] formats = { "yyyy-MM-dd", "dd.MM.yyyy", "MM/dd/yyyy" };
                            birthdateParsed = DateTime.TryParseExact(birthdateClaim, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out birthdate);

                            if (!birthdateParsed)
                            {
                                throw new Exception("invalidBirthdateClaimFormat.");
                            }
                        }

                        user = new User
                        {
                            Id = Guid.NewGuid(),
                            GivenName = claims.FirstOrDefault(c => c.Type == "given_name")?.Value,
                            FamilyName = claims.FirstOrDefault(c => c.Type == "family_name")?.Value,
                            TupasPID = pidClaim,
                            Sub = claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                            Birthdate = birthdateParsed ? birthdate : null,
                            CreatedDate = DateTime.Now,
                            // Lisää tarvittavat kentät...
                        };

                        if (!string.IsNullOrEmpty(user.GivenName) && !string.IsNullOrEmpty(user.FamilyName))
                        {
                            user.Username = $"{user.GivenName[0]}.{user.FamilyName}";
                        }

                        _dbContext.Users.Add(user);
                    }

                    user.Bank = claims.FirstOrDefault(c => c.Type == "fi_tupas_bank")?.Value;
                    user.LoginToken = GenerateLoginToken(user.Id);
                    user.LastAuthentication = DateTime.Now;

                    _dbContext.SaveChanges();
                    transaction.Commit();

                    return user.LoginToken;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    LogError(ex);
                    return $"claimsError:{ex.Message}";
                }
            }
        }

        private void LogError(Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        //Login-tokenin generointi
        public static string GenerateLoginToken(Guid userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
            new Claim("Id", userId.ToString()) // Muuttaa Guid-tyyppisen userId:n merkkijonoksi
        }),
                Expires = DateTime.UtcNow.AddDays(30), // Tokenin voimassaoloaika
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


    }

    public class LoginToken
    {
        public string Token { get; set; }
    }
}
