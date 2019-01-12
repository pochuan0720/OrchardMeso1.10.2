using Meso.Volunteer.Services;
using Meso.Volunteer.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.DynamicForms.Elements;
using Orchard.DynamicForms.Helpers;
using Orchard.DynamicForms.Models;
using Orchard.DynamicForms.Services;
using Orchard.Layouts.Models;
using Orchard.Layouts.Services;
using Orchard.Projections.Models;
using Orchard.Projections.Services;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Schedule.Models;
using Orchard.Schedule.Services;
using Orchard.Security;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;

namespace Meso.Volunteer.Controllers
{
    [Authorize]
    public class SurveyApiController : ApiController
    {
        private readonly IOrchardServices _orchardServices;
        private readonly ILayoutManager _layoutManager;
        private readonly IFormService _formService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMembershipService _membershipService;
        private readonly IRoleService _roleService;
        private readonly IProjectionManager _projectionManager;
        private readonly ICalendarService _calendarService;
        private readonly ISlugService _slugService;
        private readonly IScheduleService _scheduleService;


        public SurveyApiController(
            IOrchardServices orchardServices,
            ILayoutManager layoutManager,
            IFormService formService,
            IAuthenticationService authenticationService,
            IMembershipService membershipService,
            IRoleService roleService,
            IProjectionManager projectionManager,
            ICalendarService calendarService,
            ISlugService slugService,
            IScheduleService scheduleService)
        {
            _orchardServices = orchardServices;
            _layoutManager = layoutManager;
            _formService = formService;
            _authenticationService = authenticationService;
            _membershipService = membershipService;
            _roleService = roleService;
            _projectionManager = projectionManager;
            _calendarService = calendarService;
            _slugService = slugService;
            _scheduleService = scheduleService;
        }

        [HttpPost]
        public IHttpActionResult query(JObject inModel)
        {
            if (inModel == null || inModel["EventId"] == null)
                return BadRequest();

            int contentId = (int)inModel["EventId"];
            ContentItem content = _orchardServices.ContentManager.Get(contentId, VersionOptions.Published);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            string formName = content.Id.ToString();

            var submissions = _formService.GetSubmissions(formName);
            //var list = submissions.Select(s => new { Id = s.Id, CreatedUtc = s.CreatedUtc, Form = HttpUtility.ParseQueryString(s.FormData) }).ToList();
            var list = submissions.Select(s => new { Id = s.Id, CreatedUtc = s.CreatedUtc, Form = ToDictionary(HttpUtility.ParseQueryString(s.FormData), true, inModel["Filter"]) }).Where(x=>x.Form != null).ToList();
            return Ok(new ResultViewModel { Content = list, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private Dictionary<string, object> ToDictionary(NameValueCollection nvc, bool isDetailOwner = false, JToken filter = null)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            if (filter != null && filter["UserName"] != null && !string.IsNullOrEmpty(filter["UserName"].ToString())  && nvc["Owner"] != null && !filter["UserName"].ToString().Equals(nvc["Owner"]))
            {
                
                return null;
            }

            foreach (var key in nvc.AllKeys)
            {
                string value = string.Join(",", nvc.GetValues(key));
                if (value.Contains(","))
                    dict.Add(key, value.Split(','));
                else
                {
                    if(key.Equals("Owner") && isDetailOwner)
                    {
                        IUser user = _membershipService.GetUser(value);
                        var model = _orchardServices.ContentManager.BuildEditor(user);
                        JObject obj = UpdateModelHandler.GetData(JObject.FromObject(user), model);
                        dict.Add(key, new { UserName = user .UserName, Name = obj["Name"] });
                    }
                    else
                        dict.Add(key, value);
                }

            }

            return dict;
        }

        [HttpPost]
        public IHttpActionResult create(JObject inModel)
        {
            if (inModel == null || inModel["EventId"] == null || inModel["Form"] == null)
                return BadRequest();

            int contentId = (int)inModel["EventId"];
            ContentItem content = _orchardServices.ContentManager.Get(contentId, VersionOptions.Published);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            LayoutPart layout = content.As<LayoutPart>();
            if (layout == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var model = _orchardServices.ContentManager.BuildEditor(content);
            JObject Data = UpdateModelHandler.GetData(JObject.FromObject(content), model);
            DateTime applyStartDate = (DateTime)Data["ApplyStartDate"];
            DateTime applyEndDate = (DateTime)Data["ApplyEndDate"];
            DateTime now = DateTime.UtcNow;
            if (now < applyStartDate || now > applyEndDate.AddDays(1))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "非可參加期限內" });

            int peopleQuota = (int)Data["PeopleQuota"];
            string formName = content.Id.ToString();
            var submissions = _formService.GetSubmissions(formName);

            //額滿
            if (submissions.Count() >= peopleQuota)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "額度已滿" });

