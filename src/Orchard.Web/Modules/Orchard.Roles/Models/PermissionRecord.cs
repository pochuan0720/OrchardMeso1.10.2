using Newtonsoft.Json;

namespace Orchard.Roles.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class PermissionRecord {
        [JsonProperty]
        public virtual int Id { get; set; }
        [JsonProperty]
        public virtual string Name { get; set; }
        [JsonProperty]
        public virtual string FeatureName { get; set; }
        [JsonProperty]
        public virtual string Description { get; set; }
    }
}