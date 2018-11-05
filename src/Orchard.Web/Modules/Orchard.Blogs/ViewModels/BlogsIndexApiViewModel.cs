using System.Collections.Generic;
using Orchard.Blogs.Models;
using Orchard.UI.Navigation;

namespace Orchard.Blogs.ViewModels {

    public class BlogsIndexApiViewModel  {
        public IList<BlogPart> Blogs { get; set; }
        public int TotalCount { get; set; }
    }
}
