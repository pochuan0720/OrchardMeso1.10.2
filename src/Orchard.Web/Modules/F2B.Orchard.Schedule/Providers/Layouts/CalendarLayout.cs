using F2B.Orchard.Schedule.Services;
using Orchard.Autoroute.Services;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Layout;
using Orchard.Projections.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.Providers.Layouts
{
    [OrchardFeature("F2B.Orchard.CalendarLayout")]
    public class CalendarLayout: ILayoutProvider
    {
        public Localizer T { get; set; }
        protected dynamic Shape { get; set; }

        private readonly IScheduleService _scheduleService;
        private readonly ISlugService _slugService;

        public CalendarLayout(IShapeFactory shapeFactory, IScheduleService scheduleService, ISlugService slugService)
        {
            Shape = shapeFactory;
            T = NullLocalizer.Instance;
            _scheduleService = scheduleService;
            _slugService = slugService;
        }

        public void Describe(DescribeLayoutContext describe)
        {
            describe.For("Html", T("Html"), T("Html Layouts"))
                .Element("Calendar", T("Calendar"), T("Renders content items with schedule part as calendar"),
                DisplayLayout,
                RenderLayout,
                "CalendarLayout");
        }

        public LocalizedString DisplayLayout(LayoutContext context)
        {
            return T("Calendar");
        }

        public dynamic RenderLayout(LayoutContext context, IEnumerable<LayoutComponentResult> layoutComponentResults)
        {
            var queryId = context.LayoutRecord.QueryPartRecord.Id;

            var hideWeekends = Convert.ToString(context.State.HideWeekends) == "on";
            //var tagsAsClasses = Convert.ToString(context.State.TagsAsClasses) == "on";

            var tagColors = (string)Convert.ToString(context.State.TagColors);
            var tagColorParts = tagColors.Split('|');

            bool tagColorsEnabled = tagColorParts[0] == "on";

            dynamic tagColorTags;

            if (tagColorsEnabled && !string.IsNullOrWhiteSpace(tagColorParts[1]))
            {
                tagColorTags = tagColorParts[1]
                    .Split(':')
                    .Select(ts =>
                    {
                        var p = ts.Split(',');
                        var slug = _slugService.Slugify(p[0]);
                        return Shape.ColorSet(tag: p[0], slug: slug, bg: p[1], br: p[2], fg: p[3]);
                    }).ToList();
            }
            else
            {
                tagColorTags = null;
            }

            return Shape.Calendar(
                queryId: queryId, 
                defaultView: Convert.ToString(context.State.DisplayMode??"monthly"),
                showWeekends: hideWeekends?"false":"true",
                tagColorsEnabled: tagColorsEnabled,
                tagColors: tagColorTags
                );
        }
    }
}