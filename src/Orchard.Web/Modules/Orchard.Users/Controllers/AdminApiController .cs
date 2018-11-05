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

namespace Orchard.Users.Controllers {
    [Authorize]
    public class AdminApiController : ApiController {
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;
        private readonly IUserEventHandler _userEventHandlers;
        private readonly ISiteService _siteService;

        public AdminApiController(
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
        public IHttpActionResult index(UsersIndexApiViewModel inModel) {
            if (inModel == null)
            {
                return BadRequest();
            }

            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to list users")))
                return Unauthorized();

            string message = "";
            string code = "200";

            UserIndexOptions options = inModel.Options;
            PagerParameters pagerParameters = inModel.Pager;


            var pager = pagerParameters != null ? new Pager(_siteService.GetSiteSettings(), pagerParameters) : null;

            // default options
            if (options == null)
                options = new UserIndexOptions();

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

            if (!string.IsNullOrWhiteSpace(options.Search)) {
                users = users.Where(u => u.UserName.Contains(options.Search) || u.Email.Contains(options.Search));
            }

            var pagerShape = pager != null ? Shape.Pager(pager).TotalItemCount(users.Count()) : null;
            int totalCount = users.Count();
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

            var model = new UsersIndexApiViewModel {
                Users = results
                    .Select(x => x.Record)
                    .ToList(),
                TotalCount = totalCount
            };

            ResultViewModel outModel = new ResultViewModel { Content = model, Code = code, Message = message };

            return Ok(outModel);
        }

        [HttpPost]
        public IHttpActionResult index(int id)
        {
            string message = "";
            string code = "200";

            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();

            var user = Services.ContentManager.Get<UserPart>(id);
            UserEditApiViewModel model = null;
            if (user == null)
            {
                code = "404";
                message = "User Not Found";
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

            ResultViewModel outModel = new ResultViewModel { Content = model, Code = code, Message = message };

            return Ok(outModel);
        }

        [HttpPost]
        public IHttpActionResult update(int id, UserEditApiViewModel inModel)
        {
            string message = "";
            string code = "200";
            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();

            var user = Services.ContentManager.Get<UserPart>(id, VersionOptions.DraftRequired);

            if (user == null)
            {
                code = "404";
                message = "User Not Found.";
            }

            if (!_userService.VerifyUserUnicity(id, inModel.UserName, inModel.Email))
            {
                code = "500";
                message = "User with that username and/or email already exists.";
                //AddModelError("NotUniqueUserName", T("User with that username and/or email already exists."));
            }
            else if (!Regex.IsMatch(inModel.Email ?? "", UserPart.EmailPattern, RegexOptions.IgnoreCase))
            {
                code = "500";
                message = "You must specify a valid email address.";
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
            ResultViewModel outModel = new ResultViewModel { Code = code, Message = message};


            return Ok(outModel);
        }

        [HttpPost]
        public IHttpActionResult delete(int id)
        {
            string message = "";
            string code = "200";

            if (!Services.Authorizer.Authorize(Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();

            var user = Services.ContentManager.Get<IUser>(id);

            if (user == null)
            {
                code = "404";
                message = "User Not Found.";
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

            ResultViewModel outModel = new ResultViewModel { Code = code, Message = message };

            return Ok(outModel);
        }

    }
}
