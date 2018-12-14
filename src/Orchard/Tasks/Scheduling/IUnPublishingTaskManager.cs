using System;
using Orchard.ContentManagement;

namespace Orchard.Tasks.Scheduling {
    public interface IUnPublishingTaskManager : IDependency {
        IScheduledTask GetUnPublishTask(ContentItem item);
        void UnPublish(ContentItem item, DateTime scheduledUtc);
        void DeleteTasks(ContentItem item);
    }
}