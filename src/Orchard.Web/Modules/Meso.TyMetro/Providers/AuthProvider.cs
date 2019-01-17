using System.Linq;
using Microsoft.Owin.Security.OAuth;
using Orchard;
using Orchard.Security;
using System.Security.Claims;
using System.Threading.Tasks;
using TYMetro.Management.Api.Services.Permission;
using Meso.TyMetro.ViewModels;
using System;
using Orchard.Users.Services;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Title.Models;
using Orchard.ContentManagement;
using TYMetro.Management.Api.Models.Permission;
using Orchard.Core.Common.Models;
using Orchard.Projections.Services;
using Orchard.Projections.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Orchard.Roles.Models;
using Orchard.Roles.Services;
using Orchard.Roles.ViewModels;
using Orchard.Data;
using Meso.TyMetro.Services;

namespace Meso.TyMetro.Providers
{
    public class AuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly IOrchardServices _orchardServices;

        public AuthProvider(IWorkContextAccessor workContextAccessor)
        {
            _workContextAccessor = workContextAccessor;
            _orchardServices = _workContextAccessor.GetContext().Resolve<IOrchardServices>();
        }

        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
            return Task.FromResult<object>(null);
        }

        public override Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });
            string userName = context.UserName;
            string password = context.Password;
            string email = context.UserName;

            UserViewModel userViewModel = new UserViewModel { UserName = userName, Password = password };
            try
            {
                UserPermissionModel data = new UserPermissionServices().Login(userViewModel.ToLoginModel());

                var membershipService = _workContextAccessor.GetContext().Resolve<IMembershipService>();

                UserDataModel userDataModel = data.User;
                RoleDataModel roleDataModel = data.Role;

                IUser user = membershipService.ValidateUser(userName, password);
                if (user == null)
                {
                    //new
                    var userService = _workContextAccessor.GetContext().Resolve<IUserService>();
                    if (!userService.VerifyUserUnicity(userName, email))
                    {
                        context.SetError("invalid_grant", "Verify user unicity fail.");
                    }
                    else
                    {
                        user = membershipService.CreateUser(new CreateUserParams(userName, password, email, null, null, true));
                    }
                }
                else
                    user = membershipService.GetUser(userName);

                if (user == null)
                {
                    context.SetError("invalid_grant", "Create or find user fail.");
                }
                else
                {
                    TitlePart name = user.ContentItem.As<TitlePart>();
                    BodyPart bodyPart = user.ContentItem.As<BodyPart>();
                    name.Title = userDataModel.UserName;
                    IdentityPart identify = user.ContentItem.As<IdentityPart>();
                    identify.Identifier = userDataModel.Id;

                    //siteId
                    if (!string.IsNullOrEmpty(userDataModel.SiteID))
                    {
                        try
                        {
                            var projectionManager = _workContextAccessor.GetContext().Resolve<IProjectionManager>();
                            IEnumerable<ContentItem> contentItems = projectionManager.GetContentItems(new QueryModel { Name = "Stations" });
                            if (contentItems != null)
                            {
                                //ContentItem item = contentItems.Where(x => x.As<CommonPart>().Container.As<IdentityPart>().Identifier.Equals(userDataModel.SiteID)).FirstOrDefault();
                                ITyMetroService tyMetroService = _workContextAccessor.GetContext().Resolve<ITyMetroService>();
                                StationViewModel station = tyMetroService.GetStation(new StationViewModel { Id = userDataModel.SiteID }).First();
                                if (station != null)
                                {
                                    UserBodyViewModel bodyModel = null;
                                    if (string.IsNullOrEmpty(bodyPart.Text))
                                        bodyModel = new UserBodyViewModel();
                                    else
                                    {
                                        bodyModel = JObject.Parse(bodyPart.Text).ToObject<UserBodyViewModel>();
                                    }

                                    if (bodyModel.Station == null)
                                        bodyModel.Station = new StationViewModel();

                                    bodyModel.Station.Id = userDataModel.SiteID;
                                    bodyPart.Text = JsonConvert.SerializeObject(bodyModel);
                                }
                            }
                        }
#pragma warning disable CS0168 // 已宣告變數 'e'，但從未使用過它。
                        catch (Exception e) { }
#pragma warning restore CS0168 // 已宣告變數 'e'，但從未使用過它。
                    }

                    //Role
                    if(roleDataModel != null)
                    {
                        IRoleService roleService = _workContextAccessor.GetContext().Resolve<IRoleService>();
                        if(roleService.GetRoleByName(roleDataModel.Name) == null)
                        {
                            roleService.CreateRole(roleDataModel.Name);
                        }
                    }
                    UserRolesPart rolesPart = user.As<UserRolesPart>();
                    if(rolesPart != null)
                    {
                        AddRole(rolesPart, roleDataModel.Name);
                    }

                    //update
                    var identity = new ClaimsIdentity(context.Options.AuthenticationType);
                    identity.AddClaim(new Claim(ClaimTypes.Name, context.UserName));
                    context.Validated(identity);
                }
            }
#pragma warning disable CS0168 // 已宣告變數 'e'，但從未使用過它。
            catch (Exception e)
#pragma warning restore CS0168 // 已宣告變數 'e'，但從未使用過它。
            {
                context.SetError("invalid_grant", "The user name or password is incorrect.");
            }


            return Task.FromResult<object>(null);
        }

        private void AddRole(UserRolesPart userRolesPart, string roleName)
        {
            var model = new UserRolesViewModel { User = userRolesPart.As<IUser>(), UserRoles = userRolesPart };
            model.Roles.Add(new UserRoleEntry { Name = roleName, Granted = true });

            IRepository<UserRolesPartRecord> _userRolesRepository = _workContextAccessor.GetContext().Resolve<IRepository<UserRolesPartRecord>>();
            IRoleService _roleService = _workContextAccessor.GetContext().Resolve<IRoleService>();

            var currentUserRoleRecords = _userRolesRepository.Fetch(x => x.UserId == model.User.Id).ToArray();
            var currentRoleRecords = currentUserRoleRecords.Select(x => x.Role);
            var targetRoleRecords = model.Roles.Where(x => x.Granted).Select(x => x.RoleId > 0 ? _roleService.GetRole(x.RoleId) : _roleService.GetRoleByName(x.Name)).ToArray();
            foreach (var addingRole in targetRoleRecords.Where(x => !currentRoleRecords.Contains(x)))
            {
                _userRolesRepository.Create(new UserRolesPartRecord { UserId = model.User.Id, Role = addingRole });
            }
            foreach (var removingRole in currentUserRoleRecords.Where(x => !targetRoleRecords.Contains(x.Role)))
            {
                _userRolesRepository.Delete(removingRole);
            }
        }
    }
}