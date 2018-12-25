using Meso.Volunteer.Services;
using Meso.Volunteer.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.ViewModels;
using Orchard.DynamicForms.Elements;
using Orchard.DynamicForms.Models;
using Orchard.DynamicForms.Services;
using Orchard.Layouts.Models;
using Orchard.Layouts.Services;
using Orchard.Projections.Models;
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
    public class SurveyApiController : ApiController
    {
        private readonly IOrchardServices _orchardServices;
        private readonly ILayoutManager _layoutManager;
        private readonly IFormService _formService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IMembershipService _membershipService;

        public SurveyApiController(
            IOrchardServices orchardServices,
            ILayoutManager layoutManager,
            IFormService formService,
            IAuthenticationService authenticationService,
            IMembershipService membershipService)
        {
            _orchardServices = orchardServices;
            _layoutManager = layoutManager;
            _formService = formService;
            _authenticationService = authenticationService;
            _membershipService = membershipService;
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
            var list = submissions.Select(s => new { Id = s.Id, CreatedUtc = s.CreatedUtc, Form = ToDictionary(HttpUtility.ParseQueryString(s.FormData)) }).ToList();
            return Ok(new ResultViewModel { Content = list, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private Dictionary<string, object> ToDictionary(NameValueCollection nvc)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (var key in nvc.AllKeys)
            {
                string value = string.Join(",", nvc.GetValues(key));
                if (value.Contains(","))
                    dict.Add(key, value.Split(','));
                else
                {
                    if(key.Equals("Owner"))
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
            if (DateTime.UtcNow < applyStartDate || DateTime.UtcNow > applyEndDate)
                return InternalServerError();

            int peopleQuota = (int)Data["PeopleQuota"];
            string formName = content.Id.ToString();
            var submissions = _formService.GetSubmissions(formName);

            //額滿
            if (submissions.Count() >= peopleQuota)
                return InternalServerError();

            IUser user = _authenticationService.GetAuthenticatedUser();

            IList<NameValueCollection> list = submissions.Select(s => HttpUtility.ParseQueryString(s.FormData)).Where(nv => nv.AllKeys.Contains("Owner") && nv.GetValues("Owner").Contains(user.UserName)).ToList();
            if (list.Count > 0)
                return Conflict();

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


            nvc.Add("Owner", user.UserName);

            Submission submission = _formService.CreateSubmission(contentId.ToString(), nvc);

            return Ok(new ResultViewModel { Content = submission, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
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
            if (inModel == null || inModel["EventId"] == null)
                return BadRequest();

            int contentId = (int)inModel["EventId"];
            ContentItem content = _orchardServices.ContentManager.Get(contentId, VersionOptions.Published);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            string formName = content.Id.ToString();

            var submissions = _formService.GetSubmissions(formName);
            //var list = submissions.Select(s => new { Id = s.Id, CreatedUtc = s.CreatedUtc, Form = HttpUtility.ParseQueryString(s.FormData) }).ToList();
            var list = submissions.Select(s => new { Id = s.Id, CreatedUtc = s.CreatedUtc, Form = ToDictionary(HttpUtility.ParseQueryString(s.FormData)) }).ToList();
            return Ok(new ResultViewModel { Content = list, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
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
