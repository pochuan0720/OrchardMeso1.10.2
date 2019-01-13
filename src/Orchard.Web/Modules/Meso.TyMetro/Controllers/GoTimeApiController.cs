using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Net;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.Web;
using Orchard;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.ViewModels;
using Orchard.Projections.Services;
using Orchard.Projections.Models;
using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Meso.TyMetro.Services;
using Meso.TyMetro.ViewModels;
using System.Collections.Specialized;

namespace Meso.Common.Controllers
{
    public class GoTimeApiController : ApiController
    {
        private readonly ITyMetroService _tyMetroService;

        public GoTimeApiController(ITyMetroService tyMetroService)
        {
            _tyMetroService = tyMetroService;
        }

        [HttpGet]
        public IHttpActionResult Get([FromUri]GoTimeDataModel inModel)
        {
            return Ok(new ResultViewModel { Content = _tyMetroService.GetCurrentGoTime(inModel), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

    }
}
