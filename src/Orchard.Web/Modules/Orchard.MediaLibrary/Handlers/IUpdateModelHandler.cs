using Orchard.ContentManagement;

namespace Orchard.MediaLibrary.Handlers {
    public interface IUpdateModelHandler : IUpdateModel, IDependency
    {
        IUpdateModelHandler SetData(object _root);
    }
}