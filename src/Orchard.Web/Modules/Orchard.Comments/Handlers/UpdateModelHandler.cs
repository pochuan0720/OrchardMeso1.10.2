using Orchard.Comments.Models;
using Orchard.Localization.Services;
namespace Orchard.Comments.Handlers
{
    public class UpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, IUpdateModelHandler
    {
        public UpdateModelHandler(IDateLocalizationServices dateLocalizationServices) : base(dateLocalizationServices)
        {
        }

        public override bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            if (fields != null)
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

        IUpdateModelHandler IUpdateModelHandler.SetData(object _root)
        {
            base.SetData(_root);
            return this;
        }
    }
}