            IUser user = _authenticationService.GetAuthenticatedUser();

            IList<NameValueCollection> list = submissions.Select(s => HttpUtility.ParseQueryString(s.FormData)).Where(nv => nv.AllKeys.Contains("Owner") && nv.GetValues("Owner").Contains(user.Id.ToString())).ToList();
            if (list.Count > 0)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "此帳號已經參加" });

            //0.1 判斷是否與其他活動重疊
            SchedulePart _schedule = content.As<SchedulePart>();
            ScheduleOccurrence _occurrence = _scheduleService.GetNextOccurrence(_schedule, _schedule.StartDate);

            IEnumerable<ContentItem> eventItems = _orchardServices.ContentManager.Query(VersionOptions.Published, content.ContentType).List();
            //找前後一個月的活動
            eventItems = eventItems.Where(s => _calendarService.DateInRange(s.As<SchedulePart>(), _occurrence.Start.AddMonths(-1), _occurrence.End.AddMonths(1)));
            eventItems = eventItems
                .Where(x => _formService.GetSubmissions(x.Id.ToString()).Select(f => _calendarService.FormDataToDictionary(HttpUtility.ParseQueryString(f.FormData))).Where(d => d["Owner"] != null && d["Owner"].Equals(user.Id.ToString())).FirstOrDefault() != null);

            if (eventItems != null)
            {
                var collectionEvent = eventItems
                    .Select(c => _scheduleService.GetNextOccurrence(c.As<SchedulePart>(), c.As<SchedulePart>().StartDate))
                    .Where(o => _calendarService.DateCollection(_occurrence, o.Start, o.End)).FirstOrDefault();

                if (collectionEvent != null)
                {
                    object outModel = _calendarService.GetOccurrenceViewModel(collectionEvent, new ScheduleData(collectionEvent.Source.ContentItem, Url, _slugService, _orchardServices), false);
                    return Ok(new ResultViewModel { Content = outModel, Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "與其它行程衝突" });
                }
            }

            //0.2 判斷是否與其他認養重疊
            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = content.ContentType.Replace("Event","Attendee") });
            contentItems = contentItems.Where(x => x.As<CommonPart>().Owner != null
                    && x.As<CommonPart>().Container != null
                    && x.As<CommonPart>().Owner.Id == user.Id);

            var collection = contentItems
                .Where(s => _calendarService.DateInRange(s.As<CommonPart>().Container.As<SchedulePart>(), _occurrence.Start.AddMonths(-1), _occurrence.End.AddMonths(1)))
                .Select(c => _scheduleService.GetNextOccurrence(c.As<CommonPart>().Container.As<SchedulePart>(), c.As<CommonPart>().Container.As<SchedulePart>().StartDate))
                .Where(o => _calendarService.DateCollection(_occurrence, o.Start, o.End)).FirstOrDefault();

            if (collection != null)
            {
                object outModel = _calendarService.GetOccurrenceViewModel(collection, new ScheduleData(collection.Source.ContentItem, Url, _slugService, _orchardServices), false);
                return Ok(new ResultViewModel { Content = outModel, Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "與其它行程衝突" });
            }

            var layoutPart = _layoutManager.GetLayout(layout.Id);
            var form = _formService.FindForm(layoutPart, formName);
            if(form == null)
                return InternalServerError();

            JToken token = inModel["Form"];
            //var dict = token.Children().Cast<JProperty>().ToDictionary(jp => jp.Name, jp => getValue(jp.Value));

            NameValueCollection nvc = new NameValueCollection(token.Children().Count() + 1);
            foreach (JProperty k in token)
            {
                if (k.Value is JArray)
                    nvc.Add(k.Name, String.Join(",", k.Value));
                else
                    nvc.Add(k.Name, k.Value.ToString());

            }
            if (!ValidatForm(nvc, form))
                return InternalServerError();


            nvc.Add("Owner", user.Id.ToString());

            Submission submission = _formService.CreateSubmission(contentId.ToString(), nvc);

            return Ok(new ResultViewModel { Content = submission, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult update(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null || inModel["EventId"] == null || inModel["Form"] == null)
                return BadRequest();

            int eventId = (int)inModel["EventId"];
            ContentItem content = _orchardServices.ContentManager.Get(eventId, VersionOptions.Published);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "活動不存在" });


            LayoutPart layout = content.As<LayoutPart>();
            if (layout == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Forbidden.ToString("d"), Message = "表單不存在" });

            /*var model = _orchardServices.ContentManager.BuildEditor(content);
            JObject Data = UpdateModelHandler.GetData(JObject.FromObject(content), model);
            DateTime applyStartDate = (DateTime)Data["ApplyStartDate"];
            DateTime applyEndDate = (DateTime)Data["ApplyEndDate"];
            if (DateTime.UtcNow < applyStartDate || DateTime.UtcNow > applyEndDate)
                return InternalServerError();*/

            int id = (int)inModel["Id"];

            //IUser user = _authenticationService.GetAuthenticatedUser();

            string formName = content.Id.ToString();
            var layoutPart = _layoutManager.GetLayout(layout.Id);
            var form = _formService.FindForm(layoutPart, formName);

            JToken token = inModel["Form"];
            //var dict = token.Children().Cast<JProperty>().ToDictionary(jp => jp.Name, jp => getValue(jp.Value));

            NameValueCollection nvc = new NameValueCollection(token.Children().Count() + 1);
            foreach (JProperty k in token)
            {
                if (k.Value is JArray)
                    nvc.Add(k.Name, String.Join(",", k.Value));
                else
                    nvc.Add(k.Name, k.Value.ToString());

            }
            if (!ValidatForm(nvc, form))
                return InternalServerError();

            var submission = _formService.GetSubmission(id);
            var Form = ToDictionary(HttpUtility.ParseQueryString(submission.FormData), false);

            nvc.Add("Owner", (string)Form["Owner"]);
            submission.FormData = NameValueCollectionExtensions.ToQueryString(nvc);
            _formService.UpdateSubmission(submission);

            return Ok(new ResultViewModel { Content = new { Id = submission .Id}, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private bool ValidatForm(NameValueCollection values, Form form)
        {
            var Options = form.Elements.Where(x => x.Data.ContainsKey("InputName")).ToDictionary(y => y.Data["InputName"], y => y.Data.ContainsKey("Options") ? y.Data["Options"].Split('\n').ToList() : null);

            foreach (var key in values.AllKeys)
            {
                if (Options.Keys.Contains(key))
                {
                    //只處理有Options的
                    if (Options[key] != null)
                    {
                        //處理NumericField
                        if (Options[key].Count() == 1 && string.IsNullOrEmpty(Options[key][0]))
                        {
                            string answer = string.Join("", values.GetValues(key));
                            int i = 0;
                            bool result = int.TryParse(answer, out i); //i now = 108
                            if (!result)
                                return false;
                        }
                        else
                        {
                            string[] answers = string.Join(",", values.GetValues(key)).Split(',');
                            foreach (var a in answers)
                            {
                                bool result = Options[key].Contains(a);
                                if (!result)
                                    return false;
                            }
                        }
                    }
                }
                else
                    return false;
            }
            return true;
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();

            int contentId = (int)inModel["Id"];
            ContentItem content = _orchardServices.ContentManager.Get(contentId, VersionOptions.Latest);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            string formName = content.Id.ToString();

            var submission = _formService.GetSubmission(contentId);
            if (submission == null)
                return NotFound();

            return Ok(new ResultViewModel { Content = new { Id = submission.Id, EventId = Convert.ToInt32(submission.FormName), Form = ToDictionary(HttpUtility.ParseQueryString(submission.FormData), true) }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
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
                            IEnumerable<ContentItem> contentItems;

                            if (_orchardServices.Authorizer.Authorize(Orchard.Schedule.Permissions.ManageSchedules))
                            {
                                contentItems = _orchardServices.ContentManager.Query(VersionOptions.Latest, queryName).List();
                            }
                            else
                            {
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

            //_orchardServices.ContentManager.Query("Form").Where<CommonPartRecord>(cr => cr.OwnerId == user.Id);

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

            var occurrences = scheduleOccurrences.Select(o => _calendarService.GetOccurrenceViewModel(o, ScheduleMap[o.Source], true, user.Id)).Where(o => o != null).ToList();

            return Ok(new ResultViewModel { Content = occurrences, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }


        [HttpPost]
        public IHttpActionResult statistics(JObject inModel)
        {
            if (inModel == null || inModel["EventId"] == null)
                return BadRequest();

            int contentId = (int)inModel["EventId"];
            ContentItem content = _orchardServices.ContentManager.Get(contentId, VersionOptions.Published);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            LayoutPart layout = content.As<LayoutPart>();
            if (layout == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });
    
            string formName = content.Id.ToString();
            var submissions = _formService.GetSubmissions(formName);
            var list = submissions.Select(s => HttpUtility.ParseQueryString(s.FormData)).ToList();

            NameValueCollection allData = new NameValueCollection();
            foreach(var item in list)
            {
                foreach(var key in item.AllKeys)
                {
                    string[] values = item[key].Split(',');
                    foreach (string value in values)
                        allData.Add(key, value);

                }
            }
            var layoutPart = _layoutManager.GetLayout(layout.Id);
            var form = _formService.FindForm(layoutPart, formName);



            var Options = form.Elements.Where(x => x.Data.ContainsKey("Options")).ToDictionary(y => y.Data["InputName"], y => y.Data["Options"].Split('\n'));
            JObject obj = new JObject();
            foreach(var option in Options)
            {
                int total = allData.GetValues(option.Key).Count();
                JObject objAnswer = new JObject();
                double start = 1;
                for (int i=0;i< option.Value.Length;i++)// answer in option.Value)
                {
                    JObject objData = new JObject();
                    string answer = option.Value[i];
                    if (string.IsNullOrEmpty(answer))
                    {
                        List<string> a = allData.GetValues(option.Key).ToList();
                        int count = allData.GetValues(option.Key).Select(x => int.Parse(x)).ToList().Sum();
                        objAnswer.Add(new JProperty("Count", count));
                    }
                    else
                    {
                        List<string> a = allData.GetValues(option.Key).Where(x => x.Equals(answer)).ToList();
                        int count = a.Count();
                        double ratio = Math.Round((double)a.Count() / total, 2);// decimal.Round(a.Count() / total, 2);
                        JObject d = new JObject();
                        d.Add(new JProperty("Count", count));
                        if (i == option.Value.Length - 1)
                            d.Add(new JProperty("Ratio", Math.Round(start, 2)));
                        else
                            d.Add(new JProperty("Ratio", ratio));

                        objAnswer.Add(new JProperty(answer, d));
                        start = start - ratio;
                    }
                }

                obj.Add(new JProperty(option.Key, objAnswer));


            }


            return Ok(new ResultViewModel { Content = obj, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

    }
}
