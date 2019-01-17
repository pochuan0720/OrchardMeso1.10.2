using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Core.Title.Models;
using System;

namespace Meso.TyMetro.Services {
    public class StationService : IStationService
    {
        private readonly IOrchardServices _orchardServices;
        private readonly ITyMetroService _tyMetroService;

        public StationService(IOrchardServices orchardServices,
            ITyMetroService tyMetroService)
        {
            _orchardServices = orchardServices;
            _tyMetroService = tyMetroService;
        }

        public JObject GetContent(ContentItem item, Action<JObject, string, int> fillContent = null)
        {
            //if (fillContent == null)
            //    fillContent = FillContent;

            JObject obj = new JObject();
            obj.Add(new JProperty("Id", item.Id));
            obj.Add(new JProperty("Title", item.As<TitlePart>().Title));
            IContent container = item.As<CommonPart>().Container;
            if(container != null)
            {
                var model = _orchardServices.ContentManager.BuildEditor(container);
                return UpdateModelHandler.GetData(obj, model, fillContent);
            }

            return obj;
        }

        private void FillContent(JObject obj, string prefix, int id)
        {
            obj.Add(new JProperty(prefix, GetContent(_orchardServices.ContentManager.Get(id), FillContent)));
        }
    }
}
