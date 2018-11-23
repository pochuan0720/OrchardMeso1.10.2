using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Forms.Services;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Schedule.Providers.Layouts
{
    [OrchardFeature("Orchard.CalendarLayout")]
    public class CalendarLayoutForms : IFormProvider
    {
        protected dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public CalendarLayoutForms(
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
                        Id: "CalendarLayout",                        
                        _DisplayModeOptions: Shape.FieldSet(
                            Title: T("Default Calendar Display"),
                            _ModeMonth: Shape.Radio(
                                Id: "displayMonth", Name: "DisplayMode",
                                Title: T("Month"), Value: "month",
                                Checked: true,
                                Description: T("Display a monthly calendar by default")
                            ),
                            _ModeWeek: Shape.Radio(
                                Id: "displayWeek", Name: "DisplayMode",
                                Title: T("Week"), Value: "agendaWeek",
                                Description: T("Display a weekly calendar by default")
                            ),
                            _ModeDay: Shape.Radio(
                                Id: "displayDay", Name: "DisplayMode",
                                Title: T("Day"), Value: "agendaDay",
                                Description: T("Display a daily calendar by default")
                            )
                        ),
                        _CalendarGeneralDisplayOptions: Shape.FieldSet(
                            Title: T("General Display Options"),
                            _DisplayWeekends: Shape.Checkbox(
                                Id: "hideWeekends", Name: "HideWeekends",
                                Title: T("Hide Weekends"), Value: "on",
                                Checked: false
                            )
                        //,
                        //_UseTagsAsClasses: Shape.Checkbox(
                        //    Id: "tagsAsClasses", Name: "TagsAsClasses",
                        //    Title: T("Use tags as classes"), Value: "on",
                        //    Checked: false
                        //)
                        ),
                        _CalendarTagColors: Shape.FieldSet(
                            Title: T("Tag Colors"),
                            _Colors: Shape.TagColorPicker(
                                Id: "tagColors", Name: "TagColors",
                                Title: T("Tag Colors"), Value: "off|"
                            )
                        )
                    );
                    return f;
                };

            context.Form("CalendarLayout", form);

        }
    }
}