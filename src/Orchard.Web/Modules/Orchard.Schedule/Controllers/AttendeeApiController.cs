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
using Orchard.Projections.Services;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Projections.Models;
using Orchard.Core.Containers.Models;
using Orchard.Localization.Services;
using Orchard.Localization.Models;
using Orchard.Users.Models;

namespace Orchard.Schedule.Controllers
{
    [Authorize]
    public class AttendeeApiController : ApiController
    {
        private readonly IRoleService _roleService;
        private readonly IScheduleService _scheduleService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IOrchardServices _orchardServices;
        private readonly IContainerService _containerService;
        private readonly ISlugService _slugService;
        private readonly IUpdateModelHandler _updateModelHandler;
        private readonly IAuthenticationService _authenticationService;
        private readonly IProjectionManager _projectionManager;
        private readonly IDateLocalizationServices _dateLocalizationServices;
        private readonly IWorkContextAccessor _accessor;
        private static DateTime UnixEpochTime = new DateTime(1970, 1, 1);


        public AttendeeApiController(
            IRoleService roleService,
            IScheduleService scheduleService,
            IScheduleLayoutService scheduleLayoutService,
            IOrchardServices orchardServices,
            IContainerService containerService,
            ISlugService slugService,
            IShapeFactory shapeFactory,
            IUpdateModelHandler updateModelHandler,
            IAuthenticationService authenticationService,
            IProjectionManager projectionManager,
            IDateLocalizationServices dateLocalizationServices,
            IWorkContextAccessor accessor
            )
        {
            _roleService = roleService;
            _scheduleService = scheduleService;
            _scheduleLayoutService = scheduleLayoutService;
            _orchardServices = orchardServices;
            _containerService = containerService;
            _slugService = slugService;
            _updateModelHandler = updateModelHandler;
            _authenticationService = authenticationService;
            _projectionManager = projectionManager;
            _dateLocalizationServices = dateLocalizationServices;
            _accessor = accessor;
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
                list.Add(new { Id = item.Id , CreatedUtc = common.CreatedUtc, User = new { Id = user.Id, UserName = user.UserName, Email = user.Email, Data = Core.Common.Handlers.UpdateModelHandler.GetData(userModel) }, Data = Core.Common.Handlers.UpdateModelHandler.GetData(attendeeModel) });
            }

