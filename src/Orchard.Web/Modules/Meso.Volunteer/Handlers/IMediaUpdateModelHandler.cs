using Orchard;
using Orchard.ContentManagement;

namespace Meso.Volunteer.Handlers {
    public interface IMediaUpdateModelHandler : IUpdateModel, IDependency
    {
        IMediaUpdateModelHandler SetData(object _root);
    }
}