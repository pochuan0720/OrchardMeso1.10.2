using Orchard;
using Orchard.ContentManagement;
using Orchard.Localization.Models;
using Orchard.Localization.Services;
using Orchard.Mvc;
using Orchard.Schedule.Models;
using Orchard.Schedule.ViewModels;
using System;

namespace Meso.TyMetro.Handlers
{
    public class AccessibilityReservationUpdateModelHandler : Orchard.Core.Common.Handlers.UpdateModelHandler, IAccessibilityReservationUpdateModelHandler
    {
        private readonly IWorkContextAccessor _accessor;

        public AccessibilityReservationUpdateModelHandler(
            IDateLocalizationServices dateLocalizationServices, 
            IHttpContextAccessor httpContextAccessor, 
            IWorkContextAccessor accessor) : base(dateLocalizationServices, httpContextAccessor)
        {
            _accessor = accessor;
        }

        public override bool TryUpdateModel<TModel>(TModel model, string prefix, string[] includeProperties, string[] excludeProperties)
        {
            if (root != null)
            {
                if (typeof(EditScheduleViewModel) == model.GetType())
                {
                    if (root["StartDateTime"] == null && root["EndDateTime"] == null)
                        return false;

                    dynamic _model = model;
                    if (root["StartDateTime"] != null)
                    {
                        DateTime start = (DateTime)root["StartDateTime"];
                        _model.StartDate = _dateLocalizationServices.ConvertToLocalizedString(start, ParseFormat, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                        _model.StartTime = _dateLocalizationServices.ConvertToLocalizedTimeString(start, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                        _model.EndDate = _model.StartDate;
                        _model.EndTime = _model.StartTime;
                    }
                    if (root["EndDateTime"] != null)
                    {
                        DateTime end = (DateTime)root["EndDateTime"];
                        _model.EndDate = _dateLocalizationServices.ConvertToLocalizedString(end, ParseFormat, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                        _model.EndTime = _dateLocalizationServices.ConvertToLocalizedTimeString(end, new DateLocalizationOptions() { EnableTimeZoneConversion = false });
                    }
                    return true;
                }
                else
                {
                    return base.TryUpdateModel(model, prefix, includeProperties, excludeProperties);
                }
            }

            return false;
        }

        public new IAccessibilityReservationUpdateModelHandler SetData(object _root)
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
    }
}