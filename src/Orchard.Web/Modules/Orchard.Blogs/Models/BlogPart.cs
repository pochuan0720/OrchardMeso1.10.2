using Newtonsoft.Json;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;

namespace Orchard.Blogs.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class BlogPart : ContentPart {
        [JsonProperty("Title")]
        public string Name {
            get { return this.As<ITitleAspect>().Title; }
        }
        [JsonProperty]
        public string Description {
            get { return this.Retrieve(x => x.Description); }
            set { this.Store(x => x.Description, value); }
        }
        [JsonProperty]
        public int PostCount {
            get { return this.Retrieve(x => x.PostCount); }
            set { this.Store(x => x.PostCount, value); }
        }

        public string FeedProxyUrl {
            get { return this.Retrieve(x => x.FeedProxyUrl); }
            set { this.Store(x => x.FeedProxyUrl, value); }
        }

        public bool EnableCommentsFeed {
            get { return this.Retrieve(x => x.EnableCommentsFeed, false); }
            set { this.Store(x => x.EnableCommentsFeed, value); }
        }
        [JsonProperty]
        public int PostsPerPage {
            get { return this.Retrieve(x => x.PostsPerPage, 10); }
            set { this.Store(x => x.PostsPerPage, value); }
        }
        [JsonProperty]
        public int TotalPostCount { get; set; }
    }
}