using System;
using System.Linq;
using System.Web.Http;
using Orchard.Blogs.Extensions;
using Orchard.Blogs.Models;
using Orchard.Blogs.Routing;
using Orchard.Blogs.Services;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Feeds;
using Orchard.DisplayManagement;
using Orchard.Localization;

namespace Orchard.Blogs.Controllers {
    [Authorize]
    public class BlogPostApiController : ApiController {
        private readonly IOrchardServices _services;
        private readonly IBlogService _blogService;
        private readonly IBlogPostService _blogPostService;
        private readonly IFeedManager _feedManager;
        private readonly IArchiveConstraint _archiveConstraint;

        public BlogPostApiController(
            IOrchardServices services, 
            IBlogService blogService, 
            IBlogPostService blogPostService,
            IFeedManager feedManager,
            IShapeFactory shapeFactory,
            IArchiveConstraint archiveConstraint) {
            _services = services;
            _blogService = blogService;
            _blogPostService = blogPostService;
            _feedManager = feedManager;
            _archiveConstraint = archiveConstraint;
            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public IHttpActionResult ListByArchive(string path) {
            string message = "";
            string code = "200";

            var blogPath = _archiveConstraint.FindPath(path);
            var archive = _archiveConstraint.FindArchiveData(path);

            if (blogPath == null)
                return NotFound();

            if (archive == null)
                return NotFound();

            BlogPart blogPart = _blogService.Get(blogPath);
            if (blogPart == null)
                return NotFound();


            if (archive.ToDateTime() == DateTime.MinValue) {
                Shape.Parts_Blogs_BlogArchives(Blog: blogPart, Archives: _blogPostService.GetArchives(blogPart));
                return Ok();
                // render the archive data
                //return new ShapeResult(this, Shape.Parts_Blogs_BlogArchives(Blog: blogPart, Archives: _blogPostService.GetArchives(blogPart)));
            }

            var list = Shape.List();
            list.AddRange(_blogPostService.Get(blogPart, archive).Select(b => _services.ContentManager.BuildDisplay(b, "Summary")));

            _feedManager.Register(blogPart, _services.ContentManager.GetItemMetadata(blogPart).DisplayText);

            var viewModel = Shape.ViewModel()
                .ContentItems(list)
                .Blog(blogPart)
                .ArchiveData(archive);

            ResultViewModel outModel = new ResultViewModel { Content = viewModel, Code = code, Message = message };
            return Ok(outModel);
        }
    }
}