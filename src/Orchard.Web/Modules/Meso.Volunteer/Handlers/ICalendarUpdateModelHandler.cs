using Orchard;
using Orchard.ContentManagement;

namespace Meso.Volunteer.Handlers {
    public interface ICalendarUpdateModelHandler : IUpdateModel, IDependency
    {
        ICalendarUpdateModelHandler SetData(object _root);
    }
}