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
using Orchard.Security;
using Orchard.Roles.Models;
using Meso.TyMetro.ViewModels;
using Meso.TyMetro.Services;

namespace Meso.TyMetro.Controllers
{

    [Authorize]
    public class AccessibilityReservationApiController : ApiController
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IAccessibilityReservationUpdateModelHandler _updateModelHandler;
        private readonly IProjectionManager _projectionManager;
        private readonly IAuthenticationService _authenticationService;
        private readonly ITyMetroService _tyMetroService;
        private readonly IReservationService _reservationService;

        public AccessibilityReservationApiController(
            IOrchardServices orchardServices,
            IAccessibilityReservationUpdateModelHandler updateModelHandler,
            IProjectionManager projectionManager,
            IAuthenticationService authenticationService,
            ITyMetroService tyMetroService,
            IReservationService reservationService
            )
        {
            _orchardServices = orchardServices;
            _updateModelHandler = updateModelHandler;
            _projectionManager = projectionManager;
            _authenticationService = authenticationService;
            _tyMetroService = tyMetroService;
            _reservationService = reservationService;
        }

        [HttpGet]
        //[AcceptVerbs("OPTIONS")]
        private IHttpActionResult Get(string Name, AccessibilityReservationApiViewModel inModel)
        {
            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = Name });
            IUser user = _authenticationService.GetAuthenticatedUser();

            UserRolesPart rolesPart = user.As<UserRolesPart>();
            if (rolesPart.Roles.Contains("Admin") || rolesPart.Roles.Contains("StaffAdmin"))
                return Ok(new ResultViewModel { Content = contentItems.Select(x => _reservationService.GetContent(x, null, inModel)).Where(jReservation => jReservation != null), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            else
            {
                StationViewModel station = null;
                ResultViewModel result = _reservationService.CheckStaffCondition(out station, user);
                if (result != null)
                    return Ok(result);

                if (station == null)
                    return InternalServerError();

                IEnumerable<object> items = contentItems.Select(x => _reservationService.GetContent(x, null, inModel))
                    .Where(jReservation => jReservation != null && _reservationService.CheckStaffPermissionForReservation(jReservation, station));


                return Ok(new ResultViewModel { Content = items, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

            }

        }


        /*private IHttpActionResult CheckStaffCondition(out StationViewModel station, IUser user = null, string stationId = null)
        {
            try
            {
                if (user != null)
                {
                    BodyPart bodyPart = user.ContentItem.As<BodyPart>();
                    if (string.IsNullOrEmpty(bodyPart.Text))
                        Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "該帳號無設置站點" });

                    UserBodyViewModel bodyModel = JObject.Parse(bodyPart.Text).ToObject<UserBodyViewModel>();
                    if (bodyModel.Station == null || string.IsNullOrEmpty(bodyModel.Station.Id))
                        Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "該帳號無設置站點" });

                    stationId = bodyModel.Station.Id;
                }

                if (string.IsNullOrEmpty(stationId))
                    Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "查無該站點資訊" });

                //ContentItem content = _orchardServices.ContentManager.Get(stationId, VersionOptions.Published);
                station = _tyMetroService.GetStation(new StationViewModel { Id = stationId }).First();
                if (station == null)
                    Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "查無該站點資訊" });

            }
            catch (Exception e)
            {
                station = null;
                return InternalServerError();
            }
            return null;
        }

        private bool CheckStaffPermissionForReservation(JObject jReservation, StationViewModel station)
        {
            return (jReservation["DepStation"] != null && jReservation["DepStation"]["Code"] != null && jReservation["DepStation"]["Code"].ToString().Equals(station.Code))
                   || (jReservation["ArrStation"] != null && jReservation["ArrStation"]["Code"] != null && jReservation["ArrStation"]["Code"].ToString().Equals(station.Code));
        }

        private bool CheckStaffPermissionForStation(StationViewModel station1, StationViewModel station2)
        {
            return (!string.IsNullOrEmpty(station1.Code) && !string.IsNullOrEmpty(station2.Code) && station1.Code.Equals(station2.Code));
        }*/

        [HttpGet]
        public IHttpActionResult Get([FromUri] AccessibilityReservationApiViewModel inModel, string Version = null, int Id = 0)
        {
            bool isDraft = false;
            string Name = "AccessibilityReservation";
            if (!string.IsNullOrEmpty(Version) && Version.Equals("draft"))
            {
                Name = "AccessibilityReservationDraft";
                isDraft = true;
            }

            if (Id <= 0)
                return Get(Name, inModel);


            ContentItem content = null;
            if(isDraft)
                content = _orchardServices.ContentManager.Get(Id, VersionOptions.Draft);
            else
                content = _orchardServices.ContentManager.Get(Id, VersionOptions.Published);

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = _reservationService.GetContent(content), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        /*private JObject GetContent(ContentItem item, Action<JObject, string, int> fillContent = null)
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
        }*/

        /*private void FillContent(JObject obj, string prefix, int id)
        {
            ContentItem item =  _orchardServices.ContentManager.Get(id);
            JObject objChild = GetContent(item, FillContent);

            IContent container = item.As<CommonPart>().Container;
            if (container != null)
            {
                var model = _orchardServices.ContentManager.BuildEditor(container);
                objChild = UpdateModelHandler.GetData(objChild, model);
            }
            obj.Add(new JProperty(prefix, objChild));
        }*/

        [HttpPut]
        public IHttpActionResult Put(JObject inModel)
        {
            if (inModel == null || inModel["Title"] == null  || inModel["ServiceItem"] == null || inModel["DepStation"] == null  || inModel["ArrStation"] == null)
                return BadRequest();

            IUser user = _authenticationService.GetAuthenticatedUser();
            BodyPart bodyPart = user.ContentItem.As<BodyPart>();
            UserRolesPart rolesPart = user.As<UserRolesPart>();

            StationViewModel depStation = null;
            StationViewModel arrStation = null;
            _reservationService.CheckStaffCondition(out depStation, null, inModel["DepStation"].ToString());
            _reservationService.CheckStaffCondition(out arrStation, null, inModel["ArrStation"].ToString());

            if (!rolesPart.Roles.Contains("Admin") && !rolesPart.Roles.Contains("StaffAdmin"))
            {
                StationViewModel station;
                ResultViewModel result = _reservationService.CheckStaffCondition(out station, user);
                if (result != null)
                    return Ok(result);

                if (station == null)
                    return InternalServerError();


                if (!_reservationService.CheckStaffPermissionForStation(depStation, station) && !_reservationService.CheckStaffPermissionForStation(arrStation, station))
                    return Unauthorized();

            }

            inModel["DepStation"] = JObject.FromObject(depStation);
            inModel["ArrStation"] = JObject.FromObject(arrStation);

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

            //兩者需同時為null或不為null
            if ((inModel["DepGoTime"] != null && inModel["CarNumber"] == null) || (inModel["DepGoTime"] == null && inModel["CarNumber"] != null))
                return BadRequest();

            //if(inModel["DepStation"] != null || inModel["ArrStation"] != null)
            //    Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "不允許修改起到站" });

            var content = _orchardServices.ContentManager.Get(Id, VersionOptions.DraftRequired);

            if (content == null)
                return NotFound();
            //Permission
            IUser user = _authenticationService.GetAuthenticatedUser();
            BodyPart bodyPart = user.ContentItem.As<BodyPart>();
            UserRolesPart rolesPart = user.As<UserRolesPart>();
            JObject jReservation = null;

            if (!rolesPart.Roles.Contains("Admin") && !rolesPart.Roles.Contains("StaffAdmin"))
            {
                StationViewModel station = null;
                ResultViewModel result = _reservationService.CheckStaffCondition(out station, user);
                if (result != null)
                    return Ok(result);

                if (station == null)
                    return InternalServerError();

                jReservation = _reservationService.GetContent(content);

                if (!_reservationService.CheckStaffPermissionForReservation(jReservation, station))
                    return Unauthorized();
            }

            if (inModel["DepStation"] != null)
            {
                StationViewModel depStation = null;
                _reservationService.CheckStaffCondition(out depStation, null, inModel["DepStation"].ToString());
                inModel["DepStation"] = JObject.FromObject(depStation);
            }

            if (inModel["ArrStation"] != null)
            {
                StationViewModel arrStation = null;
                _reservationService.CheckStaffCondition(out arrStation, null, inModel["ArrStation"].ToString());
                inModel["ArrStation"] = JObject.FromObject(arrStation);
            }

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


            //GoTime to DateTime


            if (inModel["DepGoTime"] != null && inModel["CarNumber"] != null)
            {
                if(jReservation == null)
                    jReservation = _reservationService.GetContent(content);

                GoTimeViewModel arrGoTimeModel = _tyMetroService.GetArrGoTime(jReservation, inModel["DepGoTime"].ToString(), inModel["CarNumber"].ToString());
                if (arrGoTimeModel == null || arrGoTimeModel.GoTime == null)
                    return Ok(new ResultViewModel { Content = jReservation, Success = false, Code = HttpStatusCode.OK.ToString("d"), Message = "無法取得到站時刻" });

                TimeSpan depGoTime = TimeSpan.Parse(inModel["DepGoTime"].ToString());
                DateTime depGoDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now.Date.Add(depGoTime));
                inModel["DepDateTime"] = depGoDateTimeUtc;

                inModel["ArrGoTime"] = arrGoTimeModel.GoTime;
                DateTime arrGoDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now.Date.Add(arrGoTimeModel.GoTime));
                inModel["ArrDateTime"] = arrGoDateTimeUtc;
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
