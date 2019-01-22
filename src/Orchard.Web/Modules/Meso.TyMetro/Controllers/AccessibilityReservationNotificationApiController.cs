using System.Linq;
using System.Net;
using System.Web.Http;
using Orchard.Core.Common.ViewModels;
using Meso.TyMetro.Services;
using Orchard;
using Meso.TyMetro.Handlers;
using Orchard.Security;
using Orchard.Projections.Services;
using Orchard.Projections.Models;
using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Roles.Models;
using Meso.TyMetro.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using Orchard.Core.Common.Models;

namespace Meso.TyMetro.Controllers
{
    [Authorize]
    public class AccessibilityReservationNotificationApiController : ApiController
    {
        private readonly IOrchardServices _orchardServices;
        private readonly IAccessibilityReservationUpdateModelHandler _updateModelHandler;
        private readonly IProjectionManager _projectionManager;
        private readonly IAuthenticationService _authenticationService;
        private readonly ITyMetroService _tyMetroService;
        private readonly IReservationService _reservationService;

        public AccessibilityReservationNotificationApiController(
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
        public IHttpActionResult Get(string Condition)
        {
            switch (Condition)
            {
                case "DepBefore":
                    return StationBefore("StartDateTime", -900);
                case "ArrBefore":
                    return StationBefore("ArrDateTime", -300);
            }

            return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = "" });
        }

        private IHttpActionResult StationBefore(string whichDateTime, int seconds)
        {
            IEnumerable<ContentItem> contentItems = _projectionManager.GetContentItems(new QueryModel { Name = "AccessibilityReservation" });
            IUser user = _authenticationService.GetAuthenticatedUser();

            IEnumerable<JObject> items = null;
            UserRolesPart rolesPart = user.As<UserRolesPart>();
            if (rolesPart.Roles.Contains("Admin") || rolesPart.Roles.Contains("StaffAdmin"))
            {
                items = contentItems.Select(x => _reservationService.GetContent(x));
                items = items.Where(x => Check((DateTime)x[whichDateTime], seconds));
                return Ok(new ResultViewModel { Content = items, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
            {
                StationViewModel station = null;
                ResultViewModel result = _reservationService.CheckStaffCondition(out station, user);
                if (result != null)
                    return Ok(result);

                if (station == null)
                    return InternalServerError();

                BodyPart bodyPart = user.ContentItem.As<BodyPart>();
                UserBodyViewModel bodyModel = JObject.Parse(bodyPart.Text).ToObject<UserBodyViewModel>();
                
                if (whichDateTime.Equals("StartDateTime"))
                {
                    items = contentItems.Select(x => _reservationService.GetContent(x))
                        .Where(jjReservation => jjReservation["DepStation"]["Id"].ToString().Equals(bodyModel.Station.Id));

                }
                else if (whichDateTime.Equals("ArrDateTime"))
                {
                    items = contentItems.Select(x => _reservationService.GetContent(x))
                        .Where(jjReservation => jjReservation["ArrStation"]["Id"].ToString().Equals(bodyModel.Station.Id));
                }

                //int count = items.Count();

                items = items.Where(x => Check((DateTime)x[whichDateTime], seconds));

                return Ok(new ResultViewModel { Content = items, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

            }
        }

        private bool Check(DateTime d, int seconds)
        {
            if (d == DateTime.MinValue)
                return false;

            DateTime d2 = d.AddSeconds(seconds);
            if (DateTime.UtcNow > d2 && DateTime.UtcNow < d)
                return true;
            else
                return false;
        }
    }
}
