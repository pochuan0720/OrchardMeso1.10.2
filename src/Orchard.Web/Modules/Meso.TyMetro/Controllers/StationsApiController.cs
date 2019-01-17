using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Web;
using Orchard;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.ViewModels;
using Orchard.Projections.Services;
using Orchard.Projections.Models;
using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Meso.TyMetro.Services;
using Meso.TyMetro.ViewModels;

namespace Meso.Common.Controllers
{

    [Authorize]
    public class StationsApiController : ApiController
    {
        /*private readonly IOrchardServices _orchardServices;
        private readonly IUpdateModelHandler _updateModelHandler;
        private readonly IProjectionManager _projectionManager;
        private readonly IStationService _stationService;

        public StationsApiController(
            IOrchardServices orchardServices,
            IUpdateModelHandler updateModelHandler,
            IProjectionManager projectionManager,
            IStationService stationService)
        {
            _orchardServices = orchardServices;
            _updateModelHandler = updateModelHandler;
            _projectionManager = projectionManager;
            _stationService = stationService;
        }*/

        private readonly ITyMetroService _tyMetroService;

        public StationsApiController(ITyMetroService tyMetroService)
        {
            _tyMetroService = tyMetroService;
        }


        [HttpGet]
        public IHttpActionResult Get([FromUri]StationViewModel inModel)
        {
            /*IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = "Stations" });
            return Ok(new ResultViewModel { Content = contentItems.Select(x => _stationService.GetContent(x)), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });*/
            return Ok(new ResultViewModel { Content = _tyMetroService.GetStation(inModel), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        /*private object GetContent(ContentItem item, Action<JObject, string, int> fillContent = null)
        {
            JObject obj = new JObject();
            obj.Add(new JProperty("Id", item.Id));
            obj.Add(new JProperty("Title", item.As<TitlePart>().Title));
            var model = _orchardServices.ContentManager.BuildEditor(item);
            return UpdateModelHandler.GetData(obj, model, fillContent);
        }

        private void FillContent(JObject obj, string prefix, int id)
        {
            obj.Add(new JProperty(prefix, GetContent(_orchardServices.ContentManager.Get(id), FillContent)));
        }*/

        /*[HttpPut]
        public IHttpActionResult Put(string Name, JObject inModel)
        {
            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }*/
    }
}
