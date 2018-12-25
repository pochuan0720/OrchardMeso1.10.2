using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using Meso.Volunteer.Handlers;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Blogs;
using Orchard.Blogs.Handlers;
using Orchard.Blogs.Models;
using Orchard.Blogs.Services;
using Orchard.Blogs.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Contents.Settings;
using Orchard.Localization;
using Orchard.Settings;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;

namespace Meso.Volunteer.Controllers {

    /// <summary>
    /// TODO: (PH:Autoroute) This replicates a whole lot of Core.Contents functionality. All we actually need to do is take the BlogId from the query string in the BlogPostPartDriver, and remove
    /// helper extensions from UrlHelperExtensions.
    /// </summary>
    [Authorize]
    public class NewsPostApiController : ApiController {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly ISiteService _siteService;
        private readonly INewsUpdateModelHandler _updateModelHandler;

        public NewsPostApiController(IOrchardServices services, IBlogService blogService, IBlogPostService blogPostService, ISiteService siteService, INewsUpdateModelHandler updateModelHandler) {
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
        public IHttpActionResult create(JObject inModel)
        {
            if(inModel == null && inModel["NewsId"] == null)
                return BadRequest();

            int newsId = (int)inModel["NewsId"];
            var content = _blogService.Get(newsId, VersionOptions.Latest).As<BlogPart>();

            if (content == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            var blogPost = Services.ContentManager.New<BlogPostPart>("BlogPost");
            blogPost.BlogPart = content;

            if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't create news post")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't create news post" });

            Services.ContentManager.Create(blogPost, VersionOptions.Draft);
            var model = Services.ContentManager.UpdateEditor(blogPost, _updateModelHandler.SetData(inModel));

            if (!Services.Authorizer.Authorize(Permissions.PublishBlogPost, blogPost.ContentItem, T("Couldn't publish blog post")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't publish blog post" });

            Services.ContentManager.Publish(blogPost.ContentItem);


            Services.Notifier.Information(T("Your {0} has been created.", blogPost.TypeDefinition.DisplayName));

            return Ok(new ResultViewModel { Content = new { Id = blogPost.Id}, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult update(JObject inModel)
        {
            if (inModel == null || inModel["NewsId"] == null || inModel["Id"] == null)
                return BadRequest();

            //if (inModel.IsPublished != null && (bool)inModel.IsPublished)
                return EditAndPublishPOST((int)inModel["NewsId"], (int)inModel["Id"], inModel);
            //else
            //    return EditPOST(inModel.BlogId, inModel.Id, inModel);
        }

        private IHttpActionResult EditPOST(int blogId, int postId, JObject inModel)
        {
            return EditPOST(blogId, postId, inModel, contentItem => {
                if (!contentItem.Has<IPublishingControlAspect>() && !contentItem.TypeDefinition.Settings.GetModel<ContentTypeSettings>().Draftable)
                    Services.ContentManager.Publish(contentItem);
            });
        }

        private IHttpActionResult EditAndPublishPOST(int blogId, int postId, JObject inModel)
        {
            var blog = _blogService.Get(blogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            // Get draft (create a new version if needed)
            var blogPost = _blogPostService.Get(postId);//, VersionOptions.DraftRequired);
            if (blogPost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.PublishBlogPost, blogPost, T("Couldn't publish news post")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't publish news post" });

            return EditPOST(blogId, postId, inModel, contentItem => Services.ContentManager.Publish(contentItem));
        }

        private IHttpActionResult EditPOST(int blogId, int postId, JObject inModel, Action<ContentItem> conditionallyPublish)
        {
            var blog = _blogService.Get(blogId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            // Get draft (create a new version if needed)
            var blogPost = _blogPostService.Get(postId);//, VersionOptions.DraftRequired);
            if (blogPost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.EditBlogPost, blogPost, T("Couldn't edit news post")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit news post" });

            // Validate form input
            var model = Services.ContentManager.UpdateEditor(blogPost, _updateModelHandler.SetData(inModel));

            conditionallyPublish(blogPost.ContentItem);

            Services.Notifier.Information(T("Your {0} has been saved.", blogPost.TypeDefinition.DisplayName));

            return Ok(new ResultViewModel { Content = new { Id = blogPost.Id}, Success = true,  Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(JObject inModel)
        {
            //refactoring: test PublishBlogPost/PublishBlogPost in addition if published
            if (inModel == null || inModel["NewsId"] == null)
                return BadRequest();
            int newsId = (int)inModel["NewsId"];
            int postId = (int)inModel["Id"];
            var blog = _blogService.Get(newsId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var post = _blogPostService.Get(postId, VersionOptions.Latest);
            if (post == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            if (!Services.Authorizer.Authorize(Permissions.DeleteBlogPost, post, T("Couldn't delete news post")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete news post" });

            _blogPostService.Delete(post);
            Services.Notifier.Information(T("Blog post was successfully deleted"));

            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel)
        {
            if (inModel == null || inModel["Id"] == null || inModel["NewsId"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            int newsId = (int)inModel["NewsId"];
            int postId = (int)inModel["Id"];

            var blog = _blogService.Get(newsId, VersionOptions.Latest);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var blogpost = _blogPostService.Get(postId);

            if (blogpost == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var model = Services.ContentManager.BuildEditor(blogpost);

            return Ok(new ResultViewModel { Content = UpdateModelHandler.GetData(JObject.FromObject(blogpost), model), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult query(JObject inModel)
        {

            Pager pager = null;

            if (inModel == null || inModel["NewsId"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            int newsId = (int)inModel["NewsId"];

            BlogPart blogPart = _blogService.Get(newsId, VersionOptions.Latest).As<BlogPart>();

            if (blogPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var totalItemCount = _blogPostService.PostCount(blogPart, VersionOptions.Latest);

            if (inModel != null && inModel["Pager"] != null)
            {
                Pager _pager = inModel["Pager"].ToObject<Pager>();
                pager = new Pager(_siteService.GetSiteSettings(), _pager.Page, _pager.PageSize, totalItemCount);
            }

            IList<object> newsPosts;

            if (pager != null)
            {
                newsPosts = _blogPostService.Get(blogPart, pager.GetStartIndex(), pager.PageSize, VersionOptions.Latest).Select( x => getData(x.As<BlogPostPart>())).ToList();
                pager.PageSize = newsPosts.Count;
            }
            else
                newsPosts = _blogPostService.Get(blogPart, VersionOptions.Latest).Select(x => getData(x.As<BlogPostPart>())).ToList();

            return Ok(new ResultViewModel { Content = new { NewsId = newsId, Data = newsPosts, Pager = pager }, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private object getData(BlogPostPart part)
        {
            var model = Services.ContentManager.BuildEditor(part);
            JObject obj = UpdateModelHandler.GetData(JObject.FromObject(part), model);
            return obj;
        }
    }
}