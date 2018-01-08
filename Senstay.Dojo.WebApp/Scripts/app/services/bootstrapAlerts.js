(function () {

    var alertService = {
        showAlert: showAlert,
        success: success,
        info: info,
        warning: warning,
        error: error
    };

    window.alerts = alertService;

    var template = _.template("<div id='ss-bs-alert' class='alert <%= alertClass %> alert-dismissable'>" +
		"<button type='button' class='close' data-dismiss='alert' aria-hidden='true'>&times;</button>" +
		"<%= message %>" +
		"</div>");

    function showAlert(alert, options) {
        var id = (options && options.id) || undefined;
        var delay = (options && options.delay) || undefined;
        var callback = (options && options.callback) || undefined;

        var alertContainer = (id == undefined) ? $('.alert-container') : $('#' + id);
        var alertElement = $(template(alert));
        alertContainer.append(alertElement);

        $('#ss-bs-alert').prop('pointer-events', 'auto');

        $('#ss-bs-alert').on('close.bs.alert', function () {
            if (callback != undefined) callback();
        });

        var containerWidth = alertContainer.width();
        var browserWidth = $(window).width();
        var alertWidth = browserWidth < containerWidth ? browserWidth : containerWidth;
        $('#ss-bs-alert').css('width', alertWidth + 'px');

        if (delay != undefined) {
            window.setTimeout(function () {
                alertElement.fadeOut();
            }, delay);
        }
    }

    function success(message, options) {
        showAlert({ alertClass: "alert-success", message: message }, options);
    }

    function info(message, options) {
        showAlert({ alertClass: "alert-info", message: message }, options);
    }

    function warning(message, options) {
        showAlert({ alertClass: "alert-warning", message: message }, options);
    }

    function error(message, options) {
        showAlert({ alertClass: "alert-danger", message: message }, options);
    }

})();
