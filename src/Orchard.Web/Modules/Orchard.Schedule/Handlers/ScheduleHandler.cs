using Orchard.Schedule.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;

namespace Orchard.Schedule.Handlers
{
    public class ScheduleHandler : ContentHandler
    {

        private readonly IWorkContextAccessor _accessor;

        public ScheduleHandler(IRepository<SchedulePartRecord> repository, IWorkContextAccessor accessor)
        {
            Filters.Add(StorageFilter.For(repository));

            _accessor = accessor;

            OnInitializing<SchedulePart>(InitializeSchedulePart);
        }

        private void InitializeSchedulePart(InitializingContentContext context, SchedulePart part)
        {
            var settings = _accessor.GetContext().CurrentSite.As<ScheduleSettingsPart>();
            part.StartTime = settings.DefaultStartTime;
            part.Duration = settings.DefaultDuration;
            part.TimeZone = settings.TimeZone;
        }
    }
}