using System;
using Newtonsoft.Json;
using Orchard.ContentManagement.Records;
using Orchard.Data.Conventions;

namespace Orchard.Comments.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class CommentPartRecord : ContentPartRecord {

        public virtual string Author { get; set; }
        public virtual string SiteName { get; set; }
        [JsonProperty]
        public virtual string UserName { get; set; }
        public virtual string Email { get; set; }
        [JsonProperty]
        public virtual CommentStatus Status { get; set; }
        [JsonProperty]
        public virtual DateTime? CommentDateUtc { get; set; }
        [JsonProperty]
        [StringLengthMax]
        public virtual string CommentText { get; set; }

        // this is a duplicate of CommentsPartRecord FK, but
        // it's kept for compatibility and it can also prevent
        // a lazy load if only the Id value is needed 
        [JsonProperty]
        public virtual int CommentedOn { get; set; }
        public virtual int CommentedOnContainer { get; set; }
        [JsonProperty]
        public virtual int? RepliedOn { get; set; }
        [JsonProperty]
        public virtual decimal Position { get; set; }

        // inverse relationship of CommentsPartRecord.CommentPartRecords
        public virtual CommentsPartRecord CommentsPartRecord { get; set; }
    }
}
