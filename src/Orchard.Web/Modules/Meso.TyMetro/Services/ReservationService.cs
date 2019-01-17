using Meso.TyMetro.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Schedule.Models;
using Orchard.Security;
using System;
using System.Linq;
using System.Net;

namespace Meso.TyMetro.Services {
    public class ReservationService : IReservationService
    {
        private readonly IOrchardServices _orchardServices;
        private readonly ITyMetroService _tyMetroService; 

        public ReservationService(IOrchardServices orchardServices, ITyMetroService tyMetroService)
        {
            _orchardServices = orchardServices;
            _tyMetroService = tyMetroService;
        }

        public JObject GetContent(ContentItem item, Action<JObject, string, int> fillContent = null, AccessibilityReservationApiViewModel inFilterModel = null)
        {
            if (fillContent == null)
                fillContent = FillContent;

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
                if (jObj["StartDateTime"] == null)
                    jObj.Add(new JProperty("StartDateTime", TimeZoneInfo.ConvertTimeToUtc(ccurrence.Start, schedule.TimeZone)));
                else
                    jObj["StartDateTime"] = TimeZoneInfo.ConvertTimeToUtc(ccurrence.Start, schedule.TimeZone);

                jObj.Add(new JProperty("EndDateTime", TimeZoneInfo.ConvertTimeToUtc(ccurrence.Start, schedule.TimeZone)));

                //Status
                jObj.Add(new JProperty("Status", "尚未抵達"));

                if (!schedule.IsPublished)
                {
                    if (schedule.Duration.TotalMilliseconds == 0)
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
            //handle DepStation & ArrStation
            if(jObj["DepStation"] != null)
            {
                try
                {
                    jObj["DepStation"] = JObject.Parse(jObj["DepStation"].ToString());
                }
#pragma warning disable CS0168 // 已宣告變數 'e'，但從未使用過它。
                catch (Exception e) { }
#pragma warning restore CS0168 // 已宣告變數 'e'，但從未使用過它。
            }

            //handle DepStation & ArrStation
            if (jObj["ArrStation"] != null)
            {
                try
                {
                    jObj["ArrStation"] = JObject.Parse(jObj["ArrStation"].ToString());
                }
#pragma warning disable CS0168 // 已宣告變數 'e'，但從未使用過它。
                catch (Exception e) { }
#pragma warning restore CS0168 // 已宣告變數 'e'，但從未使用過它。
            }

            if(inFilterModel != null)
            {
                if(inFilterModel.ServiceItem > 0 && jObj["ServiceItem"] != null && jObj["ServiceItem"]["Id"] != null)
                {
                    if ((int)jObj["ServiceItem"]["Id"] != inFilterModel.ServiceItem)
                        return null;
                }

                if (!string.IsNullOrEmpty(inFilterModel.DepStation) && jObj["DepStation"] != null && jObj["DepStation"]["Id"] != null)
                {
                    if (!jObj["DepStation"]["Id"].ToString().Equals(inFilterModel.DepStation))
                        return null;
                }

                if (!string.IsNullOrEmpty(inFilterModel.ArrStation) && jObj["ArrStation"] != null && jObj["DepStation"]["Id"] != null)
                {
                    if (!jObj["ArrStation"]["Id"].ToString().Equals(inFilterModel.ArrStation))
                        return null;
                }

                if (!string.IsNullOrEmpty(inFilterModel.Title) && jObj["Title"] != null)
                {
                    if (!jObj["Title"].ToString().Contains(inFilterModel.Title))
                        return null;
                }

                if (!string.IsNullOrEmpty(inFilterModel.Status) && jObj["Status"] != null)
                {
                    if (!jObj["Status"].ToString().Equals(inFilterModel.Status))
                        return null;
                }

                if (inFilterModel.StartDateTimeStart != DateTime.MinValue && jObj["StartDateTime"] != null)
                {
                    DateTime startDateTime = (DateTime)jObj["StartDateTime"];
                    if (startDateTime < inFilterModel.StartDateTimeStart)
                        return null;
                }

                if (inFilterModel.StartDateTimeEnd != DateTime.MinValue && jObj["StartDateTime"] != null)
                {
                    DateTime startDateTime = (DateTime)jObj["StartDateTime"];
                    if (startDateTime > inFilterModel.StartDateTimeEnd)
                        return null;
                }
            }

            return jObj;
        }

        private void FillContent(JObject obj, string prefix, int id)
        {
            ContentItem item = _orchardServices.ContentManager.Get(id);
            JObject objChild = GetContent(item, FillContent);

            IContent container = item.As<CommonPart>().Container;
            if (container != null)
            {
                var model = _orchardServices.ContentManager.BuildEditor(container);
                objChild = UpdateModelHandler.GetData(objChild, model);
            }
            obj.Add(new JProperty(prefix, objChild));
        }

        public ResultViewModel CheckStaffCondition(out StationViewModel station, IUser user = null, string stationId = null)
        {
            try
            {
                station = null;
                if (user != null)
                {
                    BodyPart bodyPart = user.ContentItem.As<BodyPart>();
                    if (string.IsNullOrEmpty(bodyPart.Text))
                        return new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "該帳號無設置站點" };

                    UserBodyViewModel bodyModel = JObject.Parse(bodyPart.Text).ToObject<UserBodyViewModel>();
                    if (bodyModel.Station == null || string.IsNullOrEmpty(bodyModel.Station.Id))
                        return new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "該帳號無設置站點" };

                    stationId = bodyModel.Station.Id;
                }

                if (string.IsNullOrEmpty(stationId))
                    return new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "查無該站點資訊" };

                //ContentItem content = _orchardServices.ContentManager.Get(stationId, VersionOptions.Published);
                station = _tyMetroService.GetStation(new StationViewModel { Id = stationId }).First();
                if (station == null)
                    return new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "查無該站點資訊" };

            }
#pragma warning disable CS0168 // 已宣告變數 'e'，但從未使用過它。
            catch (Exception e)
#pragma warning restore CS0168 // 已宣告變數 'e'，但從未使用過它。
            {
                station = null;
            }
            return null;
        }

        public bool CheckStaffPermissionForReservation(JObject jReservation, StationViewModel station)
        {
            return (jReservation["DepStation"] != null && jReservation["DepStation"]["Code"] != null && jReservation["DepStation"]["Code"].ToString().Equals(station.Code))
                   || (jReservation["ArrStation"] != null && jReservation["ArrStation"]["Code"] != null && jReservation["ArrStation"]["Code"].ToString().Equals(station.Code));
        }

        public bool CheckStaffPermissionForStation(StationViewModel station1, StationViewModel station2)
        {
            return (!string.IsNullOrEmpty(station1.Code) && !string.IsNullOrEmpty(station2.Code) && station1.Code.Equals(station2.Code));
        }
    }
}
