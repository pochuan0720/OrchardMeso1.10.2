using Newtonsoft.Json;
using System;
using TYMetro.Management.Api.Models.Time;

namespace Meso.TyMetro.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GoTimeDataModel
    {
        [JsonProperty]
        public int Id { get; set; }
        public string TimeTable { get; set; }
        [JsonProperty]
        public string Code { get; set; }
        public string Min { get; set; }
        public string Hour { get; set; }
        //public string CarType { get; set; }
        [JsonProperty]
        public string DepOrArr { get; set; }
        [JsonProperty]
        public TimeSpan GoTime { get; set; }
        [JsonProperty]
        public DateTime GoDateTimeUtc { get; set; }
        [JsonProperty]
        public char CarDirection { get; set; }

        //Other Query
        public string DepCode { get; set; }
        public string ArrCode { get; set; }
        public bool? IsDelete { get; set; } = false;

        public GoTimeDataModel()
        {
        }

        public GoTimeDataModel(GoTimeListDataModel inMoidel)
        {
            Id = (int)inMoidel.ID;
            TimeTable = inMoidel.TimeTable;
            Code = inMoidel.StationID;
            Min = inMoidel.Min;
            Hour = inMoidel.Hour;
            //CarType = inMoidel.CarType;
            DepOrArr = inMoidel.DepOrArr;
            GoTime = (TimeSpan)inMoidel.GoTime;
            GoDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now.Date.Add(GoTime));
            CarDirection = (char)inMoidel.CarDirection;
        }

        public GoTimeListQueryModel ToGoTimeListQueryModel()
        {
            return new GoTimeListQueryModel
            {
                StationID = Code,
                DepOrArr = DepOrArr,
                CarDirection = CarDirection,
                IsDelete = IsDelete
            };
        }
    }
}