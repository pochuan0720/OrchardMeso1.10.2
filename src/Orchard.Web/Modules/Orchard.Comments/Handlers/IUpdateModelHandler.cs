using Orchard.ContentManagement;

namespace Orchard.Comments.Handlers {
    public interface IUpdateModelHandler : IUpdateModel, IDependency
    {
        IUpdateModelHandler SetData(object _root);
    }
}