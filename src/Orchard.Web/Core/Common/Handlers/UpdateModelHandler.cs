using Newtonsoft.Json;
using Orchard.ContentManagement;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using System;
using System.Xml;

namespace Orchard.Core.Common.Handlers
{
    public class UpdateModelHandler : IUpdateModel
    {
        protected XmlDocument fields = null;

        public UpdateModelHandler(object _fields)
        {
            if (_fields != null)
            {
                string jsonObj = JsonConvert.SerializeObject(_fields);
                fields = JsonConvert.DeserializeXmlNode(jsonObj, "Data");
            }
        }

        public void AddModelError(string key, LocalizedString errorMessage)
        {
            throw new NotImplementedException();
        }

        public bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            if (fields != null)
            {
                string path = "/Data";
                if (typeof(TextFieldDriverViewModel) == model.GetType())
                {
                    path = path  + "/ " + prefix.Replace(".", "/");

                    var viewModel = model as TextFieldDriverViewModel;
                    viewModel.Text = fields.SelectSingleNode(path).InnerText;
                    return true;
                }
                else if (typeof(TitlePart) == model.GetType())
                {
                    var part = model as TitlePart;
                    part.Title = fields.SelectSingleNode("/Data/Title").InnerText;
                }
                else if (typeof(BodyEditorViewModel) == model.GetType())
                {
                    var viewModel = model as BodyEditorViewModel;
                    viewModel.Text = fields.SelectSingleNode(path + "/Text").InnerText;
                }
            }

            return false;
        }
    }
}