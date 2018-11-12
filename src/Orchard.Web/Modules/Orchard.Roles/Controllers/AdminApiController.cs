using System.Linq;
using System.Net;
using System.Web.Http;
using Orchard.Core.Common.ViewModels;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Roles.Services;
using Orchard.Roles.ViewModels;
using Orchard.Security;
using Orchard.UI.Notify;

namespace Orchard.Roles.Controllers {
    [Authorize]
    public class AdminApiController : ApiController {
        private readonly IRoleService _roleService;
        private readonly IAuthorizationService _authorizationService;

        public AdminApiController(
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
        public IHttpActionResult Index()
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            var model = _roleService.GetRoles().OrderBy(r => r.Name).ToList();
            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult Index(int id)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage roles" });

            var model = _roleService.GetRole(id);

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult Index(string name)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage roles" });

            var model = _roleService.GetRoleByName(name);

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        public void AddModelError(string key, LocalizedString errorMessage)
        {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}
