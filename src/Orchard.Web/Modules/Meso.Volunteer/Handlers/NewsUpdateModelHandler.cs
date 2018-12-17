using Orchard.Blogs.Models;
using Orchard.Core.Common.ViewModels;
using Orchard.Localization.Services;

namespace Meso.Volunteer.Handlers
{
    public class NewsUpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, INewsUpdateModelHandler
    {
        public NewsUpdateModelHandler(IDateLocalizationServices dateLocalizationServices) : base(dateLocalizationServices)
        {
        }

        public override bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            if (root != null)
            {
                dynamic _model = model;
                if (typeof(BlogPart) == model.GetType())
                {
                    _model.Description = root["Description"].ToString();
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

        INewsUpdateModelHandler INewsUpdateModelHandler.SetData(object _root)
        {
            base.SetData(_root);
            return this;
        }
    }
}