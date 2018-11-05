using Orchard.Blogs.Models;
using Orchard.Core.Common.Models;
using Orchard.Core.Title.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Orchard.Blogs.ViewModels
{
    public class BlogEditApiViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int? PostsPerPage { get; set; }
        public object Data { get; set; }
    }
}