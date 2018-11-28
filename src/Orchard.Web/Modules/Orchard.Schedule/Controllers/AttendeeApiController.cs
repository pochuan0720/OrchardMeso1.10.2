using Orchard.Schedule.Models;
using Orchard.Schedule.Services;
using Orchard.Schedule.ViewModels;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
using Orchard.Core.Common.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using Orchard.DisplayManagement;
using Orchard.Logging;
using Orchard.Localization;
using Orchard.Schedule.Handlers;
using Orchard.UI.Notify;
using Orchard.Core.Containers.Services;
using Orchard.Core.Common.Models;
using Orchard.Security;
using Newtonsoft.Json.Linq;
using Orchard.Core.Common.Handlers;

namespace Orchard.Schedule.Controllers
{
    [Authorize]
    public class AttendeeApiController : ApiController
    {
        private readonly IScheduleService _scheduleService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IOrchardServices _orchardServices;
        private readonly IContainerService _containerService;
        private readonly ISlugService _slugService;
        private readonly IUpdateModelHandler _updateModelHandler;
        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);


        public AttendeeApiController(
            IScheduleService scheduleService,
            IScheduleLayoutService scheduleLayoutService,
            IOrchardServices orchardServices,
            IContainerService containerService,
            ISlugService slugService,
            IShapeFactory shapeFactory,
            IUpdateModelHandler updateModelHandler
            )
        {
            _scheduleService = scheduleService;
            _scheduleLayoutService = scheduleLayoutService;
            _orchardServices = orchardServices;
            _containerService = containerService;
            _slugService = slugService;
            _updateModelHandler = updateModelHandler;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }
        dynamic Shape { get; set; }

        [HttpPost]
        public IHttpActionResult query(JObject inModel)
        {
            if (inModel == null || inModel["ContainerId"] == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });


            IEnumerable<ContentItem> contentItems = _containerService.GetContentItems((int)inModel["ContainerId"]);


            if (contentItems == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            IList<object> list = new List<object>();
            foreach(ContentItem item in contentItems)
            {
                CommonPart common = item.As<CommonPart>();
                IUser user = common.Owner;
                var userModel = _orchardServices.ContentManager.BuildEditor(user);
                var attendeeModel = _orchardServices.ContentManager.BuildEditor(item);
                list.Add(new { Id = item.Id , CreatedUtc = common.CreatedUtc, User = new { UserName = user.UserName, Email = user.Email, Data = Core.Common.Handlers.UpdateModelHandler.GetData(userModel) }, Data = Core.Common.Handlers.UpdateModelHandler.GetData(attendeeModel) });
            }

            return Ok(new ResultViewModel { Content = list, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {

            if (inModel["Id"] == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"], VersionOptions.DraftRequired);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var contentModel = _orchardServices.ContentManager.BuildEditor(content);

            CommonPart common = content.As<CommonPart>();
            IUser user = common.Owner;
            var userModel = _orchardServices.ContentManager.BuildEditor(user);
            var attendeeModel = _orchardServices.ContentManager.BuildEditor(content);

            return Ok(new ResultViewModel { Content = new { Id = content.Id, CreatedUtc = common.CreatedUtc, User = new { UserName = user.UserName, Email = user.Email, Data = Core.Common.Handlers.UpdateModelHandler.GetData(userModel) }, Data = Core.Common.Handlers.UpdateModelHandler.GetData(attendeeModel) }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(JObject inModel)
        {
            if (inModel == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, T("Not authorized to manage content")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var content = _orchardServices.ContentManager.New<ContentPart>(inModel["ContentType"].ToString());

            if (content != null)
            {
                _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
                var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));
                _orchardServices.ContentManager.Publish(content.ContentItem);
                return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
        }

        [HttpPost]
        public IHttpActionResult update(JObject inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"], VersionOptions.DraftRequired);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, content, T("Couldn't edit content")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit content" });

            _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));

            _orchardServices.ContentManager.Publish(content);
            _orchardServices.Notifier.Information(T("schedule information updated"));

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(JObject inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, T("Couldn't delete schedule")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete schedule" });

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"]);
            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            _orchardServices.ContentManager.Remove(content);

            _orchardServices.Notifier.Information(T("content deleted"));

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }
    }
}
