
using System.Collections.Generic;
using System.Linq;
using Meso.TyMetro.ViewModels;
using TYMetro.Management.Api.Models.Time;
using TYMetro.Management.Api.Services.Time;

namespace Meso.TyMetro.Services {

        
    public class TyMetroService : ITyMetroService
    {


        public TyMetroService()
        {
            ;
        }

        public IEnumerable<GoTimeDataModel> GetCurrentGoTime(GoTimeDataModel inModel)
        {

            if (!string.IsNullOrEmpty(inModel.DepCode) && !string.IsNullOrEmpty(inModel.ArrCode) && inModel.CarDirection.Equals('\0'))
            {
                string depCode = inModel.DepCode.Substring(1);
                string arrCode = inModel.ArrCode.Substring(1);
                if (depCode.EndsWith("a"))
                    depCode = depCode.Substring(0, depCode.Length - 1);
                if (arrCode.EndsWith("a"))
                    arrCode = arrCode.Substring(0, arrCode.Length - 1);

                if (int.Parse(depCode) > int.Parse(arrCode))
                    inModel.CarDirection = 'N';
                else
                    inModel.CarDirection = 'S';

                if(!string.IsNullOrEmpty(inModel.DepOrArr))
                {
                    if (inModel.DepOrArr.Equals("dep"))
                        inModel.Code = inModel.DepCode;
                    else if(inModel.DepOrArr.Equals("arr"))
                        inModel.Code = inModel.ArrCode;
                }
            }

            var data = new GoTimeListService().Query(inModel.ToGoTimeListQueryModel());
            return data.Data.Select( d => new GoTimeDataModel(d)).OrderBy(x=>x.GoTime).GroupBy(row => row.TimeTable).FirstOrDefault();
        }
    }
}