
using System.Net;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Core.Common.ViewModels;

using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Meso.TyMetro.Services;
using Meso.TyMetro.ViewModels;
using System.Collections.Specialized;

namespace Meso.TyMetro.Controllers
{
    public class GoTimeApiController : ApiController
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IReservationService _reservationService;
        private readonly ITyMetroService _tyMetroService;

        public GoTimeApiController(IOrchardServices orchardServices,
            IReservationService reservationService,
            ITyMetroService tyMetroService)
        {
            _orchardServices = orchardServices;
            _reservationService = reservationService;
            _tyMetroService = tyMetroService;
        }

        [HttpGet]
        public IHttpActionResult Get([FromUri]GoTimeViewModel inModel)
        {
            if (inModel.ARId > 0)
            {
                ContentItem item = _orchardServices.ContentManager.Get(inModel.ARId);
                if (item == null)
                    return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "無該筆預約單" });

                JObject jReservation = _reservationService.GetContent(item);

                return Ok(new ResultViewModel { Content = _tyMetroService.GetGoTime(jReservation), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
                return Ok(new ResultViewModel { Content = _tyMetroService.GetGoTime(inModel), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

    }
}
