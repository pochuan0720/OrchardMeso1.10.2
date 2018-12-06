using System.Collections.Generic;
using Orchard.Environment.Extensions.Models;
using Orchard.Security.Permissions;

namespace Orchard.Schedule {
    public class Permissions : IPermissionProvider {
        public static readonly Permission AddSchedule = new Permission { Description = "Add schedule", Name = "AddSchedule" };
        public static readonly Permission ManageSchedules = new Permission { Description = "Manage schedules", Name = "ManageSchedules" };

        public virtual Feature Feature { get; set; }

        public IEnumerable<Permission> GetPermissions() {
            return new[] {
                AddSchedule,
                ManageSchedules,
            };
        }

        public IEnumerable<PermissionStereotype> GetDefaultStereotypes() {
            return new[] {
                new PermissionStereotype {
                    Name = "Administrator",
                    Permissions = new[] {ManageSchedules, AddSchedule}
                },
                /*new PermissionStereotype {
                    Name = "Anonymous",
                    Permissions = new[] {ManageSchedules}
                },
                new PermissionStereotype {
                    Name = "Authenticated",
                    Permissions = new[] {ManageSchedules}
                },*/
                new PermissionStereotype {
                    Name = "Editor",
                    Permissions = new[] {ManageSchedules}
                },
                new PermissionStereotype {
                    Name = "Moderator",
                    Permissions = new[] {ManageSchedules, AddSchedule}
                },
                new PermissionStereotype {
                    Name = "Author",
                    Permissions = new[] {AddSchedule}
                },
                new PermissionStereotype {
                    Name = "Contributor",
                    Permissions = new[] {AddSchedule}
                },
            };
        }
    }
}
