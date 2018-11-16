using System;
using System.Net;
using System.Web;
using System.Web.Http;
using Orchard.Blogs.Extensions;
using Orchard.Blogs.Handlers;
using Orchard.Blogs.Models;
using Orchard.Blogs.Services;
using Orchard.Blogs.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Contents.Settings;
using Orchard.Localization;
using Orchard.Mvc;
using Orchard.UI.Notify;

namespace Orchard.Blogs.Controllers {

    /// <summary>
    /// TODO: (PH:Autoroute) This replicates a whole lot of Core.Contents functionality. All we actually need to do is take the BlogId from the query string in the BlogPostPartDriver, and remove
    /// helper extensions from UrlHelperExtensions.
    /// </summary>
    [Authorize]
    public class BlogPostApiController : ApiController {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;

        public BlogPostApiController(IOrchardServices services, IBlogService blogService, IBlogPostService blogPostService) {
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
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            var blogPost = Services.ContentManager.New<BlogPostPart>("BlogPost");
            blogPost.BlogPart = blog;

            if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't create blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't create blog post" });

            Services.ContentManager.Create(blogPost, VersionOptions.Draft);
            var model = Services.ContentManager.UpdateEditor(blogPost, new UpdateModelHandler(inModel));

            /*if (!ModelState.IsValid)
            {
                Services.TransactionManager.Cancel();
                return View(model);
            }*/

            //if (inModel.Publish != null && (bool)inModel.Publish)
            //{
                if (!Services.Authorizer.Authorize(Permissions.PublishBlogPost, blogPost.ContentItem, T("Couldn't publish blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't publish blog post" });

            Services.ContentManager.Publish(blogPost.ContentItem);
            //}

            Services.Notifier.Information(T("Your {0} has been created.", blogPost.TypeDefinition.DisplayName));

            return Ok(new ResultViewModel { Content = blogPost, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult update(int blogId, int postId, BlogPostEditApiViewModel inModel)
        {
            if (inModel.Publish != null && (bool)inModel.Publish)
                return EditAndPublishPOST(blogId, postId, inModel);
            else
                return EditPOST(blogId, postId, inModel);
        }

        private IHttpActionResult EditPOST(int blogId, int postId, BlogPostEditApiViewModel inModel)
        {
            return EditPOST(blogId, postId, inModel, contentItem => {
                if (!contentItem.Has<IPublishingControlAspect>() && !contentItem.TypeDefinition.Settings.GetModel<ContentTypeSettings>().Draftable)
                    Services.ContentManager.Publish(contentItem);
            });
        }

        private IHttpActionResult EditAndPublishPOST(int blogId, int postId, BlogPostEditApiViewModel inModel)
        {
            var blog = _blogService.Get(blogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            // Get draft (create a new version if needed)
            var blogPost = _blogPostService.Get(postId, VersionOptions.DraftRequired);
            if (blogPost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.PublishBlogPost, blogPost, T("Couldn't publish blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't publish blog post" });

            return EditPOST(blogId, postId, inModel, contentItem => Services.ContentManager.Publish(contentItem));
        }

        private IHttpActionResult EditPOST(int blogId, int postId, BlogPostEditApiViewModel inModel, Action<ContentItem> conditionallyPublish)
        {
            var blog = _blogService.Get(blogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            // Get draft (create a new version if needed)
            var blogPost = _blogPostService.Get(postId, VersionOptions.DraftRequired);
            if (blogPost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't edit blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit blog post" });

            // Validate form input
            var model = Services.ContentManager.UpdateEditor(blogPost, new UpdateModelHandler(inModel));

            conditionallyPublish(blogPost.ContentItem);

            Services.Notifier.Information(T("Your {0} has been saved.", blogPost.TypeDefinition.DisplayName));

            return Ok(new ResultViewModel { Content = blogPost, Success = true,  Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(int blogId, int postId)
        {
            //refactoring: test PublishBlogPost/PublishBlogPost in addition if published

            var blog = _blogService.Get(blogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var post = _blogPostService.Get(postId, VersionOptions.Latest);
            if (post == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.DeleteBlogPost, post, T("Couldn't delete blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            _blogPostService.Delete(post);
            Services.Notifier.Information(T("Blog post was successfully deleted"));

            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult index(int blogId, int postId)
        {
            var blog = _blogService.Get(blogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var blogpost = _blogPostService.Get(postId);
            if (blogpost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = blogpost, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }
    }
}