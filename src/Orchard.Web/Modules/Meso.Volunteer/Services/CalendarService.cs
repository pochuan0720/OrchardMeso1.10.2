using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Schedule.Models;
using Orchard.Environment.Extensions;
using Orchard.ContentManagement;
using Orchard.Projections.Services;
using Orchard.Schedule.ViewModels;
using Orchard;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Projections.Models;
using Orchard.Core.Containers.Services;
using Orchard.Core.Containers.Models;
using Orchard.Security;
using Orchard.Schedule.Services;
using Meso.Volunteer.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard.PublishLater.Models;
using Meso.Volunteer.Handlers;
using Orchard.Layouts.Models;
using Orchard.DynamicForms.Services;
using Orchard.Localization.Services;
using Orchard.Users.Models;
using Orchard.Roles.Models;
using Orchard.Localization.Models;

namespace Meso.Volunteer.Services {

    using Occurrence = Dictionary<string, object>;   
        
    public class CalendarService : ICalendarService
    {
        private readonly IOrchardServices _services;
        private readonly IProjectionManager _projectionManager;
        private readonly IContainerService _containerService;
        private readonly IScheduleLayoutService _scheduleLayoutService;
        private readonly IFormService _formService;
        private readonly ICalendarUpdateModelHandler _updateModelHandler;
        private readonly IDateLocalizationServices _dateLocalizationServices;
        private readonly IWorkContextAccessor _accessor;

        public CalendarService(IOrchardServices services, 
            IContainerService containerService, 
            IProjectionManager projectionManager, 
            IScheduleLayoutService scheduleLayoutService,
            IFormService formService,
            ICalendarUpdateModelHandler updateModelHandler,
            IDateLocalizationServices dateLocalizationServices,
            IWorkContextAccessor accessor) {
            _services = services;
            _containerService = containerService;
            _projectionManager = projectionManager;
            _scheduleLayoutService = scheduleLayoutService;
            _formService = formService;
            _dateLocalizationServices = dateLocalizationServices;
            _updateModelHandler = updateModelHandler;
            _accessor = accessor;
        }

        public Occurrence GetOccurrenceObject(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData) {
            return _scheduleLayoutService.GetOccurrenceObject(scheduleEvent, scheduleData);
        }

        public object GetOccurrenceViewModel(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData, bool withAttendee)
        { 
            DateTime? publishLater = scheduleEvent.Source.As<PublishLaterPart>().ScheduledPublishUtc.Value;
            ContainerPart containerPart = scheduleEvent.Source.As<ContainerPart>();
            LayoutPart layoutPart = scheduleEvent.Source.As<LayoutPart>();
            SchedulePart schedule = scheduleEvent.Source.As<SchedulePart>();
            object obj = null;
            
