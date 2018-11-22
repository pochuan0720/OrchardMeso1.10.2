(function (jq) {
    var Event = function (e) {
        var self = this;
        self.start = moment(e.start);
        self.end = moment(e.end);

        self.startDate = moment(self.start).startOf('day');
        self.endDate = moment(self.end).startOf('day');

        self.startTime = self.start.format("h:mma");
        self.endTime = self.end.format("h:mma");

        self.title = e.title;
        self.url = e.url;
        self.allDay = e.allDay;

        self.classes = ["event"].concat(e.className).join(" ");
        self.tags = e.tags;

        return self;
    }

    var Day = function (date) {
        var self = this;
        self.date = ko.observable(moment(date));
        self.events = ko.observableArray([]);

        self.formattedDate = ko.computed(function () {
            return self.date().format("dddd, MMMM D, YYYY");
        });
    }

    Day.prototype.addEvent = function (e) {
        this.events.push(e);
        this.events = this.events.sort(function (a, b) {
            if (a.allDay) return -1;
            else if (b.allDay) return 1;
            else if (a.start - b.start != 0) return a.start - b.start;
            else return b.end - a.end;
        });
    }

    var upcomingModel = function () {
        var self = this;

        self.previousTrigger = jq('#eventsStart');
        self.nextTrigger = jq('#eventsEnd');
        self.container = jq('#eventsContainer');

        self.url = jq('#upcomingEvents').data()["eventurl"];
        
        self.current = ko.observable(1);
        
        self.page = ko.observable();

        self.pageDisplay = ko.computed(function () { return self.page() !== undefined ? self.page : 1; });
        
        self.events = ko.observableArray([]);
        self.days = ko.observableArray([]);

        self.dayDictionary = {};

        self.updateEvents = function (prepend) {
            var start = self.current();
                        
            return jq.ajax(self.url + "/" + start)
            .done(function (data) {
                var currentEvents = self.events();
                var toAdd = data.map(function (e) { return new Event(e); });
                if (prepend) {
                    currentEvents = toAdd.concat(currentEvents);
                } else {
                    currentEvents = currentEvents.concat(toAdd);
                }
                self.events(currentEvents);

                var days = self.days();

                currentEvents.forEach(function (e) {
                    var d = moment(e.startDate);
                    while (d < e.end) {
                        var dt = d.toDate();
                        if (self.dayDictionary[dt] === undefined) {
                            var day = new Day(d);
                            self.dayDictionary[dt] = day;
                            days.push(day)
                        }
                        self.dayDictionary[dt].addEvent(e);
                        d.add(moment.duration(1, 'day'));
                    }
                });

                days.sort(function (a, b) { return a.date() - b.date(); })

                self.days(days);
            });
        }

        self.previousPage = function () {
            if (self.page > 1) {
                self.page = self.page - 1;
            }

            self.current(self.page);
            
            self.updateEvents(true)
            .then(function () {
                var oldHeight = self.container.data('oldHeight');
                var newHeight = self.container[0].scrollHeight;

                self.container.scrollTop(newHeight - oldHeight);
            });

            return false;
        };

        self.nextPage = function () {
            self.page = self.page + 1;
            
            self.current(self.page);

            self.updateEvents(false);

            return false;
        }


        //self.container.on('scroll', function () {
        //    var offset = jq(this).offset();
        //    var oldHeight = jq(this)[0].scrollHeight;
        //    var containerHeight = jq(this).height();
        //    var startOffset = self.previousTrigger.offset().top + self.previousTrigger.height();
        //    var endOffset = self.nextTrigger.offset().top;

        //    jq(this).data('oldHeight', oldHeight);

        //    if (offset.top - startOffset <= 0) {
        //        self.previousPage();
        //    } else if (offset.top + containerHeight >= endOffset) {
        //        self.nextPage();
        //    }
        //});

    }

    var vm = new upcomingModel();
    console.log(vm.current());
    ko.applyBindings(vm);
    vm.updateEvents().then(function () {
        vm.container.scrollTop(vm.nextTrigger.height() + 1);
    });
})(jQuery);