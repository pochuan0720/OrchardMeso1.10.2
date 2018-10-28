using System.Linq;
using System.Web.Http;
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

        [Authorize]
        [HttpGet]
        public IHttpActionResult Index()
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Unauthorized();

            var model = new RolesIndexViewModel { Rows = _roleService.GetRoles().OrderBy(r => r.Name).ToList() };
            return Ok(model);
        }

        [Authorize]
        [HttpGet]
        public IHttpActionResult Index(int id)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Unauthorized();

            var model = _roleService.GetRole(id);

            return Ok(model);
        }

        [Authorize]
        [HttpGet]
        public IHttpActionResult Index(string name)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageRoles, T("Not authorized to manage roles")))
                return Unauthorized();

            var model = _roleService.GetRoleByName(name);

            return Ok(model);
        }

        public void AddModelError(string key, LocalizedString errorMessage)
        {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}
