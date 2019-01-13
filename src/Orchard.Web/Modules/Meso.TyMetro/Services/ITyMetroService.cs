using Meso.TyMetro.ViewModels;
using Orchard;
using System.Collections.Generic;


namespace Meso.TyMetro.Services {
    public interface ITyMetroService : IDependency {
        IEnumerable<GoTimeDataModel> GetCurrentGoTime(GoTimeDataModel inModel);
    }
}
