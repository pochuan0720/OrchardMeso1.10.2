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
using Orchard.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Orchard.Core.Common.Handlers
{
    public class UpdateModelHandler : IUpdateModelHandler
    {
        protected JObject root = null;
        protected readonly IDateLocalizationServices _dateLocalizationServices;
        protected readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateModelHandler(IDateLocalizationServices dateLocalizationServices)
        {
            _dateLocalizationServices = dateLocalizationServices;
        }

        public UpdateModelHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public UpdateModelHandler(IDateLocalizationServices dateLocalizationServices, IHttpContextAccessor httpContextAccessor)
        {
            _dateLocalizationServices = dateLocalizationServices;
            _httpContextAccessor = httpContextAccessor;
        }

        public IUpdateModelHandler SetData(object _root)
        {
            if (_root != null)
            {
                root = JObject.FromObject(_root);
            }
            return this;
        }

        public void AddModelError(string key, LocalizedString errorMessage)
        {
            throw new NotImplementedException();
        }

        public virtual bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties) where TModel : class
        {
            dynamic _model = model;
            string type = model.GetType().ToString();
            if (root != null)
            {
                if (type.StartsWith("Orchard.Fields.Fields") || type.EndsWith("FieldViewModel"))
                {
                    prefix = prefix.Split('.')[1];
                    if (type.Equals("Orchard.MediaLibrary.ViewModels.MediaLibraryPickerFieldViewModel") && root[prefix] != null)
                    {
                        _model.SelectedIds = string.Join(",", root[prefix]);
                        return true;
                    }
                    else if ((type.Equals("Orchard.Fields.Fields.InputField") ||
                        type.Equals("Orchard.Fields.ViewModels.NumericFieldViewModel")) && root[prefix] != null)
                    {
                        _model.Value = root[prefix].ToString();
                        return true;
                    }
                    else if (type.Equals("Orchard.Fields.Fields.EnumerationField") && root[prefix] != null)
                    {
                        var node = root[prefix];
                        _model.Value = node.ToString();
                        return true;
                    }
                    else if (type.Equals("Orchard.Fields.ViewModels.DateTimeFieldViewModel") && root[prefix] != null)
                    {
                        DateTime dt = (DateTime)root[prefix];
                        string date = _dateLocalizationServices.ConvertToLocalizedDateString(dt, new DateLocalizationOptions());// dt.Date.ToString("MM/dd/yyyy");
                        string time = _dateLocalizationServices.ConvertToLocalizedTimeString(dt, new DateLocalizationOptions());// dt.Date.ToString("h:mm", new CultureInfo("en-US"));
                        _model.Editor = new DateTimeEditor()
                        {
                            Date = date,
                            Time = time,
                        };
                        return true;
                    }
                    else if (type.Equals("Orchard.Taxonomies.ViewModels.TaxonomyFieldViewModel") && root[prefix] != null)
                    {
                        _model.Terms = CreateEntryList(root, prefix, "Orchard.Taxonomies.ViewModels.TermEntry, Orchard.Taxonomies");
                        return true;
                    }
                    else if (typeof(TextFieldDriverViewModel) == model.GetType() && root[prefix] != null)
                    {
                        _model.Text = root[prefix].ToString();
                        return true;
                    }
                    else if (type.Equals("Orchard.Fields.Fields.BooleanField") && root[prefix] != null)
                    {
                        var node = root[prefix];
                        _model.Value = (bool)node;
                        return true;
                    }

                }
                else
                {
                    if (typeof(TitlePart) == model.GetType() && root[prefix] != null)
                    {
                        _model.Title = root[prefix].ToString();
                        return true;
                    }
                    else if (typeof(BodyEditorViewModel) == model.GetType() && root[prefix] != null)
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
                    else if (typeof(OwnerEditorViewModel) == model.GetType() && root["Owner"] != null)
                    {
                        _model.Owner = root["Owner"].ToString();
                        return true;
                    }
                    else if (type.Equals("Orchard.Roles.ViewModels.UserRolesViewModel") && root[prefix] != null)
                    {
                        _model.Roles = CreateEntryList(root, prefix, "Orchard.Roles.ViewModels.UserRoleEntry, Orchard.Roles");
                        return true;
                    }
                    else if (type.Equals("Orchard.PublishLater.ViewModels.PublishLaterViewModel") && root[prefix] != null)
                    {
                        var data = root[prefix];

                        if (!string.IsNullOrEmpty(data.ToString()))
                        {
                            DateTime dt = (DateTime)data;

                            _model.IsPublishLater = true;

                            string date = _dateLocalizationServices.ConvertToLocalizedDateString(dt, new DateLocalizationOptions());// dt.Date.ToString("MM/dd/yyyy");
                            string time = _dateLocalizationServices.ConvertToLocalizedTimeString(dt, new DateLocalizationOptions());// dt.Date.ToString("h:mm", new CultureInfo("en-US"));
                            _model.Editor = new DateTimeEditor()
                            {
                                Date = date,
                                Time = time,
                            };
                        }
                        else
                        {
                            _model.IsPublishLater = false;
                        }

                        return true;
                    }
                    else if (type.Equals("Orchard.ArchiveLater.ViewModels.ArchiveLaterViewModel") && root[prefix] != null)
                    {
                        var data = root[prefix];

                        if (!string.IsNullOrEmpty(data.ToString()))
                        {
                            DateTime dt = (DateTime)data;

                            _model.ArchiveLater = true;

                            string date = _dateLocalizationServices.ConvertToLocalizedDateString(dt, new DateLocalizationOptions());// dt.Date.ToString("MM/dd/yyyy");
                            string time = _dateLocalizationServices.ConvertToLocalizedTimeString(dt, new DateLocalizationOptions());// dt.Date.ToString("h:mm", new CultureInfo("en-US"));
                            _model.Editor = new DateTimeEditor()
                            {
                                Date = date,
                                Time = time,
                            };
                        }
                        else
                        {
                            _model.ArchiveLater = false;
                        }

                        return true;
                    }
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
                    //entry.RoleId = int.Parse(value.ToString());
                    entry.Name = value.ToString();
                    entry.Granted = true;
                } catch (Exception e) {};
                checkeds.Add(entry);
            }

            return checkeds;
        }


        static public JObject GetData(JObject obj, dynamic model)
        {

            foreach (var item in model.Content.Items)
            {
                if (item.TemplateName != null && item.TemplateName.StartsWith("Fields/"))
                {
                    string prefix = ((string)item.Prefix).Split('.')[1];
                    if (item.TemplateName.Equals("Fields/Input.Edit"))
                        obj.Add(new JProperty(prefix, item.Model.Value));
                    else if (item.TemplateName.Equals("Fields/Enumeration.Edit"))
                    {
                        string value = item.Model.Value;
                        if (!string.IsNullOrEmpty(value) && (value.StartsWith(";") || value.EndsWith(";")))
                        {
                            string[] values = value.Split(';');
                            values = values.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                            item.Model.Value = string.Join(";", values);

                        }
                        obj.Add(new JProperty(prefix, item.Model.Value));
                    }
                    else if (item.TemplateName.Equals("Fields/Numeric.Edit"))
                        if (string.IsNullOrEmpty(item.Model.Value))
                            obj.Add(new JProperty(prefix, ""));
                        else
                            obj.Add(new JProperty(prefix, int.Parse(item.Model.Value)));
                    else if (item.TemplateName.Equals("Fields/DateTime.Edit"))
                        obj.Add(new JProperty(prefix, item.ContentField.DateTime));
                    else if (item.TemplateName.Equals("Fields/MediaLibraryPicker.Edit"))
                    {
                        List<JObject> list = new List<JObject>();
                        foreach (var a in item.Model.Objects)
                            list.Add(JObject.FromObject(a));
                        obj.Add(new JProperty(prefix, list.ToArray()));
                    }
                    else if (item.TemplateName.Equals("Fields/TaxonomyField"))
                    {
                        var viewModel = item.Model;
                        var checkedTerms = new List<int>();
                        foreach (var term in viewModel.Terms)
                        {
                            if (term.IsChecked)
                                checkedTerms.Add(term.Id);
                        }
                        obj.Add(new JProperty(prefix, checkedTerms.ToArray()));
                    }
                    else if (item.TemplateName.Equals("Fields/Boolean.Edit"))
                        obj.Add(new JProperty(prefix, item.ContentField.Value));
                    //else
                    //    obj.Add(new JProperty(item.Prefix, item.TemplateName));
                }
            }

            return obj;
        }
    }
}