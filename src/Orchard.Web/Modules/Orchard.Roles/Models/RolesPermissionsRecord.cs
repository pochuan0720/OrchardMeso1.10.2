using Newtonsoft.Json;

namespace Orchard.Roles.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class RolesPermissionsRecord {
        [JsonProperty]
        public virtual int Id { get; set; }
        public virtual RoleRecord Role { get; set; }
        [JsonProperty]
        public virtual PermissionRecord Permission { get; set; }
    }
}