using System.Web.Http;
using Orchard.Blogs.Extensions;
using Orchard.Blogs.Handlers;
using Orchard.Blogs.Models;
using Orchard.Blogs.Services;
using Orchard.Blogs.ViewModels;
using Orchard.ContentManagement;
using Orchard.Core.Common.ViewModels;
using Orchard.Localization;
using Orchard.Mvc;
using Orchard.UI.Notify;

namespace Orchard.Blogs.Controllers {

    /// <summary>
    /// TODO: (PH:Autoroute) This replicates a whole lot of Core.Contents functionality. All we actually need to do is take the BlogId from the query string in the BlogPostPartDriver, and remove
    /// helper extensions from UrlHelperExtensions.
    /// </summary>
    [Authorize]
    public class BlogPostAdminApiController : ApiController {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;

        public BlogPostAdminApiController(IOrchardServices services, IBlogService blogService, IBlogPostService blogPostService) {
            Services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        [HttpPost]
        public IHttpActionResult create(int blogId, BlogPostEditApiViewModel inModel)
        {
            var blog = _blogService.Get(blogId, VersionOptions.Latest).As<BlogPart>();

            if (blog == null)
                return NotFound();

            var blogPost = Services.ContentManager.New<BlogPostPart>("BlogPost");
            blogPost.BlogPart = blog;

            if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't create blog post")))
                return Unauthorized();

            string message = "";
            string code = "200";

            Services.ContentManager.Create(blogPost, VersionOptions.Draft);
            var model = Services.ContentManager.UpdateEditor(blogPost, new UpdateModelHandler(inModel));

            /*if (!ModelState.IsValid)
            {
                Services.TransactionManager.Cancel();
                return View(model);
            }*/

            if (inModel.Publish != null && (bool)inModel.Publish)
            {
                if (!Services.Authorizer.Authorize(Permissions.PublishBlogPost, blogPost.ContentItem, T("Couldn't publish blog post")))
                    return Unauthorized();

                Services.ContentManager.Publish(blogPost.ContentItem);
            }

            Services.Notifier.Information(T("Your {0} has been created.", blogPost.TypeDefinition.DisplayName));

            ResultViewModel outModel = new ResultViewModel { Content = blogPost, Code = code, Message = message };

            return Ok(outModel);
        }
    }
}