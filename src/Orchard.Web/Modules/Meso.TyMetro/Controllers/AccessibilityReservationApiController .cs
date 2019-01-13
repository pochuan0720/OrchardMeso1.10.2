using System.Linq;
using System;
using System.Net;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Core.Common.ViewModels;
using Orchard.Projections.Services;
using Orchard.Projections.Models;
using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Meso.TyMetro.Handlers;
using Orchard.Core.Common.Handlers;
using Orchard.Schedule.Models;
using System.Web;
using Orchard.Core.Common.Models;

namespace Meso.TyMetro.Controllers
{

    [Authorize]
    public class AccessibilityReservationApiController : ApiController
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IAccessibilityReservationUpdateModelHandler _updateModelHandler;
        private readonly IProjectionManager _projectionManager;

        public AccessibilityReservationApiController(
            IOrchardServices orchardServices,
            IAccessibilityReservationUpdateModelHandler updateModelHandler,
            IProjectionManager projectionManager)
        {
            _orchardServices = orchardServices;
            _updateModelHandler = updateModelHandler;
            _projectionManager = projectionManager;
        }

        [HttpGet]
        //[AcceptVerbs("OPTIONS")]
        private IHttpActionResult Get(string Name)
        {
            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = Name });
            return Ok(new ResultViewModel { Content = contentItems.Select(x => GetContent(x, FillContent)), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpGet]
        public IHttpActionResult Get(string Version = null, int Id = 0)
        {
            bool isDraft = false;
            string Name = "AccessibilityReservation";
            if (!string.IsNullOrEmpty(Version) && Version.Equals("draft"))
            {
                Name = "AccessibilityReservationDraft";
                isDraft = true;
            }

            if (Id <= 0)
                return Get(Name);


            ContentItem content = null;
            if(isDraft)
                content = _orchardServices.ContentManager.Get(Id, VersionOptions.Draft);
            else
                content = _orchardServices.ContentManager.Get(Id, VersionOptions.Published);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = GetContent(content, FillContent), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private object GetContent(ContentItem item, Action<JObject, string, int> fillContent = null)
        {
            JObject obj = new JObject();
            obj.Add(new JProperty("Id", item.Id));
            obj.Add(new JProperty("Title", item.As<TitlePart>().Title));
            obj.Add(new JProperty("ModifiedUtc", item.As<CommonPart>().ModifiedUtc));

            var model = _orchardServices.ContentManager.BuildEditor(item);
            JObject jObj = UpdateModelHandler.GetData(obj, model, fillContent);

            SchedulePart schedule = item.As<SchedulePart>();
            if (schedule != null)
            {
                obj.Add(new JProperty("IsPublished", schedule.IsPublished));
                ScheduleOccurrence ccurrence = new ScheduleOccurrence(schedule, schedule.StartDate);
                if(jObj["StartDateTime"] == null)
                    jObj.Add(new JProperty("StartDateTime", TimeZoneInfo.ConvertTimeToUtc(ccurrence.Start, schedule.TimeZone)));
                else
                    jObj["StartDateTime"] = TimeZoneInfo.ConvertTimeToUtc(ccurrence.Start, schedule.TimeZone);

                jObj.Add(new JProperty("EndDateTime", TimeZoneInfo.ConvertTimeToUtc(ccurrence.Start, schedule.TimeZone)));

                //Status
                jObj.Add(new JProperty("Status", "尚未抵達"));

                if (!schedule.IsPublished)
                {
                    if(schedule.Duration.TotalMilliseconds == 0)
                        jObj["Status"] = "結案(旅客未搭乘)";
                    else
                        jObj["Status"] = "結案";
                }
                else
                {
                    if (jObj["DepDateTime"] != null)
                    {
                        DateTime depDateTime = (DateTime)jObj["DepDateTime"];
                        if (depDateTime != DateTime.MinValue.ToUniversalTime())
                            jObj["Status"] = "旅客已上車";

                    }
                }
            }


            return jObj;
        }

        private void FillContent(JObject obj, string prefix, int id)
        {
            obj.Add(new JProperty(prefix, GetContent(_orchardServices.ContentManager.Get(id), FillContent)));
        }

        [HttpPut]
        public IHttpActionResult Put(JObject inModel)
        {
            if (inModel == null)
                return BadRequest();

            var content = _orchardServices.ContentManager.New<ContentPart>("AccessibilityReservation");

            if (content == null)
                return NotFound();

            var model = _orchardServices.ContentManager.BuildEditor(content);

            _orchardServices.ContentManager.Create(content, VersionOptions.Draft);
            var editorShape = _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));
            _orchardServices.ContentManager.Publish(content.ContentItem);

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult Post(int Id, JObject inModel)
        {
            if (inModel == null)
                return BadRequest();

            var content = _orchardServices.ContentManager.Get(Id, VersionOptions.DraftRequired);

            if (content == null)
                return NotFound();

            if (inModel["EndDateTime"] != null && inModel["StartDateTime"] == null)
            {
                SchedulePart schedule = content.As<SchedulePart>();
                inModel["StartDateTime"] = TimeZoneInfo.ConvertTimeToUtc(schedule.StartDate + schedule.StartTime, schedule.TimeZone);
            }
            else if (inModel["StartDateTime"] != null && inModel["EndDateTime"] == null)
            {
                SchedulePart schedule = content.As<SchedulePart>();
                ScheduleOccurrence ccurrence = new ScheduleOccurrence(schedule, schedule.StartDate);
                inModel["EndDateTime"] = TimeZoneInfo.ConvertTimeToUtc(ccurrence.End, schedule.TimeZone);
            }

            var model = _orchardServices.ContentManager.BuildEditor(content);
            _orchardServices.ContentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));

            if (inModel["IsPublished"] != null && !(bool)inModel["IsPublished"])
            {
                _orchardServices.ContentManager.Unpublish(content);
            }
            else
            {
                _orchardServices.ContentManager.Publish(content);
            }

            return Ok(new ResultViewModel { Content = new { Id = content.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }
    }
}
