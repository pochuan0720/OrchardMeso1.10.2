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
using Orchard.UI.Notify;
using Orchard.Core.Containers.Services;
using Orchard.Core.Common.Models;
using Orchard.Security;
using Newtonsoft.Json.Linq;
using Orchard.Projections.Services;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Projections.Models;
using Orchard;
using Meso.Volunteer.Handlers;
using Meso.Volunteer.Services;
using System.Net.Http;
using Orchard.DynamicForms.Services;

namespace Meso.Volunteer.Controllers
{
    [Authorize]
    public class AttendeeApiController : ApiController
    {
        private int[] months = {2, 4, 6, 8, 10, 12 };
        private readonly IRoleService _roleService;
        private readonly IMembershipService _membershipService;
        private readonly IScheduleService _scheduleService;
        private readonly ICalendarService _calendarService;
        private readonly IOrchardServices _orchardServices;
        private readonly IContainerService _containerService;
        private readonly ISlugService _slugService;
        private readonly ICalendarUpdateModelHandler _updateModelHandler;
        private readonly IAuthenticationService _authenticationService;
        private readonly IProjectionManager _projectionManager;
        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);
        private readonly IFormService _formService;
        private readonly IAttendeeService _attendeeService;


        public AttendeeApiController(
            IRoleService roleService,
            IMembershipService membershipService,
            IScheduleService scheduleService,
            ICalendarService calendarService,
            IOrchardServices orchardServices,
            IContainerService containerService,
            ISlugService slugService,
            IShapeFactory shapeFactory,
            ICalendarUpdateModelHandler updateModelHandler,
            IAuthenticationService authenticationService,
            IProjectionManager projectionManager,
            IFormService formService,
            IAttendeeService attendeeService
            )
        {
            _roleService = roleService;
            _membershipService = membershipService;
            _scheduleService = scheduleService;
            _calendarService = calendarService;
            _orchardServices = orchardServices;
            _containerService = containerService;
            _slugService = slugService;
            _updateModelHandler = updateModelHandler;
            _authenticationService = authenticationService;
            _projectionManager = projectionManager;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
            Shape = shapeFactory;
            _formService = formService;
            _attendeeService = attendeeService;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }
        dynamic Shape { get; set; }

        public IHttpActionResult query(JObject inModel)
        {
            if (inModel == null)
                return BadRequest();

            if (inModel["ContainerId"] != null)
                return queryByContainer((int)inModel["ContainerId"]);

            if (!_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules, T("Not authorized to manage content")))
                return Unauthorized();

            IEnumerable<ContentItem> allContentItems = null;
            IUser user = _authenticationService.GetAuthenticatedUser();
            allContentItems = _attendeeService.GetAttendees(user, inModel["Query"].ToObject<QueryModel>());
            /*UserRolesPart rolesPart = user.As<UserRolesPart>();

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
                            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = queryName });
                            if (allContentItems == null)
                                allContentItems = contentItems;
                            else
                                allContentItems = allContentItems.Select(x => x).Concat(contentItems.Select(y => y));
                        }
                    }
                }
            }*/

