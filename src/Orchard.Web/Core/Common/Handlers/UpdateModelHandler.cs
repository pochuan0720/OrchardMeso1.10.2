using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard.ContentManagement;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Orchard.Core.Common.Handlers
{
    public class UpdateModelHandler : IUpdateModel
    {
        protected JObject fields = null;

        public UpdateModelHandler(object _fields)
        {
            if (_fields != null)
            {
                fields = JObject.FromObject(_fields);
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

                //string path = "/Data";
                string type = model.GetType().ToString();
                dynamic _model = model;
                if (type.Equals("Orchard.MediaLibrary.ViewModels.MediaLibraryPickerFieldViewModel"))
                {
                    _model.SelectedIds = string.Join(",", fields.GetValue(prefix));
                }
                else if (type.Equals("Orchard.Fields.Fields.InputField") ||
                    type.Equals("Orchard.Fields.Fields.EnumerationField") ||
                    type.Equals("Orchard.Fields.ViewModels.NumericFieldViewModel"))
                {
                    _model.Value = fields.GetValue(prefix).ToString();
                }
                else if (type.Equals("Orchard.Fields.ViewModels.DateTimeFieldViewModel"))
                {
                    _model.Value = (DateTime)fields.GetValue(prefix);
                }
                else if (type.Equals("Orchard.Taxonomies.ViewModels.TaxonomyFieldViewModel"))
                {
                    _model.Terms = CreateEntryList(prefix, "Orchard.Taxonomies.ViewModels.TermEntry, Orchard.Taxonomies");
                }
                else if (type.Equals("Orchard.Roles.ViewModels.UserRolesViewModel"))
                {
                    _model.Roles = CreateEntryList(prefix, "Orchard.Roles.ViewModels.UserRoleEntry, Orchard.Roles");
                }
                else if (typeof(TextFieldDriverViewModel) == model.GetType())
                {
                    //path = path  + "/ " + prefix.Replace(".", "/");

                    //var viewModel = model as TextFieldDriverViewModel;
                    _model.Text = fields.GetValue(prefix).ToString();
                    return true;
                }
                else if (typeof(TitlePart) == model.GetType())
                {
                    //var part = model as TitlePart;
                    _model.Title = fields.GetValue("Title").ToString();//fields.SelectSingleNode("/Data/Title").InnerText;
                }
                else if (typeof(BodyEditorViewModel) == model.GetType())
                {
                    //var viewModel = model as BodyEditorViewModel;
                    _model.Text = fields.GetValue("Text").ToString(); //fields.SelectSingleNode(path + "/Text").InnerText;
                }
                else
                    return false;
            }
            else
                return false;

            return true;
        }

        private dynamic CreateEntryList(string prefix, string typeName)
        {
            Type elementType = Type.GetType(typeName, true);
            Type listType = typeof(List<>).MakeGenericType(new Type[] { elementType });
            dynamic checkeds = Activator.CreateInstance(listType);

            //IList<> checkedTerms = new List<>();
            foreach (var value in fields.GetValue(prefix))
            {
                dynamic entry = Activator.CreateInstance(elementType);

                //TermEntry
                try
                {
                    entry.Id = int.Parse(value.ToString());
                    entry.IsChecked = true;
                } catch (Exception e) {};

                //UserRoleEntry
                try
                {
                    entry.RoleId = int.Parse(value.ToString());
                    entry.Granted = true;
                } catch (Exception e) {};
                checkeds.Add(entry);
            }

            return checkeds;
        }
    }
}