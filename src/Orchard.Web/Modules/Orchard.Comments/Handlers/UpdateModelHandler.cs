using Newtonsoft.Json;
using Orchard.Comments.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using System;
using System.Xml;

namespace Orchard.Comments.Handlers
{
    public class UpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, IUpdateModel
    {
        public UpdateModelHandler(object _field) : base(_field)
        {

        }

        public new bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            if (fields != null)
            {
                if (typeof(CommentPart) == model.GetType())
                {
                    dynamic _model = model;

                    _model.CommentText = fields.GetValue("CommentText").ToString();
                    _model.CommentedOn = (int)fields.GetValue("CommentedOn");
                    _model.RepliedOn = (int)fields.GetValue("RepliedOn");
                    return true;
                }
                else
                {
                    return base.TryUpdateModel(model, prefix, includeProperties, excludeProperties);
                }
            }

            return false;
        }
    }
}