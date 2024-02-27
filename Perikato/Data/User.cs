using Perikato.Data.Carriers;

namespace Perikato.Data
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } // esim. O.Korhonen
        public string GivenName { get; set; } //Given name
        public string FamilyName { get; set; } //Familyname
        public string? Email { get; set; } //Email
        public string? Phonenumber { get; set; } //Email
        public string? DefaultVehicle { get; set; }
        public string? SSN { get; set; } //Social security number (not in use)
        public string TupasPID { get; set; } //Identification number by bank
        public string Sub { get; set; } //esim. fi_tupas
        public string Bank { get; set; } //esim. nordea
        public DateTime? Birthdate { get; set; } //Birthdate
        public DateTime CreatedDate { get; set; } //User created (date)        
        public string? LoginToken { get; set; } //Token for login process (will be returned for phoneapp)

        public DateTime? LastAuthentication { get; set; } //Login token created date

        public virtual ICollection<Routes> Routes { get; set; }
    }
}
