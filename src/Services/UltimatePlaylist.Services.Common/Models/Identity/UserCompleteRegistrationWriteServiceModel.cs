#region usings

using System;

#endregion

namespace UltimatePlaylist.Services.Common.Models.Identity
{
    public class UserCompleteRegistrationWriteServiceModel
    {
        public string Token { get; set; }

        public string ExternalToken { get; set; }

        public string Username { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }        

        public string PhoneNumber { get; set; }

        public DateTime BirthDate { get; set; }

        public Guid GenderExternalId { get; set; }

        public string ZipCode { get; set; }

        public bool IsTermsAndConditionsRead { get; set; }

        public bool IsAgeAgreementRead { get; set; }

        public virtual string ConcurrencyStamp { get; set; }

        public string Provider { get; set; }
    }
}