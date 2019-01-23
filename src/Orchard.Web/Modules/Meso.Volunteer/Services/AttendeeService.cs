using System.Collections.Generic;
using System.Linq;
using Orchard.Schedule.Models;
using Orchard.ContentManagement;
using Orchard.Projections.Services;
using Orchard;
using Orchard.Core.Common.Models;
using Orchard.Projections.Models;
using Orchard.Core.Containers.Services;
using Orchard.Security;
using Orchard.Schedule.Services;
using Newtonsoft.Json.Linq;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Autoroute.Services;
using System.Web.Http.Routing;

namespace Meso.Volunteer.Services { 
        
    public class AttendeeService : IAttendeeService
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IProjectionManager _projectionManager;
        private readonly IContainerService _containerService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IRoleService _roleService;
        private readonly ICalendarService _calendarService;
        private readonly ISlugService _slugService;

        public AttendeeService(IOrchardServices services, 
            IContainerService containerService, 
            IProjectionManager projectionManager, 
            IScheduleLayoutService scheduleLayoutService,
            IRoleService roleService,
            ICalendarService calendarService,
            ISlugService slugService) {
            _orchardServices = services;
            _containerService = containerService;
            _projectionManager = projectionManager;
            _scheduleLayoutService = scheduleLayoutService;
            _roleService = roleService;
            _calendarService = calendarService;
            _slugService = slugService;
        }

        public IEnumerable<ContentItem> GetAttendees(IUser user,  QueryModel query, string contentType = null)
        {
            IEnumerable<ContentItem> allContentItems = null;
            UserRolesPart rolesPart = user.As<UserRolesPart>();

            if (rolesPart != null)
            {
                string queryName = "";
                IEnumerable<string> userRoles = rolesPart.Roles;
                foreach (var role in userRoles)
                {
                    foreach (var permissionName in _roleService.GetPermissionsForRoleByName(role))
                    {
                        string possessedName = permissionName;
                        if (query != null)
                            contentType = query.Name;

                        if (possessedName.StartsWith("View_" + contentType))
                        {
                            queryName = possessedName.Substring("View_".Length);
                            if (!possessedName.EndsWith("Cancel"))
                            {
                                IEnumerable<ContentItem> contentItems = Enumerable.Empty<ContentItem>();
                                if (query != null)
                                    contentItems = _projectionManager.GetContentItems(new QueryModel { Name = queryName });
                                else if (!string.IsNullOrEmpty(queryName))
                                {
                                    if (_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules))
                                    {
                                        contentItems = _orchardServices.ContentManager.Query(VersionOptions.Latest, queryName).List();
                                    }
                                    else
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
            }

            return allContentItems;
        }

        public JObject GetAttendee(UrlHelper Url, IContent content, JObject inModel = null, bool withUser = true)
        {
            CommonPart common = content.As<CommonPart>();
            IContent container = content.As<CommonPart>().Container;
            var attendeeModel = _orchardServices.ContentManager.BuildEditor(content);
            JObject attendee = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(new JObject(), attendeeModel);

            if (inModel != null)
            {

                if (inModel["AttendState"] != null && (attendee["AttendState"] == null || attendee["AttendState"].Type == JTokenType.Null || (bool)inModel["AttendState"] != (bool)attendee["AttendState"]))
                    return null;

                if (inModel["IsAttendFee"] != null && (attendee["IsAttendFee"] == null || attendee["IsAttendFee"].Type == JTokenType.Null || (bool)inModel["IsAttendFee"] != (bool)attendee["IsAttendFee"]))
                    return null;
            }

            attendee.Add(new JProperty("Id", content.Id));
            attendee.Add(new JProperty("CreatedUtc", common.CreatedUtc));

            if (withUser)
            {
                IUser user = common.Owner;
                if (user != null)
                {
                    var userModel = _orchardServices.ContentManager.BuildEditor(user);
                    attendee.Add(new JProperty("User", Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(JObject.FromObject(user), userModel)));
                }
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
    }
}