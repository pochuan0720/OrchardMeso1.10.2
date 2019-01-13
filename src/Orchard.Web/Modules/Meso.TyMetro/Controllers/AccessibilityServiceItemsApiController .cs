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

namespace Meso.Common.Controllers
{

    [Authorize]
    public class AccessibilityServiceItemsApiController : ApiController
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IUpdateModelHandler _updateModelHandler;
        private readonly IProjectionManager _projectionManager;

        public AccessibilityServiceItemsApiController(
            IOrchardServices orchardServices,
            IUpdateModelHandler updateModelHandler,
            IProjectionManager projectionManager)
        {
            _orchardServices = orchardServices;
            _updateModelHandler = updateModelHandler;
            _projectionManager = projectionManager;
        }

        [HttpGet]
        public IHttpActionResult Get(string culture = null)
        {
            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = "AccessibilityServiceItems" });
            return Ok(new ResultViewModel { Content = contentItems.Select(x => GetContent(x, FillContent)), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private object GetContent(ContentItem item, Action<JObject, string, int> fillContent = null)
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
        }

        [HttpPut]
        public IHttpActionResult Put(string Name, JObject inModel)
        {
            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }
    }
}
