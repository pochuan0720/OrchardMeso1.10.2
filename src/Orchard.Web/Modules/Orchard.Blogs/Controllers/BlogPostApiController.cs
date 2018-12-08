using System;
using System.Collections.Generic;
using System.Linq;
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
using Orchard.Settings;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;
using UpdateModelHandler = Orchard.Blogs.Handlers.UpdateModelHandler;

namespace Orchard.Blogs.Controllers {

    /// <summary>
    /// TODO: (PH:Autoroute) This replicates a whole lot of Core.Contents functionality. All we actually need to do is take the BlogId from the query string in the BlogPostPartDriver, and remove
    /// helper extensions from UrlHelperExtensions.
    /// </summary>
    [Authorize]
    public class BlogPostApiController : ApiController {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly ISiteService _siteService;
        private readonly IUpdateModelHandler _updateModelHandler;

        public BlogPostApiController(IOrchardServices services, IBlogService blogService, IBlogPostService blogPostService, ISiteService siteService, IUpdateModelHandler updateModelHandler) {
            Services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            _updateModelHandler = updateModelHandler;
            _siteService = siteService;
            T = NullLocalizer.Instance;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        [HttpPost]
        public IHttpActionResult create(BlogPostEditApiViewModel inModel)
        {
            var blog = _blogService.Get(inModel.BlogId, VersionOptions.Latest).As<BlogPart>();

            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            var blogPost = Services.ContentManager.New<BlogPostPart>("BlogPost");
            blogPost.BlogPart = blog;

            if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't create blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't create blog post" });

            Services.ContentManager.Create(blogPost, VersionOptions.Draft);
            var model = Services.ContentManager.UpdateEditor(blogPost, _updateModelHandler.SetData(inModel));

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
        public IHttpActionResult update(BlogPostEditApiViewModel inModel)
        {
            if (inModel.IsPublished != null && (bool)inModel.IsPublished)
                return EditAndPublishPOST(inModel.BlogId, inModel.Id, inModel);
            else
                return EditPOST(inModel.BlogId, inModel.Id, inModel);
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
            var blogPost = _blogPostService.Get(postId);//, VersionOptions.DraftRequired);
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
            var blogPost = _blogPostService.Get(postId);//, VersionOptions.DraftRequired);
            if (blogPost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't edit blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit blog post" });

            // Validate form input
            var model = Services.ContentManager.UpdateEditor(blogPost, _updateModelHandler.SetData(inModel));

            conditionallyPublish(blogPost.ContentItem);

            Services.Notifier.Information(T("Your {0} has been saved.", blogPost.TypeDefinition.DisplayName));

            return Ok(new ResultViewModel { Content = new { Id = blogPost.Id}, Success = true,  Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(BlogPostEditApiViewModel inModel)
        {
            //refactoring: test PublishBlogPost/PublishBlogPost in addition if published

            var blog = _blogService.Get(inModel.BlogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var post = _blogPostService.Get(inModel.Id, VersionOptions.Latest);
            if (post == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.DeleteBlogPost, post, T("Couldn't delete blog post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete blog post" });

            _blogPostService.Delete(post);
            Services.Notifier.Information(T("Blog post was successfully deleted"));

            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(BlogPostsIndexApiViewModel inModel)
        {
            if (inModel == null || inModel.Id == null || inModel.BlogId == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });


            var blog = _blogService.Get((int)inModel.BlogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var blogpost = _blogPostService.Get((int)inModel.Id);

            if (blogpost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var model = Services.ContentManager.BuildEditor(blogpost);
            blogpost.Data = UpdateModelHandler.GetData(model);

            return Ok(new ResultViewModel { Content = blogpost, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult query(BlogPostsIndexApiViewModel inModel)
        {

            Pager pager = null;

            if(inModel == null || inModel.BlogId == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            BlogPart blogPart = _blogService.Get((int)inModel.BlogId, VersionOptions.Latest).As<BlogPart>();

            if (blogPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var totalItemCount = _blogPostService.PostCount(blogPart, VersionOptions.Latest);
            if (inModel != null && inModel.Pager != null)
                pager = new Pager(_siteService.GetSiteSettings(), inModel.Pager.Page, inModel.Pager.PageSize, totalItemCount);

            IList<BlogPostPart> blogPosts;

            if (pager != null)
            {
                blogPosts = _blogPostService.Get(blogPart, pager.GetStartIndex(), pager.PageSize, VersionOptions.Latest).Select( x => getData(x.As<BlogPostPart>())).ToList();
                pager.PageSize = blogPosts.Count;
            }
            else
                blogPosts = _blogPostService.Get(blogPart, VersionOptions.Latest).Select(x => getData(x.As<BlogPostPart>())).ToList();

            BlogPostsIndexApiViewModel model = new BlogPostsIndexApiViewModel { BlogId = inModel.BlogId, Data = blogPosts, Pager = pager };

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private BlogPostPart getData(BlogPostPart part)
        {
            var model = Services.ContentManager.BuildEditor(part);
            part.Data = UpdateModelHandler.GetData(model);
            return part;
        }
    }
}