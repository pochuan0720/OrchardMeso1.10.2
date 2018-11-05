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
                    string path = "/Data";
                    BlogPart viewModel = model as BlogPart;
                    if (fields.SelectSingleNode(path + "/Description") != null)
                        viewModel.Description = fields.SelectSingleNode(path + "/Description").InnerText;
                    if(fields.SelectSingleNode(path + "/PostsPerPage") != null)
                        viewModel.PostsPerPage = int.Parse(fields.SelectSingleNode(path + "/PostsPerPage").InnerText);
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