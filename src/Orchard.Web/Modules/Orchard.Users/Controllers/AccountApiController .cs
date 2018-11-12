using System.Linq;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Users.Events;
using Orchard.Users.Models;
using Orchard.Users.Services;
using Orchard.Users.ViewModels;
using Orchard.Settings;
using Orchard.UI.Navigation;
using System.Web.Http;
using System.Xml;
using System;
using Orchard.Core.Common.ViewModels;
using System.Text.RegularExpressions;
using Orchard.Core.Settings.Models;
using Orchard.UI.Notify;
using Orchard.Core.Common.Handlers;
using System.Net;
using System.Web;

namespace Orchard.Users.Controllers {
    [Authorize]
    public class AccountApiController : ApiController {
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;
        private readonly IUserEventHandler _userEventHandlers;
        private readonly ISiteService _siteService;

        public AccountApiController(
            IOrchardServices services,
            IMembershipService membershipService,
            IUserService userService,
            IShapeFactory shapeFactory,
            IUserEventHandler userEventHandlers,
            ISiteService siteService) {
            Services = services;
            _membershipService = membershipService;
            _userService = userService;
            _userEventHandlers = userEventHandlers;
            _siteService = siteService;

            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        dynamic Shape { get; set; }
        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        [HttpPost]
        public IHttpActionResult list(UsersIndexApiViewModel inModel) {
            if (inModel == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to list users")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to list users" });

            UserIndexOptions options;
            // default options
            if (inModel.Options == null)
                options = new UserIndexOptions();
            else
                options = inModel.Options;

            var users = Services.ContentManager
                .Query<UserPart, UserPartRecord>();

            switch (options.Filter) {
                case UsersFilter.Approved:
                    users = users.Where(u => u.RegistrationStatus == UserStatus.Approved);
                    break;
                case UsersFilter.Pending:
                    users = users.Where(u => u.RegistrationStatus == UserStatus.Pending);
                    break;
                case UsersFilter.EmailPending:
                    users = users.Where(u => u.EmailStatus == UserStatus.Pending);
                    break;
            }
            Pager pager = null;
           if(inModel.Pager != null)
                pager = new Pager(_siteService.GetSiteSettings(), inModel.Pager.GetStartIndex(),  inModel.Pager.PageSize, users.Count());

            if (!string.IsNullOrWhiteSpace(options.Search)) {
                users = users.Where(u => u.UserName.Contains(options.Search) || u.Email.Contains(options.Search));
            }

            switch (options.Order) {
                case UsersOrder.Name:
                    users = users.OrderBy(u => u.UserName);
                    break;
                case UsersOrder.Email:
                    users = users.OrderBy(u => u.Email);
                    break;
                case UsersOrder.CreatedUtc:
                    users = users.OrderBy(u => u.CreatedUtc);
                    break;
                case UsersOrder.LastLoginUtc:
                    users = users.OrderBy(u => u.LastLoginUtc);
                    break;
            }

            var results = pager != null ? users
                .Slice(pager.GetStartIndex(), pager.PageSize)
                .ToList() : users.List();

            pager.PageSize = results.ToList().Count;

            var model = new UsersIndexApiViewModel {
                Users = results
                    .Select(x => x.Record)
                    .ToList(),
                Pager = pager
            };

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult index(int id)
        {

            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to manage users")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            var user = Services.ContentManager.Get<UserPart>(id);
            UserEditApiViewModel model = null;
            if (user == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });
            }
            else
            {
                model = new UserEditApiViewModel();
                model.UserName = user.UserName;
                model.Email = user.Email;
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(user.ContentItem.VersionRecord.Data);
                model.Data = doc.DocumentElement.FirstChild;
            }

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult update(int id, UserEditApiViewModel inModel)
        {
            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to manage users")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            var user = Services.ContentManager.Get<UserPart>(id, VersionOptions.DraftRequired);

            if (user == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });
            }

            if (!_userService.VerifyUserUnicity(id, inModel.UserName, inModel.Email))
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "User with that username and/or email already exists." });
                //AddModelError("NotUniqueUserName", T("User with that username and/or email already exists."));
            }
            else if (!Regex.IsMatch(inModel.Email ?? "", UserPart.EmailPattern, RegexOptions.IgnoreCase))
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "You must specify a valid email address." });
                //ModelState.AddModelError("Email", T("You must specify a valid email address."));
            }

            string previousName = user.UserName;

            var model = Services.ContentManager.UpdateEditor(user, new UpdateModelHandler(inModel.Data));

            // also update the Super user if this is the renamed account
            if (string.Equals(Services.WorkContext.CurrentSite.SuperUser, previousName, StringComparison.Ordinal))
            {
                _siteService.GetSiteSettings().As<SiteSettingsPart>().SuperUser = inModel.UserName;
            }

            Services.ContentManager.Publish(user.ContentItem);

            Services.Notifier.Information(T("User information updated"));

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(int id)
        {

            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();

            var user = Services.ContentManager.Get<IUser>(id);

            if (user == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });
            }
            else
            {

                if (string.Equals(Services.WorkContext.CurrentSite.SuperUser, user.UserName, StringComparison.Ordinal))
                {
                    Services.Notifier.Error(T("The Super user can't be removed. Please disable this account or specify another Super user account."));
                }
                else if (string.Equals(Services.WorkContext.CurrentUser.UserName, user.UserName, StringComparison.Ordinal))
                {
                    Services.Notifier.Error(T("You can't remove your own account. Please log in with another account."));
                }
                else
                {
                    Services.ContentManager.Remove(user.ContentItem);
                    Services.Notifier.Information(T("User {0} deleted", user.UserName));
                }
            }

            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult self()
        {
            UserPart user = (UserPart)_membershipService.GetUser(User.Identity.Name);
            UserProfileViewModel model = new UserProfileViewModel();
            model.CreatedUtc = (DateTime)user.CreatedUtc;
            model.UserName = user.UserName;
            model.Email = user.Email;
            XmlDocument doc = new XmlDocument();
            if (user.ContentItem.VersionRecord.Data != null)
            {
                doc.LoadXml(user.ContentItem.VersionRecord.Data);
                model.Data = doc.DocumentElement.FirstChild;
            }

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult register(UserCreateViewModel inModel)
        {
            if (inModel == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!ModelState.IsValid)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }

            if (!_userService.VerifyUserUnicity(inModel.UserName, inModel.Email))
            {
                //AddModelError("NotUniqueUserName", T("User with that email already exists."));
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "User with that email already exists." });
            }

            IUser user = _membershipService.CreateUser(new CreateUserParams(inModel.UserName, inModel.Password, inModel.Email, null, null, true));
            ResultViewModel outModel = null;
            if (user != null)
            {
                var model = Services.ContentManager.UpdateEditor(user, new UpdateModelHandler(inModel.Data));
                return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.InternalServerError.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.InternalServerError) });
        }
    }
}
