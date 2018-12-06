using System;
using System.Linq;
using System.Web.Http;
using Orchard.Comments.Models;
using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.UI.Notify;
using Orchard.Comments.ViewModels;
using Orchard.Comments.Services;
using Orchard.Settings;
using Orchard.Comments.Handlers;
using Orchard.Core.Common.ViewModels;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Orchard.Comments.Controllers {

    [Authorize]
    public class CommentApiController : ApiController {
        private readonly IOrchardServices _orchardServices;
        private readonly ICommentService _commentService;
        private readonly ISiteService _siteService;
        private readonly IContentManager _contentManager;
        private readonly IUpdateModelHandler _updateModelHandler;

        public CommentApiController(
            IOrchardServices orchardServices,
            ICommentService commentService,
            ISiteService siteService,
            IShapeFactory shapeFactory,
            IUpdateModelHandler updateModelHandler) {
            _orchardServices = orchardServices;
            _commentService = commentService;
            _siteService = siteService;
            _contentManager = _orchardServices.ContentManager;
            _updateModelHandler = updateModelHandler;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
            Shape = shapeFactory;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }
        dynamic Shape { get; set; }

        /*[HttpPost]
        public IHttpActionResult query(CommentsDetailsViewModel inModel) {
            // Default options
            CommentDetailsOptions options = inModel.Options;

            if (options == null)
                options = new CommentDetailsOptions();

            // Filtering
            IContentQuery<CommentPart, CommentPartRecord> comments;
            switch (options.Filter) {
                case CommentDetailsFilter.All:
                    comments = _commentService.GetCommentsForCommentedContent(inModel.CommentedItemId);
                    break;
                case CommentDetailsFilter.Approved:
                    comments = _commentService.GetCommentsForCommentedContent(inModel.CommentedItemId, CommentStatus.Approved);
                    break;
                case CommentDetailsFilter.Pending:
                    comments = _commentService.GetCommentsForCommentedContent(inModel.CommentedItemId, CommentStatus.Pending);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            var entries = comments.List();
            if(entries.Count() == 0)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = entries, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }*/

        [HttpPost]
        public IHttpActionResult query(CommentsDetailsViewModel inModel)
        {

            var commentsPart = _orchardServices.ContentManager.Get(inModel.CommentedItemId).As<CommentsPart>();

            if(commentsPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });


            var model = _orchardServices.ContentManager.BuildDisplay(commentsPart);

            object obj = null;
            foreach (var item in model.Content.Items)
            {
                if (item.List != null)
                {
                    var List = item.List;
                    int count = item.CommentCount;
                    //object obj = printItems(new List<object>(), List);
                    obj = printItems(List);
                    break;// string name = item.TemplateName;
                }
            }


            //outModel.Data = UpdateModelHandler.GetData(model);

            return Ok(new ResultViewModel { Content = obj, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        private JObject printItems(dynamic parent)
        {

            JObject obj = new JObject();
            if (parent.ContentPart != null)
            {
                JToken token = JToken.FromObject(parent.ContentPart);
                obj.Add("Item", token);
            }
            //else
            //    return parent.Items[0] == null ? null : printItems(parent.Items[0]);


            JArray list = new JArray();
            foreach (dynamic item in parent.Items)
            {
                JToken token = printItems(item);
                list.Add(token);
            }
            obj.Add("Items", list);
            return obj;

        }

        /*private object printItems(List<object> _list, dynamic parent)
        {
            if (parent.Items.Count == 0)
            {
                //_list.Add(parent.ContentPart);
                return parent.ContentPart;// _list;
            }
            else
            {
                foreach (dynamic item in parent.Items)
                {
                    _list.Add(item.ContentPart);
                    object result = printItems(new List<object>(), item);
                    if(result is List<object>)
                        _list.Add(result);
                }
                return _list;
            }
        }*/

        [HttpPost]
        public IHttpActionResult create(CommentEditApiViewModel inModel)
        {
            if (!_orchardServices.Authorizer.Authorize(Permissions.AddComment, T("Couldn't add comment")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't add comment" });

            var comment = _orchardServices.ContentManager.New<CommentPart>("Comment");
            var editorShape = _orchardServices.ContentManager.UpdateEditor(comment, _updateModelHandler.SetData(inModel));


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
        public IHttpActionResult update(CommentEditApiViewModel inModel)
        {
            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageComments, T("Couldn't edit comment")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't edit comment" });

            var commentPart = _contentManager.Get<CommentPart>(inModel.Id);

            if(commentPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            var editorShape = _contentManager.UpdateEditor(commentPart, _updateModelHandler.SetData(inModel));

            return Ok(new ResultViewModel { Content = commentPart, Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult find(CommentEditApiViewModel inModel)
        {

            var commentPart = _contentManager.Get<CommentPart>(inModel.Id);

            if (commentPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            return Ok(new ResultViewModel { Content = commentPart.As<CommentPart>(), Success = true, Code = HttpStatusCode.OK.ToString("d"), Message = "" });
        }

        [HttpPost]
        public IHttpActionResult delete(CommentEditApiViewModel inModel)
        {
            if (!_orchardServices.Authorizer.Authorize(Permissions.ManageComments, T("Couldn't delete comment")))
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.Unauthorized.ToString("d"), Message = "Couldn't delete comment" });

            var commentPart = _contentManager.Get<CommentPart>(inModel.Id);
            if (commentPart == null)
                return Ok(new ResultViewModel { Success = false, Code = HttpStatusCode.NotFound.ToString("d"), Message = HttpWorkerRequest.GetStatusDescription((int)HttpStatusCode.NotFound) });

            int commentedOn = commentPart.Record.CommentedOn;
            _commentService.DeleteComment(inModel.Id);

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
