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
        private readonly IUpdateModelHandler _updateModelHandler;

        public BlogApiController(
            IOrchardServices services,
            IBlogService blogService,
            IBlogPostService blogPostService,
            IContentManager contentManager,
            ITransactionManager transactionManager,
            IShapeFactory shapeFactory,
            IUpdateModelHandler updateModelHandler) {
            Services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            _contentManager = contentManager;
            _transactionManager = transactionManager;
            _updateModelHandler = updateModelHandler;
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
            _contentManager.UpdateEditor(blog, _updateModelHandler.SetData(inModel));

            _contentManager.Publish(blog.ContentItem);

            return Ok(new ResultViewModel { Content = blog, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(BlogsIndexApiViewModel inModel) {

            if(inModel == null || inModel.Id == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            BlogPart blog = _blogService.Get((int)inModel.Id, VersionOptions.Latest).As<BlogPart>();

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Not allowed to edit blog")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Not allowed to edit blog" });

            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});


            return Ok(new ResultViewModel { Content = blog, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(BlogEditApiViewModel inModel) {
            if (inModel == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, T("Couldn't delete blog")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete blog" });

            var blog = _blogService.Get(inModel.Id, VersionOptions.DraftRequired);
            if (blog == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

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
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound)});

            if (!Services.Authorizer.Authorize(Permissions.ManageBlogs, blog, T("Couldn't edit blog")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit blog" });

             Services.ContentManager.UpdateEditor(blog, _updateModelHandler.SetData(inModel));

            _contentManager.Publish(blog);
            Services.Notifier.Information(T("Blog information updated"));

            return Ok(new ResultViewModel { Content = blog.As<BlogPart>(), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult query(BlogsIndexApiViewModel inModel) {

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
                Data = list,
                TotalCount = totalCount
            };

            return Ok(new ResultViewModel { Content = model, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });

        }

    }
}