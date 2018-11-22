using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard.ContentManagement;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Orchard.Core.Common.Handlers
{
    public class UpdateModelHandler : IUpdateModel
    {
        protected JObject fields = null;

        public UpdateModelHandler()
        {
        }

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
                    DateTime dt = (DateTime)fields.GetValue(prefix);
                    string date = dt.Date.ToString("MM/dd/yyyy");
                    string time = dt.Date.ToString("h:mm", new CultureInfo("en-US"));
                    _model.Editor = new DateTimeEditor() {
                        Date = date,
                        Time = time,
                    };
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

        static public JObject GetData(dynamic model)
        {
            JObject obj = new JObject();

            foreach (var item in model.Content.Items)
            {
                if (item.TemplateName != null)
                {
                    if (item.TemplateName.Equals("Fields/Input.Edit") || item.TemplateName.Equals("Fields/Enumeration.Edit"))
                        obj.Add(new JProperty(item.Prefix, item.Model.Value));
                    else if (item.TemplateName.Equals("Fields/Numeric.Edit"))
                        if (string.IsNullOrEmpty(item.Model.Value))
                            obj.Add(new JProperty(item.Prefix, ""));
                        else
                            obj.Add(new JProperty(item.Prefix, int.Parse(item.Model.Value)));
                    else if (item.TemplateName.Equals("Fields/DateTime.Edit"))
                        obj.Add(new JProperty(item.Prefix, item.ContentField.DateTime));
                    else if (item.TemplateName.Equals("Fields/MediaLibraryPicker.Edit"))
                        obj.Add(new JProperty(item.Prefix, item.Model.Field.Ids));
                    else if (item.TemplateName.Equals("Fields/TaxonomyField"))
                    {
                        var viewModel = item.Model;
                        var checkedTerms = new List<int>();
                        foreach (var term in viewModel.Terms)
                        {
                            if (term.IsChecked)
                                checkedTerms.Add(term.Id);
                        }
                        obj.Add(new JProperty(item.Prefix, checkedTerms.ToArray()));
                    }

                    else if (item.TemplateName.Equals("Parts/Roles.UserRoles"))
                        obj.Add(new JProperty(item.Prefix, item.Model.UserRoles.Roles));
                    else
                        obj.Add(new JProperty(item.Prefix, item.TemplateName));
                }
            }

            return obj;
        }
    }
}