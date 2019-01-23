using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TYMetro.Management.Api.Models.Time;

namespace Meso.TyMetro.ViewModels
{
    public class CalendarViewModel
    {
        public int Id { get; set; }
        public DateTime date { get; set; }
        public string timetable { get; set; }

        public bool IsDelete { get; set; } = false;

        public CalendarViewModel()
        {

        }

        public CalendarViewModel(CalendarDataModel inModel)
        {
            Id = inModel.Id;
            date = inModel.date;
            timetable = inModel.timetable;
            IsDelete = (bool)inModel.IsDelete;
        }



        public CalendarQueryModel ToQueryModel()
        {
            return new CalendarQueryModel { date = date, IsDelete = IsDelete };
        }
    }
}