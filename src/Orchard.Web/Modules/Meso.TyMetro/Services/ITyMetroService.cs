using Meso.TyMetro.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using System.Collections.Generic;


namespace Meso.TyMetro.Services {
    public interface ITyMetroService : IDependency {
        IEnumerable<GoTimeViewModel> GetGoTime(GoTimeViewModel inModel);
        IEnumerable<GoTimeViewModel> GetGoTime(JObject jReservation, string DepOrArr = "dep");
        GoTimeViewModel GetArrGoTime(JObject jReservation, string depGoTime, string carNumber);

        //
        IEnumerable<StationViewModel> GetStation(StationViewModel lang);
    }
}
