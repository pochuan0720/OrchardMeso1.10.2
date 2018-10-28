using System.Collections.Generic;
using Newtonsoft.Json;
using Orchard.Data.Conventions;

namespace Orchard.Roles.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class RoleRecord {
        public RoleRecord() {
            RolesPermissions = new List<RolesPermissionsRecord>();
        }
        [JsonProperty]
        public virtual int Id { get; set; }
        [JsonProperty]
        public virtual string Name { get; set; }

        [JsonProperty]
        [CascadeAllDeleteOrphan]
        public virtual IList<RolesPermissionsRecord> RolesPermissions { get; set; }
    }
}