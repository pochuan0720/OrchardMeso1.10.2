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

namespace Meso.Common.Controllers {

    [Authorize]
    public class ContentApiController : ApiController {
        private readonly IOrchardServices _orchardServices;
        private readonly IUpdateModelHandler _updateModelHandler;
        private readonly IProjectionManager _projectionManager;

        public ContentApiController(
            IOrchardServices orchardServices,
            IUpdateModelHandler updateModelHandler,
             IProjectionManager projectionManager) {
            _orchardServices = orchardServices;
            _updateModelHandler = updateModelHandler;
            _projectionManager = projectionManager;
        }

        [HttpGet]
        public IHttpActionResult Get(string Name) {

            IEnumerable<ContentItem>  contentItems = _projectionManager.GetContentItems(new QueryModel { Name = Name });

            return Ok(new ResultViewModel { Content = contentItems.Select(x => GetContent(x)), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private object GetContent(ContentItem item)
        {
            JObject obj = new JObject();
            obj.Add(new JProperty("Title", item.As<TitlePart>().Title));
            var model = _orchardServices.ContentManager.BuildEditor(item);
            return UpdateModelHandler.GetData(obj, model);
        }
    }
}
