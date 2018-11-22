using System.Collections.Generic;
using Orchard.Blogs.Models;
using Orchard.UI.Navigation;

namespace Orchard.Blogs.ViewModels {

    public class BlogsIndexApiViewModel  {
        public int? Id { get; set; }
        public IList<BlogPart> Data { get; set; }
        public int TotalCount { get; set; }
    }
}
