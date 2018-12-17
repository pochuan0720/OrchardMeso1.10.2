using Orchard.Comments.Models;
using Orchard.Localization.Services;
namespace Meso.Volunteer.Handlers
{
    public class CommentUpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, ICommentUpdateModelHandler
    {
        public CommentUpdateModelHandler(IDateLocalizationServices dateLocalizationServices) : base(dateLocalizationServices)
        {
        }

        public override bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            if (root != null)
            {
                if (typeof(CommentPart) == model.GetType())
                {
                    dynamic _model = model;

                    _model.CommentText = root["CommentText"].ToString();
                    _model.CommentedOn = (int)root["CommentedOn"];
                    _model.RepliedOn = (int)root["RepliedOn"];
                    return true;
                }
                else
                {
                    return base.TryUpdateModel(model, prefix, includeProperties, excludeProperties);
                }
            }

            return false;
        }

        ICommentUpdateModelHandler ICommentUpdateModelHandler.SetData(object _root)
        {
            base.SetData(_root);
            return this;
        }
    }
}