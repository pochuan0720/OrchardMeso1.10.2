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

namespace Meso.Volunteer.Controllers
{
    [Authorize]
    public class AttendeeApiController : ApiController
    {
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
            IProjectionManager projectionManager
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
                .Where(x => x.As<CommonPart>().Container != null
                    && _calendarService.DateInRange(x.As<CommonPart>().Container.As<SchedulePart>(), startDate, endDate))
                .Select(a => getAttendee(a, inModel, true)).Where(y=>y!=null);
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
                list.Add(getAttendee(item));
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

            return Ok(new ResultViewModel { Content = getAttendee(content), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(JObject inModel)
        {
            if (inModel == null || inModel["ContentType"] == null)
            {
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null)
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var containerItem = _orchardServices.ContentManager.Get((int)inModel["ContainerId"], VersionOptions.Published);

            if (containerItem == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            IUser user;
            if (_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules, T("Not authorized to manage content")) && inModel["Owner"] != null)
                user = _membershipService.GetUser(inModel["Owner"].ToString());
            else
                user = _authenticationService.GetAuthenticatedUser();

            if (user == null)
                return NotFound();

            // 1. 檢查是否可參加(一年允許取消六次)

            int cancelCount = _orchardServices.ContentManager.Query(VersionOptions.Published, inModel["ContentType"].ToString() + "Cancel").List()
                .Where(i => i.As<CommonPart>().Owner.Id == user.Id && ((DateTime)i.As<CommonPart>().PublishedUtc).ToString("yyyy").Equals(DateTime.UtcNow.ToString("2019"))).Count();

            if (cancelCount > 6)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "本年度已取消" + cancelCount + "次" });

            // 2. 檢查剩餘可參與額度
            //Get Scheduler model
            SchedulePart schedule = containerItem.As<SchedulePart>();
            object outObj = _calendarService.GetOccurrenceViewModel(new ScheduleOccurrence(schedule, schedule.StartDate), new ScheduleData(containerItem, Url, _slugService, _orchardServices));
            JObject outModel = JObject.FromObject(outObj);
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
            return Ok(new ResultViewModel { Content = getAttendee(content), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

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
                _orchardServices.ContentManager.Unpublish(content);
                _orchardServices.Notifier.Information(T("content Unpublished"));
                //return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
                try
                {
                    //New a Cancel Content
                    string cancelContentType = inModel["ContentType"].ToString() + "Cancel";
                    _calendarService.Notification(content, cancelContentType);
                    return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

                } catch(Exception e)
                {
                    _orchardServices.ContentManager.Publish(content);
                    _orchardServices.Notifier.Information(T("content Published"));
                    return InternalServerError();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
                }
            }

            return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

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
                .Select(a => getAttendee(a, null, false));
            if (attendees == null)
                return Ok(new ResultViewModel { Content = Enumerable.Empty<object>(), Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            return Ok(new ResultViewModel { Content = attendees, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private object getAttendee(IContent content, JObject inModel = null, bool withUser = true)
        {
            CommonPart common = content.As<CommonPart>();
            IContent container = content.As<CommonPart>().Container;
            var attendeeModel = _orchardServices.ContentManager.BuildEditor(content);
            JObject attendee = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(new JObject(), attendeeModel);

            if (inModel != null)
            {

                if (inModel["AttendState"] != null && (attendee["AttendState"] == null  || attendee["AttendState"].Type == JTokenType.Null || (bool)inModel["AttendState"] != (bool)attendee["AttendState"]))
                    return null;

                if (inModel["IsAttendFee"] != null && (attendee["IsAttendFee"] == null || attendee["IsAttendFee"].Type == JTokenType.Null || (bool)inModel["IsAttendFee"] != (bool)attendee["IsAttendFee"]))
                    return null;
            }

            attendee.Add(new JProperty("Id", content.Id));
            attendee.Add(new JProperty("CreatedUtc", common.CreatedUtc));

            if (withUser)
            {
                IUser user = common.Owner;
                var userModel = _orchardServices.ContentManager.BuildEditor(user);
                attendee.Add(new JProperty("User", Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(JObject.FromObject(user), userModel)));
            }

            object containerModel = null;
            if (container != null)
            {
                SchedulePart schedule = container.As<SchedulePart>();
                containerModel = _calendarService.GetOccurrenceViewModel(new ScheduleOccurrence(schedule, schedule.StartDate), new ScheduleData(container.ContentItem, Url, _slugService, _orchardServices), false);
            }
            attendee.Add(new JProperty("Container", containerModel));

            if (inModel != null && inModel["Place"] != null)
            {
                string place = inModel["Place"].ToString();
                JObject containerJModel = JObject.FromObject(containerModel);
                if (!inModel["Place"].ToString().Equals(containerJModel["Place"].ToString()))
                    return null;
            }


            return attendee;
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

            allContentItems = allContentItems.Select(c => c.As<CommonPart>().Container.ContentItem); ;
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
                .Select(a => getAttendee(a, null, false));
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
