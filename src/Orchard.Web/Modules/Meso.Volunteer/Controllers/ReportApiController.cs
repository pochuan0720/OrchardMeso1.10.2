using Meso.Volunteer.Services;
using Meso.Volunteer.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.Projections.Models;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Schedule.Models;
using Orchard.Schedule.Services;
using Orchard.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Meso.Volunteer.Controllers
{
    public class ReportApiController : ApiController
    {
        private readonly IRoleService _roleService;
        private readonly IScheduleService _scheduleService;
        private readonly ICalendarService _calendarService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IOrchardServices _orchardServices;
        private readonly ISlugService _slugService;
        private readonly IAttendeeService _attendeeService;

        public ReportApiController(
            IRoleService roleService,
            IScheduleService scheduleService,
            ICalendarService calendarService,
            IAuthenticationService authenticationService,
            IOrchardServices orchardServices,
            ISlugService slugService,
             IAttendeeService attendeeService)
        {
            _roleService = roleService;
            _scheduleService = scheduleService;
            _calendarService = calendarService;
            _authenticationService = authenticationService;
            _orchardServices = orchardServices;
            _slugService = slugService;
            _attendeeService = attendeeService;
        }


        [HttpPost]
        public IHttpActionResult VolunteerSummary(VolunteerApiViewMode inModel)
        {
            if (inModel == null || inModel.ContentType == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            IEnumerable<ContentItem> allContentItems = null;
            IUser user = _authenticationService.GetAuthenticatedUser();
            allContentItems = _attendeeService.GetAttendees(user, null, inModel.ContentType);

            if (allContentItems == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            Dictionary<IContent, ScheduleData> ScheduleMap =
                allContentItems
                .Select(c => new { k = (IContent)c, v = new ScheduleData(c, Url, _slugService, _orchardServices) })
                .ToDictionary(c => c.k, c => c.v);

            var scheduleOccurrences = allContentItems
                .Select(c => c.As<SchedulePart>())
                .Where(s => _calendarService.DateInRange(s, inModel.StartDate, inModel.EndDate))
                .SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c, inModel.StartDate, inModel.EndDate))
                .OrderBy(o => o.Start);

            var occurrences = scheduleOccurrences.Select(o => _calendarService.GetOccurrenceViewModel(o, ScheduleMap[o.Source]));

            Dictionary<string, VolunteerSummaryApiViewModel> list = new Dictionary<string, VolunteerSummaryApiViewModel>();


            foreach (JObject occurrence in occurrences)
            {
                DateTime startDate = (DateTime)occurrence["StartDate"];
                DateTime endDate = (DateTime)occurrence["EndDate"];

                int days = (endDate.Date - startDate.Date).Days + 1;

                string place = occurrence["Place"].ToString();
                if (inModel.Place != null && !inModel.Place.Equals(place))
                    continue;

                foreach (JToken attendee in occurrence["Attendee"])
                {
                    if (inModel.AttendState != null)
                    {
                        var state = attendee["AttendState"];
                        bool isAttended = string.IsNullOrEmpty(state.ToString()) ? false : (bool)attendee["AttendState"];

                        if (inModel.AttendState != isAttended)
                            continue;
                    }

                    JToken _userData = attendee["User"];
                    string account = _userData["UserName"].ToString();
                    string name = _userData["Name"].ToString();
                    if (list.ContainsKey(account))
                    {
                        if (list[account].PlaceDays.ContainsKey(place))
                        {
                            list[account].PlaceDays[place] += days;
                        }
                        else
                            list[account].PlaceDays.Add(place, days);
                        list[account].TotalDays += days;
                        list[account].Attendee.Add(new { Id = occurrence["Id"], Place = place, StartDate = startDate, EndDate = endDate, Days = days });
                    }
                    else
                    {
                        VolunteerSummaryApiViewModel model = new VolunteerSummaryApiViewModel { UserName = account, Name = name, TotalDays = days };
                        model.PlaceDays = new Dictionary<string, int>();
                        model.PlaceDays.Add(place, days);
                        model.Attendee = new List<object>();
                        model.Attendee.Add(new { Id = occurrence["Id"], Place = place, StartDate = startDate, EndDate = endDate, Days = days });
                        list.Add(account, model);
                    }
                }
            }

            if (list.Count == 0)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

            return Ok(new ResultViewModel { Content = list.Values.OrderBy(x => x.UserName), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }


        [HttpPost]
        public IHttpActionResult PointsSummary(VolunteerApiViewMode inModel)
        {
            if (inModel == null || inModel.ContentType == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            IEnumerable<ContentItem> contentItems = null;
            IUser user = _authenticationService.GetAuthenticatedUser();

            contentItems = _attendeeService.GetAttendees(user, null, inModel.ContentType).Where(x=>x.IsPublished());

            if (contentItems == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            IEnumerable<JObject> caculatePointsObjects = contentItems.Where(x => x.As<CommonPart>().Container != null
            && _calendarService.DateInRange(x.As<CommonPart>().Container.As<SchedulePart>(), inModel.StartDate, inModel.EndDate))
                .Select(a => CaculatePointsObject(a)).Where(x=>x!=null);

            var data = caculatePointsObjects.GroupBy(x => new { UserName = x["UserName"].ToString(), Name = x["Name"].ToString() } , (key, group) => new
            {
                UserName = key.UserName,
                Name = key.Name, 
                Points = group.Sum(k => (int)k["AttendPoint"])
            }).OrderBy(d => d.UserName).ToList();

            return Ok(new ResultViewModel { Content = data, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private JObject CaculatePointsObject(ContentItem item)
        {
            JObject inModel = new JObject();
            inModel.Add(new JProperty("AttendState", true));
            JObject attendee = _attendeeService.GetAttendee(Url, item, inModel, true);
            if (attendee == null)
                return null;

            object obj = new
            {
                Name = attendee["User"]["Name"],
                UserName = attendee["User"]["UserName"],
                AttendPoint = attendee["AttendPoint"] == null || string.IsNullOrEmpty(attendee["AttendPoint"].ToString()) ? 0 : (int)attendee["AttendPoint"]
            };

            return JObject.FromObject(obj);
        }

    }
}