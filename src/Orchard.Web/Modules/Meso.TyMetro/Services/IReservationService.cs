using Meso.TyMetro.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.ViewModels;
using Orchard.Security;
using System;


namespace Meso.TyMetro.Services {
    public interface IReservationService : IDependency {
        JObject GetContent(ContentItem item, Action<JObject, string, int> fillContent = null, AccessibilityReservationApiViewModel inFilterModel = null);

        ResultViewModel CheckStaffCondition(out StationViewModel station, IUser user = null, string stationId = null);
        bool CheckStaffPermissionForReservation(JObject jReservation, StationViewModel station);
        bool CheckStaffPermissionForStation(StationViewModel station1, StationViewModel station2);
    }
}
