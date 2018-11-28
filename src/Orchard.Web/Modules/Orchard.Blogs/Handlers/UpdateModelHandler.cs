using Newtonsoft.Json;
using Orchard.Blogs.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using Orchard.Localization.Services;
using System;
using System.Xml;

namespace Orchard.Blogs.Handlers
{
    public class UpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler
    {
        public UpdateModelHandler(IDateLocalizationServices dateLocalizationServices) : base(dateLocalizationServices)
        {
        }

        public new bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            if (fields != null)
            {
                dynamic _model = model;
                if (typeof(BlogPart) == model.GetType())
                {
                    _model.Description = root["Description"].ToString();
                    _model.PostsPerPage = (int)root["PostsPerPage"];
                    return true;
                }
                else if (typeof(BodyEditorViewModel) == model.GetType())
                {
                    //var viewModel = model as BodyEditorViewModel;
                    _model.Text = root["Text"].ToString(); //fields.SelectSingleNode(path + "/Text").InnerText;
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