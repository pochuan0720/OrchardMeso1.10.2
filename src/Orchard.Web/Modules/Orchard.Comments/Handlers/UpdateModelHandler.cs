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
                    string path = "/Data";
                    CommentPart viewModel = model as CommentPart;
                    if (fields.SelectSingleNode(path + "/CommentText") != null)
                        viewModel.CommentText = fields.SelectSingleNode(path + "/CommentText").InnerText;

                    if (fields.SelectSingleNode(path + "/CommentedOn") != null)
                        viewModel.CommentedOn = int.Parse(fields.SelectSingleNode(path + "/CommentedOn").InnerText);

                    if (fields.SelectSingleNode(path + "/RepliedOn") != null)
                        viewModel.RepliedOn = int.Parse(fields.SelectSingleNode(path + "/RepliedOn").InnerText);
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