#region Usings

using System;

#endregion

namespace UltimatePlaylist.MobileApi.Models.Identity
{
    public class UserRegistrationRequestModel
    {
        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string PhoneNumber { get; set; }

        public DateTime? BirthDate { get; set; }

        public Guid? GenderExternalId { get; set; }

        public string ZipCode { get; set; }

        public bool? IsTermsAndConditionsRead { get; set; }
        public bool? IsAgeAgreementRead { get; set; }

        public string Device { get; set; }
    }
}