            if (allContentItems == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            DateTime startDate = (DateTime)inModel["StartDate"];
            DateTime endDate = (DateTime)inModel["EndDate"];

            IEnumerable<object> attendees = allContentItems
                .Where(x => x.As<CommonPart>().Container != null
                    && _calendarService.DateInRange(x.As<CommonPart>().Container.As<SchedulePart>(), startDate, endDate))
                .Select(a => _attendeeService.GetAttendee(Url, a, inModel, true)).Where(y=>y!=null);
            if (attendees == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = attendees, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private IHttpActionResult queryByContainer(int containerId)
        {

            IEnumerable<ContentItem> contentItems = _containerService.GetContentItems(containerId);

            if (contentItems == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            IList<object> list = new List<object>();
            foreach(ContentItem item in contentItems)
            {
                object attendee = _attendeeService.GetAttendee(Url, item);
                if(attendee != null)
                    list.Add(attendee);
            }

            return Ok(new ResultViewModel { Content = list, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {

            if (inModel["Id"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"]);//, VersionOptions.DraftRequired);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            //var contentModel = _orchardServices.ContentManager.BuildEditor(content);

            return Ok(new ResultViewModel { Content = _attendeeService.GetAttendee(Url, content), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(JObject inModel)
        {
            if (inModel == null || inModel["ContentType"] == null)
            {
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if(inModel["ContainerId"] == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "ContainerId不可為空" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null)
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var containerItem = _orchardServices.ContentManager.Get((int)inModel["ContainerId"], VersionOptions.Published);

            if (containerItem == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "尚未發佈" });

            IUser user;
            if (_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules, T("Not authorized to manage content")))
            {
                if(inModel["Owner"] == null)
                    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "指派帳號不可為空" });

                user = _membershipService.GetUser(inModel["Owner"].ToString());
            }
            else
                user = _authenticationService.GetAuthenticatedUser();

            if (user == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "帳號不存在" });

            SchedulePart schedule = containerItem.As<SchedulePart>();
            ScheduleOccurrence _occurrence = _scheduleService.GetNextOccurrence(schedule, schedule.StartDate);
            //Get Scheduler model
            object outObj = _calendarService.GetOccurrenceViewModel(new ScheduleOccurrence(schedule, schedule.StartDate), new ScheduleData(containerItem, Url, _slugService, _orchardServices));
            JObject outModel = JObject.FromObject(outObj);

            //判斷此服勤狀態 for解說
            if (contentType.Equals(inModel["ContentType"].ToString()))
            {
                if (outModel["FormState"] == null || !outModel["FormState"].ToString().Equals("審核通過"))
                    return Ok(new ResultViewModel { Content = outModel, Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "狀態非審核通過" });
            }

            //IContentQuery<ContentItem> contentItems = _orchardServices.ContentManager.Query(VersionOptions.Published, containerItem.ContentType);
            IEnumerable<ContentItem> contentItems = null;
            contentItems = _projectionManager.GetContentItems(new QueryModel { Name = contentType });
            contentItems = contentItems.Where(x => x.As<CommonPart>().Owner != null
                    && x.As<CommonPart>().Container != null
                    && x.As<CommonPart>().Owner.Id == user.Id);


            //0.1 判斷是否與其他認養重疊
            var collection = contentItems
                .Where(s => _calendarService.DateInRange(s.As<CommonPart>().Container.As<SchedulePart>(), _occurrence.Start.AddMonths(-1), _occurrence.End.AddMonths(1)))
                .Select(c => _scheduleService.GetNextOccurrence(c.As<CommonPart>().Container.As<SchedulePart>(), c.As<CommonPart>().Container.As<SchedulePart>().StartDate))
                .Where(o => _calendarService.DateCollection(_occurrence, o.Start, o.End)).FirstOrDefault();

            if (collection != null)
            {
                object outModel1 = _calendarService.GetOccurrenceViewModel(collection, new ScheduleData(collection.Source.ContentItem, Url, _slugService, _orchardServices), false);
                return Ok(new ResultViewModel { Content = outModel1, Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "與其它行程衝突" });
            }

            //0.2 判斷是否與其他活動重疊

            IEnumerable<ContentItem> eventItems = _orchardServices.ContentManager.Query(VersionOptions.Published, contentType.Replace("Attendee", "Event")).List();
            //找前後一個月的活動
            eventItems = eventItems.Where(s => _calendarService.DateInRange(s.As<SchedulePart>(), _occurrence.Start.AddMonths(-1), _occurrence.End.AddMonths(1)));
            eventItems = eventItems
                .Where(x => _formService.GetSubmissions(x.Id.ToString()).Select(f => _calendarService.FormDataToDictionary(HttpUtility.ParseQueryString(f.FormData))).Where(d => d.Keys.Contains("Owner") && d["Owner"] != null && d["Owner"].Equals(user.Id.ToString())).FirstOrDefault() != null);

            if (eventItems != null)
            {
                collection = eventItems
                    .Select(c => _scheduleService.GetNextOccurrence(c.As<SchedulePart>(), c.As<SchedulePart>().StartDate))
                    .Where(o => _calendarService.DateCollection(_occurrence, o.Start, o.End)).FirstOrDefault();

                if (collection != null)
                {
                    object outModel2 = _calendarService.GetOccurrenceViewModel(collection, new ScheduleData(collection.Source.ContentItem, Url, _slugService, _orchardServices), false);
                    return Ok(new ResultViewModel { Content = outModel2, Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "與其它行程衝突" });
                }
            }


            // 1. 檢查是否可參加(一年允許取消六次)
            int cancelCount = _orchardServices.ContentManager.Query(VersionOptions.Draft, inModel["ContentType"].ToString()).List()
                .Where(i => i.As<CommonPart>().Owner != null && i.As<CommonPart>().Owner.Id == user.Id && ((DateTime)i.As<CommonPart>().PublishedUtc).ToString("yyyy").Equals(DateTime.UtcNow.ToString("yyyy"))).Count();

            if (cancelCount >= 6)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "本年度已取消" + cancelCount + "次" });

            // 2. 檢查剩餘可參與額度
            int volunteerQuota = (int)outModel["VolunteerQuota"];
            JToken[] attendees = outModel["Attendee"].ToArray();
            if (attendees.Length >= volunteerQuota)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "額度已滿" });

            // 3. 檢查是否已經參加
            foreach (JToken attendee in attendees)
            {
                //JObject attendee = JObject.FromObject(obj);
                string userName = attendee["User"]["UserName"].ToString();
                if (userName.Equals(user.UserName))
                    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "此帳號已經參加" });
            }

            if (contentType.Equals("Attendee") && months.Contains(DateTime.Now.Month))
            {
                if (contentItems != null && contentItems.Count() > 0)
                {
                    SchedulePart containerSchedule = containerItem.As<SchedulePart>();
                    DateTime containerStartDate = containerSchedule.StartDate;

                    if (containerStartDate.Month > 6) // 每年7~12月份服勤
                    {
                        //15日以前先開放未達5點之志工登記服勤
                        if (containerStartDate.Day < 15)
                        {
                            int year = DateTime.UtcNow.Year;
                            DateTime startDate = new DateTime(year, 1, 1);
                            DateTime endDate = DateTime.UtcNow;
                            int totalAttendPoint = 0;
                            contentItems.Where(x => _calendarService.DateInRange(x.As<CommonPart>().Container.As<SchedulePart>(), startDate, endDate) && IsAttended(x, ref totalAttendPoint)).ToList();

                            if(totalAttendPoint >=5 )
                            {
                                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "今年已達點數" + totalAttendPoint + ", 先開放未達5點之志工登記服勤" });
                            }
                        }
                    }

                    //
                    if (DateTime.Now.Day < 15) //15日以前，每個人限定服勤登記合併得３天，汶水只能認養２天
                    {
                        DateTime date = DateTime.UtcNow.AddMonths(1);
                        DateTime startDate = new DateTime(date.Year, date.Month, 1).ToUniversalTime();
                        //DateTime endDate = startDate.AddMonths(2);

                        contentItems = contentItems.Where(x => _calendarService.DateInRange(x.As<CommonPart>().Container.As<SchedulePart>(), startDate, DateTime.MaxValue));
                        int bb = contentItems.Count();
                        IEnumerable<ScheduleOccurrence> occurrences = contentItems.SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c.As<CommonPart>().Container.As<SchedulePart>(), startDate, DateTime.MaxValue));

                        _occurrence = _scheduleService.GetNextOccurrence(schedule, startDate);
                        int attendDays = (_occurrence.End.Date - _occurrence.Start.Date).Days + 1;
                        int days = 0;
                        foreach (ScheduleOccurrence occurrence in occurrences)
                        {
                            days += (occurrence.End.Date - occurrence.Start.Date).Days + 1;
                            if (days + attendDays > 3)
                                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "含本次合併" + (days + attendDays) + "天, 每個人限定服勤登記合併得3天" });
                        }


                        if (outModel["Place"] != null && outModel["Place"].ToString().Equals("汶水"))
                        {
                            days = 0;
                            int cc = contentItems.Count();
                            //int dd = contentItems.Where(x => InPlaces(x.As<CommonPart>().Container.As<SchedulePart>(), new[] { outModel["Place"].ToString() })).Count();
                            occurrences = contentItems.Where(x => InPlaces(x.As<CommonPart>().Container.As<SchedulePart>(), new[] { outModel["Place"].ToString() }))
                                    .SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c.As<CommonPart>().Container.As<SchedulePart>(), startDate, DateTime.MaxValue));
                            foreach (ScheduleOccurrence occurrence in occurrences)
                            {
                                days += (occurrence.End.Date - occurrence.Start.Date).Days + 1;
                                if (days + attendDays > 2)
                                    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "汶水含本次合併" + (days + attendDays) + "天, 汶水只能認養2天" });
                            }
                        }
                    }
                }
            }

            var content = _orchardServices.ContentManager.New<ContentPart>(contentType);

            if (content == null)
                return InternalServerError();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });

            //_orchardServices.ContentManager.BuildEditor(content);

            _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
            //init
            if (inModel["AttendState"] == null)
                inModel["AttendState"] = false;
            if (inModel["IsAttendFee"] == null)
                inModel["IsAttendFee"] = false;
                

            var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));
            _orchardServices.ContentManager.Publish(content.ContentItem);
            return Ok(new ResultViewModel { Content = _attendeeService.GetAttendee(Url, content), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

        }
        private bool IsAttended(IContent content, ref int totalAttendPoint)
        {
            var attendeeModel = _orchardServices.ContentManager.BuildEditor(content);
            JObject attendee = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(new JObject(), attendeeModel);
            if (attendee["AttendState"] != null && (bool)attendee["AttendState"])
            {
                if(attendee["AttendPoint"] != null && !string.IsNullOrEmpty(attendee["AttendPoint"].ToString()))
                    totalAttendPoint += (int)attendee["AttendPoint"];
                return true;
            }
            else
                return false;

        }

        private bool InPlaces(IContent content, string[] places)
        {
            var model = _orchardServices.ContentManager.BuildEditor(content);
            JObject container = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(new JObject(), model);
            if (container["Place"] != null && places.Contains(container["Place"].ToString()))
                return true;
            else
                return false;

        }


        [HttpPost]
        public IHttpActionResult update(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null || inModel["ContentType"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"]);//, VersionOptions.DraftRequired);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null || !contentType.Equals(content.ContentType))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });


            _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));

            _orchardServices.ContentManager.Publish(content);
            _orchardServices.Notifier.Information(T("schedule information updated"));

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null || inModel["ContentType"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"], VersionOptions.Latest);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!_orchardServices.Authorizer.Authorize(Orchard.Core.Contents.Permissions.PublishContent, content, T("Couldn't Unpublish content")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit content" });

            if (!content.As<CommonPart>().IsPublished())
                return InternalServerError();

            /*string contentType = GetContentType("Delete", ref inModel);
            if (!string.IsNullOrEmpty(contentType) && content.ContentType.Equals(contentType))
            {
                _orchardServices.ContentManager.Remove(content);
                _orchardServices.Notifier.Information(T("content deleted"));
                return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }*/

            string contentType = GetContentType("Publish", ref inModel);
            if (!string.IsNullOrEmpty(contentType) && content.ContentType.Equals(contentType))
            {

                //return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
                if (_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules, T("Not authorized to manage content")))
                {
                    _orchardServices.ContentManager.Remove(content);
                    return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
                }
                else
                {
                    _orchardServices.ContentManager.Unpublish(content);
                    _orchardServices.Notifier.Information(T("content Unpublished"));
                    try
                    {
                        //New a Cancel Content
                        string cancelContentType = inModel["ContentType"].ToString() + "Cancel";
                        _calendarService.Notification(content, cancelContentType);
                        return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

                    }
                    catch (Exception e)
                    {
                        _orchardServices.ContentManager.Publish(content);
                        _orchardServices.Notifier.Information(T("content Published"));
                        return InternalServerError();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
                    }
                }
                
            }

            return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

        }

        [HttpPost]
        public IHttpActionResult clean(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null || inModel["ContentType"] == null)
                return BadRequest();

            if (!_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules, T("Not authorized to manage content")))
                return Unauthorized();

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"], VersionOptions.Draft);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            _orchardServices.ContentManager.Remove(content);

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        public IHttpActionResult self(JObject inModel)
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
                            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = queryName });
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

            DateTime startDate = (DateTime)inModel["StartDate"];
            DateTime endDate = (DateTime)inModel["EndDate"];

            IEnumerable<object> attendees = allContentItems
                .Where(x => x.As<CommonPart>().Owner.Id == user.Id 
                    && x.As<CommonPart>().Container != null 
                    && _calendarService.DateInRange(x.As<CommonPart>().Container.As<SchedulePart>(), startDate, endDate))
                .Select(a => _attendeeService.GetAttendee(Url, a, null, false)).Where(y => y != null);


            if (attendees == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            return Ok(new ResultViewModel { Content = attendees, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }




        private IHttpActionResult old_self(JObject inModel)
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
                            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems( new QueryModel { Name = queryName });
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


            //IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(inModel.Query);
            //IEnumerable<ContentItem> contentItems = _containerService.GetContentItems((int)inModel["ContainerId"]);

            //contentItems = contentItems.Select(c => c.As<CommonPart>().Container.ContentItem).Where(x => x.As<CommonPart>().Owner.Id == user.Id);

            allContentItems = allContentItems.Select(c => c.As<CommonPart>()).Where(x => x.Owner.Id == user.Id && x.Container != null).Select(c => c.Container.ContentItem);
            if (allContentItems == null)
                return  Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            allContentItems = allContentItems.GroupBy(x => x.Id).Select(g => g.First());
            /*IList<object> list = new List<object>();
            foreach (ContentItem item in contentItems)
            {
                CommonPart common = item.As<CommonPart>();
                IUser owner = common.Owner;
                var userModel = _orchardServices.ContentManager.BuildEditor(user);
                var attendeeModel = _orchardServices.ContentManager.BuildEditor(item);
                list.Add(new { Id = item.Id, CreatedLocal = common.CreatedLocal, User = new { Id = user.Id, UserName = user.UserName, Email = user.Email, Data = Core.Common.Handlers.UpdateModelHandler.GetData(userModel) }, Data = Core.Common.Handlers.UpdateModelHandler.GetData(attendeeModel) });
            }

            return Ok(new ResultViewModel { Content = list, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });*/

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
        public IHttpActionResult cancellist(JObject inModel)
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
                            IEnumerable<ContentItem> contentItems = _orchardServices.ContentManager.Query(VersionOptions.Draft, queryName).List();// _projectionManager.GetContentItems(new QueryModel { Name = queryName });
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

            //ContentItem.VersionRecord != null && content.ContentItem.VersionRecord.Published

            allContentItems = allContentItems.Where(c => c.As<CommonPart>().Container != null)
                .Select(c => c.As<CommonPart>().Container.ContentItem);
            allContentItems = allContentItems.GroupBy(x => x.Id).Select(g => g.First());

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

        public IHttpActionResult cancelhistory(JObject inModel)
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
                        if (possessedName.StartsWith("View_" + query.Name) && !possessedName.EndsWith("Cancel"))
                        {
                            queryName = possessedName.Substring("View_".Length);
                            IEnumerable<ContentItem> contentItems = _orchardServices.ContentManager.Query(VersionOptions.Draft, queryName).List();//_projectionManager.GetContentItems(new QueryModel { Name = queryName });
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

            DateTime startDate = (DateTime)inModel["StartDate"];
            DateTime endDate = (DateTime)inModel["EndDate"];

            IEnumerable<object> attendees = allContentItems
                .Where(x => x.As<CommonPart>().Container != null
                    && _calendarService.DateInRange(x.As<CommonPart>().Container.As<SchedulePart>(), startDate, endDate))
                .Select(a => _attendeeService.GetAttendee(Url, a, null, false)).Where(y => y != null); ;
            if (attendees == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            return Ok(new ResultViewModel { Content = attendees, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private string GetContentType(string prefix, ref JObject inModel)
        {
            string prefixOthers = prefix + "_";
            string prefixOwner = prefix + "Own_";

            IList<string> contentTypesOthers = new List<string>();
            IList<string> contentTypesOwner = new List<string>();


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
                        if (possessedName.StartsWith(prefixOthers + inModel["ContentType"]))
                        {
                            contentTypesOthers.Add(possessedName.Substring(prefixOthers.Length));
                        }
                        else if (possessedName.StartsWith(prefixOwner + inModel["ContentType"]))
                        {
                            contentTypesOwner.Add(possessedName.Substring(prefixOwner.Length));
                        }
                    }
                }
            }

            if (contentTypesOthers.Count() == 0 && contentTypesOwner.Count() == 0)
                return null;


            if (contentTypesOthers.Count() == 0)
            {
                if (contentTypesOwner.Count() == 1)
                {
                    //inModel.Owner = user.UserName;
                    return contentTypesOwner[0];
                }
                else
                    return null;
            }
            else if (contentTypesOthers.Count() == 1)
                return contentTypesOthers[0];
            else
                return inModel["ContentType"].ToString();
        }


    }


}
