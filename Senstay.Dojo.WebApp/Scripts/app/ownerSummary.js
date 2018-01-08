"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.OwnerSummary = function () {
    var $payoutMethod = $('#revenuePayoutMethod'),
        $month = $('#revenueMonth'),
        $ownerSummaryView = $('#ownerSummaryView'),

        init = function () {
            installEvents();
            $(window).scrollTop();
            var month = DojoWeb.Helpers.getQueryString('month');
            var payoutmethod = DojoWeb.Helpers.getQueryString('payoutmethod');
            if (month != undefined) {
                payoutmethod = decodeURIComponent(payoutmethod);
                var statementDate = kendo.parseDate(month);
                $month.data('kendoDatePicker').value(statementDate);
                $payoutMethod.data('kendoComboBox').value(payoutmethod);
                getOwnerSummary();
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
            $payoutMethod.unbind('change').on('change', function (e) {
                ensureRequiredSelected();
            });

            $month.unbind('change').on('change', function (e) {
                ensureRequiredSelected();
                rebindComboBox();
            });

            // searchable dropdown with color coded properties
            var height = $(window).height() - 300;
            $payoutMethod.kendoComboBox({
                height: height,
                placeholder: 'Select a payout method...',
                filter: 'startswith',
                dataTextField: 'PayoutMethodAndPropertyCode',
                dataValueField: 'PayoutMethod',
                dataSource: {
                    type: 'json',
                    transport: {
                        read: {
                            url: '/Property/GetPayoutMethods?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy')
                        }
                    }
                },
                template: '# if (data.Empty == 1) { #' +
                             '<span style="color:Gray;text-decoration:line-through;">#: data.PayoutMethodAndPropertyCode #</span>' +
                          '# } else if (data.SummaryFinalized == 1) { #' +
                             '<span style="color:orange;">#: data.PayoutMethodAndPropertyCode #</span>' +
                          '# } else if (data.StatementFinalized == 1) { #' +
                             '<span style="color:purple;">#: data.PayoutMethodAndPropertyCode #</span>' +
                          //'# } else if (data.StatementPartialFinalized == 1) { #' +
                          //   '<span style="color:green;">#: data.PayoutMethodAndPropertyCode #</span>' +
                          '# } else { #' +
                              '<span style="color:black;">#: data.PayoutMethodAndPropertyCode #</span>' +
                          '# } #',
                dataBound: onPayoutMethodDataBound
            });

            // prevent combobox to scroll the page while it is scrolling
            var widget = $payoutMethod.data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());

            $('#printSummary').kendoButton({
                spriteCssClass: 'fa fa-print green',
                click: printSummary
            });

            $('#rebalanceSummary').kendoButton({
                spriteCssClass: 'fa fa-balance-scale purple',
                click: rebalanceSummary
            });

            $('#finalizeSummary').kendoButton({
                spriteCssClass: 'fa fa-check orange',
                click: finalizeSummary
            });
        },

        onPayoutMethodDataBound = function (e) {
            var payoutMethods = $payoutMethod.data('kendoComboBox');
            var legendCount = _.countBy(payoutMethods.dataSource.data(), function (p) {
                if (p.Empty == 1)
                    return 'noData';
                else if (p.SummaryFinalized == 1)
                    return 'summaryFinalized';
                else if (p.StatementFinalized == 1)
                    return 'statementFinalized';
                else
                    return 'hasData';
            });
            // add count to legend
            $('#legend-no-data').html('No Data (' + (legendCount.noData == undefined ? 0 : legendCount.noData) + ')');
            $('#legend-summary-finalized').html('Summary Finalized (' + (legendCount.summaryFinalized == undefined ? 0 : legendCount.summaryFinalized) + ')');
            $('#legend-statement-finalized').html('Statement Finalized (' + (legendCount.statementFinalized == undefined ? 0 : legendCount.statementFinalized) + ')');
            $('#legend-has-data').html('To-Do (' + (legendCount.hasData == undefined ? 0 : legendCount.hasData) + ')');
        },

        getOwnerSummary = function () {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/OwnerStatement/OwnerSummary',
                data: { month: month, payoutMethod: $payoutMethod.val() },
                success: function (htmlPartialView) {
                    DojoWeb.Busy.hide();
                    $('#ownerSummaryView').html(htmlPartialView);
                    rebindComboBox()

                    $('.summary-button').removeClass('hide'); // the print summary button
                    var isFinalized = $('#IsFinalized').val().toLocaleLowerCase() == 'true';
                    var isRebalanced = $('#IsRebalanced').val().toLocaleLowerCase() == 'true';
                    renderFinalizeButton(isFinalized, isRebalanced);

                    if (isFinalized) clearModifyNotice();

                    if (!isRebalanced) {
                        var message = "DojoApp recommends using the 'Rebalance' button to redistribute the balances among properties.";
                        DojoWeb.ActionAlert.fail('rebalance-alert', message);
                    }

                    var modifyAlert = $('#ModifyAlert').text();
                    if (modifyAlert != '') DojoWeb.ActionAlert.warn('rebalance-alert', modifyAlert);

                    // hide rebalance and finalize button if it is not the statement month or beyond
                    var selectedMonth = $month.data('kendoDatePicker').value();
                    var lastMonth = (new Date()).addMonths(-1);
                    //if (kendo.toString(selectedMonth, 'yyyy-MM-dd') >= kendo.toString(lastMonth, 'yyyy-MM-01')) {
                    if ($('#IsEditFreezed').val().toLocaleLowerCase() == 'false') {
                        $('.rebalance-button').removeClass('hide');
                        $('.finalize-button').removeClass('hide');
                        $('.statementSummary-note-body textarea').prop('disabled', false);
                    } else {
                        $('.rebalance-button').addClass('hide');
                        $('.finalize-button').addClass('hide');
                        $('.statementSummary-note-body textarea').prop('disabled', true);
                    }

                    if ($('#PaidPayout').val() != '0') {
                        $('#paidPayoutAmount').html('$' + $('#PaidPayout').val());
                    }
                    else
                        $('#paidPayoutAmount').html('');
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                }
            });
        },

        printSummary = function () {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $.ajax({
                type: 'POST',
                url: '/OwnerStatement/PrintSummary',
                data: { month: month, payoutMethod: $payoutMethod.val() },
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

        rebalanceSummary = function (e) {
            e.preventDefault();
            DojoWeb.RebalanceForm.showDialog(currentMonth(), payoutMethod());
        },

        finalizeSummary = function () {
            var text = $('#finalizeSummary span.finalizeText').text();
            var isFinalized = text.indexOf('Finalize') == 0 ? true : false;
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $.ajax({
                type: 'POST',
                url: '/OwnerStatement/FinalizeSummary',
                data: { month: month, payoutMethod: $payoutMethod.val(), note: $('#SummaryNotes').val(), isFinalized: isFinalized },
                success: function (result) {
                    renderFinalizeButton(isFinalized, true);
                    getOwnerSummary(); // refresh the summary content
                    DojoWeb.Notification.show(kendo.format('Owner Summary result is {0}.', (isFinalized ? 'finalized' : 'unfinalized')));
                },
                error: function (jqXHR, status, errorThrown) {
                }
            });
        },

        clearModifyNotice = function () {
            $('#approval-alert').html('');
            },

        rebindComboBox = function () {
            $payoutMethod.data('kendoComboBox').dataSource.transport.options.read.url =
                '/Property/GetPayoutMethods?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $payoutMethod.data('kendoComboBox').dataSource.read();
        },

        ensureRequiredSelected = function () {
            if ($payoutMethod.val() != '' && $month.val() != '') {
                getOwnerSummary();
                return true;
            }
            else {
                $('#ownerSummaryView').html('');
                return false;
            }
        },

        renderFinalizeButton = function (isFinalized, isRebalanced) {
            if (isFinalized) {
                $('#finalizeSummary span.finalizeText').text('Unfinalize Summary');
                $('#finalizeSummary span.k-sprite').removeClass('purple').addClass('orange');
                //$('#finalizeSummary').data('kendoButton').enable(true); // always enable unfinalize button
            }
            else {
                $('#finalizeSummary span.finalizeText').text('Finalize Summary');
                $('#finalizeSummary span.k-sprite').removeClass('orange').addClass('purple');
                //$('#finalizeSummary').data('kendoButton').enable(isRebalanced); // enable only when rebalance is done
            }
        },

        renderPaidAmount = function (amount) {
            $('#paidPayoutAmount').html(amount);
        },

        payoutMethod = function () {
            return $payoutMethod.val();
        },

        currentMonth = function () {
            return $month.data('kendoDatePicker').value();
        }

    return {
        init: init,
        getOwnerSummary: getOwnerSummary,
        renderPaidAmount: renderPaidAmount,
        payoutMethod: payoutMethod,
        currentMonth: currentMonth
    }
}();

DojoWeb.RebalanceForm = function () {
    var $formDialog = $('#formDialog'),
        $form = $('#RebalanceEntryForm'),
        _fromProperty = '#FromProperty',
        _toProperty = '#ToProperty',
        $fromProperty = undefined,
        $toProperty = undefined,
        $add = undefined,
        $save = undefined,
        $cancel = undefined,
        _rebalanceTemplate = '<div id="rebalance-item-{0}" class="rebalance-note"><div class="removeRebalance float-left" data-id="rebalance-item-{0}" data-from="{1}" data-to="{2}" data-amount="{3}"><span class="fa fa-remove red"></span></div><div class="float-left"><span>Rebalance: Debit {4} to {2}; Credit {3} from {1}</span></div><br/></div>',
        _rebalanceItemIndex = 1,
        _propertyBalances = [],
        _rebalanceTransactions = [],

        showDialog = function (month, payoutMethod) {
            DojoWeb.Busy.show();
            $.ajax({
                url: '/OwnerStatement/SummaryRebalance',
                data: { month: kendo.toString(month, 'MM/dd/yyyy'), payoutMethod: payoutMethod },
                success: function (data) {
                    DojoWeb.Busy.hide();
                    if ($formDialog.length > 0) {
                        $('#formDialog .dialog-body').html(data);
                        if (!$formDialog.data('kendoWindow')) {
                            var monthName = kendo.toString(month, 'MMMM');
                            $formDialog.kendoWindow({
                                width: 680,
                                title: kendo.format('Rebalance {0} Statements for "{1}"', monthName, payoutMethod),
                                actions: [],
                                resizable: false,
                                modal: false
                            });
                        }
                        $formDialog.data('kendoWindow').open().center(); // open() needs to come before center()

                        $formDialog.data('kendoWindow').bind('close', function (e) {
                            if (_rebalanceTransactions.length > 0) {
                                DojoWeb.OwnerSummary.getOwnerSummary(); // refresh owner summary grid
                            }
                            //e.preventDefault(); // will prevent kendo window to be closed
                        });

                        initEvents();
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                    }
                }
            })
        },

        initEvents = function () {
            // set the jquery object after the dialog is created
            $fromProperty = $(_fromProperty),
            $toProperty = $(_toProperty),
            $add = $('#RebalanceAdd'),
            $save = $('#RebalanceSave'),
            $cancel = $('#RebalanceCancel'),

            _rebalanceTransactions = []; // initialize the transaction array

            DojoWeb.Plugin.initDatePicker('.app-simple-datepicker');

            $fromProperty.unbind('change').on('change', function (e) {
                showPropertyBalance('fromBalance', $(this).val());
            });

            $toProperty.unbind('change').on('change', function (e) {
                showPropertyBalance('toBalance', $(this).val());
            });

            $add.unbind('click').on('click', function (e) {
                e.preventDefault();
                applyRebalance();
            });

            $save.unbind('click').on('click', function (e) {
                e.preventDefault();
                saveRebalances();
            });

            $cancel.unbind('click').on('click', function (e) {
                e.preventDefault();
                closeDialog();
            });

            // need to do it this way to make click event available to dynamically created elements 
            $('body').on('click', 'div.removeRebalance', function (e) {
                var id = $(this).data('id');
                var from = $(this).data('from');
                var to = $(this).data('to');
                var amount = DojoWeb.Helpers.removeUsCurrencyMask($(this).data('amount'));

                // reverse the transaction
                if (amount.indexOf('-') < 0)
                    amount = '-' + amount;
                else
                    amount.replace('-', '');
                removeRebalance(id, from, to, amount);
            });

            DojoWeb.Helpers.preventBackspaceForDropdown('.noBackspace select');

            _propertyBalances = jQuery.parseJSON($('#PropertyBalancesJson').val());

            showSaveButton(_rebalanceTransactions.length == 0);
        },

        applyRebalance = function () {
            if (validate()) {
                var fromAddress = $fromProperty.find('option:selected').text();
                var fromCombo = fromAddress.split(' | ');
                if (fromCombo.length > 1) fromAddress = fromCombo[1];
                var toAddress = $toProperty.find('option:selected').text();
                var toCombo = toAddress.split(' | ');
                if (toCombo.length > 1) toAddress = toCombo[1];

                var formData = {
                    FromProperty: $fromProperty.val(),
                    ToProperty: $toProperty.val(),
                    FromAddress: fromAddress,
                    ToAddress: toAddress,
                    RebalanceAmount: $('#RebalanceAmount').val(),
                };

                var html = $('#rebalanceResult').html(); // existing transaction records
                var debit = DojoWeb.Helpers.formatCurrency(-formData.RebalanceAmount);
                var credit = DojoWeb.Helpers.formatCurrency(formData.RebalanceAmount);
                var transaction = kendo.format(_rebalanceTemplate, _rebalanceItemIndex, formData.FromProperty, formData.ToProperty, credit, debit);
                $('#rebalanceResult').html(html + transaction);
                _rebalanceItemIndex++;

                _rebalanceTransactions.push({
                    FromProperty: formData.FromProperty,
                    ToProperty: formData.ToProperty,
                    FromAddress: fromAddress,
                    ToAddress: toAddress,
                    TransactionAmount: parseFloat(formData.RebalanceAmount)
                });

                // update from and to property balances
                updateBalances(formData.FromProperty, formData.ToProperty, formData.RebalanceAmount);

                showSaveButton(_rebalanceTransactions.length == 0);
            }
        },

        removeRebalance = function (id, from, to, money) {
            updateBalances(from, to, money);
            var amount = -parseFloat(money); // need to revese the sign to get the original value
            _rebalanceTransactions = _.reject(_rebalanceTransactions, function (item) {
                return item.FromProperty == from &&
                       item.ToProperty == to &&
                       Math.abs(item.TransactionAmount - amount) < 0.01;
            });
            $('#' + id).remove();

            showSaveButton(_rebalanceTransactions.length == 0);
        },

        saveRebalances = function () {
            if (_rebalanceTransactions.length == 0) { // nothing to do
                closeDialog();
                return;
            }

            var formData = {
                PayoutMethod: $('#PayoutMethod').val(),
                PayoutMonth: $('#PayoutMonth').val(),
                PayoutYear: $('#PayoutYear').val(),
                Transactions: _rebalanceTransactions,
            };

            DojoWeb.Busy.cursor('#formDialog', true);
            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/OwnerStatement/RebalanceSummary',
                data: formData,
                success: function (result) {
                    DojoWeb.Busy.hide();
                    DojoWeb.Busy.cursor('#formDialog', false);
                    if (result == 'success') {
                        closeDialog();
                        DojoWeb.OwnerSummary.getOwnerSummary();
                    }
                    else {
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                }
            });
        },

        updateBalances = function (from, to, money) {
            // update from and to property balances
            var fromProperty = _.findWhere(_propertyBalances, { PropertyCode: from });
            var toProperty = _.findWhere(_propertyBalances, { PropertyCode: to });
            var amount = parseFloat(money);
            fromProperty.PropertyBalance -= amount;
            toProperty.PropertyBalance += amount;
            showPropertyBalance('fromBalance', from);
            showPropertyBalance('toBalance', to);

        },

        showPropertyBalance = function (id, propertyCode) {
            var property = _.findWhere(_propertyBalances, { PropertyCode: propertyCode });
            if (property !== undefined) {
                var currency = DojoWeb.Helpers.formatCurrency(property.PropertyBalance);
                $('#' + id).html(kendo.format(' --- [ {0} ]', currency));
            }
        },

        showSaveButton = function (hide) {
            $save.prop('disabled', hide);
        },

        closeDialog = function () {
            if ($formDialog && $formDialog.data('kendoWindow')) $formDialog.data('kendoWindow').close();
        },

        validate = function () {
            var invalidCount = 0;
            invalidCount += DojoWeb.Validation.validateInputGroup('#RebalanceAmount', 'Rebalance Amount is required.');
            if (invalidCount == 0) {
                invalidCount += DojoWeb.Validation.validatePositiveDecimal('#RebalanceAmount', 'Rebalance Amount must be positive');
            }
            invalidCount += DojoWeb.Validation.validateDropdown(_fromProperty, 'From Property is required.');
            invalidCount += DojoWeb.Validation.validateDropdown(_toProperty, 'To Property is required.');
            //invalidCount += DojoWeb.Validation.validateDate('#RebalanceDate', 'Rebalance Date is required.');
            return invalidCount == 0;
        }

    return {
        showDialog: showDialog
    }
}();
