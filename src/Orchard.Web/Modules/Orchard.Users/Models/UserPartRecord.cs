using System;
using System.Web.Security;
using Newtonsoft.Json;
using Orchard.ContentManagement.Records;

namespace Orchard.Users.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class UserPartRecord : ContentPartRecord {
        [JsonProperty]
        public virtual string UserName { get; set; }
        [JsonProperty]
        public virtual string Email { get; set; }
        public virtual string NormalizedUserName { get; set; }

        public virtual string Password { get; set; }
        public virtual MembershipPasswordFormat PasswordFormat { get; set; }
        public virtual string HashAlgorithm { get; set; }
        public virtual string PasswordSalt { get; set; }

        public virtual UserStatus RegistrationStatus { get; set; }
        public virtual UserStatus EmailStatus { get; set; }
        public virtual string EmailChallengeToken { get; set; }
        [JsonProperty]
        public virtual DateTime? CreatedUtc { get; set; }
        [JsonProperty]
        public virtual DateTime? LastLoginUtc { get; set; }
        public virtual DateTime? LastLogoutUtc { get; set; }
    }
}