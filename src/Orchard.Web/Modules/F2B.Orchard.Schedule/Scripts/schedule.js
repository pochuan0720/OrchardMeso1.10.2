var bindScheduleViewModel = (function ($) {
    return function (model, dialogId) {
        var dateFormat;
        var altDateFormat;

        switch (model.DateFormat) {
            case "DMY":
                dateFormat = "DD/MM/YYYY";
                altDateFormat = "DD-MM-YYYY";
                break;
            case "MDY":
                dateFormat = "MM/DD/YYYY";
                altDateFormat = "MM-DD-YYYY";
                break;
            case "YMD":
                dateFormat = "YYYY/MM/DD";
                altDateFormat = "YYYY-MM-DD";
                break;
            default:
                dateFormat = "MM/DD/YYYY";
                altDateFormat = "MM-DD-YYYY";
                break;
        }
        function dateIsValid(date) {
            return moment(date, dateFormat).isValid();
        }

        function timeIsValid(time) {
            return moment(time, ['HH:mm', 'hh:mm a', 'hh:mm A']).isValid();
        }

        function buildBitFieldComputed(self, bitField, bitValue) {
            return ko.computed({
                read: function () {
                    return ((bitField() & bitValue) != 0);
                },
                write: function (value) {
                    if (value) {
                        bitField(bitField() | bitValue);
                    } else {
                        bitField(bitField() & ~bitValue);
                    }
                },
                owner: self
            });
        }

        function momentDateComputed(self, sourceDate, predicate) {
            if (arguments.length == 2) { predicate = function () { return true; }}
            return ko.computed({
                read: function () {
                    if (predicate()) {
                        return sourceDate().format(dateFormat);
                    } else { return ""; }
                },
                write: function (newValue) {
                    var m = moment(newValue, [dateFormat, altDateFormat]);
                    if (m.isValid()) {
                        var t = sourceDate();
                        sourceDate(m.hours(t.hours()).minutes(t.minutes()));
                    }
                },
                owner: self
            });
        }

        function momentTimeComputed(self, sourceDate) {
            return ko.computed({
                read: function () {
                    return sourceDate().format('h:mm A');
                },
                write: function (newValue) {
                    var t = moment(newValue, ['HH:mm', 'hh:mm a', 'hh:mm A']);
                    var d = sourceDate();
                    if (t.isValid()) {
                        d.hours(t.hours()).minutes(t.minutes());
                        sourceDate(d);
                    } else {
                        sourceDate.notifySubscribers(d);
                    }
                }
            });
        }

        var daysOfWeek = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
        var ordinals = ["first", "second", "third", "fourth", "fifth"];

        function bitMaskToStringList(mask, values) {
            var selected = [];
            for (var ndx = 0; ndx < values.length; ndx++) {
                var bit = (1 << ndx);
                if ((bit & mask) != 0) {
                    selected.push(values[ndx]);
                }
            }

            return selected.join(", ")
        }

        var scheduleModel = function () {
            var self = this;

            self.dialogId = '#' + dialogId;

            self.allDay = ko.observable(model.AllDay);

            self.startDateTime = ko.observable(moment(model.StartDate + ' ' + model.StartTime,  dateFormat + " HH:mm"));
            self.displayStartDate = momentDateComputed(self, self.startDateTime);
            self.displayStartTime = momentTimeComputed(self, self.startDateTime);

            self.saveStartDate = ko.computed(function () { return self.startDateTime().format(dateFormat); });
            self.saveStartTime = ko.computed(function () { return self.startDateTime().format('HH:mm'); });

            self.duration = ko.observable(moment.duration(model.Duration, 'minutes'));

            self.endDateTime = ko.computed({
                read: function() {
                    return moment(self.startDateTime() + self.duration());
                },
                write: function (newValue) {
                    self.duration(moment.duration(newValue - self.startDateTime()));
                },
                owner: self
            });

            self.displayEndDate = momentDateComputed(self, self.endDateTime);
            self.displayEndTime = momentTimeComputed(self, self.endDateTime);
            self.saveEndDate = ko.computed(function () { return self.endDateTime().format(dateFormat); });
            self.saveEndTime = ko.computed(function () { return self.endDateTime().format('HH:mm'); });

            self.lastDayOrWeekOfMonth = ko.observable(model.LastDayOrWeek).store();

            self.durationIsValid = ko.computed(function () {
                if (self.allDay()) {
                    return self.duration().asMinutes() > -1440; // 1440 minutes in a day
                }
                else
                {
                    return self.duration().asMinutes() >= 0.0;
                }
            });

            self.endValue = ko.observable(model.EndValue).store();
            self.terminalDate = ko.observable(moment(model.TerminalDate, dateFormat)).store();
            self.displayTerminalDate = momentDateComputed(self, self.terminalDate, function () { return self.endValue() === 'date'; });
            self.saveTerminalDate = ko.computed(function () { return self.terminalDate().format(dateFormat); });

            self.internalOccurrences = ko.observable(model.Occurrences);

            self.occurrences = ko.computed({
                read: function() {
                    if (self.endValue() === 'times') return self.internalOccurrences();
                    else return '';
                },
                write: function(newValue) {
                    self.internalOccurrences(newValue);
                },
                owner: self
            }).store();

            self.monthWeek = ko.computed(function () {
                return Math.ceil(self.startDateTime().date() / 7);
            });
            self.dayWeek = ko.computed(function () {
                return daysOfWeek[self.startDateTime().day()];
            });
            self.ordinalMonthWeek = ko.computed(function () {
                return ordinals[self.monthWeek() - 1];
            });

            self.weeksFromEndOfMonth = ko.computed(function () {
                var maxDays = self.startDateTime().daysInMonth();
                var day = self.startDateTime().date();

                var weeks = Math.floor((maxDays - day) / 7);

                return weeks;
            });

            self.repeat = ko.observable(model.Repeat).store();
            self.repeatInterval = ko.observable(model.RepeatInterval).store();
            self.repeatType = ko.observable(model.RepeatType).store();
            self.repeatDays = ko.observable(model.RepeatDays).store();
            self.repeatMonths = ko.observable(model.RepeatMonths).store();
            self.repeatWeeks = ko.observable(model.RepeatWeeks).store();
            self.repeatUntil = ko.observable(model.RepeatUntil).store();

            self.repeatSet = ko.observable(model.Repeat).store();
            self.repeatSummaryFinal = ko.observable("");

            self.offset = ko.observable(model.Offset).store();

            self.repeatIntervalOptions = [];
            for (var i = 1; i <= 30; i++) self.repeatIntervalOptions.push(i);

            self.rptSU = buildBitFieldComputed(self, self.repeatDays, 1);
            self.rptMO = buildBitFieldComputed(self, self.repeatDays, 2);
            self.rptTU = buildBitFieldComputed(self, self.repeatDays, 4);
            self.rptWE = buildBitFieldComputed(self, self.repeatDays, 8);
            self.rptTH = buildBitFieldComputed(self, self.repeatDays, 16);
            self.rptFR = buildBitFieldComputed(self, self.repeatDays, 32);
            self.rptSA = buildBitFieldComputed(self, self.repeatDays, 64);

            self.rptJan = buildBitFieldComputed(self, self.repeatMonths, 0x001);
            self.rptFeb = buildBitFieldComputed(self, self.repeatMonths, 0x002);
            self.rptMar = buildBitFieldComputed(self, self.repeatMonths, 0x004);
            self.rptApr = buildBitFieldComputed(self, self.repeatMonths, 0x008);
            self.rptMay = buildBitFieldComputed(self, self.repeatMonths, 0x010);
            self.rptJun = buildBitFieldComputed(self, self.repeatMonths, 0x020);
            self.rptJul = buildBitFieldComputed(self, self.repeatMonths, 0x040);
            self.rptAug = buildBitFieldComputed(self, self.repeatMonths, 0x080);
            self.rptSep = buildBitFieldComputed(self, self.repeatMonths, 0x100);
            self.rptOct = buildBitFieldComputed(self, self.repeatMonths, 0x200);
            self.rptNov = buildBitFieldComputed(self, self.repeatMonths, 0x400);
            self.rptDec = buildBitFieldComputed(self, self.repeatMonths, 0x800);

            self.rptWeek1 = buildBitFieldComputed(self, self.repeatWeeks, 0x01);
            self.rptWeek2 = buildBitFieldComputed(self, self.repeatWeeks, 0x02);
            self.rptWeek3 = buildBitFieldComputed(self, self.repeatWeeks, 0x04);
            self.rptWeek4 = buildBitFieldComputed(self, self.repeatWeeks, 0x08);
            self.rptWeekLst = buildBitFieldComputed(self, self.repeatWeeks, 0x10);

            self.excludedDate = ko.observable();
            self.excludedDates = ko.observableArray(model.ExcludedDates.split(",").map(function (d) { return moment(d, dateFormat); }).filter(function(m) { return m.isValid(); }));
            self.selectedExcludedDate = ko.observable();
            self.addExcludedDate = function () {
                self.excludedDates.push(moment(self.excludedDate(), dateFormat));
                self.excludedDates(self.excludedDates().sort(function (a, b) { return a - b; }));
                self.excludedDate('');
            }
            self.removeExcludedDate = function () {
                self.excludedDates.remove(self.selectedExcludedDate());
            };
            self.isValidExcludedDate = ko.computed(function () {
                var ed = self.excludedDate();

                if (ed === undefined || ed === '') return false;

                var m = moment(ed, dateFormat);
                if (!m.isValid()) return false;
                var eds = self.excludedDates();
                for (e in eds) {
                    if (m - eds[e] === 0) return false;
                }

                return true;
            });
            self.formatDate = function(item) {
                return item.format(dateFormat);
            };
            
            self.excludedDatesList = ko.computed(function () {
                return self.excludedDates().map(function(d) { return d.format(dateFormat); }).join(',')
            });

            self.fromEndOfMonth = ko.observable(model.FromEndOfMonth).store();
            self.fromStartOrEndOfMonth = ko.computed({
                read: function () {
                    return self.fromEndOfMonth() ? "end" : "start";
                },
                write: function (newValue) {
                    self.fromEndOfMonth(newValue === "end");
                },
                owner: self
            });
            
            self.displayExcludedDates = ko.computed(function () {
                return self.excludedDates().length > 0;
            });
            self.excludedDatesSize = ko.computed(function () {
                return Math.min(self.excludedDates().length, 5);
            });
            self.excludedDateSelected = ko.computed(function () {

            });

            self.ordinalDayInMonth = ko.computed(function () {
                return self.startDateTime().format("Do");
            });

            self.repeatSummary = ko.computed(function () {
                if (!self.repeat()) {
                    return undefined;
                }

                var interval = self.repeatInterval();
                var end = self.endValue();
                var offset = self.offset();
                var summary = "";

                switch (self.repeatType()) {
                    case "Daily":
                        if (interval === 1) summary = "Daily";
                        else summary = "Every " + interval + " days";
                        break;
                    case "Weekly":
                        if (interval === 1) summary = "Weekly";
                        else summary = "Every " + interval + " weeks";
                        var days = self.repeatDays();
                        if (days === 62) {
                            summary += " on weekdays";
                        } else if (days !== 0) {
                            summary += " on ";
                            summary += bitMaskToStringList(days, daysOfWeek);
                        }
                        break;
                    case "MonthlyByDay":
                        if (interval == 1) summary = "Monthly";
                        else summary = "Every " + interval + " months";
                        if (self.fromEndOfMonth()) {
                            var daysFromEnd = self.startDateTime().daysInMonth() - self.startDateTime().date();
                            if (daysFromEnd === 0) { summary += " on the last day"; }
                            else if (daysFromEnd === 1) { summary += " a day from the end of the month"; }
                            else { summary += " " + daysFromEnd + " days from the end of the month"; }
                        } else {
                            summary += " on the " + self.startDateTime().format("Do");
                        }
                        break;
                    case "MonthlyByWeek":
                        if (interval === 1) summary = "Monthly";
                        else summary = "Every " + interval + " months";
                        var dayWeek = self.dayWeek();
                        if (self.fromEndOfMonth()) {
                            var weeksFromEnd = self.weeksFromEndOfMonth();

                            if (weeksFromEnd === 0) summary += " on the last " + dayWeek;
                            else if (weeksFromEnd === 1) summary += " on the next to last " + dayWeek;
                            else summary += " on the " + ordinals[weeksFromEnd - 1] + " to last " + dayWeek;
                        } else {
                            summary += " on the " + self.ordinalMonthWeek() + " " + dayWeek;
                        }
                        break;
                    case "Yearly":
                        if (interval === 1) summary = "Annually";
                        else summary = "Every " + interval + " years";
                        summary += ' on ' + self.startDateTime().format('MMMM D');
                        break;
                    default:
                        summary = "Unknown";
                }

                switch (end) {
                    case 'times': summary += ', ' + self.occurrences() + ' times'; break;
                    case 'date': summary += ', until ' + self.terminalDate().format('MMM D, YYYY'); break;
                }

                if (offset != 0) {
                    if (offset < -1) summary += ', ' + -offset + ' days prior';
                    else if (offset === -1) summary += ', a day prior';
                    else if (offset === 1) summary += ', a day later';
                    else summary += ', ' + offset + ' days later';
                }

                return summary;
            });

            self.byMonth = ko.computed(function () {
                var rpt = self.repeatType();
                return (rpt === "MonthlyByDay" || rpt === "MonthlyByWeek");
            });

            self.repeatIntervalUnit = ko.computed(function () {
                var unit = "UNKNOWN";
                switch (self.repeatType()) {
                    case "Daily": unit = "Day"; break;
                    case "Weekly": unit = "Week"; break;
                    case "MonthlyByWeek":
                    case "MonthlyByDay": unit = "Month"; break;
                    case "Yearly": unit = "Year"; break;
                }

                if (self.repeatInterval() > 1) unit += "s";

                return unit;
            });

            self.showDays = ko.computed(function () {
                var rptType = self.repeatType();
                return (rptType === 'Weekly');
            });

            self.showMonths = ko.computed(function () {
                var rptType = self.repeatType();
                return (rptType === 'Yearly');
            });

            //self.showLastDayOfMonth = ko.computed(function () {
            //    var rptType = self.repeatType();
            //    var sdt = self.startDateTime();
            //    return (rptType == 'MonthlyByDay' &&  sdt.date() == sdt.daysInMonth());
            //});

            //self.isLastWeekDayOfMonth = ko.computed(function () {
            //    if (self.monthWeek() == 5) return true;
            //    if (self.monthWeek() != 4) return false;
            //    if (self.startDateTime().date() + 7 > self.startDateTime().daysInMonth()) return true;
            //    return false;
            //});

            //self.showLastWeekDayOfMonth = ko.computed(function () {
            //    var rpt = self.repeatType();
            //    return (rpt == 'MonthlyByWeek' && self.isLastWeekDayOfMonth());
            //});

            self.editRepeat = function () {
                $(self.dialogId).dialog("open");
                return true;
            };
        }

        var mdl = new scheduleModel();
        var date = mdl.startDateTime();

        mdl.working = false;
        mdl.backup = {};

        mdl.displayStartDate.subscribe(function (newValue) {
            var rptDays = mdl.repeatDays();
            if (rptDays == 0 || (rptDays & (rptDays - 1)) === 0) {
                mdl.repeatDays(1 << new Date(newValue).getDay());
            }
        });

        mdl.startDateTime.subscribe(function () {
            if (!mdl.working) {
                mdl.repeatSummaryFinal(mdl.repeatSummary());
            }
        });
        

        mdl.repeat.subscribe(function (newValue) {
            var set = mdl.repeatSet();
            if (newValue) mdl.repeatSet(true);
            if (!set && newValue) mdl.editRepeat();
        });

        mdl.endValue.subscribe(function (newValue) {
            var rptType = mdl.repeatType();
            var occurrences = (rptType === 'Daily' || rptType === 'Yearly') ? 5 : 35;
            var interval = mdl.repeatInterval();

            switch (newValue) {
                case "times":
                    mdl.occurrences(occurrences);
                    break;
                case "date":
                    // create a new suggested end date
                    var date = mdl.startDateTime();
                    var duration = moment.duration(0, 'days');
                    switch (rptType) {
                        case 'Daily': duration = moment.duration(occurrences * interval, 'days'); break;
                        case 'Weekly': duration = moment.duration(occurrences * interval * 7, 'days'); break;
                        case 'Yearly': duration = moment.duration(occurrences * interval, 'years'); break;
                    }

                    mdl.terminalDate(moment(date + duration));
                    break;
            }
        });

        if (mdl.repeatDays() === 0) mdl.repeatDays(1 << date.day());
        if (mdl.repeatMonths() === 0) mdl.repeatMonths(1 << date.month());

        ko.applyBindings(mdl);

        mdl.repeatSummaryFinal(mdl.repeatSummary());

        return {
            backup: function () {
                mdl.working = true;
            },
            finish: function (commit) {
                if (!commit) {
                    mdl.repeat.revert();
                    mdl.repeatSet.revert();
                    mdl.repeatInterval.revert();
                    mdl.repeatType.revert();
                    mdl.repeatDays.revert();
                    mdl.repeatMonths.revert();
                    mdl.repeatWeeks.revert();
                    mdl.repeatUntil.revert();
                    mdl.endValue.revert();  // This must come before terminalDate and occurrences since changing it can cause them to update
                    mdl.terminalDate.revert();
                    mdl.occurrences.revert();
                    mdl.lastDayOrWeekOfMonth.revert();
                    mdl.offset.revert();
                } else {
                    mdl.repeat.commit();
                    mdl.repeatSet.commit();
                    mdl.repeatInterval.commit();
                    mdl.repeatType.commit();
                    mdl.repeatDays.commit();
                    mdl.repeatMonths.commit();
                    mdl.repeatWeeks.commit();
                    mdl.repeatUntil.commit();
                    mdl.terminalDate.commit();
                    mdl.endValue.commit();
                    mdl.occurrences.commit();
                    mdl.lastDayOrWeekOfMonth.commit();
                    mdl.offset.commit();
                    mdl.repeatSummaryFinal(mdl.repeatSummary());
                }
                mdl.working = false;
            },
            model: function () { return mdl; }
        }
    }
})(jQuery);