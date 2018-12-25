using System.Linq;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Users.Events;
using Orchard.Users.Models;
using Orchard.Users.Services;
using Orchard.Settings;
using Orchard.UI.Navigation;
using System;
using Orchard.Core.Common.ViewModels;
using System.Text.RegularExpressions;
using Orchard.Core.Settings.Models;
using Orchard.UI.Notify;
using Orchard.Core.Common.Handlers;
using System.Net;
using System.Web.Http;
using Orchard;
using Newtonsoft.Json.Linq;
using Orchard.Roles.Models;
using System.Collections.Generic;
using System.Web;
using Meso.Users.ViewModels;

namespace Meso.Volunteer.Controllers {
    [Authorize]
    public class AccountApiController : ApiController {
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;
        private readonly IUserEventHandler _userEventHandlers;
        private readonly ISiteService _siteService;
        private readonly IUpdateModelHandler _updateModelHandler;

        public AccountApiController(
            IOrchardServices services,
            IMembershipService membershipService,
            IUserService userService,
            IShapeFactory shapeFactory,
            IUserEventHandler userEventHandlers,
            ISiteService siteService,
            IUpdateModelHandler updateModelHandler) {
            Services = services;
            _membershipService = membershipService;
            _userService = userService;
            _userEventHandlers = userEventHandlers;
            _siteService = siteService;
            _updateModelHandler = updateModelHandler;
            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        dynamic Shape { get; set; }
        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        [HttpPost]
        public IHttpActionResult query(JObject inModel) {

            if (!Services.Authorizer.Authorize(Orchard.Users.Permissions.ManageUsers, T("Not authorized to list users")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to list users" });

            Filter filter = null;
            if (inModel["Filter"] != null)
                filter = inModel["Filter"].ToObject<Filter>();

            var users = Services.ContentManager
                .Query<UserPart, UserPartRecord>();

            /*if(options.Filter.UserRoles != null && options.Filter.UserRoles.Length > 0)
            {
                users = users.OrderBy(u => u.UserName);
            }*/

            /*if (!string.IsNullOrWhiteSpace(options.Search)) {
                users = users.Where(u => u.UserName.Contains(options.Search) || u.Email.Contains(options.Search));
            }*/

            users = users.OrderBy(u => u.UserName);

            /*switch (UsersOrder.Name) {
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
            }*/

            //Paging
            Pager pager = null;
            IEnumerable<object> results; 
            if (inModel["Pager"] != null)
            {
                Pager _pager = inModel["Pager"].ToObject<Pager>();
                pager = new Pager(_siteService.GetSiteSettings(), _pager.GetStartIndex(), _pager.PageSize, users.Count());
                results = users.Slice(pager.GetStartIndex(), pager.PageSize).Select(x => getUser(x, filter)).Where(x => x != null);
                pager.PageSize = results.ToList().Count;
            }
            else
                results = users.List().Select( x => getUser(x, filter));

            var model = new {
                Data = results.ToList(),
                Pager = pager
            };

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private object getUser(UserPart user, Filter filter)
        {
            var model = Services.ContentManager.BuildEditor(user);
            JObject obj = UpdateModelHandler.GetData(JObject.FromObject(user), model);
            if (filter.UserRoles != null && filter.UserRoles.Length > 0)
            {
                foreach (string role in filter.UserRoles)
                {
                    if (obj["UserRoles"] != null && obj["UserRoles"].ToList().Contains(role))
                        return obj;
                }
                return null;
            }
            else
                return obj;
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            if (!Services.Authorizer.Authorize(Orchard.Users.Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            var user = Services.ContentManager.Get<UserPart>((int)inModel["Id"]);

            if (user == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });
            }
            else
            {
                var model = Services.ContentManager.BuildEditor(user);

                return Ok(new ResultViewModel { Content = UpdateModelHandler.GetData(JObject.FromObject(user), model), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }

            
        }

        [HttpPost]
        public IHttpActionResult update(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();

            if (!Services.Authorizer.Authorize(Orchard.Users.Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            int Id = (int)inModel["Id"];
            var user = Services.ContentManager.Get<UserPart>(Id, VersionOptions.DraftRequired);

            if (user == null)
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });
            }

            /*if (!_userService.VerifyUserUnicity(id, inModel.UserName, inModel.Email))
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "User with that username and/or email already exists." });
                //AddModelError("NotUniqueUserName", T("User with that username and/or email already exists."));
            }
            else if (!Regex.IsMatch(inModel.Email ?? "", UserPart.EmailPattern, RegexOptions.IgnoreCase))
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "You must specify a valid email address." });
                //ModelState.AddModelError("Email", T("You must specify a valid email address."));
            }*/

            string previousName = user.UserName;

            var model = Services.ContentManager.UpdateEditor(user, _updateModelHandler.SetData(inModel));

            var editModel = new Orchard.Users.ViewModels.UserEditViewModel { User = user };
            if(inModel["UserName"] != null)
                editModel.UserName = inModel["UserName"].ToString();
            if(inModel["Email"] != null)
                editModel.Email = inModel["Email"].ToString();
            if (!_userService.VerifyUserUnicity(Id, editModel.UserName, editModel.Email))
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "User with that username and/or email already exists." });
            }
            else if (!Regex.IsMatch(editModel.Email ?? "", UserPart.EmailPattern, RegexOptions.IgnoreCase))
            {
                // http://haacked.com/archive/2007/08/21/i-knew-how-to-validate-an-email-address-until-i.aspx    
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = "You must specify a valid email address." });
            }
            else
            {
                // also update the Super user if this is the renamed account
                if (string.Equals(Services.WorkContext.CurrentSite.SuperUser, previousName, StringComparison.Ordinal))
                {
                    _siteService.GetSiteSettings().As<SiteSettingsPart>().SuperUser = editModel.UserName;
                }

                user.NormalizedUserName = editModel.UserName.ToLowerInvariant();
            }


            Services.ContentManager.Publish(user.ContentItem);

            Services.Notifier.Information(T("User information updated"));

            return Ok(new ResultViewModel { Content = new { Id = user .Id}, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null)
                return BadRequest();

            if (!Services.Authorizer.Authorize(Orchard.Users.Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            var user = Services.ContentManager.Get<IUser>((int)inModel["Id"], VersionOptions.Latest);

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

            return Ok(new ResultViewModel { Content = new { Id = user.Id }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult self()
        {
            IUser user = _membershipService.GetUser(User.Identity.Name);
            var model = Services.ContentManager.BuildEditor(user);
            string[] roles = user.As<UserRolesPart>().Roles.ToArray();
            JObject obj = JObject.FromObject(user);
            obj.Add(new JProperty("UserRoles", roles));
            return Ok(new ResultViewModel { Content = UpdateModelHandler.GetData(obj, model), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult register(JObject inModel)
        {
            if (inModel == null || inModel["UserName"] == null || inModel["Password"] == null 
                || inModel["Email"] == null)
                return BadRequest();

            string userName = inModel["UserName"].ToString();
            string password = inModel["Password"].ToString();
            string email = inModel["Email"].ToString();

            if (!_userService.VerifyUserUnicity(userName, email))
            {
                //AddModelError("NotUniqueUserName", T("User with that email already exists."));
                return Conflict();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Conflict.ToString("d"), Message = "User with that email already exists." });
            }

            IUser user = _membershipService.CreateUser(new CreateUserParams(userName, password, email, null, null, true));

            if (user == null)
                return InternalServerError();


            /*try
            {
                var editor = Shape.EditorTemplate(TemplateName: "Parts/Roles.UserRoles", Model: Activator.CreateInstance(Type.GetType("Orchard.Roles.ViewModels.UserRolesViewModel, Orchard.Roles", true)), Prefix: null);
                Services.ContentManager.BuildEditor(user);
            }catch (Exception e) { };*/
            //Services.ContentManager.BuildEditor(user);
                
            var model = Services.ContentManager.UpdateEditor(user, _updateModelHandler.SetData(inModel));
            return Ok(new ResultViewModel { Content = new { Id = user.Id}, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(JObject inModel)
        {
            if (!Services.Authorizer.Authorize(Orchard.Users.Permissions.ManageUsers, T("Not authorized to manage users")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not authorized to manage users" });

            return register(inModel);
        }
    }
}
