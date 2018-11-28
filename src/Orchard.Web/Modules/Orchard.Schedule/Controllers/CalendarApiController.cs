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
using Newtonsoft.Json.Linq;
using Orchard.Projections.Models;
using Orchard.Core.Common.Handlers;

namespace Orchard.Schedule.Controllers
{
    [Authorize]
    public class CalendarApiController : ApiController
    {
        private readonly IScheduleService _scheduleService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IOrchardServices _orchardServices;
        private readonly ISlugService _slugService;
        private readonly IUpdateModelHandler _updateModelHandler;
        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);

        public CalendarApiController(
            IScheduleService scheduleService,
            IScheduleLayoutService scheduleLayoutService,
            IOrchardServices orchardServices,
            ISlugService slugService,
            IShapeFactory shapeFactory,
            IUpdateModelHandler updateModelHandler
            )
        {
            _scheduleService = scheduleService;
            _scheduleLayoutService = scheduleLayoutService;
            _orchardServices = orchardServices;
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
        public IHttpActionResult query(SchedulesIndexApiViewMode inModel)
        {
            if (inModel == null || inModel.Query == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            IEnumerable<ContentItem> contentItems = _scheduleLayoutService.GetProjectionContentItems(inModel.Query);
         
            if (contentItems == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            /*var scheduleOccurrences = contentItems
                .Select(c => c.As<SchedulePart>())
                .Where(s => _scheduleLayoutService.DateInRange(s, startDate, endDate))
                .SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c, startDate, endDate))
                .OrderBy(o => o.Start);

            var Schedules = contentItems
                .Select(c => c.As<SchedulePart>())
                .Where(s => _scheduleLayoutService.DateInRange(s, startDate, endDate));*/

            Dictionary<IContent, ScheduleData> ScheduleMap =
                contentItems
                .Select(c => new { k = (IContent)c, v = new ScheduleData(c, Url, _slugService, _orchardServices) })
                .ToDictionary(c => c.k, c => c.v);

            var scheduleOccurrences = contentItems
                .Select(c => c.As<SchedulePart>())
                .Where(s => _scheduleLayoutService.DateInRange(s, inModel.StartDate, inModel.EndDate))
                .SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c, inModel.StartDate, inModel.EndDate))
                .OrderBy(o => o.Start);

            var occurrences = scheduleOccurrences.Select(o => _scheduleLayoutService.GetOccurrenceViewModel(o, ScheduleMap[o.Source])).ToList();



            return Ok(new ResultViewModel { Content = occurrences, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(SchedulesIndexApiViewMode inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var contentItem = _orchardServices.ContentManager.Get(inModel.Id, VersionOptions.DraftRequired);

            if (contentItem == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            SchedulePart schedule = contentItem.As<SchedulePart>();
            ScheduleApiViewMode outModel = _scheduleLayoutService.GetOccurrenceViewModel(new ScheduleOccurrence(schedule, schedule.StartDate), new ScheduleData(contentItem, Url, _slugService, _orchardServices));

            return Ok(new ResultViewModel { Content = outModel, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(ScheduleEditApiViewMode inModel)
        {
            if (inModel == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, T("Not authorized to manage schedules")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            var schedule = _orchardServices.ContentManager.New<SchedulePart>(inModel.ContentType);

            if (schedule != null)
            {
                _orchardServices.ContentManager.Create(schedule, VersionOptions.Draft);
                var editorShape = _orchardServices.ContentManager.UpdateEditor(schedule, _updateModelHandler.SetData(inModel));
                _orchardServices.ContentManager.Publish(schedule.ContentItem);
                return Ok(new ResultViewModel { Content = new { Id = schedule.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
        }

        [HttpPost]
        public IHttpActionResult update(ScheduleEditApiViewMode inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var schedule = _orchardServices.ContentManager.Get(inModel.Id, VersionOptions.DraftRequired);

            if (schedule == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, schedule, T("Couldn't edit schedule")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit schedule" });

            _orchardServices.ContentManager.UpdateEditor(schedule, _updateModelHandler.SetData(inModel));

            _orchardServices.ContentManager.Publish(schedule);
            _orchardServices.Notifier.Information(T("schedule information updated"));

            return Ok(new ResultViewModel { Content = new { Id = schedule.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(ScheduleEditApiViewMode inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, T("Couldn't delete schedule")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete schedule" });

            var schedule = _orchardServices.ContentManager.Get(inModel.Id);
            if (schedule == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            Delete(inModel.Id);

            _orchardServices.Notifier.Information(T("schedule deleted"));

            return Ok(new ResultViewModel { Content = new { Id = schedule.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        // DELETE api/<controller>/5
        private void Delete(int id)
        {
            Delete(id, "all", DateTime.MinValue);
        }

        private void Delete(int id, string mode, DateTime date)
        {
            switch (mode)
            {
                case "all": _scheduleService.RemoveScheduleItem(id); break;
                case "single": _scheduleService.RemoveSingleDateForScheduleItem(id, date); break;
                case "following": _scheduleService.RemoveFollowingDatesForScheduleItem(id, date); break;
            }
        }
    }
}
