using System;
using Orchard.ContentManagement.Utilities;

namespace Orchard.ContentManagement.Aspects {
    public interface IUnPublishingControlAspect {
        LazyField<DateTime?> ScheduledUnPublishUtc { get; }
    }
}