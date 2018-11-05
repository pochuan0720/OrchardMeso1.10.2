using Orchard.Localization;
using System.Web.Http;
using Orchard.Users.Services;
using Orchard.Security;
using Orchard.Users.ViewModels;
using Orchard.Users.Models;
using System;
using Orchard.ContentManagement;
using System.Xml;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Common.Handlers;

namespace Orchard.Users.Controllers {
    [Authorize]
    public class AccountApiController : ApiController {
        private readonly IContentManager _contentManager;
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;

        public AccountApiController(
            IOrchardServices services,
            IContentManager contentManager,
            IMembershipService membershipService,
            IUserService userService) {
            Services = services;
            _contentManager = contentManager;
            _membershipService = membershipService;
            _userService = userService;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        [HttpPost]
        public IHttpActionResult index() {
            string message = "";
            string code = "200";

            UserPart user = (UserPart)_membershipService.GetUser(User.Identity.Name);
            UserProfileViewModel model = new UserProfileViewModel();
            model.CreatedUtc = (DateTime)user.CreatedUtc;
            model.UserName = user.UserName;
            model.Email = user.Email;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(user.ContentItem.VersionRecord.Data);
            model.Data = doc.DocumentElement.FirstChild;
            ResultViewModel outModel = new ResultViewModel { Content = model, Code = code, Message = message };

            return Ok(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult register(UserCreateViewModel inModel)
        {
            if (inModel == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string message = "";
            string code = "200";

            if (!_userService.VerifyUserUnicity(inModel.UserName, inModel.Email))
            {
                //AddModelError("NotUniqueUserName", T("User with that email already exists."));
                code = "404";
                message = "User with that email already exists.";
            }

            IUser user = _membershipService.CreateUser(new CreateUserParams(inModel.UserName, inModel.Password, inModel.Email, null, null, true));
            ResultViewModel outModel = null;
            if (user != null)
            {
                var model = Services.ContentManager.UpdateEditor(user, new UpdateModelHandler(inModel.Data));
                outModel = new ResultViewModel { Content = model, Code = code, Message = message };
                return Ok(outModel);
            }

            outModel = new ResultViewModel { Code = code, Message = message };
            return Ok(outModel);
        }


    }
}
