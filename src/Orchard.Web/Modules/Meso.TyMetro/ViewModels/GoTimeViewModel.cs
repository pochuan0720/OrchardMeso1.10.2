using Newtonsoft.Json;
using System;
using TYMetro.Management.Api.Models.Time;

namespace Meso.TyMetro.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class GoTimeViewModel
    {
        [JsonProperty]
        public int Id { get; set; }
        public string TimeTable { get; set; }
        [JsonProperty]
        public string Code { get; set; }
        public string Min { get; set; }
        public string Hour { get; set; }
        [JsonProperty]
        public string CarType { get; set; }
        [JsonProperty]
        public string DepOrArr { get; set; }
        [JsonProperty]
        public TimeSpan GoTime { get; set; }
        //[JsonProperty]
        //public DateTime GoDateTimeUtc { get; set; }
        [JsonProperty]
        public char CarDirection { get; set; }
        [JsonProperty]
        public string CarNumber { get; set; }

        //Other Query
        public int ARId { get; set; }
        public int DepId { get; set; }
        public int ArrId { get; set; }
        public bool? IsDelete { get; set; } = false;

        public GoTimeViewModel()
        {
        }

        public GoTimeViewModel(GoTimeListDataModel inMoidel)
        {
            Id = (int)inMoidel.ID;
            TimeTable = inMoidel.TimeTable;
            Code = inMoidel.StationID;
            Min = inMoidel.Min;
            Hour = inMoidel.Hour;
            CarType = inMoidel.CarType;
            DepOrArr = inMoidel.DepOrArr;
            GoTime = (TimeSpan)inMoidel.GoTime;
            //GoDateTimeUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now.Date.Add(GoTime));
            CarDirection = (char)inMoidel.CarDirection;
            CarNumber = inMoidel.CarNumber;
        }

        public GoTimeListQueryModel ToQueryModel()
        {
            return new GoTimeListQueryModel
            {
                StationID = Code,
                TimeTable = TimeTable,
                DepOrArr = DepOrArr,
                CarDirection = CarDirection,
                IsDelete = IsDelete
            };
        }
    }
}