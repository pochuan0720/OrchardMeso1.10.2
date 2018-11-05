using Newtonsoft.Json;
using Orchard.Data.Conventions;

namespace Orchard.ContentManagement.Records {

    public abstract class ContentPartRecord {
        [JsonRequired]
        public virtual int Id { get; set; }
        [CascadeAllDeleteOrphan]
        public virtual ContentItemRecord ContentItemRecord { get; set; }
    }
}
