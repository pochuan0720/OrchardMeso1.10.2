using Orchard.ContentManagement;

namespace Orchard.Blogs.Handlers {
    public interface IUpdateModelHandler : IUpdateModel, IDependency
    {
        IUpdateModelHandler SetData(object _root);
    }
}