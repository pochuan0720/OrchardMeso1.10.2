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
    using Orchard.Comments.Handlers;
    using Orchard.Core.Common.ViewModels;
    using Orchard.Settings;
    using System.Net;
    using System.Web;

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
            if(entries.Count==0)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = entries, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult create(CommentEditApiViewModel inModel)
        {
            if (!_orchardServices.Authorizer.Authorize(Permissions.AddComment, T("Couldn't add comment")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't add comment" });

            var comment = _orchardServices.ContentManager.New<CommentPart>("Comment");
            var editorShape = _orchardServices.ContentManager.UpdateEditor(comment, new UpdateModelHandler(inModel));


            if (ModelState.IsValid)
            {
                _orchardServices.ContentManager.Create(comment);

                var commentPart = comment.As<CommentPart>();

                var commentsPart = _orchardServices.ContentManager.Get(commentPart.CommentedOn).As<CommentsPart>();

                // is it a response to another comment ?
                if (commentPart.RepliedOn.HasValue && commentsPart != null && commentsPart.ThreadedComments)
                {
                    var replied = _orchardServices.ContentManager.Get(commentPart.RepliedOn.Value);
                    if (replied != null)
                    {
                        var repliedPart = replied.As<CommentPart>();

                        // what is the next position after the anwered comment
                        if (repliedPart != null)
                        {
                            // the next comment is the one right after the RepliedOn one, at the same level
                            var nextComment = _commentService.GetCommentsForCommentedContent(commentPart.CommentedOn)
                                .Where(x => x.RepliedOn == repliedPart.RepliedOn && x.CommentDateUtc > repliedPart.CommentDateUtc)
                                .OrderBy(x => x.Position)
                                .Slice(0, 1)
                                .FirstOrDefault();

                            // the previous comment is the last one under the RepliedOn
                            var previousComment = _commentService.GetCommentsForCommentedContent(commentPart.CommentedOn)
                                .Where(x => x.RepliedOn == commentPart.RepliedOn)
                                .OrderByDescending(x => x.Position)
                                .Slice(0, 1)
                                .FirstOrDefault();

                            if (nextComment == null)
                            {
                                commentPart.Position = repliedPart.Position + 1;
                            }
                            else
                            {
                                if (previousComment == null)
                                {
                                    commentPart.Position = (repliedPart.Position + nextComment.Position) / 2;
                                }
                                else
                                {
                                    commentPart.Position = (previousComment.Position + nextComment.Position) / 2;
                                }
                            }
                        }
                    }

                }
                else
                {
                    // new comment, last in position
                    commentPart.RepliedOn = null;
                    commentPart.Position = comment.Id;
                }

                if (commentPart.Status == CommentStatus.Pending)
                {
                    // if the user who submitted the comment has the right to moderate, don't make this comment moderated
                    if (_orchardServices.Authorizer.Authorize(Permissions.ManageComments))
                    {
                        commentPart.Status = CommentStatus.Approved;
                        _orchardServices.Notifier.Information(T("Your comment has been posted."));
                    }
                    else
                    {
                        _orchardServices.Notifier.Information(T("Your comment will appear after the site administrator approves it."));
                    }
                }
                else
                {
                    _orchardServices.Notifier.Information(T("Your comment has been posted."));
                }

                // send email notification
                var siteSettings = _orchardServices.WorkContext.CurrentSite.As<CommentSettingsPart>();
                if (siteSettings.NotificationEmail)
                {
                    _commentService.SendNotificationEmail(commentPart);
                }
                return Ok(new ResultViewModel { Content = commentPart, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
            }
            else
            {
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.BadRequest.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.BadRequest) });
            }
        }

        [HttpPost]
        public IHttpActionResult update(int contentId, CommentEditApiViewModel inModel)
        {
            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageComments, T("Couldn't edit comment")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit comment" });

            var commentPart = _contentManager.Get<CommentPart>(contentId);

            if(commentPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var editorShape = _contentManager.UpdateEditor(commentPart, new UpdateModelHandler(inModel));

            return Ok(new ResultViewModel { Content = commentPart, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(int contentId)
        {
            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageComments, T("Couldn't delete comment")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete comment" });

            var commentPart = _contentManager.Get<CommentPart>(contentId);
            if (commentPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            int commentedOn = commentPart.Record.CommentedOn;
            _commentService.DeleteComment(contentId);

            return Ok(new ResultViewModel { Content = commentPart, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
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