            return Ok(new ResultViewModel { Content = list, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {

            if (inModel["Id"] == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get((int)inModel["Id"]);//, VersionOptions.DraftRequired);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            //var contentModel = _orchardServices.ContentManager.BuildEditor(content);

            CommonPart common = content.As<CommonPart>();
            IUser user = common.Owner;
            var userModel = _orchardServices.ContentManager.BuildEditor(user);
            var attendeeModel = _orchardServices.ContentManager.BuildEditor(content);

            return Ok(new ResultViewModel { Content = new { Id = content.Id, CreatedUtc = common.CreatedUtc, User = new { UserName = user.UserName, Email = user.Email, Data = Core.Common.Handlers.UpdateModelHandler.GetData(userModel) }, Data = Core.Common.Handlers.UpdateModelHandler.GetData(attendeeModel) }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(AttendeeEditApiViewMode inModel)
        {
            if (inModel == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            //if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, T("Not authorized to manage content")))
            //    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            var containerItem = _orchardServices.ContentManager.Get(inModel.ContainerId, VersionOptions.Published);

            if (containerItem == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });



            IUser user = _authenticationService.GetAuthenticatedUser();

            // 1. 檢查是否可參加(一年允許取消六次)
            int cancelCount = _orchardServices.ContentManager.Query(VersionOptions.Published, inModel.ContentType + "Cancel").List()
                .Where(i => i.As<CommonPart>().Owner.Id == user.Id && ((DateTime)i.As<CommonPart>().PublishedUtc).ToString("yyyy").Equals(DateTime.UtcNow.ToString("2019"))).Count();

            if (cancelCount > 6)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.Forbidden) });

            // 2. 檢查剩餘可參與額度
            //Get Scheduler model
            SchedulePart schedule = containerItem.As<SchedulePart>();
            ScheduleApiViewMode outModel = _scheduleLayoutService.GetOccurrenceViewModel(new ScheduleOccurrence(schedule, schedule.StartDate), new ScheduleData(containerItem, Url, _slugService, _orchardServices));
            int volunteerQuota = (int)outModel.Data[containerItem.ContentType + ".VolunteerQuota"];
            if (outModel.Attendee.Length >= volunteerQuota)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.Forbidden) });

            // 3. 檢查是否已經參加

            foreach ( object obj in outModel.Attendee)
            {
                JObject attendee = JObject.FromObject(obj);
                string userName = attendee["User"]["UserName"].ToString();
                if(userName.Equals(user.UserName))
                    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.Conflict) });
            }

            var content = _orchardServices.ContentManager.New<ContentPart>(contentType);

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
        public IHttpActionResult update(AttendeeEditApiViewMode inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get(inModel.Id);//, VersionOptions.DraftRequired);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, content, T("Couldn't edit content")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit content" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null || !contentType.Equals(content.ContentType))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });


            _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));

            _orchardServices.ContentManager.Publish(content);
            _orchardServices.Notifier.Information(T("schedule information updated"));

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(AttendeeEditApiViewMode inModel)
        {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var content = _orchardServices.ContentManager.Get(inModel.Id, VersionOptions.Latest);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            bool other = _orchardServices.Authorizer.Authorize(Orchard.Core.Contents.Permissions.PublishContent, content, T("Couldn't Unpublish content"));

            if (!_orchardServices.Authorizer.Authorize(Orchard.Core.Contents.Permissions.PublishContent, content, T("Couldn't Unpublish content")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit content" });

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
                    string cancelContentType = inModel.ContentType + "Cancel";
                    var calcelItem = _orchardServices.ContentManager.New<ContentPart>(cancelContentType);

                    if (calcelItem != null)
                    {
                        CommonPart common = content.As<CommonPart>();
                        SchedulePart schedulePart = common.Container.As<SchedulePart>();
                        var scheduleModel = _orchardServices.ContentManager.BuildEditor(schedulePart);
                        JObject scheduleObject = Core.Common.Handlers.UpdateModelHandler.GetData(scheduleModel);
                        ScheduleOccurrence occurrence = new ScheduleOccurrence(schedulePart, schedulePart.StartDate);
                        IUser user = common.Owner;

                        _orchardServices.ContentManager.Create(calcelItem, VersionOptions.Draft);
                        AttendeeCancelViewMode cancelModel = new AttendeeCancelViewMode();
                        string people = common.Container.As<ContainerPart>().ItemCount + "/" + scheduleObject[schedulePart.ContentItem.ContentType + ".VolunteerQuota"].ToString();
                        string place = scheduleObject[schedulePart.ContentItem.ContentType + ".Place"].ToString();
                        if (scheduleObject[schedulePart.ContentItem.ContentType + ".Item"] != null)
                        {
                            string item = scheduleObject[schedulePart.ContentItem.ContentType + ".Item"].ToString();
                            if(item.Equals("帶隊解說") || item.Equals("各項活動支援")) //申請單位 XX(單位名稱) X/X人
                            {
                                cancelModel.Title = scheduleObject[schedulePart.ContentItem.ContentType + ".ApplyUnit"].ToString() +" " + people;
                            }
                            else //XX(地區)駐站 X/X人(保育全部套用此規則)
                                cancelModel.Title = place + " 駐站 " + people;
                        }
                        else
                            cancelModel.Title = schedulePart.Title;

                        cancelModel.Owner = user.UserName;
                        JObject obj = new JObject();
                        var userModel = _orchardServices.ContentManager.BuildEditor(user);
                        JObject userObject = Core.Common.Handlers.UpdateModelHandler.GetData(userModel);
                        obj.Add(new JProperty(cancelContentType + ".AttendeeId", content.Id));
                        obj.Add(new JProperty(cancelContentType + ".Name", userObject["User.Name"]));
                        obj.Add(new JProperty(cancelContentType + ".Email", user.Email));
                        obj.Add(new JProperty(cancelContentType + ".Place", place));
                        obj.Add(new JProperty(cancelContentType + ".StartDate", _dateLocalizationServices.ConvertToLocalizedString(occurrence.Start, ParseFormat, new DateLocalizationOptions())));
                        obj.Add(new JProperty(cancelContentType + ".EndDate", _dateLocalizationServices.ConvertToLocalizedString(occurrence.End, ParseFormat, new DateLocalizationOptions())));

                        //mailto list
                        IList<string> roles = user.ContentItem.As<UserRolesPart>().Roles;
                        var users = _orchardServices.ContentManager.Query<UserPart, UserPartRecord>().List();
                        IEnumerable<string> alluserEmails = null;
                        IEnumerable<string> allAdminEmails = null;
                        foreach (string role in roles)
                        {
                            IEnumerable<string> userEmails = null;
                            userEmails = users.Where(i => i.ContentItem.As<UserRolesPart>().Roles.Contains(role)).Select(x => x.Email);
                            if (alluserEmails == null)
                                alluserEmails = userEmails;
                            else
                                alluserEmails = alluserEmails.Select(x => x).Concat(userEmails.Select(y => y));

                            IEnumerable<string> adminEmails = null;
                            adminEmails = users.Where(i => i.ContentItem.As<UserRolesPart>().Roles.Contains(role + "管理員")).Select(x => x.Email);
                            if (allAdminEmails == null)
                                allAdminEmails = adminEmails;
                            else
                                allAdminEmails = allAdminEmails.Select(x => x).Concat(adminEmails.Select(y => y));
                        }

                        var mailTo = String.Join(";", alluserEmails.ToList().ToArray());
                        var mailToAdmin = String.Join(";", allAdminEmails.ToList().ToArray());
                        obj.Add(new JProperty(cancelContentType + ".MailTo", mailTo));
                        obj.Add(new JProperty(cancelContentType + ".MailToAdmin", mailToAdmin));
                        cancelModel.Data = obj;



                        var editorShape = _orchardServices.ContentManager.UpdateEditor(calcelItem, _updateModelHandler.SetData(cancelModel));
                        _orchardServices.ContentManager.Publish(calcelItem.ContentItem);


                        return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
                    }
                } catch(Exception e)
                {
                    _orchardServices.ContentManager.Publish(content);
                    _orchardServices.Notifier.Information(T("content Published"));
                    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
                }
            }

            return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

        }

        [HttpPost]
        public IHttpActionResult self(SchedulesIndexApiViewMode inModel)
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
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            //IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(inModel.Query);
            //IEnumerable<ContentItem> contentItems = _containerService.GetContentItems((int)inModel["ContainerId"]);

            //contentItems = contentItems.Select(c => c.As<CommonPart>().Container.ContentItem).Where(x => x.As<CommonPart>().Owner.Id == user.Id);

            allContentItems = allContentItems.Select(c => c.As<CommonPart>()).Where(x => x.Owner.Id == user.Id && x.Container != null).Select(c => c.Container.ContentItem);
            if (allContentItems == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


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

            var scheduleOccurrences = allContentItems
                .Select(c => c.As<SchedulePart>())
                .Where(s => _scheduleLayoutService.DateInRange(s, inModel.StartDate, inModel.EndDate))
                .SelectMany(c => _scheduleService.GetOccurrencesForDateRange(c, inModel.StartDate, inModel.EndDate))
                .OrderBy(o => o.Start);

            var occurrences = scheduleOccurrences.Select(o => _scheduleLayoutService.GetOccurrenceViewModel(o, ScheduleMap[o.Source])).ToList();



            return Ok(new ResultViewModel { Content = occurrences, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult cancellist(SchedulesIndexApiViewMode inModel)
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
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            //ContentItem.VersionRecord != null && content.ContentItem.VersionRecord.Published

            allContentItems = allContentItems.Select(c => c.As<CommonPart>().Container.ContentItem); ;
            allContentItems = allContentItems.GroupBy(x => x.Id).Select(g => g.First());

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
        private string GetContentType(string prefix, ref AttendeeEditApiViewMode inModel)
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
                        if (possessedName.StartsWith(prefixOthers + inModel.ContentType))
                        {
                            contentTypesOthers.Add(possessedName.Substring(prefixOthers.Length));
                        }
                        else if (possessedName.StartsWith(prefixOwner + inModel.ContentType))
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
                return inModel.ContentType;
        }



        private string _dateFormat;
        private string DateFormat
        {
            get { return _dateFormat ?? (_dateFormat = _accessor.GetContext().CurrentSite.As<ScheduleSettingsPart>().DateFormat); }
        }

        private string ParseFormat
        {
            get
            {
                switch (DateFormat)
                {
                    case "DMY":
                        return "dd/MM/yyyy";
                    case "MDY":
                        return "MM/dd/yyyy";
                    case "YMD":
                        return "yyyy/MM/dd";
                    default:
                        return "MM/dd/yyyy";
                }
            }
        }


    }


}
