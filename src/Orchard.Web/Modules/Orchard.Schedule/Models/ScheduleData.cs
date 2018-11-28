using Orchard.Schedule.Settings;
using Orchard;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
using Orchard.Core.Contents;
using Orchard.Core.Title.Models;
using Orchard.Tags.Models;
using System.Linq;
using System.Web.Http.Routing;
using Orchard.Core.Common.Models;

namespace Orchard.Schedule.Models {
    public class ScheduleData {
        private readonly ISlugService _slugService;

        public ScheduleData() { }

        public ScheduleData(ContentItem content, UrlHelper url, ISlugService slugService, IOrchardServices orchard) {
            _slugService = slugService;

            var schedulePart = content.As<SchedulePart>();
            var titlePart = content.As<TitlePart>();
            var bodyPart = content.As<BodyPart>();
            var tagsPart = content.As<TagsPart>();

            var scheduleSettings = schedulePart.TypePartDefinition.Settings.GetModel<ScheduleSettings>();
            var metadata = content.ContentManager.GetItemMetadata(content);

            Id = content.Id;
            Title = (titlePart != null) ? titlePart.Title : "Untitled";
            Body = (bodyPart != null) ? bodyPart.Text : "Untitled";
            DisplayUrl = url.Route("", metadata.DisplayRouteValues);

            Tags = tagsPart != null ? tagsPart.CurrentTags.ToArray() : null;

            CanDelete = orchard.Authorizer.Authorize(Core.Contents.Permissions.DeleteContent, content);

            BackgroundColor = string.Format("#{0:X6}", scheduleSettings.EventBackgroundColor);
            BorderColor = string.Format("#{0:X6}", scheduleSettings.EventBorderColor);
            TextColor = string.Format("#{0:X6}", scheduleSettings.EventForegroundColor);

            AllDay = schedulePart.AllDay;
        }

        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string DisplayUrl { get; set; }

        public string[] Tags { get; set; }
        public string[] Classes {
            get {
                return (Tags != null) ? Tags.Select(t => string.Format("tag-{0}", _slugService.Slugify(t))).ToArray() : null;
            }
        }
        public bool CanDelete { get; set; }

        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public string TextColor { get; set; }

        public bool AllDay { get; set; }
    }
}