using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.ContentManagement;

namespace Meso.Volunteer.Handlers {
    public interface INewsUpdateModelHandler : IUpdateModel, IDependency
    {
        INewsUpdateModelHandler SetData(object _root);
    }
}