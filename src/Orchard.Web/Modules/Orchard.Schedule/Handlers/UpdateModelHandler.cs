using Orchard.ContentManagement;
using Orchard.Core.Common.ViewModels;
using Orchard.Localization.Models;
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
                    _model.StartDate = _dateLocalizationServices.ConvertToLocalizedDateString(start, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    _model.StartTime = _dateLocalizationServices.ConvertToLocalizedTimeString(start, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    DateTime end = (DateTime)root["EndDate"];
                    _model.EndDate = _dateLocalizationServices.ConvertToLocalizedDateString(end, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    _model.EndTime = _dateLocalizationServices.ConvertToLocalizedTimeString(end, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
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