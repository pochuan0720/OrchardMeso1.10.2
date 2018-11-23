(function ($) {
    ko.bindingHandlers.datePicker = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            //var linkedElement = allBindings.get('value');
            var linkedElement = allBindings().value;
            var fromDate = allBindings().fromDate;
            var toDate = allBindings().toDate;
            var options = {
                format: "m/d/Y",
                formatDate: "m/d/Y"
            };
            var additionalOptions = valueAccessor() || {};
            if ("dateFormat" in additionalOptions) {
                var format = "m/d/Y";
                switch (additionalOptions["dateFormat"]) {
                    case "DMY":
                        format = "d/m/Y";
                        break;
                    case "MDY":
                        format = "m/d/Y";
                        break;
                    case "YMD":
                        format = "Y/m/d";
                        break;
                }
                options["format"] = format;
                options["formatDate"] = format;

                delete additionalOptions["dateFormat"];
            }
            options = $.extend(options, additionalOptions);

            options.timepicker = false;
            options.onChangeDateTime = function (dp, $input) {
                linkedElement($input.val());
            };

            var dtp = $(element).datetimepicker(options).data().xdsoft_datetimepicker;

            if (fromDate !== undefined) {
                dtp.setOptions({ minDate: fromDate() });
                fromDate.subscribe(function (nv) {
                    dtp.setOptions({ minDate: nv });
                });
            }

            if (toDate !== undefined) {
                dtp.setOptions({ maxDate: toDate() });
                toDate.subscribe(function (nv) {
                    dtp.setOptions({ maxDate: nv });
                });
            }
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {

        }
    };
    ko.bindingHandlers.timePicker = {
        init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
            //var linkedElement = allBindings.get('value');
            var linkedElement = allBindings().value;
            var options = {
                format: 'g:i A',
                formatTime: 'g:i A'
            };
            var additionalOptions = valueAccessor() || {};
            options = $.extend(options, additionalOptions);

            options.datepicker = false;
            options.onChangeDateTime = function (dp, $input) {
                linkedElement($input.val());
            };

            $(element).datetimepicker(options);
        },
        update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {

        }
    };

    // "bitField: array, value: element"
    //ko.bindingHandlers.bitField = {
    //    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
    //        var bindingValue = ko.utils.unwrapObservable(valueAccessor());

    //        if (bindingValue['template']) {
    //            ko.virtualElements.emptyNode(element);
    //        } else {
    //            var templateNodes = ko.virtualElements.childNodes(element),
    //                container = ko.utils.moveCleanedNodesToContainerElement(templateNodes);
    //            new ko.templateSources.anonymousTemplate(element)['nodes'](container);
    //        }
    //    },
    //    update: function (element, valueAccessor, allBinding, viewModel, bindingContext) {

    //    }
    //}

    //ko.virtualElements.allowedBindings.bitField = true;
})(jQuery);