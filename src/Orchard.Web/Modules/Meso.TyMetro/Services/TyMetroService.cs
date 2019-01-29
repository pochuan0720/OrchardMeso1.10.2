
using System;
using System.Collections.Generic;
using System.Linq;
using Meso.TyMetro.ViewModels;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.ContentManagement;
using TYMetro.Management.Api.Models.Sites;
using TYMetro.Management.Api.Models.Time;
using TYMetro.Management.Api.Services.Sites;
using TYMetro.Management.Api.Services.Time;
using GoTimeViewModel = Meso.TyMetro.ViewModels.GoTimeViewModel;
using StationViewModel = Meso.TyMetro.ViewModels.StationViewModel;

namespace Meso.TyMetro.Services {

        
    public class TyMetroService : ITyMetroService
    {
        private readonly IOrchardServices _orchardServices;

        public TyMetroService(IOrchardServices orchardServices)
        {
            _orchardServices = orchardServices;
        }

        public IEnumerable<GoTimeViewModel> GetGoTime(GoTimeViewModel inModel)
        {
            var data = new GoTimeListService().Query(inModel.ToQueryModel());
            return data.Data.Select(d => new GoTimeViewModel(d)).OrderBy(x => x.GoTime).GroupBy(row => row.TimeTable).FirstOrDefault();
        }

        /*public IEnumerable<GoTimeViewModel> GetGoTime(int ARId, string DepOrArr = "dep")
        {
            ContentItem item = _orchardServices.ContentManager.Get(ARId);
            if (item == null)
                return Enumerable.Empty<GoTimeViewModel>();

            JObject jReservation = _reservationService.GetContent(item);

            return GetGoTime(jReservation);
        }*/

        public IEnumerable<GoTimeViewModel> GetGoTime(JObject jReservation, string DepOrArr = "dep")
        {

            if (jReservation == null)
                return Enumerable.Empty<GoTimeViewModel>();

            GoTimeViewModel depGoTime = new GoTimeViewModel();
            depGoTime.DepOrArr = DepOrArr;
            if (DepOrArr.Equals("arr"))
                depGoTime.Code = jReservation["ArrStation"]["Code"].ToString();
            else
                depGoTime.Code = jReservation["DepStation"]["Code"].ToString();


            if (jReservation["DepStation"]["Seq"] != null && jReservation["ArrStation"]["Seq"] != null)
            {
                if ((int)jReservation["DepStation"]["Seq"] > (int)jReservation["ArrStation"]["Seq"])
                    depGoTime.CarDirection = 'N';
                else
                    depGoTime.CarDirection = 'S';
            }

            //if (!string.IsNullOrEmpty(inModel.DepCode) && !string.IsNullOrEmpty(inModel.ArrCode) && inModel.CarDirection.Equals('\0'))
            //{
            /*string depCode = inModel.DepCode.Substring(1);
            string arrCode = inModel.ArrCode.Substring(1);
            if (depCode.EndsWith("a"))
                depCode = depCode.Substring(0, depCode.Length - 1);
            if (arrCode.EndsWith("a"))
                arrCode = arrCode.Substring(0, arrCode.Length - 1);*/

            /*if (int.Parse(depCode) > int.Parse(arrCode))
                inModel.CarDirection = 'N';
            else
                inModel.CarDirection = 'S';*/

            /*if(!string.IsNullOrEmpty(inModel.DepOrArr))
            {
                if (inModel.DepOrArr.Equals("dep"))
                    inModel.Code = inModel.DepCode;
                else if(inModel.DepOrArr.Equals("arr"))
                    inModel.Code = inModel.ArrCode;
            }*/
            //}
            CalendarViewModel calendar = new CalendarViewModel { date = DateTime.Now.Date};
            var dataCandendar = new CalendarService().Query(calendar.ToQueryModel());
            CalendarDataModel ddataModel = dataCandendar.Data.Count > 0 ? dataCandendar.Data.First() : null;
            if (ddataModel != null)
            {
                calendar = new CalendarViewModel(ddataModel);
                depGoTime.TimeTable = calendar.timetable;
                var data = new GoTimeListService().Query(depGoTime.ToQueryModel());
                return data.Data.Select(d => new GoTimeViewModel(d)).OrderBy(x => x.GoTime);
            }
            else
            {
                var data = new GoTimeListService().Query(depGoTime.ToQueryModel());
                return data.Data.Select(d => new GoTimeViewModel(d)).OrderBy(x => x.GoTime).GroupBy(row => row.TimeTable).FirstOrDefault();
            }
        }

        public GoTimeViewModel GetArrGoTime(JObject jReservation, string depGoTime, string carNumber)
        {
            IEnumerable<GoTimeViewModel> arrGoTimeList = GetGoTime(jReservation, "arr");

            TimeSpan _depGoTime = TimeSpan.Parse(depGoTime);

            foreach (GoTimeViewModel model in arrGoTimeList)
            {
                if (model.GoTime > _depGoTime && model.CarNumber.Equals(carNumber))
                    return model;
            }

            return null;
        }

        public IEnumerable<StationViewModel> GetStation(StationViewModel inModel)
        {
            var data =  new SitesService().Query(inModel.ToQueryModel());

            return data.Data.Select(d => new StationViewModel(d, inModel.culture)).OrderBy(x => x.Seq);
        }
    }
}