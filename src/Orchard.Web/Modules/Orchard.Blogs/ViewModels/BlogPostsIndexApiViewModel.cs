using Orchard.Blogs.Models;
using Orchard.UI.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Orchard.Blogs.ViewModels
{
    public class BlogPostsIndexApiViewModel
    {
        public IList<BlogPostPart> BlogPosts { get; set; }
        public Pager Pager { get; set; }
    }
}