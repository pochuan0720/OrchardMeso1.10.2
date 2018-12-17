using Orchard;
using Orchard.ContentManagement;

namespace Meso.Volunteer.Handlers {
    public interface ICommentUpdateModelHandler : IUpdateModel, IDependency
    {
        ICommentUpdateModelHandler SetData(object _root);
    }
}