            if (containerPart != null && withAttendee)
            {
                IList<string> selectedItemContentTypes = containerPart.ItemContentTypes.Select(x => x.Name).ToList();


                IList<object> list = new List<object>();
                if (containerPart.ItemCount > 0)
                {
                    var contentItems = _containerService.GetContentItems(containerPart.Id);
                    foreach (ContentItem _item in contentItems)
                    {
                        CommonPart common = _item.As<CommonPart>();
                        IUser user = common.Owner;
                        var userModel = _services.ContentManager.BuildEditor(user);
                        var attendeeModel = _services.ContentManager.BuildEditor(_item);
                        JObject attendee = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(new JObject(), attendeeModel);
                        attendee.Add(new JProperty("Id", _item.Id));
                        attendee.Add(new JProperty("CreatedUtc", common.CreatedUtc));
                        attendee.Add(new JProperty("User", Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(JObject.FromObject(user), userModel)));
                        list.Add(attendee);
                    }
                }

                obj = new
                {
                    Id = scheduleData.Id,
                    Title = scheduleData.Title,
                    Body = scheduleData.Body,
                    StartDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.Start, schedule.TimeZone),
                    EndDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.End, schedule.TimeZone),
                    IsPublished = schedule.IsPublished,
                    PublishLater = publishLater == null ? publishLater : (DateTime)publishLater,
                    Attendee = list.ToArray(),
                    Container = selectedItemContentTypes.ToArray<string>()
                };
                
            }
            else if(layoutPart != null && withAttendee)
            {
                var submissions = _formService.GetSubmissions(scheduleData.Id.ToString());
                obj = new
                {
                    Id = scheduleData.Id,
                    Title = scheduleData.Title,
                    Body = scheduleData.Body,
                    StartDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.Start, schedule.TimeZone),
                    EndDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.End, schedule.TimeZone),
                    IsPublished = schedule.IsPublished,
                    PublishLater = publishLater == null ? publishLater : (DateTime)publishLater,
                    AttendeeCount = submissions.Count()
                };
            }
            else
            {
                obj = new
                {
                    Id = scheduleData.Id,
                    Title = scheduleData.Title,
                    Body = scheduleData.Body,
                    StartDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.Start, schedule.TimeZone),
                    EndDate = TimeZoneInfo.ConvertTimeToUtc(scheduleEvent.End, schedule.TimeZone),
                    IsPublished = schedule.IsPublished,
                    PublishLater = publishLater == null ? publishLater : (DateTime)publishLater
                };
            }

            var model = _services.ContentManager.BuildEditor(schedule); ;
            return CalendarUpdateModelHandler.GetData(JObject.FromObject(obj), model);
        }

        public bool DateInRange(SchedulePart part, DateTime start, DateTime end) {
            return _scheduleLayoutService.DateInRange(part, start, end);
        }
        
        public bool DateInFuture(SchedulePart part) {
            return _scheduleLayoutService.DateInFuture(part);
        }

        public IEnumerable<ContentItem> GetProjectionContentItems(QueryModel query)
        {
            IEnumerable<ContentItem> results = null;
            try
            {
                results = _projectionManager.GetContentItems(query);
                if (results == null) return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            results = results.Where(c => c.Has<SchedulePart>());
            if (results.Count() == 0) return null;

            return results;
        }

        public IEnumerable<ContentItem> GetProjectionContentItems(int queryId) {
            return _scheduleLayoutService.GetProjectionContentItems(queryId);
        }

        public void Notification(ContentItem content, string contentType, JObject obj)
        {
            if (obj == null)
                obj = new JObject();

            //New a Cancel Content
            var cancelItem = _services.ContentManager.New<ContentPart>(contentType);

            CommonPart common = content.As<CommonPart>();
            SchedulePart schedulePart = content.As<SchedulePart>();
            if(schedulePart == null)
                schedulePart = common.Container.As<SchedulePart>();
            var scheduleModel = _services.ContentManager.BuildEditor(schedulePart);

            JObject scheduleObject = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(JObject.FromObject(schedulePart), scheduleModel);
            ScheduleOccurrence occurrence = new ScheduleOccurrence(schedulePart, schedulePart.StartDate);
            IUser user = common.Owner;

            _services.ContentManager.Create(cancelItem, VersionOptions.Draft);
            //AttendeeCancelViewMode cancelModel = new AttendeeCancelViewMode();
            string people = schedulePart.As<ContainerPart>().ItemCount + "/" + scheduleObject["VolunteerQuota"].ToString();
            string place = scheduleObject["Place"].ToString();

            //Title
            string title = "";
            if (scheduleObject["Item"] != null)
            {
                string item = scheduleObject["Item"].ToString();
                if (item.Equals("帶隊解說") || item.Equals("各項活動支援")) //申請單位 XX(單位名稱) X/X人
                {
                    title = scheduleObject["ApplyUnit"].ToString() + " " + people;
                }
                else //XX(地區)駐站 X/X人(保育全部套用此規則)
                    title = place + " 駐站 " + people;
            }
            else
                title = schedulePart.Title;

            obj.Add(new JProperty("Title", title));
            //Owner
            obj.Add(new JProperty("Owner", user.UserName));

            var userModel = _services.ContentManager.BuildEditor(user);

            JObject userObject = Orchard.Core.Common.Handlers.UpdateModelHandler.GetData(JObject.FromObject(user), userModel);
            obj.Add(new JProperty("ContentId", content.Id));
            obj.Add(new JProperty("Name", userObject["Name"]));
            obj.Add(new JProperty("Place", place));
            obj.Add(new JProperty("StartDate", _dateLocalizationServices.ConvertToLocalizedString(occurrence.Start, ParseFormat, new DateLocalizationOptions())));
            obj.Add(new JProperty("EndDate", _dateLocalizationServices.ConvertToLocalizedString(occurrence.End, ParseFormat, new DateLocalizationOptions())));

            //mailto list
            IList<string> roles = user.ContentItem.As<UserRolesPart>().Roles;
            var users = _services.ContentManager.Query<UserPart, UserPartRecord>().List();
            IEnumerable<string> alluserEmails = null;
            IEnumerable<string> allAdminEmails = null;
            foreach (string _role in roles)
            {
                string role = _role;
                if (role.EndsWith("管理員"))
                    role = role.Substring(0, role.Length - 3);

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
            obj.Add(new JProperty("MailTo", mailTo));
            obj.Add(new JProperty("MailToAdmin", mailToAdmin));

            var editorShape = _services.ContentManager.UpdateEditor(cancelItem, _updateModelHandler.SetData(obj));
            _services.ContentManager.Publish(cancelItem.ContentItem);
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