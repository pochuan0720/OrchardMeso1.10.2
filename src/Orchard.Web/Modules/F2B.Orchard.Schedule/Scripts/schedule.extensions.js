(function () {
    // from 11/2013 issue of MSDN magazine, pg 48
    ko.subscribable.fn.store = function () {
        var self = this;
        var oldValue = self();

        var observable = ko.computed({
            read: function () {
                return self();
            },
            write: function (value) {
                oldValue = self();
                self(value);
            }
        });

        this.revert = function () {
            self(oldValue);
        };

        this.commit = function () {
            oldValue = self();
        }

        return this;
    };
})();