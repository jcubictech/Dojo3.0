"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.ConfirmationPicker = function () {
    var _picker = undefined,

        install = function (id) {
            _picker = new ConfirmationCodePicker(document.getElementById(id));
    }

    return {
        install: install
    }
}();

function ConfirmationCodePicker(input, options) {
    var searchUrl = (options && options.searchUrl) || window.location.protocol + '//' + window.location.host + '/Reservation/Search';
    var pageSize = (options && options.pageSize) || 14;

    // Inputs
    var $input = $(input);

    // Outputs
    var selected = null;

    // Members
    var currentResults = [];
    var confirmationSkipToken = null;
    var lastDisplayed = null;
    var lastInput;
    var isPaging = false;
    var isResultsOpen = false;
    var oldConfirmation = $input.val();    // use for forced selection validation
    var noSelection = false; // use for forced selection validation

    // Public Methods
    this.Selected = function () {
        return selected;
    };

    function SendQuery(confirmationQuery) {
        return $.ajax({
            url: searchUrl,
            type: "POST",
            data: { query: confirmationQuery },
            beforeSend: function (jqxhr, settings) {
                jqxhr.overrideMimeType("application/json");
            }
        });
    }

    function Page() {
        var $resultsDiv = $input.catcomplete("widget");
        if (($resultsDiv.scrollTop() + $resultsDiv.innerHeight() >= $resultsDiv[0].scrollHeight) && !isPaging && confirmationSkipToken) {
            isPaging = true;
            $input.catcomplete("search", lastDisplayed);
        }
    };

    function BindPagingListener() {

        isPaging = false;
        $input.catcomplete("widget").bind("scroll", Page);
    };

    function Search(inputValue, callback) {

        lastInput = inputValue;
        selected = null;

        var confirmationDeffered = new $.Deferred().resolve({ value: [] }, "success");

        if ((inputValue == lastDisplayed && confirmationSkipToken) || inputValue != lastDisplayed)
            confirmationDeffered = SendQuery(inputValue);

        var recordResults = function () {
            return function (confirmationQ) {

                if (confirmationQ[1] == "success" && confirmationQ[0].error == undefined) {

                    var confirmationCodes = confirmationQ[0].value;

                    if (confirmationQ[0]["odata.nextLink"] != undefined) {
                        confirmationSkipToken = confirmationQ[0]["odata.nextLink"]
                                        .substring(confirmationQ[0]["odata.nextLink"].indexOf("$skiptoken"),
                                                   confirmationQ[0]["odata.nextLink"].length);
                    }
                    else {
                        confirmationSkipToken = null;
                    }

                    if (lastDisplayed == null || inputValue != lastDisplayed) {
                        currentResults = [];
                    }

                    for (var i = 0; i < confirmationCodes.length; i++) {
                        var propertyPart = ' [' + confirmationCodes[i].PropertyCode + ' | ' + confirmationCodes[i].TransactionDate + ']';
                        currentResults.push({
                            label: confirmationCodes[i].ConfirmationCode + propertyPart,
                            value: confirmationCodes[i].ConfirmationCode + propertyPart,
                        });
                    }
                }
                else {
                    currentResults = [];
                    callback([{ label: "Error During Confirmation Code Search" }]);
                    selected = null;
                    return;
                }

                if (inputValue == lastInput) {
                    lastDisplayed = inputValue;
                    callback(currentResults);
                }
            };
        };

        $.when(confirmationDeffered)
         .always(recordResults());
    };

    function Listen() {
        $input.catcomplete({
            source: function (request, response) {
                $('.' + this.element[0].id).show();
                Search(request.term, response);
            },
            minLength: 0,
            delay: 200,
            open: function (event, ui) {
                $('.' + event.target.id).hide();
                isResultsOpen = true;
                if (isPaging) {
                    event.target.scrollTop = 0;
                    isPaging = false;
                }
                Page();
            },
            select: function (event, ui) {
                selected = {
                    objectId: ui.item.objectId,
                    displayName: ui.item.label,
                    objectType: ui.item.objectType,
                };
                oldConfirmation = ui.item.label;
                noSelection = false;
            },
            close: function (event, ui) {
                isResultsOpen = false;
                lastDisplayed = null;
                confirmationSkipToken = null;
                currentResults = [];

                // validate via forced selection
                ensureSelection(event);
            },
        });

        $input.focus(function (event) {
            var $input = $('#' + event.target.id);
            if (!isResultsOpen)
                $(this).catcomplete("search", this.value);
        });

        $input.blur(function (event) {
            ensureSelection(event);
        });

        $input.catcomplete("widget").css("max-height", "200px")
            .css("overflow-y", "scroll")
            .css("overflow-x", "hidden");

        BindPagingListener();
    }

    function ensureSelection(event) {
        $('.' + event.target.id).hide();
        var $input = $('#' + event.target.id);
        var currentText = $input.val();
        var $span = $input.siblings('span[data-valmsg-for="' + event.target.id + '"]');
        if ($span != undefined) {
            if (selected == null && currentText != '' && (currentText != oldConfirmation || noSelection)) {
                $span.addClass('field-validation-error').removeClass('field-validation-valid');
                $span.html('Please pick a confirmation code from the dropdown list.');
            }
            else {
                $span.addClass('field-validation-valid').removeClass('field-validation-error');
                $span.html('');
                $('#' + event.target.id).trigger("change");
            }
        }
    };

    // Activate
    Listen();
}

$.widget("custom.catcomplete", $.ui.autocomplete, {
    _create: function () {
        this._super();
        this.widget().menu("option", "items", "> :not(.ui-autocomplete-category)");
    },
    _renderMenu: function (ul, items) {
        var that = this;

        $.each(items, function (index, item) {
            that._renderItemData(ul, item);
        });
    },
    _renderItem: function (ul, item) {

        var label = $("<div>").addClass("confirmation-result-label").css("display", "inline-block").append(item.label);
        var type = $("<div>").addClass("confirmation-result-type")
            .css("text-align", "right")
            .css("display", "inline-block")
            .css("float", "right")
            .append(item.objectType);
        var toappend = [label, type];

        return $("<li>").addClass("confirmation-result-elem").attr("data-selected", "false")
            .attr("data-objectId", item.objectId).append(toappend).appendTo(ul);
    }
});