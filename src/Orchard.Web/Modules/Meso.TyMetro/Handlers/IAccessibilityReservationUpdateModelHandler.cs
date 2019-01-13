using Orchard;
using Orchard.ContentManagement;

namespace Meso.TyMetro.Handlers {
    public interface IAccessibilityReservationUpdateModelHandler : IUpdateModel, IDependency
    {
        IAccessibilityReservationUpdateModelHandler SetData(object _root);
    }
}