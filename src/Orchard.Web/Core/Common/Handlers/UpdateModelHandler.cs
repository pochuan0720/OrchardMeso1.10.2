using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.OwnerEditor;
using Orchard.Core.Common.ViewModels;
using Orchard.Core.Containers.ViewModels;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using Orchard.Localization.Models;
using Orchard.Localization.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace Orchard.Core.Common.Handlers
{
    public class UpdateModelHandler : IUpdateModelHandler
    {
        protected JObject root = null;
        protected JToken fields = null;
        protected readonly IDateLocalizationServices _dateLocalizationServices;

        public UpdateModelHandler(IDateLocalizationServices dateLocalizationServices)
        {
            _dateLocalizationServices = dateLocalizationServices;
        }

        public IUpdateModelHandler SetData(object _root)
        {
            if (_root != null)
            {
                root = JObject.FromObject(_root);
                fields = root["Data"];
            }
            return this;
        }

        public void AddModelError(string key, LocalizedString errorMessage)
        {
            throw new NotImplementedException();
        }

        public bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            dynamic _model = model;
            string type = model.GetType().ToString();
            if (fields != null)
            {
                if (type.Equals("Orchard.MediaLibrary.ViewModels.MediaLibraryPickerFieldViewModel") && fields[prefix] != null)
                {
                    _model.SelectedIds = string.Join(",", fields[prefix]);
                    return true;
                }
                else if ((type.Equals("Orchard.Fields.Fields.InputField") ||
                    type.Equals("Orchard.Fields.ViewModels.NumericFieldViewModel")) && fields[prefix] != null)
                {
                    _model.Value = fields[prefix].ToString();
                    return true;
                }
                else if (type.Equals("Orchard.Fields.Fields.EnumerationField") && fields[prefix] != null)
                {
                    var node = fields[prefix];
                    _model.Value = node.ToString();
                    return true;
                }
                else if (type.Equals("Orchard.Fields.ViewModels.DateTimeFieldViewModel") && fields[prefix] != null)
                {
                    DateTime dt = (DateTime)fields[prefix];
                    string date = _dateLocalizationServices.ConvertToLocalizedDateString(dt, new DateLocalizationOptions() { EnableTimeZoneConversion = false });// dt.Date.ToString("MM/dd/yyyy");
                    string time = _dateLocalizationServices.ConvertToLocalizedTimeString(dt, new DateLocalizationOptions() { EnableTimeZoneConversion = false });// dt.Date.ToString("h:mm", new CultureInfo("en-US"));
                    _model.Editor = new DateTimeEditor()
                    {
                        Date = date,
                        Time = time,
                    };
                    return true;
                }
                else if (type.Equals("Orchard.Taxonomies.ViewModels.TaxonomyFieldViewModel") && fields[prefix] != null)
                {
                    _model.Terms = CreateEntryList(fields, prefix, "Orchard.Taxonomies.ViewModels.TermEntry, Orchard.Taxonomies");
                    return true;
                }
                else if (typeof(TextFieldDriverViewModel) == model.GetType() && fields[prefix] != null)
                {
                    _model.Text = fields[prefix].ToString();
                    return true;
                }
            }

            if (root != null)
            {
                if (typeof(TitlePart) == model.GetType() && root[prefix] != null)
                {
                    _model.Title = root[prefix].ToString();
                    return true;
                }
                else if (typeof(BodyEditorViewModel) == model.GetType() && root[prefix] != null )
                {
                    _model.Text = root[prefix].ToString();
                    return true;
                }
                else if (typeof(ContainerEditorViewModel) == model.GetType() && root["ContainerId"] != null)
                {
                    _model.ContainerId = (int)root["ContainerId"];
                    return true;
                }
                else if (typeof(ContainerViewModel) == model.GetType() && root[prefix] != null)
                {
                    _model.SelectedItemContentTypes = root[prefix].ToObject<List<string>>();
                    return true;
                }
                else if (typeof(OwnerEditorViewModel) == model.GetType() && root[prefix] != null)
                {
                    _model.Owner = root["Owner"].ToString();
                    return true;
                }
                else if (typeof(OwnerEditorViewModel) == model.GetType() && root[prefix] != null)
                {
                    _model.Owner = root["Owner"].ToString();
                    return true;
                }
                else if (type.Equals("Orchard.Roles.ViewModels.UserRolesViewModel") && root[prefix] != null)
                {
                    _model.Roles = CreateEntryList(root, prefix, "Orchard.Roles.ViewModels.UserRoleEntry, Orchard.Roles");
                    return true;
                }
            }

            return false;
        }

        private dynamic CreateEntryList(JToken data, string prefix, string typeName)
        {
            Type elementType = Type.GetType(typeName, true);
            Type listType = typeof(List<>).MakeGenericType(new Type[] { elementType });
            dynamic checkeds = Activator.CreateInstance(listType);

            //IList<> checkedTerms = new List<>();
            foreach (var value in data[prefix])
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


        static public JObject GetData( dynamic model)
        {
            JObject obj = new JObject();

            foreach (var item in model.Content.Items)
            {
                if (item.TemplateName != null)
                {
                    if (item.TemplateName.Equals("Fields/Input.Edit"))
                        obj.Add(new JProperty(item.Prefix, item.Model.Value));
                    else if (item.TemplateName.Equals("Fields/Enumeration.Edit"))
                    {
                        string value = item.Model.Value;
                        if (!string.IsNullOrEmpty(value) && (value.StartsWith(";") || value.EndsWith(";")))
                        {
                            string[] values = value.Split(';');
                            values = values.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            item.Model.Value = string.Join(";", values);

                        }
                        obj.Add(new JProperty(item.Prefix, item.Model.Value));
                    }
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
                    //else
                    //    obj.Add(new JProperty(item.Prefix, item.TemplateName));
                }
            }

            return obj;
        }
    }
}