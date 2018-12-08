using Orchard.ContentManagement;

namespace Orchard.Schedule.Handlers {
    public interface IUpdateModelHandler : IUpdateModel, IDependency
    {
        IUpdateModelHandler SetData(object _root);
    }
}