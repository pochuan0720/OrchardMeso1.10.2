using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentManagement.ViewModels;
using System.Collections.Generic;

namespace F2B.Orchard.Schedule.Settings
{
    public class ScheduleSettingsEvents : ContentDefinitionEditorEventsBase
    {
        public override IEnumerable<TemplateViewModel> TypePartEditor(ContentTypePartDefinition definition)
        {
            if (definition.PartDefinition.Name != "SchedulePart")
                yield break;

            var settings = definition.Settings.GetModel<ScheduleSettings>();

            yield return DefinitionTemplate(settings);
        }

        public override IEnumerable<TemplateViewModel> TypePartEditorUpdate(ContentTypePartDefinitionBuilder builder, IUpdateModel updateModel)
        {
            if (builder.Name != "SchedulePart")
                yield break;

            var settings = new ScheduleSettings();

            if (updateModel.TryUpdateModel(settings, "ScheduleSettings", null, null))
            {
                settings.Build(builder);
            }

            yield return DefinitionTemplate(settings);
        }
    }
}