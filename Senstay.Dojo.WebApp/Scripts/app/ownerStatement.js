"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.OwnerStatement = function () {
    var 
        $property = $('#revenuePropertyCode'),
        $month = $('#revenueMonth'),
        $ownerSummaryView = $('#ownerSummaryView'),
        _finalized = '<div><i class="fa fa-calendar-check-o fa-4x green"></i></div><div class="finalize-text">Un-finalize Statement</div>',
        _finalizing = '<div><i class="fa fa-calendar-check-o fa-4x blue"></i></div><div class="finalize-text">Finalize Statement</div>',

        init = function () {
            installEvents();
            $(window).scrollTop();
            DojoWeb.Notification.init('actionAlert', 3000);

            // deal with link from statement summary that has month and property code parameters
            var month = DojoWeb.Helpers.getQueryString('month');
            var propertyCodeWithVertical = DojoWeb.Helpers.getQueryString('propertyCode');
            if (month != undefined) {
                var statementDate = kendo.parseDate(month);
                $month.data('kendoDatePicker').value(statementDate);
                var tokens = propertyCodeWithVertical.split('-');
                $property.data('kendoComboBox').value(tokens[0]);
                $property.change(); // trigger statement partial view to load
            }

            
        },

        installEvents = function () {
            // month picker
            $month.kendoDatePicker({
                start: 'year', // defines the start view
                depth: 'year', // defines when the calendar should return date            
                format: 'MMMM yyyy', // display month and year in the input
                dateInput: true // specifies that DateInput is used for masking the input element
            });
            $month.attr('readonly', 'readonly'); // no direct typing

            // key input monitor to start query for reservations
            $property.unbind('change').on('change', function (e) {
                ensureRequiredSelected();
            });

            $month.unbind('change').on('change', function (e) {
                ensureRequiredSelected();
                rebindComboBox();
            });

            $('#printStatement').kendoButton({
                spriteCssClass: 'fa fa-print green',
                click: print
            });

            // searchable dropdown with color coded properties
            var height = $(window).height() - 300;
            $property.kendoComboBox({
                height: height,
                placeholder: 'Select a property...',
                filter: 'contains',
                dataTextField: 'PropertyCodeWithPayoutMethod',
                dataValueField: 'PropertyCode',
                dataSource: {
                    type: 'json',
                    //serverFiltering: true,
                    transport: {
                        read: {
                            url: '/Property/GetOwnerStatementPropertyList?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy'),
                        }
                    }
                },
                template: '# if (data.Empty == 1) { #' +
                             '<span style="color:Gray;text-decoration:line-through;">#: data.PropertyCodeWithPayoutMethod #</span>' +
                          '# } else if (data.Finalized == 1) { #' +
                             '<span style="color:purple;">#: data.PropertyCodeWithPayoutMethod #</span>' +
                          '# } else if (data.ReservationApproved == 1 && data.ResolutionApproved == 1 && data.ExpenseApproved == 1) { #' +
                             '<span style="color:blue;">#: data.PropertyCodeWithPayoutMethod #</span>' +
                          '# } else if (data.ReservationReviewed == 1 && data.ResolutionAReviewed == 1 && data.ExpenseReviewed == 1) { #' +
                             '<span style="color:green;">#: data.PropertyCodeWithPayoutMethod #</span>' +
                          '# } else { #' +
                              '<span style="color:black;">#: data.PropertyCodeWithPayoutMethod #</span>' +
                          '# } #',
                dataBound: onPropertyDataBound
            });

            // prevent combobox to scroll the page while it is scrolling
            var widget = $property.data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        onPropertyDataBound = function (e) {
            var properties = $property.data('kendoComboBox');
            var legendCount = _.countBy(properties.dataSource.data(), function (p) {
                if (p.Empty == 1)
                    return 'noData';
                else if (p.Finalized == 1)
                    return 'finalized';
                else if (p.ReservationApproved == 1 && p.ResolutionApproved == 1 && p.ExpenseApproved == 1)
                    return 'approved';
                else if (p.ReservationReviewed == 1 && p.ResolutionReviewed == 1 && p.ExpenseReviewed == 1)
                    return 'reviewed';
                else
                    return 'hasData';
            });
            // add count to legend
            $('#legend-no-data').html('No Data (' + (legendCount.noData == undefined ? 0 : legendCount.noData) + ')');
            $('#legend-finalized').html('Finalized (' + (legendCount.finalized == undefined ? 0 : legendCount.finalized) + ')');
            $('#legend-approved').html('Approved (' + (legendCount.approved == undefined ? 0 : legendCount.approved) + ')');
            $('#legend-reviewed').html('Reviewed (' + (legendCount.reviewed == undefined ? 0 : legendCount.reviewed) + ')');
            $('#legend-has-data').html('To-Do (' + (legendCount.hasData == undefined ? 0 : legendCount.hasData) + ')');
        },

        getOwnerStatement = function () {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/OwnerStatement/OwnerStatement',
                data: { month: month, propertyCode: $property.val() },
                success: function (htmlPartialView) {
                    DojoWeb.Busy.hide();
                    $('#ownerStatementView').html(htmlPartialView);
                    rebindComboBox();

                    var approvalAlert = $('#ApprovalAlert').text();
                    if (approvalAlert != '') DojoWeb.ActionAlert.fail('approval-alert', approvalAlert);

                    var modifyAlert = $('#ModifyAlert').text();
                    if (modifyAlert != '') DojoWeb.ActionAlert.warn('approval-alert', modifyAlert);

                    $('.statement-button').removeClass('hide');

                    var text = $property.data("kendoComboBox").text();
                    var parts = text.split('|');
                    var payoutmethod = parts[0].trim();
                    var label = parts.length > 2 && parts[2].trim() != '' ? parts[2].trim() : payoutmethod;
                    $('#summary-link').html(makeSummaryLink(month, payoutmethod, label));

                    // adjust statement summary note height to align with statement summary height
                    if ($('.advance-payment-item').length > 0) {
                        if ($('.cleaning-fee-item').length > 0)
                            $('.statementSummary-note-body').css('height', '252px');
                        else
                            $('.statementSummary-note-body').css('height', '230px');
                    }
                    else {
                        if ($('.cleaning-fee-item').length > 0)
                            $('.statementSummary-note-body').css('height', '219px');
                        else
                            $('.statementSummary-note-body').css('height', '198px');
                    }

                    // hide finalize button if it is not the statement month or beyond
                    var selectedMonth = $month.data('kendoDatePicker').value();
                    var lastMonth = (new Date()).addMonths(-1);
                    //if (kendo.toString(selectedMonth, 'yyyy-MM-dd') >= kendo.toString(lastMonth, 'yyyy-MM-01')) {
                    if ($('#IsEditFreezed').val().toLocaleLowerCase() == 'false') {
                        $('.stateSummary-finalize-row').removeClass('hide');
                        $('.statementSummary-note-body textarea').prop('disabled', false);
                    }
                    else {
                        $('.stateSummary-finalize-row').addClass('hide');
                        var height = $('.statement-container').css('height');
                        $('.statementSummary-note').css('height', height);
                        $('.statementSummary-note-body textarea').prop('disabled', true);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                }
            });
        },

        finalizeStatement = function () {
            // toggle finalize status
            var $finalize = $('#finalizeStatement');
            var $finalizeCue = $('.finalize-text');
            if ($finalizeCue.length > 0) {
                if ($finalizeCue.text() == 'Finalize Statement')
                    finalize($finalize, true, _finalized);
                else // retract finalization
                    finalize($finalize, false, _finalizing);
            }
        },

        finalize = function ($finalize, isFinalized, html) {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            var propertyCode = $property.val();
            var note = $('#StatementNotes').val();
            $.ajax({
                type: 'POST',
                url: '/OwnerStatement/Finalize',
                data: { month: month, propertyCode: propertyCode, note: note, isFinalized: isFinalized },
                success: function (result) {
                    DojoWeb.Busy.hide();
                    $finalize.html(html);
                    if (isFinalized) {
                        DojoWeb.Notification.show('The owner statement is finalized.');
                        clearModifyNotice();
                    }
                    else {
                        DojoWeb.Notification.show('The owner statement is un-finalized.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    alert('Fail to finalize the owner statement.');
                }
            });
        },

        print = function (month, statementDate) {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $.ajax({
                type: 'POST',
                url: '/OwnerStatement/PrintStatement',
                data: { month: month, propertyCode: $property.val() },
                success: function (htmllView) {
                    // show printable page to a separate browser tab
                    var newtab = window.open();
                    newtab.document.write(htmllView);
                    newtab.document.close();
                },
                error: function (jqXHR, status, errorThrown) {
                }
            });
        },

        rebindComboBox = function () {
            $property.data('kendoComboBox').dataSource.transport.options.read.url =
                '/Property/GetOwnerStatementPropertyList?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $property.data('kendoComboBox').dataSource.read();
        },

        ensureRequiredSelected = function () {
            if ($property.val() != '' && $month.val() != '')
                getOwnerStatement();
            else
                $('#ownerStatementView').html('');
        },

        clearModifyNotice = function () {
            $('#approval-alert').html('');
        },

        makeSummaryLink = function (month, payoutmethod, label) {
            return '<div><a href="/OwnerStatement/OwnerSummaryView?month=' + month + '&payoutmethod=' + encodeURIComponent(payoutmethod) + '" target="_blank">' + label + '</a></div>';
        }

    return {
        init: init,
        finalizeStatement: finalizeStatement,
        print: print,
    }
}();

DojoWeb.OwnerStatementForm = function () {
    var $ImportDate = $('#StatementDate'),
        init = function () {
            initEvents();
            $ImportDate.data('kendoDatePicker').value('');
        },

        initEvents = function () {
            $ImportDate.kendoDatePicker({
                start: 'year', // defines the start view
                depth: 'year', // defines when the calendar should return date            
                format: 'MMMM yyyy', // display month and year in the input
                dateInput: true // specifies that DateInput is used for masking the input element
            }).data('kendoDatePicker');

            $('#ownerStatementImport').unbind('click').on('click', function (e) {
                var month = kendo.toString($ImportDate.data('kendoDatePicker').value(), 'MM/dd/yyyy');
                if (month != null) {
                    var type = $('input[name="statement-type"]:checked').val();
                    if (type != undefined) {
                        doImport(month, type);
                    }
                    else {
                        alert("Please select a statement type.")
                    }
                }
                else {
                    alert("Please select a month.")
                }
            });
        },

        doImport = function (month, type) {
            DojoWeb.Busy.show();
            var url = type == 'Summary' ? kendo.format('/OwnerStatement/BackFillOwnerSummaries?month={0}', month)
                                        : kendo.format('/OwnerStatement/BackFillStatements?month={0}', month);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    DojoWeb.Busy.hide();
                    if (result.indexOf('success') != 0) {
                        DojoWeb.ActionAlert.fail('import-alert', result);
                    }
                    else {
                        var month = kendo.toString($ImportDate.data('kendoDatePicker').value(), 'MMM yyyy');
                        if (result.indexOf('Summary') > 0)
                            DojoWeb.ActionAlert.success('import-alert', 'Owner Summaries for ' + month + ' are backfilled.');
                        else
                            DojoWeb.ActionAlert.success('import-alert', 'Owner Statements for ' + month + ' are backfilled.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error while backfilling owner statements.';
                        alert(message);
                    }
                }
            });
        },

        toggleDateTyping = function (disabled) {
            // enable/disable typing directly into datepicker
            $('.k-datepicker input').prop('readonly', disabled);
        },

        refreshForm = function () {
        },

        serverError = function () {
        }

    return {
        init: init
    }
}();
