using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Orchard.Comments.Models;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Mvc.Extensions;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;
using Orchard.Comments.ViewModels;
using Orchard.Comments.Services;

namespace Orchard.Comments.Controllers {
    using Orchard.Core.Common.ViewModels;
    using Orchard.Settings;
    using System.Net;

    [Authorize]
    public class CommentApiController : ApiController {
        private readonly IOrchardServices _orchardServices;
        private readonly ICommentService _commentService;
        private readonly ISiteService _siteService;
        private readonly IContentManager _contentManager;

        public CommentApiController(
            IOrchardServices orchardServices,
            ICommentService commentService,
            ISiteService siteService,
            IShapeFactory shapeFactory) {
            _orchardServices = orchardServices;
            _commentService = commentService;
            _siteService = siteService;
            _contentManager = _orchardServices.ContentManager;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }
        dynamic Shape { get; set; }

        [HttpPost]
        public IHttpActionResult details(int contentId, CommentDetailsOptions options) {
            // Default options
            if (options == null)
                options = new CommentDetailsOptions();

            // Filtering
            IContentQuery<CommentPart, CommentPartRecord> comments;
            switch (options.Filter) {
                case CommentDetailsFilter.All:
                    comments = _commentService.GetCommentsForCommentedContent(contentId);
                    break;
                case CommentDetailsFilter.Approved:
                    comments = _commentService.GetCommentsForCommentedContent(contentId, CommentStatus.Approved);
                    break;
                case CommentDetailsFilter.Pending:
                    comments = _commentService.GetCommentsForCommentedContent(contentId, CommentStatus.Pending);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var entries = comments.List().Select(comment => comment.Record).ToList();

            return Ok(new ResultViewModel { Content = entries, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private CommentEntry CreateCommentEntry(CommentPart item)
        {
            return new CommentEntry
            {
                Comment = item.Record,
                //CommentedOn = _commentService.GetCommentedContent(item.CommentedOn),
                //IsChecked = false,
            };
        }
    }
}
