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
using Orchard.UI.Notify;
using Orchard.Security;
using Orchard.Roles.Services;
using Orchard.Roles.Models;
using Newtonsoft.Json.Linq;
using Meso.Volunteer.Handlers;
using Meso.Volunteer.Services;
using Orchard.Core.Common.Handlers;
using Orchard.Users.Models;
using Orchard;
using Orchard.Schedule.Services;
using Orchard.Schedule.Models;
using Orchard.Projections.Models;

namespace Meso.Volunteer.Controllers
{
    [Authorize]
    public class CalendarApiController : ApiController
    {
        private readonly IRoleService _roleService;
        private readonly IScheduleService _scheduleService;
        private readonly ICalendarService _calendarService;
        private readonly IOrchardServices _orchardServices;
        private readonly ISlugService _slugService;
        private readonly ICalendarUpdateModelHandler _updateModelHandler;
        private readonly IAuthenticationService _authenticationService;
        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);
        private static IMembershipService _membershipService;
        public CalendarApiController(
            IRoleService roleService,
            IScheduleService scheduleService,
            ICalendarService calendarService,
            IOrchardServices orchardServices,
            ISlugService slugService,
            IShapeFactory shapeFactory,
            ICalendarUpdateModelHandler updateModelHandler,
            IAuthenticationService authenticationService,
            IMembershipService membershipService
            )
        {
            _roleService = roleService;
            _scheduleService = scheduleService;
            _calendarService = calendarService;
            _orchardServices = orchardServices;
            _slugService = slugService;
            _updateModelHandler = updateModelHandler;
            _authenticationService = authenticationService;
            _membershipService = membershipService;
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
            if (inModel == null || inModel["Query"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            IEnumerable<ContentItem> allContentItems = null;
            string queryName = "";
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
                        QueryModel query = inModel["Query"].ToObject<QueryModel>();
                        if (possessedName.StartsWith("View_" + query.Name))
                        {
                            queryName = possessedName.Substring("View_".Length);
                            //IEnumerable<ContentItem> contentItems = _calendarService.GetProjectionContentItems(new QueryModel { Name = queryName});
                            IEnumerable<ContentItem> contentItems;

                            if (_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules))
                            {
                                //contentItems = _calendarService.GetProjectionContentItems(new QueryModel { Name = "Latest_" + queryName });
                                contentItems = _orchardServices.ContentManager.Query(VersionOptions.Latest, queryName).List();
                            }
                            else
                            {
                                //contentItems = _calendarService.GetProjectionContentItems(new QueryModel { Name = "Published_" + queryName });
                                contentItems = _orchardServices.ContentManager.Query(VersionOptions.Published, queryName).List();
                            }

                            if (allContentItems == null)
                                allContentItems = contentItems;
                            else
                                allContentItems = allContentItems.Select(x => x).Concat(contentItems.Select(y => y));
                        }
                    }
                }
            }

            if (allContentItems == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            Dictionary<IContent, ScheduleData> ScheduleMap =
                allContentItems
                .Select(c => new { k = (IContent)c, v = new ScheduleData(c, Url, _slugService, _orchardServices) })
                .ToDictionary(c => c.k, c => c.v);

            DateTime startDate = (DateTime)inModel["StartDate"];
            DateTime endDate = (DateTime)inModel["EndDate"];
            var scheduleOccurrences = allContentItems
                .Select(c => c.As<SchedulePart>())
                .Where(s => _calendarService.DateInRange(s, startDate, endDate))
                .SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c, startDate, endDate))
                .OrderBy(o => o.Start);

            var occurrences = scheduleOccurrences.Select(o => _calendarService.GetOccurrenceViewModel(o, ScheduleMap[o.Source])).ToList();

            return Ok(new ResultViewModel { Content = occurrences, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            int id = (int)inModel["Id"];
            var contentItem = _orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules) ? _orchardServices.ContentManager.Get(id, VersionOptions.Latest) : _orchardServices.ContentManager.Get(id, VersionOptions.Published);//, VersionOptions.DraftRequired);

            if (contentItem == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            QueryModel query = inModel["Query"].ToObject<QueryModel>();
            JObject tmp = JObject.FromObject(new { ContentType = query.Name });
            string contentType = GetContentType("View", ref tmp);
            if (contentType == null || !contentType.Equals(contentItem.ContentType))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to view content" });

            SchedulePart schedule = contentItem.As<SchedulePart>();
            object outModel = _calendarService.GetOccurrenceViewModel(new ScheduleOccurrence(schedule, schedule.StartDate), new ScheduleData(contentItem, Url, _slugService, _orchardServices));

            return Ok(new ResultViewModel { Content = outModel, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(JObject inModel)
        {
            if (inModel == null)
            {
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.AddSchedule, T("Not authorized to manage schedules")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null)
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var content = _orchardServices.ContentManager.New<SchedulePart>(contentType);

            if (content == null)
                return InternalServerError();

            _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
            var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));
            if (inModel["IsPublished"] != null && (bool)inModel["IsPublished"])
            {
                _orchardServices.ContentManager.Publish(content.ContentItem);
                if (inModel["IsMailTo"] != null && (bool)inModel["IsMailTo"])
                    _calendarService.Notification(content.ContentItem, inModel["ContentType"].ToString() + "Notify");
            }
            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult batch(JObject inModel)
        {
            if (inModel == null)
            {
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.AddSchedule, T("Not authorized to manage schedules")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null)
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var statuses = new List<object>();

            if (inModel["Places"] == null)
                return BadRequest();

            foreach (string place in inModel["Places"])
            {
                if (inModel["IsDaily"] != null && (bool)inModel["IsDaily"])
                {
                    foreach (DateTime day in EachDay((DateTime)inModel["StartDate"], (DateTime)inModel["EndDate"]))
                    {
                        var content = _orchardServices.ContentManager.New<SchedulePart>(contentType);
                        if (content != null)
                        {
                            JObject _inModel = (JObject)inModel.DeepClone();
                            _inModel["StartDate"] = day;
                            _inModel["EndDate"] = day.AddDays(1).AddMinutes(-1);

                            setJObject(_inModel, "Place", place);
                            if (contentType.Equals("Appointment"))
                            {
                                IUser user = _membershipService.GetUser(place);
                                if (user != null)
                                {
                                    var model = _orchardServices.ContentManager.BuildEditor(user);
                                    JObject Data = UpdateModelHandler.GetData(JObject.FromObject(user.As<UserPart>()), model);
                                    _inModel["Title"] = Data["Nickname"].ToString();
                                    if (_inModel["PeopleQuota"] == null)
                                        _inModel.Add(new JProperty("PeopleQuota", 100));
                                    if (_inModel["VolunteerQuota"] == null)
                                        _inModel.Add(new JProperty("VolunteerQuota", 1));

                                    if (Data["Unit"] != null)
                                        setJObject(_inModel, "ApplyUnit", Data["Unit"]);

                                    if (Data["Nickname"] != null)
                                        setJObject(_inModel, "Item", Data["Nickname"]);


                                    if (Data["Name"] != null)
                                        setJObject(_inModel, "Contact", Data["Name"]);

                                    if (Data["OrgTel"] != null)
                                        setJObject(_inModel, "ContactTel", Data["OrgTel"]);

                                    if (Data["MobileTel"] != null)
                                        setJObject(_inModel, "ContactMobile", Data["MobileTel"]);

                                    setJObject(_inModel, "FormState", "審核通過");
                                    setJObject(_inModel, "Source", "處內");
                                    setJObject(_inModel, "Email", user.Email);
                                }
                            }

                            _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
                            var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(_inModel));
                            if (inModel["IsPublished"] != null && (bool)inModel["IsPublished"])
                            {
                                _orchardServices.ContentManager.Publish(content.ContentItem);
                                if (_inModel["IsMailTo"] != null && (bool)_inModel["IsMailTo"])
                                    _calendarService.Notification(content.ContentItem, inModel["ContentType"].ToString() + "Notify");
                            }

                            statuses.Add(new
                            {
                                Id = content.Id,
                                Date = day,
                                Place = place
                            });
                        }
                    }
                }
                else
                {
                    var content = _orchardServices.ContentManager.New<SchedulePart>(contentType);
                    if (content != null)
                    {
                        JObject _inModel = (JObject)inModel.DeepClone();

                        if (_inModel["Place"] != null)
                            _inModel["Place"] = place;
                        else
                            _inModel.Add(new JProperty("Place", place));

                        _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
                        var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(_inModel));
                        if (inModel["IsPublished"] != null && (bool)inModel["IsPublished"])
                        {
                            _orchardServices.ContentManager.Publish(content.ContentItem);
                            if (inModel["IsMailTo"] != null && (bool)inModel["IsMailTo"])
                                _calendarService.Notification(content.ContentItem, inModel["ContentType"].ToString() + "Notify");
                        }

                        statuses.Add(new
                        {
                            Id = content.Id,
                            Place = place
                        });
                    }
                }
            }

            return Ok(new ResultViewModel { Content = statuses , Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult update(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            int id = (int)inModel["Id"];
            var schedule = _orchardServices.ContentManager.Get(id, VersionOptions.DraftRequired);

            if (schedule == null)
                return  Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules, schedule, T("Couldn't edit schedule")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit schedule" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null || !contentType.Equals(schedule.ContentType))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            _orchardServices.ContentManager.UpdateEditor(schedule, _updateModelHandler.SetData(inModel));

            if (inModel["IsPublished"] != null && (bool)inModel["IsPublished"])
            {
                _orchardServices.ContentManager.Publish(schedule);
                if (inModel["IsMailTo"] != null && (bool)inModel["IsMailTo"])
                    _calendarService.Notification(schedule, inModel["ContentType"].ToString() + "Notify");
            }
            else
                _orchardServices.ContentManager.Unpublish(schedule);

            _orchardServices.Notifier.Information(T("schedule information updated"));

            return Ok(new ResultViewModel { Content = new { Id = schedule.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            if (!_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules, T("Couldn't delete schedule")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete schedule" });

            int id = (int)inModel["Id"];

            var schedule = _orchardServices.ContentManager.Get(id, VersionOptions.Latest);
            if (schedule == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            string contentType = GetContentType("Delete", ref inModel);
            if (contentType == null || !contentType.Equals(schedule.ContentType))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to delete content" });

            Delete(id);

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

        private string GetContentType(string prefix, ref JObject inModel)
        {
            prefix = prefix + "_";
            IList<string> contentTypes = new List<string>();
            IList<string> attendees = new List<string>();

            string contentType = "";
            string attendee = "";
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
                        if (possessedName.StartsWith(prefix + inModel["ContentType"]))
                        {
                            contentType = possessedName.Substring(prefix.Length);
                            contentTypes.Add(contentType);
                        }

                        if (inModel["Container"] != null)
                        {
                            if (possessedName.StartsWith(prefix + inModel["Container"][0]) && !possessedName.EndsWith("Cancel"))
                            {
                                attendee = possessedName.Substring(prefix.Length);
                                attendees.Add(attendee);
                            }
                        }
                    }
                }
            }

            if (inModel["Container"] != null)
            {
                if (attendees.Count() == 0)
                    return null;
                else if (attendees.Count() == 1)
                    inModel["Container"] = JArray.FromObject(attendees);
            }

            if (contentTypes.Count() == 0)
                return null;
            else if (contentTypes.Count() == 1)
                return contentTypes[0];
            else
                return inModel["ContentType"].ToString();
        }

        private IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }

        private void setJObject(JObject obj, string key, object value)
        {
            if (obj[key] != null)
                obj.Remove(key);
            
            obj.Add(new JProperty(key, value));
        }
    }
}
