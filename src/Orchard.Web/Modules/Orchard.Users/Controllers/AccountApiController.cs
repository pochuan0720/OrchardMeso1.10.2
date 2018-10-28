using Orchard.Localization;
using System.Web.Http;
using Orchard.Users.Services;
using Orchard.Security;
using Orchard.Users.ViewModels;
using Orchard.Users.Models;
using System;

namespace Orchard.Users.Controllers {
    [Authorize]
    public class AccountApiController : ApiController {
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;

        public AccountApiController(
            IMembershipService membershipService,
            IUserService userService) {
            _membershipService = membershipService;
            _userService = userService;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        [Authorize]
        [HttpGet]
        public IHttpActionResult index() {
            UserPart user = (UserPart)_membershipService.GetUser(User.Identity.Name);
            UserProfileViewModel model = new UserProfileViewModel();
            model.CreatedUtc = (DateTime)user.CreatedUtc;
            model.UserName = user.UserName;
            model.Email = user.Email;
            return Ok(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult register(UserCreateViewModel model) {
            if (model == null) {
                return BadRequest();
            }

            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            
            if (!_userService.VerifyUserUnicity(model.UserName, model.Email)) {
                AddModelError("NotUniqueUserName", T("User with that email already exists."));
                return BadRequest(ModelState);
            }

            var user = _membershipService.CreateUser(new CreateUserParams(model.UserName, model.Password, model.Email, null, null, true));
            if (user != null) {
                return Ok();
            }

            return InternalServerError();
        }

        public void AddModelError(string key, LocalizedString errorMessage) {
            ModelState.AddModelError(key, errorMessage.ToString());
        }
    }
}
