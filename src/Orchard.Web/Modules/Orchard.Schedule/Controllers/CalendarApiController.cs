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
using Orchard.Projections.Models;
using Orchard.Security;
using Orchard.Roles.Services;
using Orchard.Roles.Models;
using Newtonsoft.Json.Linq;

namespace Orchard.Schedule.Controllers
{
    [Authorize]
    public class CalendarApiController : ApiController
    {
        private readonly IRoleService _roleService;
        private readonly IScheduleService _scheduleService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IOrchardServices _orchardServices;
        private readonly ISlugService _slugService;
        private readonly IUpdateModelHandler _updateModelHandler;
        private readonly IAuthenticationService _authenticationService;
        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);
        private static IMembershipService _membershipService;
        public CalendarApiController(
            IRoleService roleService,
            IScheduleService scheduleService,
            IScheduleLayoutService scheduleLayoutService,
            IOrchardServices orchardServices,
            ISlugService slugService,
            IShapeFactory shapeFactory,
            IUpdateModelHandler updateModelHandler,
            IAuthenticationService authenticationService,
            IMembershipService membershipService
            )
        {
            _roleService = roleService;
            _scheduleService = scheduleService;
            _scheduleLayoutService = scheduleLayoutService;
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
        public IHttpActionResult query(SchedulesIndexApiViewMode inModel)
        {
            if (inModel == null || inModel.Query == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

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
                        if (possessedName.StartsWith("View_" + inModel.Query.Name))
                        {
                            queryName = possessedName.Substring("View_".Length);
                            //IEnumerable<ContentItem> contentItems = _scheduleLayoutService.GetProjectionContentItems(new QueryModel { Name = queryName});
                            IEnumerable<ContentItem> contentItems;

                            if (_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules))
                            {
                                //contentItems = _scheduleLayoutService.GetProjectionContentItems(new QueryModel { Name = "Latest_" + queryName });
                                contentItems = _orchardServices.ContentManager.Query(VersionOptions.Latest, queryName).List();
                            }
                            else
                            {
                                //contentItems = _scheduleLayoutService.GetProjectionContentItems(new QueryModel { Name = "Published_" + queryName });
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
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            Dictionary<IContent, ScheduleData> ScheduleMap =
                allContentItems
                .Select(c => new { k = (IContent)c, v = new ScheduleData(c, Url, _slugService, _orchardServices) })
                .ToDictionary(c => c.k, c => c.v);

            var scheduleOccurrences = allContentItems
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

            var contentItem = _orchardServices.Authorizer.Authorize(Permissions.ManageSchedules) ? _orchardServices.ContentManager.Get(inModel.Id, VersionOptions.Latest) : _orchardServices.ContentManager.Get(inModel.Id, VersionOptions.Published);//, VersionOptions.DraftRequired);

            if (contentItem == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            ScheduleEditApiViewMode tmp = new ScheduleEditApiViewMode { ContentType = inModel.Query.Name };
            string contentType = GetContentType("View", ref tmp);
            if (contentType == null || !contentType.Equals(contentItem.ContentType))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to view content" });


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

            if (!_orchardServices.Authorizer.Authorize(Permissions.AddSchedule, T("Not authorized to manage schedules")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            string contentType = GetContentType("Edit", ref inModel);

            if(contentType == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var content = _orchardServices.ContentManager.New<SchedulePart>(contentType);


            if (content != null)
            {
                _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
                var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));
                if(inModel.IsPublished != null && (bool)inModel.IsPublished)
                    _orchardServices.ContentManager.Publish(content.ContentItem);
                return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
        }

        [HttpPost]
        public IHttpActionResult batch(ScheduleEditApiViewMode inModel)
        {
            if (inModel == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!_orchardServices.Authorizer.Authorize(Permissions.AddSchedule, T("Not authorized to manage schedules")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var statuses = new List<object>();

            foreach (string place in inModel.Places)
            {
                if (inModel.IsDaily != null && (bool)inModel.IsDaily)
                {
                    foreach (DateTime day in EachDay(inModel.StartDate, inModel.EndDate))
                    {
                        var content = _orchardServices.ContentManager.New<SchedulePart>(contentType);
                        if (content != null)
                        {
                            ScheduleEditApiViewMode _inModel = (ScheduleEditApiViewMode)inModel.Clone();
                            _inModel.StartDate = day;
                            _inModel.EndDate = day.AddDays(1).AddMinutes(-1);

                            if (contentType.Equals("Appointment"))
                            {
                                IUser user = _membershipService.GetUser(place);
                                var model = _orchardServices.ContentManager.BuildEditor(user);
                                JObject Data = UpdateModelHandler.GetData(model);
                                if (user != null)
                                {
                                    _inModel.Title = Data["User.Nickname"].ToString();
                                    if (_inModel.Data == null)
                                        _inModel.Data = new JObject();
                                    else
                                    {
                                        if(_inModel.Data["Appointment.PeopleQuota"] == null)
                                            _inModel.Data.Add(new JProperty("Appointment.PeopleQuota", 100));
                                        if (_inModel.Data["Appointment.VolunteerQuota"] == null)
                                            _inModel.Data.Add(new JProperty("Appointment.VolunteerQuota", 1));
                                    }

                                    if (Data["User.Unit"] != null)
                                        setJObject(_inModel.Data, "Appointment.ApplyUnit", Data["User.Unit"]);

                                    setJObject(_inModel.Data, "Appointment.Place", place);

                                    if (Data["User.Nickname"] != null)
                                        setJObject(_inModel.Data, "Appointment.Item", Data["User.Nickname"]);


                                    if (Data["User.Name"] != null)
                                        setJObject(_inModel.Data, "Appointment.Contact", Data["User.Name"]);

                                    if (Data["User.OrgTel"] != null)
                                        setJObject(_inModel.Data, "Appointment.ContactTel", Data["User.OrgTel"]);

                                    if (Data["User.MobileTel"] != null)
                                        setJObject(_inModel.Data, "Appointment.ContactMobile", Data["User.MobileTel"]);

                                    setJObject(_inModel.Data, "Appointment.FormState", "審核通過");
                                    setJObject(_inModel.Data, "Appointment.Source", "處內");
                                    setJObject(_inModel.Data, "Appointment.Email", user.Email);
                                }
                            }
                            else if (contentType.Equals("Appointment2"))
                            {
                                setJObject(_inModel.Data, "Appointment2.Place", place);
                            }

                            _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
                            var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(_inModel));
                            if (inModel.IsPublished != null && (bool)inModel.IsPublished)
                                _orchardServices.ContentManager.Publish(content.ContentItem);

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
                        ScheduleEditApiViewMode _inModel = (ScheduleEditApiViewMode)inModel.Clone();
                        string key_place = contentType + ".Place";
                        if (_inModel.Data[key_place] != null)
                            _inModel.Data[key_place] = place;
                        else
                            _inModel.Data.Add(new JProperty(key_place, place));

                        _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
                        var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));
                        if (inModel.IsPublished != null && (bool)inModel.IsPublished)
                            _orchardServices.ContentManager.Publish(content.ContentItem);

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
        public IHttpActionResult update(ScheduleEditApiViewMode inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var schedule = _orchardServices.ContentManager.Get(inModel.Id, VersionOptions.DraftRequired);

            if (schedule == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, schedule, T("Couldn't edit schedule")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit schedule" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null || !contentType.Equals(schedule.ContentType))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            _orchardServices.ContentManager.UpdateEditor(schedule, _updateModelHandler.SetData(inModel));

            if (inModel.IsPublished != null && (bool)inModel.IsPublished)
                _orchardServices.ContentManager.Publish(schedule);
            else
                _orchardServices.ContentManager.Unpublish(schedule);

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

            var schedule = _orchardServices.ContentManager.Get(inModel.Id, VersionOptions.Latest);
            if (schedule == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            string contentType = GetContentType("Delete", ref inModel);
            if (contentType == null || !contentType.Equals(schedule.ContentType))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to delete content" });

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

        private string GetContentType(string prefix, ref ScheduleEditApiViewMode inModel)
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
                        if (possessedName.StartsWith(prefix + inModel.ContentType))
                        {
                            contentType = possessedName.Substring(prefix.Length);
                            contentTypes.Add(contentType);
                        }

                        if (inModel.Container != null)
                        {
                            if (possessedName.StartsWith(prefix + inModel.Container[0]))
                            {
                                attendee = possessedName.Substring(prefix.Length);
                                attendees.Add(attendee);
                            }
                        }
                    }
                }
            }

            if (inModel.Container != null)
            {
                if (attendees.Count() == 0)
                    return null;
                else if (attendees.Count() == 1)
                    inModel.Container = attendees.ToArray();
            }

            if (contentTypes.Count() == 0)
                return null;
            else if (contentTypes.Count() == 1)
                return contentTypes[0];
            else
                return inModel.ContentType;
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
