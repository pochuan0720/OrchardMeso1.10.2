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
            IProjectionManager projectionManager
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

            var contentModel = _orchardServices.ContentManager.BuildEditor(content);

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

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, T("Not authorized to manage content")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });

            string contentType = GetContentType("Edit", ref inModel);

            if (contentType == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage content" });


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

            var content = _orchardServices.ContentManager.Get(inModel.Id);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageSchedules, content, T("Couldn't delete content")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete content" });

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
                return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }

            return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

        }

        [HttpPost]
        public IHttpActionResult self(SchedulesIndexApiViewMode inModel)
        {
            if (inModel == null || inModel.ContentType == null)
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
                        if (possessedName.StartsWith("View_" + inModel.ContentType))
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
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            //IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(inModel.Query);
            //IEnumerable<ContentItem> contentItems = _containerService.GetContentItems((int)inModel["ContainerId"]);

            //contentItems = contentItems.Select(c => c.As<CommonPart>().Container.ContentItem).Where(x => x.As<CommonPart>().Owner.Id == user.Id);
            allContentItems = allContentItems.Select(c => c.As<CommonPart>()).Where(x => x.Owner.Id == user.Id).Select(c => c.Container.ContentItem);
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
            if (inModel == null || inModel.ContentType == null)
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
                        if (possessedName.StartsWith("View_" + inModel.ContentType))
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
    }

}
