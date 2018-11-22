using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Forms.Services;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace F2B.Orchard.Schedule.Providers.Layouts
{
    [OrchardFeature("F2B.Orchard.CalendarLayout")]
    public class UpcomingLayoutForms : IFormProvider
    {
        protected dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public UpcomingLayoutForms(
            IShapeFactory shapeFactory)
        {
            Shape = shapeFactory;
            T = NullLocalizer.Instance;
        }

        public void Describe(DescribeContext context)
        {
            Func<IShapeFactory, object> form =
                shape =>
                {

                    var f = Shape.Form(
                        Id: "UpcomingEventsLayout",
                        _DisplayProperties: Shape.Fieldset(
                            Title: T("Display Properties"),
                            _NumberOfEvents: Shape.TextBox(
                                Id: "eventCount", Name: "EventCount",
                                Title: T("Number of upcoming events to show "),
                                Value: 5,
                                Description: T("This will limit the number of computed upcoming events based on schedules. In the projection, you should enter '0' for the maximum items to retrieve."),
                                Classes: new[] { "small-text", "tokenized" }
                            ),
                            _EventDisplayMode: Shape.TextBox(
                                Id: "eventDisplayMode", Name: "EventDisplayMode",
                                Title: T("Display mode for individual events"),
                                Value: "Blurb",
                                Description: T("This is the display mode for each of the scheduled items that is displayed in the list of upcoming events"),
                                Classes: new[] {"small-text", "tokenized"}
                            )
                        )
                    );
                    return f;
                };

            context.Form("UpcomingEventsLayout", form);

        }
    }
}