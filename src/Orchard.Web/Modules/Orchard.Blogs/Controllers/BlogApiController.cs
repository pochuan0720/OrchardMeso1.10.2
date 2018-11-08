using System.Linq;
using Orchard.Blogs.Models;
using Orchard.Blogs.Services;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;
using Orchard.Settings;
using System.Web.Http;
using Orchard.Core.Common.ViewModels;
using Orchard.Blogs.ViewModels;
using System.Collections.Generic;
using Orchard.Blogs.Handlers;
using System.Net;
using System.Web;

namespace Orchard.Blogs.Controllers {

    [Authorize]
    public class BlogApiController : ApiController {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly IContentManager _contentManager;
        private readonly ITransactionManager _transactionManager;
        private readonly ISiteService _siteService;

        public BlogApiController(
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
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't create blog" });

            bool state = ModelState.IsValid;

            BlogPart blog = Services.ContentManager.New<BlogPart>("Blog");

            _contentManager.Create(blog, VersionOptions.Draft);
            _contentManager.UpdateEditor(blog, new UpdateModelHandler(inModel));

            _contentManager.Publish(blog.ContentItem);

            return Ok(new ResultViewModel { Content = blog, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        
        [HttpPost]
        public IHttpActionResult index(int blogId) {

            BlogPart blog = _blogService.Get(blogId, VersionOptions.Latest).As<BlogPart>();

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Not allowed to edit blog")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not allowed to edit blog" });

            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});


            return Ok(new ResultViewModel { Content = blog, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(int blogId) {
            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, T("Couldn't delete blog")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete blog" });

            var blog = _blogService.Get(blogId, VersionOptions.DraftRequired);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            _blogService.Delete(blog);

            Services.Notifier.Information(T("Blog deleted"));

            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }


        [HttpPost]
        public IHttpActionResult update(int blogId, BlogEditApiViewModel inModel) {
            var blog = _blogService.Get(blogId, VersionOptions.DraftRequired);

            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Couldn't edit blog")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit blog" });

             Services.ContentManager.UpdateEditor(blog, new UpdateModelHandler(inModel));

            _contentManager.Publish(blog);
            Services.Notifier.Information(T("Blog information updated"));

            return Ok(new ResultViewModel { Content = blog.As<BlogPart>(), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult index() {

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

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

        }

        [HttpPost]
        public IHttpActionResult posts(int blogId, BlogPostsIndexApiViewModel inModel) {
            Pager pager = null;

            BlogPart blogPart = _blogService.Get(blogId, VersionOptions.Latest).As<BlogPart>();

            if (blogPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            var totalItemCount = _blogPostService.PostCount(blogPart, VersionOptions.Latest);
            if (inModel != null && inModel.Pager != null)
                pager = new Pager(_siteService.GetSiteSettings(), inModel.Pager.Page, inModel.Pager.PageSize, totalItemCount);

            IList<BlogPostPart> blogPosts;

            if (pager != null)
            {
                blogPosts = _blogPostService.Get(blogPart, pager.GetStartIndex(), pager.PageSize, VersionOptions.Latest).ToArray();
                pager.PageSize = blogPosts.Count;
            }
            else
                blogPosts = _blogPostService.Get(blogPart, VersionOptions.Latest).ToArray();

            BlogPostsIndexApiViewModel model = new BlogPostsIndexApiViewModel { BlogPosts  = blogPosts, Pager  = pager };

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

    }
}