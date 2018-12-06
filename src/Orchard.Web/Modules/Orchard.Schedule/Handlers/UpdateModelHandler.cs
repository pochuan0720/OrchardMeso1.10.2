using Orchard.ContentManagement;
using Orchard.Localization.Services;
using Orchard.Mvc;
using Orchard.Schedule.Models;
using Orchard.Schedule.ViewModels;
using System;
using System.Globalization;

namespace Orchard.Schedule.Handlers
{
    public class UpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, IUpdateModelHandler
    {
        public UpdateModelHandler(IDateLocalizationServices dateLocalizationServices, IHttpContextAccessor httpContextAccessor) : base(dateLocalizationServices, httpContextAccessor)
        {
        }

        public override bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            if (fields != null)
            {
                if (typeof(EditScheduleViewModel) == model.GetType())
                {
                    dynamic _model = model;
                    DateTime start = (DateTime)root["StartDate"];
                    DateTime end = (DateTime)root["EndDate"];
                    _model.StartDate = start.ToString("MM/dd/yyyy");
                    _model.StartTime = start.ToString("h:mm:ss", new CultureInfo("en-US"));
                    _model.EndDate = end.ToString("MM/dd/yyyy");
                    _model.EndTime = end.ToString("h:mm:ss", new CultureInfo("en-US"));
                    return true;
                }
                else
                {
                    return base.TryUpdateModel(model, prefix, includeProperties, excludeProperties);
                }
            }

            return false;
        }

        public new IUpdateModelHandler SetData(object _root)
        {
            base.SetData(_root);
            return this;
        }
    }
}