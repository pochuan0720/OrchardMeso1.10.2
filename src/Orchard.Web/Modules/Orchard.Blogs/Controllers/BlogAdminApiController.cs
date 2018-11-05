using System.Linq;
using Orchard.Blogs.Extensions;
using Orchard.Blogs.Models;
using Orchard.Blogs.Routing;
using Orchard.Blogs.Services;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Mvc;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;
using Orchard.Settings;
using System.Web.Http;
using Orchard.Core.Common.ViewModels;
using Orchard.Blogs.ViewModels;
using System.Collections.Generic;
using Orchard.Core.Title.Models;
using Orchard.Blogs.Handlers;

namespace Orchard.Blogs.Controllers {

    [Authorize]
    public class BlogAdminApiController : ApiController {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly IContentManager _contentManager;
        private readonly ITransactionManager _transactionManager;
        private readonly ISiteService _siteService;

        public BlogAdminApiController(
            IOrchardServices services,
            IBlogService blogService,
            IBlogPostService blogPostService,
            IContentManager contentManager,
            ITransactionManager transactionManager,
            ISiteService siteService,
            IShapeFactory shapeFactory) {
            Services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            _contentManager = contentManager;
            _transactionManager = transactionManager;
            _siteService = siteService;
            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        dynamic Shape { get; set; }
        public Localizer T { get; set; }
        public IOrchardServices Services { get; set; }

        [HttpPost]
        public IHttpActionResult create(BlogEditApiViewModel inModel) {
            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, T("Couldn't create blog")))
                return Unauthorized();

            string message = "";
            string code = "200";
            bool state = ModelState.IsValid;

            BlogPart blog = Services.ContentManager.New<BlogPart>("Blog");

            _contentManager.Create(blog, VersionOptions.Draft);
            var model = _contentManager.UpdateEditor(blog, new UpdateModelHandler(inModel));

            if (!ModelState.IsValid) {
                _transactionManager.Cancel();
                code = "500";
                return Ok(model);
            }

            _contentManager.Publish(blog.ContentItem);

            ResultViewModel outModel = new ResultViewModel { Content = model, Code = code, Message = message };

            return Ok(outModel);
        }

        
        [HttpPost]
        public IHttpActionResult index(int blogId) {
            string message = "";
            string code = "200";

            BlogPart blog = _blogService.Get(blogId, VersionOptions.Latest).As<BlogPart>();

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Not allowed to edit blog")))
                return Unauthorized();

            if (blog == null)
                return NotFound();


            ResultViewModel outModel = new ResultViewModel { Content = blog, Code = code, Message = message };

            return Ok(outModel);
        }

        [HttpPost]
        public IHttpActionResult delete(int blogId) {
            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, T("Couldn't delete blog")))
                return Unauthorized();

            string message = "";
            string code = "200";

            var blog = _blogService.Get(blogId, VersionOptions.DraftRequired);
            if (blog == null)
                return NotFound();
            _blogService.Delete(blog);

            Services.Notifier.Information(T("Blog deleted"));

            ResultViewModel outModel = new ResultViewModel { Content = blog, Code = code, Message = message };

            return Ok(outModel);
        }


        [HttpPost]
        public IHttpActionResult update(int blogId, BlogEditApiViewModel inModel) {
            var blog = _blogService.Get(blogId, VersionOptions.DraftRequired);

            if (blog == null)
                return NotFound();

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Couldn't edit blog")))
                return Unauthorized();

            string message = "";
            string code = "200";

            var model = Services.ContentManager.UpdateEditor(blog, new UpdateModelHandler(inModel));

            _contentManager.Publish(blog);
            Services.Notifier.Information(T("Blog information updated"));
            ResultViewModel outModel = new ResultViewModel { Code = code, Message = message };
            return Ok(outModel);
        }

        [HttpPost]
        public IHttpActionResult Remove(int id) {
            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, T("Couldn't delete blog")))
                return Unauthorized();

            string message = "";
            string code = "200";

            var blog = _blogService.Get(id, VersionOptions.Latest);

            if (blog == null)
                return NotFound();

            _blogService.Delete(blog);

            Services.Notifier.Information(T("Blog was successfully deleted"));
            ResultViewModel outModel = new ResultViewModel { Code = code, Message = message };
            return Ok(outModel);
        }

        [HttpPost]
        public IHttpActionResult index() {
            string message = "";
            string code = "200";
            var list = Services.New.List();
            list.AddRange(_blogService.Get(VersionOptions.Latest)
                .Where(x => Services.Authorizer.Authorize(Permissions.MetaListOwnBlogs, x))
                .Select(b =>
                {
                    BlogPart blog = b.As<BlogPart>();
                    blog.TotalPostCount = _blogPostService.PostCount(b, VersionOptions.Latest);
                    return blog;
                }));

            int totalCount = list.Items.Count;
            var model = new BlogsIndexApiViewModel
            {
                Blogs = list,
                TotalCount = totalCount
            };
            ResultViewModel outModel = new ResultViewModel { Content = model, Code = code, Message = message };
            return Ok(outModel);

        }

        [HttpPost]
        public IHttpActionResult posts(int blogId, BlogPostsIndexApiViewModel inModel) {
            Pager pager = null;
            if (inModel != null && inModel.Pager != null)
                 pager = new Pager(_siteService.GetSiteSettings(), inModel.Pager);

            BlogPart blogPart = _blogService.Get(blogId, VersionOptions.Latest).As<BlogPart>();

            string message = "";
            string code = "200";

            if (blogPart == null)
                return NotFound();
            IList<BlogPostPart> blogPosts;

            if(pager != null)
                blogPosts = _blogPostService.Get(blogPart, pager.GetStartIndex(), pager.PageSize, VersionOptions.Latest).ToArray();
            else
                blogPosts = _blogPostService.Get(blogPart, VersionOptions.Latest).ToArray();
            //var blogPostsShapes = blogPosts.Select(bp => _contentManager.BuildDisplay(bp, "SummaryAdmin")).ToArray();

            //var blog = Services.ContentManager.BuildDisplay(blogPart, "DetailAdmin");

            //var list = Shape.List();
            //list.AddRange(blogPostsShapes);
            //blog.Content.Add(Shape.Parts_Blogs_BlogPost_ListAdmin(ContentItems: list), "5");

            var totalItemCount = _blogPostService.PostCount(blogPart, VersionOptions.Latest);
            //blog.Content.Add(Shape.Pager(pager).TotalItemCount(totalItemCount), "Content:after");

            BlogPostsIndexApiViewModel model = new BlogPostsIndexApiViewModel { BlogPosts  = blogPosts, TotalCount  = totalItemCount };

            ResultViewModel outModel = new ResultViewModel { Content = model, Code = code, Message = message };
            return Ok(outModel);
        }

    }
}