using System;
using System.ComponentModel.DataAnnotations;
using Orchard.ContentManagement;
using Orchard.Users.Models;

namespace Orchard.Users.ViewModels
{

    public class UserProfileViewModel
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public DateTime CreatedUtc { get; set; }

        public object Data { get; set; }
    }
}