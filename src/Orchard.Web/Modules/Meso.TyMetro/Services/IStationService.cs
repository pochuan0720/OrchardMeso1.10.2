using Meso.TyMetro.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.ContentManagement;
using System;
using System.Collections.Generic;


namespace Meso.TyMetro.Services {
    public interface IStationService : IDependency {
        JObject GetContent(ContentItem item, Action<JObject, string, int> fillContent = null);
    }
}
