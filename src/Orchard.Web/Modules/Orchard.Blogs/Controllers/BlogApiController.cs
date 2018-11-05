using System.Linq;
using Orchard.Blogs.Extensions;
using Orchard.Blogs.Services;
using Orchard.Core.Feeds;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Themes;
using Orchard.UI.Navigation;
using Orchard.Settings;
using Orchard.ContentManagement;
using Orchard.Blogs.Models;
using System.Web.Http;
using Orchard.Core.Common.ViewModels;
using Orchard.Blogs.ViewModels;
using System.Collections.Generic;

namespace Orchard.Blogs.Controllers {

    [Authorize]
    public class BlogApiController : ApiController {
        private readonly IOrchardServices _services;
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly IFeedManager _feedManager;
        private readonly ISiteService _siteService;

        public BlogApiController(
            IOrchardServices services, 
            IBlogService blogService,
            IBlogPostService blogPostService,
            IFeedManager feedManager, 
            IShapeFactory shapeFactory,
            ISiteService siteService) {
            _services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            _feedManager = feedManager;
            _siteService = siteService;
            Logger = NullLogger.Instance;
            Shape = shapeFactory;
            T = NullLocalizer.Instance;
        }

        dynamic Shape { get; set; }
        protected ILogger Logger { get; set; }
        public Localizer T { get; set; }

        [HttpPost]
        public IHttpActionResult index() {
            string message = "";
            string code = "200";

            var blogs = _blogService.Get()
                .Where(b => _services.Authorizer.Authorize(Orchard.Core.Contents.Permissions.ViewContent,b));

            //var list = Shape.List();
            //list.AddRange(blogs);

            //var viewModel = Shape.ViewModel()
            //    .ContentItems(list);

            ResultViewModel outModel = new ResultViewModel { Content = blogs, Code = code, Message = message };

            return Ok(outModel);
        }

        [HttpPost]
        public IHttpActionResult posts(int blogId, PagerParameters pagerParameters) {
            string message = "";
            string code = "200";
            Pager pager = pagerParameters == null ? null :  new Pager(_siteService.GetSiteSettings(), pagerParameters);

            var blogPart = _blogService.Get(blogId, VersionOptions.Published).As<BlogPart>();
            if (blogPart == null)
                return NotFound();

            if (!_services.Authorizer.Authorize(Orchard.Core.Contents.Permissions.ViewContent, blogPart, T("Cannot view content"))) {
                return Unauthorized();
            }
            IList<BlogPostPart> blogPosts;
            pager.PageSize = blogPart.PostsPerPage;

            _feedManager.Register(blogPart, _services.ContentManager.GetItemMetadata(blogPart).DisplayText);
            if (pager != null)
                blogPosts = _blogPostService.Get(blogPart, pager.GetStartIndex(), pager.PageSize).ToArray();
            else
                blogPosts = _blogPostService.Get(blogPart).ToArray();

            //dynamic blog = _services.ContentManager.BuildDisplay(blogPart);

            //var list = Shape.List();
            //list.AddRange(blogPosts);
            //blog.Content.Add(Shape.Parts_Blogs_BlogPost_List(ContentItems: list), "5");

            var totalItemCount = _blogPostService.PostCount(blogPart);
            //blog.Content.Add(Shape.Pager(pager).TotalItemCount(totalItemCount), "Content:after");

            BlogPostsIndexApiViewModel model = new BlogPostsIndexApiViewModel { BlogPosts = blogPosts, TotalCount = totalItemCount };

            ResultViewModel outModel = new ResultViewModel { Content = model, Code = code, Message = message };

            return Ok(outModel);
        }
    }
}
