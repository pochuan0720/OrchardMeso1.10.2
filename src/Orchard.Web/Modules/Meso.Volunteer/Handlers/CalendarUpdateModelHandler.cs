using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Common.Handlers;
using Orchard.Layouts.Services;
using Orchard.Layouts.ViewModels;
using Orchard.Localization.Models;
using Orchard.Localization.Services;
using Orchard.Mvc;
using Orchard.Schedule.Models;
using Orchard.Schedule.ViewModels;
using System;
using System.Linq;
using System.Web;

namespace Meso.Volunteer.Handlers
{
    public class CalendarUpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, ICalendarUpdateModelHandler
    {
        private readonly IWorkContextAccessor _accessor;
        private readonly ILayoutModelMapper _mapper;

        public CalendarUpdateModelHandler(
            IDateLocalizationServices dateLocalizationServices, 
            IHttpContextAccessor httpContextAccessor, 
            IWorkContextAccessor accessor,
            ILayoutModelMapper mapper) : base(dateLocalizationServices, httpContextAccessor)
        {
            _accessor = accessor;
            _mapper = mapper;
        }

        public override bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            if (root != null)
            {
                if (typeof(EditScheduleViewModel) == model.GetType())
                {
                    dynamic _model = model;
                    DateTime start = (DateTime)root["StartDate"];
                    _model.StartDate = _dateLocalizationServices.ConvertToLocalizedString(start, ParseFormat, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    _model.StartTime = _dateLocalizationServices.ConvertToLocalizedTimeString(start, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    DateTime end = (DateTime)root["EndDate"];
                    _model.EndDate = _dateLocalizationServices.ConvertToLocalizedString(end, ParseFormat, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    _model.EndTime = _dateLocalizationServices.ConvertToLocalizedTimeString(end, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    return true;
                }
                else if (typeof(LayoutPartViewModel) == model.GetType())// && root["Form"] != null)
                {
                    dynamic _model = model;
                    LayoutEditor editor = _model.LayoutEditor;
                    string formname = Convert.ToString(editor.Content.Id);
                    //formname = formname + "_" + (root["Title"] != null ? root["Title"].ToString() : "Untitled");
                    string json = "";
                    if(!string.IsNullOrEmpty(formname))
                        json = "{\"type\":\"Canvas\",\"children\":[{\"type\":\"Form\",\"data\":\"StoreSubmission=true&FormName=" + formname + "\",\"name\":\"" + formname + "\",\"contentType\":\"Orchard.DynamicForms.Elements.Form\",\"children\":[]}]}";
                    else
                        json = "{\"type\":\"Canvas\",\"children\":[{\"type\":\"Form\",\"data\":\"StoreSubmission=true\",\"contentType\":\"Orchard.DynamicForms.Elements.Form\",\"children\":[]}]}";

                    JObject layout = JObject.Parse(json);
                    var newItems = new JArray();
                    if (root["Form"] != null)
                    {
                        JToken items = root["Form"];

                        foreach (var child in items)
                        {
                            JObject obj = new JObject();
                            obj.Add(new JProperty("type", "Content"));
                            if (child["ContentType"].ToString().Equals("NumericField"))
                            {
                                child["Data"]["Options"] = new JArray();
                                child["ContentType"] = "TextField";
                            }
                            var query = String.Join("&", child["Data"].Children().Cast<JProperty>().Select(jp => jp.Name + "=" + getValue(jp.Value)));
                            obj.Add(new JProperty("data", query));
                            obj.Add(new JProperty("contentType", "Orchard.DynamicForms.Elements." + child["ContentType"]));
                            newItems.Add(obj);
                        }
                    }
                    //Add owner
                    //newItems.Add(JObject.Parse("{\"type\":\"Content\",\"data\":\"InputName=Owner\",\"contentType\":\"Orchard.DynamicForms.Elements.TextField\"}"));
                    newItems.Add(JObject.Parse("{\"type\":\"Content\",\"data\":\"Text=Submit\",\"contentType\":\"Orchard.DynamicForms.Elements.Button\"}"));
                    layout["children"][0]["children"] = newItems;


                    editor.Data = layout.ToString();
                    //var describeContext = new DescribeElementsContext { Content = editor.Content };
                    //string result = layout.ToString();
                    //var elementInstances = _mapper.ToLayoutModel(layout.ToString(), describeContext).ToArray();
                }
                else
                {
                    return base.TryUpdateModel(model, prefix, includeProperties, excludeProperties);
                }
            }

            return false;
        }

        private string getValue(JToken token)
        {
            if (token is JArray)
            {
                return HttpUtility.UrlEncode(String.Join("\n", token));
            }
            else
                return HttpUtility.UrlEncode(token.ToString());
        }

        private JToken getValue(string data)
        {
            var collection = HttpUtility.ParseQueryString(data);
            return JsonConvert.SerializeObject(collection.AllKeys.ToDictionary(x => x, x => x.Equals("Options") ? collection[x].Split() : (object)collection[x]));
        }

        private object getOptions(string key, string options)
        {
            if (key.Equals("Options"))
            {
                var collection = options.Split('\n');
                return collection;
            }
            else
                return options;
        }

        public new ICalendarUpdateModelHandler SetData(object _root)
        {
            base.SetData(_root);
            return this;
        }

        private string _dateFormat;
        private string DateFormat
        {
            get { return _dateFormat ?? (_dateFormat = _accessor.GetContext().CurrentSite.As<ScheduleSettingsPart>().DateFormat); }
        }

        private string ParseFormat
        {
            get
            {
                switch (DateFormat)
                {
                    case "DMY":
                        return "dd/MM/yyyy";
                    case "MDY":
                        return "MM/dd/yyyy";
                    case "YMD":
                        return "yyyy/MM/dd";
                    default:
                        return "MM/dd/yyyy";
                }
            }
        }

        public new static JObject GetData(JObject obj, dynamic model)
        {
            foreach (var item in model.Content.Items)
            {
                if (item.TemplateName != null && item.TemplateName.Equals("Parts.Layout"))
                {
                    string Data = item.Model.LayoutEditor.Data;
                    JObject layout = JObject.Parse(Data);
                    if (layout["children"] != null && layout["children"].Count() > 0)
                    {
                        JToken items = layout["children"][0]["children"];
                        var newItems = new JArray();

                        foreach (var child in items)
                        {
                            JObject _obj = new JObject();
                            string data = child["data"].ToString();
                            string contentType = child["contentType"].ToString();
                            string[] tokens = contentType.Split('.');
                            contentType = tokens[tokens.Length - 1];
                            if (!contentType.Equals("Button"))
                            {
                                
                                var collection = HttpUtility.ParseQueryString(data);
                                if (contentType.Equals("TextField") && collection.AllKeys.Contains("Options"))
                                {
                                    collection.Remove("Options");
                                    contentType = "NumericField";
                                }
                                JObject dataToken = JObject.FromObject(collection.AllKeys.ToDictionary(x => x, x => x.Equals("Options") ? collection[x].Split('\n') : (object)collection[x]));
                                _obj.Add(new JProperty("Data", dataToken));
                                _obj.Add(new JProperty("ContentType", contentType));
                                newItems.Add(_obj);
                            }
                        }

                        obj.Add(new JProperty("Form", newItems));
                    }
                }
            }

            return UpdateModelHandler.GetData(obj, model);
        }
    }
}