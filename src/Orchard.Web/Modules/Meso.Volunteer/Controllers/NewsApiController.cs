using System.Linq;
using Orchard.Blogs.Models;
using Orchard.Blogs.Services;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.UI.Notify;
using System.Web.Http;
using Orchard.Core.Common.ViewModels;
using Orchard.Blogs.ViewModels;
using System.Net;
using System.Web;
using Orchard;
using Orchard.Blogs;
using Meso.Volunteer.Handlers;
using Newtonsoft.Json.Linq;
using Meso.Volunteer.ViewModels;

namespace Meso.Volunteer.Controllers {

    [Authorize]
    public class NewsApiController : ApiController {
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly IContentManager _contentManager;
        private readonly ITransactionManager _transactionManager;
        private readonly INewsUpdateModelHandler _updateModelHandler;

        public NewsApiController(
            IOrchardServices services,
            IBlogService blogService,
            IBlogPostService blogPostService,
            IContentManager contentManager,
            ITransactionManager transactionManager,
            IShapeFactory shapeFactory,
            INewsUpdateModelHandler updateModelHandler) {
            Services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            _contentManager = contentManager;
            _transactionManager = transactionManager;
            _updateModelHandler = updateModelHandler;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }
        public IOrchardServices Services { get; set; }

        [HttpPost]
        public IHttpActionResult create(JObject inModel) {
            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, T("Couldn't create content")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't create content" });

            //bool state = ModelState.IsValid;

            BlogPart content = Services.ContentManager.New<BlogPart>("Blog");

            _contentManager.Create(content, VersionOptions.Draft);
            _contentManager.UpdateEditor(content, _updateModelHandler.SetData(inModel));

            _contentManager.Publish(content.ContentItem);

            return Ok(new ResultViewModel { Content = content, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(JObject inModel) {

            if (inModel == null || inModel["Id"] == null)
                return BadRequest();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            int id = (int)inModel["Id"];
            BlogPart blog = _blogService.Get(id, VersionOptions.Latest).As<BlogPart>();

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Not allowed to find content")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not allowed to edit blog" });

            if (blog == null)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            return Ok(new ResultViewModel { Content = blog, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(JObject inModel) {
            if (inModel == null || inModel["Id"] == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            if (!Services.Authorizer.Authorize(Permissions.DeleteBlogPost, T("Couldn't delete blog")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete blog" });

            int id = (int)inModel["Id"];

            var blog = _blogService.Get(id, VersionOptions.Latest);
            if (blog == null)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            _blogService.Delete(blog);

            Services.Notifier.Information(T("Blog deleted"));

            return Ok(new ResultViewModel { Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult update(BlogEditApiViewModel inModel) {

            if(inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            var blog = _blogService.Get(inModel.Id, VersionOptions.DraftRequired);

            if (blog == null)
                return NotFound();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Couldn't edit blog")))
                return Unauthorized();// Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit blog" });

             Services.ContentManager.UpdateEditor(blog, _updateModelHandler.SetData(inModel));

            _contentManager.Publish(blog);
            Services.Notifier.Information(T("Blog information updated"));

            return Ok(new ResultViewModel { Content = blog.As<BlogPart>(), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult query(JObject inModel) {

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
            var model = new {
                Data = list,
                TotalCount = totalCount
            };

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

        }

    }
}