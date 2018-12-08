using System;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Common.Models;
using Orchard.Security;
using Orchard.Core.Title.Models;
using Newtonsoft.Json;

namespace Orchard.Blogs.Models {
    [JsonObject(MemberSerialization.OptIn)]
    public class BlogPostPart : ContentPart {
        [JsonProperty]
        public string Title {
            get { return this.As<TitlePart>().Title; }
            set { this.As<TitlePart>().Title = value; }
        }
        [JsonProperty]
        public string Text {
            get { return this.As<BodyPart>().Text; }
            set { this.As<BodyPart>().Text = value; }
        }

        public BlogPart BlogPart {
            get { return this.As<ICommonPart>().Container.As<BlogPart>(); }
            set { this.As<ICommonPart>().Container = value; }
        }
        [JsonProperty]
        public IUser Creator {
            get { return this.As<ICommonPart>().Owner; }
            set { this.As<ICommonPart>().Owner = value; }
        }
        [JsonProperty]
        public bool IsPublished {
            get { return ContentItem.VersionRecord != null && ContentItem.VersionRecord.Published; }
        }

        public bool HasDraft {
            get {
                return (
                           (ContentItem.VersionRecord != null) && (
                               (ContentItem.VersionRecord.Published == false) ||
                               (ContentItem.VersionRecord.Published && ContentItem.VersionRecord.Latest == false)));
            }
        }

        public bool HasPublished {
            get {
                return IsPublished || ContentItem.ContentManager.Get(Id, VersionOptions.Published) != null;
            }
        }

        [JsonProperty]
        public DateTime? CreatedUtc
        {
            get { return this.As<ICommonPart>().CreatedUtc; }
        }

        [JsonProperty]
        public DateTime? PublishedUtc {
            get { return this.As<ICommonPart>().PublishedUtc; }
        }

        /*[JsonProperty]
        public DateTime? PublishedLocal
        {
            get { return PublishedUtc == null ? PublishedUtc : ((DateTime)PublishedUtc).ToLocalTime(); }
        }*/

        [JsonProperty]
        public object Data { get; set; }
    }
}