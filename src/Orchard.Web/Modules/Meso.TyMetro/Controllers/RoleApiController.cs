using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Core.Common.ViewModels;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Roles.ViewModels;
using Orchard.Security;
using Orchard.UI.Notify;

namespace Meso.TyMetro.Controllers {
    [Authorize]
    public class RoleApiController : ApiController {
        private readonly IRoleService _roleService;
        private readonly IAuthorizationService _authorizationService;

        public RoleApiController(
            IOrchardServices services,
            IRoleService roleService,
            INotifier notifier,
            IAuthorizationService authorizationService)
        {
            Services = services;
            _roleService = roleService;
            _authorizationService = authorizationService;

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        [HttpPost]
        public IHttpActionResult query()
        {

            if (!Services.Authorizer.Authorize(Orchard.Roles.Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Unauthorized();

            var model = _roleService.GetRoles().OrderBy(r => r.Name).ToList();
            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        
        public IHttpActionResult find(JObject inModel)
        {
            if (inModel == null || (inModel["Id"] == null && inModel["Name"] == null))
                return BadRequest();

            if (!Services.Authorizer.Authorize(Orchard.Roles.Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage roles" });

            RoleRecord model = null;
            if (inModel["Id"] != null)
                model = _roleService.GetRole((int)inModel["Id"]);
            else if (!string.IsNullOrEmpty(inModel["Name"].ToString()))
                model = _roleService.GetRoleByName(inModel["Name"].ToString());
            else
                return BadRequest();

            if (model == null)
                return NotFound();

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        public void AddModelError(string key, LocalizedString errorMessage)
        {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}
