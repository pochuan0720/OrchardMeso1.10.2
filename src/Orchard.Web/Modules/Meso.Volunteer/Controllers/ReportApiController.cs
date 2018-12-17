using Meso.Volunteer.Services;
using Meso.Volunteer.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
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

        public ReportApiController(
            IRoleService roleService,
            IScheduleService scheduleService,
            ICalendarService calendarService,
            IAuthenticationService authenticationService,
            IOrchardServices orchardServices,
            ISlugService slugService)
        {
            _roleService = roleService;
            _scheduleService = scheduleService;
            _calendarService = calendarService;
            _authenticationService = authenticationService;
            _orchardServices = orchardServices;
            _slugService = slugService;
        }


        [HttpPost]
        public IHttpActionResult VolunteerSummary(VolunteerApiViewMode inModel)
        {
            if (inModel == null || inModel.ContentType == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            IEnumerable<ContentItem> allContentItems = null;
            string queryName = "";
            //string attendeeName = "";
            IUser user = _authenticationService.GetAuthenticatedUser();
            UserRolesPart rolesPart = user.As<UserRolesPart>();

            if (rolesPart != null)
            {
                IEnumerable<string> userRoles = rolesPart.Roles;
                foreach (var role in userRoles)
                {
                    foreach (var permissionName in _roleService.GetPermissionsForRoleByName(role))
                    {
                        string possessedName = permissionName;
                        if (possessedName.StartsWith("View_" + inModel.ContentType))
                        {
                            queryName = possessedName.Substring("View_".Length);
                            //IEnumerable<ContentItem> contentItems = _scheduleLayoutService.GetProjectionContentItems(new QueryModel { Name = queryName});
                            IEnumerable<ContentItem> contentItems;

                            if (_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules))
                            {
                                //contentItems = _calendarService.GetProjectionContentItems(new QueryModel { Name = "Latest_" + queryName });
                                contentItems = _orchardServices.ContentManager.Query(VersionOptions.Latest, queryName).List();
                            }
                            else
                                contentItems = _orchardServices.ContentManager.Query(VersionOptions.Published, queryName).List();
                            //contentItems = _calendarService.GetProjectionContentItems(new QueryModel { Name = "Published_" + queryName });
                            //contentItems = _orchardServices.ContentManager.Query(VersionOptions.Published, queryName).List();

                            if (allContentItems == null)
                                allContentItems = contentItems;
                            else
                                allContentItems = allContentItems.Select(x => x).Concat(contentItems.Select(y => y));
                        }

                        /*if (string.IsNullOrEmpty(attendeeName) && possessedName.StartsWith("View_Attendee"))
                        {
                            attendeeName = possessedName.Substring("View_".Length);

                        }*/
                    }
                }
            }

            if (allContentItems == null)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


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


            foreach(JObject occurrence in occurrences)
            {
                DateTime startDate = (DateTime)occurrence["StartDate"];
                DateTime endDate = (DateTime)occurrence["EndDate"];

                int days = (endDate.Date - startDate.Date).Days;

                string place = occurrence["Place"].ToString();
                if (inModel.Place != null && !inModel.Place.Equals(place))
                    continue;

                foreach (JToken attendee in occurrence["Attendee"])
                {
                    if(inModel.AttendState != null)
                    {
                        var state = attendee["AttendState"];
                        bool isAttended = string.IsNullOrEmpty(state.ToString())  ? false : (bool)attendee["AttendState"];

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
                        list[account].Attendee.Add(new { Id = occurrence["Id"], Place = place , StartDate = startDate, EndDate = endDate, Days = days });
                    }
                    else
                    {
                        VolunteerSummaryApiViewModel model = new VolunteerSummaryApiViewModel { UserName = account, Name = name, TotalDays = days};
                        model.PlaceDays = new Dictionary<string, int>();
                        model.PlaceDays.Add(place, days);
                        model.Attendee = new List<object>();
                        model.Attendee.Add(new { Id = occurrence["Id"], Place = place, StartDate = startDate, EndDate = endDate, Days = days });
                        list.Add(account, model);
                    }
                }
            }

            if (list.Count == 0)
                return NotFound();

            return Ok(new ResultViewModel { Content = list.Values.OrderBy(x => x.UserName), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }


    }
}
