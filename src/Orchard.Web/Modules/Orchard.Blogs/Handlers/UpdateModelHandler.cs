using Newtonsoft.Json;
using Orchard.Blogs.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using System;
using System.Xml;

namespace Orchard.Blogs.Handlers
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
                if (typeof(BlogPart) == model.GetType())
                {
                    dynamic _model = model;
                    _model.Description = fields.GetValue("Description").ToString();
                    _model.PostsPerPage = (int)fields.GetValue("PostsPerPage");
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