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
using Orchard.Core.Common.Handlers;

namespace Orchard.Schedule.Controllers
{

    public class CalendarApiController : ApiController
    {
        private readonly IScheduleService _scheduleService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IOrchardServices _orchardServices;
        private readonly ISlugService _slugService;
        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);

        public CalendarApiController(
            IScheduleService scheduleService,
            IScheduleLayoutService scheduleLayoutService,
            IOrchardServices orchardServices,
            ISlugService slugService,
            IShapeFactory shapeFactory
            )
        {
            _scheduleService = scheduleService;
            _scheduleLayoutService = scheduleLayoutService;
            _orchardServices = orchardServices;
            _slugService = slugService;
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
                schedule.StartDate = inModel.StartDate;
                schedule.EndDate = inModel.EndDate;
                var editorShape = _orchardServices.ContentManager.UpdateEditor(schedule, new UpdateModelHandler(inModel));
                return Ok(new ResultViewModel { Content = schedule, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
        }
    }
}
