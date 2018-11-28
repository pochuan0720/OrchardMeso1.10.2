﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Schedule.Models;
using Orchard.Environment.Extensions;
using Orchard.ContentManagement;
using Orchard.Projections.Services;
using Orchard.Schedule.ViewModels;
using Orchard;
using Orchard.Core.Common.Handlers;
using Orchard.Core.Common.Models;
using Orchard.Projections.Models;
using Orchard.Core.Containers.Services;
using Orchard.Core.Containers.Models;

namespace Orchard.Schedule.Services {

    using Occurrence = Dictionary<string, object>;   
        
    //[OrchardFeature("Orchard.CalendarLayout")]
    public class ScheduleLayoutService : IScheduleLayoutService {

        private readonly IOrchardServices _services;
        private readonly IProjectionManager _projectionManager;
        private readonly IContainerService _containerService;
        
        public ScheduleLayoutService(IOrchardServices services, IContainerService containerService, IProjectionManager projectionManager) {
            _services = services;
            _containerService = containerService;
            _projectionManager = projectionManager;
        }

        public Occurrence GetOccurrenceObject(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData) {
            string[] emptyTags = new string[] { };

            return new Occurrence {
                { "id", scheduleData.Id },
                { "start", scheduleEvent.Start },
                { "end", scheduleEvent.End },
                { "allDay", scheduleData.AllDay },
                { "title", scheduleData.Title},
                { "url", scheduleData.DisplayUrl},
                { "deletable", scheduleData.DisplayUrl },
                { "defaultBackgroundColor", scheduleData.BackgroundColor },
                { "defaultBorderColor", scheduleData.BorderColor },
                { "defaultTextColor", scheduleData.TextColor },
                { "tags", scheduleData.Tags??emptyTags },
                { "className", scheduleData.Classes??emptyTags },
                //{ "backgroundColor", string.Format("#{0:X6}", settings.EventColor) },
            };
        }

        public ScheduleApiViewMode GetOccurrenceViewModel(ScheduleOccurrence scheduleEvent, ScheduleData scheduleData)
        {
            var model = _services.ContentManager.BuildEditor(scheduleEvent.Source);
            int[] Ids = new int[0];
            IList<string> selectedItemContentTypes = new List<string>();

            ContainerPart containerPart = scheduleEvent.Source.As<ContainerPart>();
            if (containerPart != null)
            {
                foreach (var item in model.Content.Items)
                {
                    if (item.TemplateName != null)
                    {
                        if (item.TemplateName.Equals("Parts.Common.Container"))
                        {
                            int itemCount = containerPart.ItemCount;
                            if (itemCount > 0)
                            {
                                int containerId = item.ContentItem.Id;
                                Ids = _containerService.GetContentItemsQuery(containerId).List().Select(x => x.ContentItem.Id).ToArray();
                            }
                        }
                        else if (item.TemplateName.Equals("Container"))
                        {
                            selectedItemContentTypes = item.Model.SelectedItemContentTypes;
                        }
                    }
                }
            }


            return new ScheduleApiViewMode
            {
                Id = scheduleData.Id,
                Title = scheduleData.Title,
                Body = scheduleData.Body,
                StartDate = scheduleEvent.Start,
                EndDate = scheduleEvent.End,
                Attendee = Ids,
                Container = selectedItemContentTypes.ToArray<string>(),
                Data = UpdateModelHandler.GetData(model)
            };
        }

        public bool DateInRange(SchedulePart part, DateTime start, DateTime end) {
            if (part.ScheduleType == ScheduleRepeatType.Single) {
                return (part.StartDate + part.StartTime >= start && part.StartDate <= end);
            }
            else if (part.EndDate.HasValue) {
                return part.StartDate <= end && part.EndDate.Value >= start;
            }
            else {
                return part.StartDate <= end;
            }
        }
        
        public bool DateInFuture(SchedulePart part) {
            var start = DateTime.UtcNow;

            if (part.ScheduleType == ScheduleRepeatType.Single) {
                return (part.StartDate + part.Duration >= start);
            }
            else if (part.EndDate.HasValue) {
                return part.EndDate.Value >= start;
            }
            else {
                return true;
            }
        }

        public IEnumerable<ContentItem> GetProjectionContentItems(QueryModel query)
        {
            IEnumerable<ContentItem> results = null;
            try
            {
                results = _projectionManager.GetContentItems(query);
                if (results == null) return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            results = results.Where(c => c.Has<SchedulePart>());
            if (results.Count() == 0) return null;

            return results;
        }

        public IEnumerable<ContentItem> GetProjectionContentItems(int queryId) {
            IEnumerable<ContentItem> results = null;
            try {
                results = _projectionManager.GetContentItems(queryId);
                if (results == null) return null;
            }
            catch (ArgumentException) {
                return null;
            }

            results = results.Where(c => c.Has<SchedulePart>());
            if (results.Count() == 0) return null;

            return results;
        }
    }
}