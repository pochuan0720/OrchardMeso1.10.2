using Orchard.Schedule.Services;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Layout;
using Orchard.Projections.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Schedule.Models;
using Orchard;
using Orchard.ContentManagement;

namespace Orchard.Schedule.Providers.Layouts
{
    using Occurrence = Dictionary<string, object>;

    [OrchardFeature("Orchard.CalendarLayout")]
    public class UpcomingLayout: ILayoutProvider
    {
        private readonly IScheduleService _scheduleService;
        private readonly IOrchardServices _orchard;
        public Localizer T { get; set; }
        protected dynamic Shape { get; set; }
        
        public UpcomingLayout(IShapeFactory shapeFactory, IScheduleService scheduleService, IOrchardServices orchard)
        {
            _scheduleService = scheduleService;
            _orchard = orchard;
            Shape = shapeFactory;
            T = NullLocalizer.Instance;            
        }

        public void Describe(DescribeLayoutContext describe)
        {
            describe.For("Html", T("Html"), T("Html Layouts"))
                .Element("UpcomingEvents", T("Upcoming Events"), T("Renders content items with schedule part as list"),
                DisplayLayout,
                RenderLayout,
                "UpcomingEventsLayout");
        }

        public LocalizedString DisplayLayout(LayoutContext context)
        {
            return T("Upcoming Events");
        }

        public dynamic RenderLayout(LayoutContext context, IEnumerable<LayoutComponentResult> layoutComponentResults)
        {
            var queryId = context.LayoutRecord.QueryPartRecord.Id;

            //var startDate = DateTime.UtcNow;
            var startDate = DateTime.Now;
            //const int count = 5;
            int count = Convert.ToInt32(context.State.EventCount);
            string displayMode = Convert.ToString(context.State.EventDisplayMode);

            var contentItems = layoutComponentResults.Select(lcr => lcr.ContentItem).ToList();

            if (!contentItems.Any()) return Shape.Upcoming();

            var occurrences = _scheduleService.GetOccurrencesFromDate(contentItems.Select(c => c.As<SchedulePart>()), startDate, count);

            return Shape.Upcoming(Id: queryId, Items: occurrences.Select(i => Shape.ScheduleOccurrence(
                Start: i.Start, 
                End: i.End,
                AllDay: i.AllDay,
                ContentItem: _orchard.ContentManager.BuildDisplay(i.Source, displayMode))));
        }
    }
}