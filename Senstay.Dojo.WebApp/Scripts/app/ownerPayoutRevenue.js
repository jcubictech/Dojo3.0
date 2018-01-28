"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.OwnerPayoutRevenue = function () {
    var _gridId = undefined,
        $grid = undefined,
        $account = $('#revenueAccount'),
        $month = $('#revenueMonth'),
        $dataCount = $('#dataCount'),
        _editableCheckboxCue = 'manual',
        _allowEdit = false,
        _canEdit = false,
        _action = undefined,
        _saveExpandedColor = '#ffffff',

        init = function (id) {
            $grid = $('#' + id);
            _gridId = '#' + id;
            _allowEdit = $('.revenue-field-readonly').length == 0;
            _canEdit = _allowEdit;
            installEvents();
            DojoWeb.Notification.init('actionAlert', 3000); // install ajax action response messaging

            // if month and source data are available, we retrieve the data
            if ($('#Month').length > 0 && $('#Source').length > 0 && $('#Month').val() != '' && $('#Source').val() != '') {
                $month.data('kendoDatePicker').value($month.data('kendoDatePicker').value());
                $account.data('kendoComboBox').value($('#Source').val());
                if ($('#OwnerPayoutId').length > 0) {
                    var ownerPayoutId = $('#OwnerPayoutId').val();
                    getOwnerPayouts(ownerPayoutId);
                }
                else
                    getOwnerPayouts();
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

            // key input monitor to start query for Owner Payouts
            $account.unbind('change').unbind('click').on('change', function (e) {
                if (requiredSelected()) {
                    getData();
                }
            });

            $month.unbind('change').unbind('click').on('change', function (e) {
                rebindComboBox();
                if (requiredSelected()) {
                    getData();
                }
            });

            $('#discrepancyPayout input').unbind('click').on('click', function (e) {
                if (e.target.checked) {
                    $grid.data('kendoGrid').dataSource.filter({
                        field: 'IsAmountMatched',
                        operator: 'eq',
                        value: false
                    });
                }
                else {
                    $grid.data('kendoGrid').dataSource.filter({
                        logic: 'or',
                        filters: [
                          { field: 'IsAmountMatched', operator: 'eq', value: true },
                          { field: 'IsAmountMatched', operator: 'eq', value: false }
                        ]
                    });
                }
            });
            showMatchedPayout(false);

            // searchable dropdown with color coded properties
            var height = $(window).height() - 300;
            $account.kendoComboBox({
                height: height,
                placeholder: 'Select a source...',
                filter: 'startswith',
                dataTextField: 'Account',
                dataValueField: 'Account',
                dataSource: {
                    type: 'json',
                    transport: {
                        read: {
                            url: '/Property/GetOwnerPayoutAccounts?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy'),
                        }
                    }
                },
                template: '# if (data.Count == 0) { #' +
                             '<span style="color:gray;text-decoration:line-through;">#: data.Account #</span>' +
                          '# } else { #' +
                              '<span style="font-weight:bold;">#: data.Account #</span>' +
                          '# } #',
                dataBound: onAccountDataBound
            });
            // prevent combobox to scroll the page while it is scrolling
            var widget = $account.data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());

            $('#AllMismatchPayouts').kendoButton({
                spriteCssClass: 'fa fa-filter red',
                click: showMismatchePayouts
            });
        },

        getData = function () {
            var selectedMonth = $month.data('kendoDatePicker').value();
            $.ajax({
                type: 'POST',
                url: '/OwnerPayment/IsEditFreezed',
                data: { month: kendo.toString(selectedMonth, 'MM/dd/yyyy') },
                success: function (freeze) {
                    if (freeze == '1') { // currently not editable
                        _canEdit = false;
                        getOwnerPayouts();
                    }
                    else {
                        _canEdit = _allowEdit ? true : false;
                        getOwnerPayouts();
                    }
                },
                error: function (jqXHR, status, errorThrown) { // ignore the error
                    getOwnerPayouts();
                }
            });
        },

        initGridEvents = function () {
            // new Owner Payout - add dialog class cue to actiate it
            $(_gridId + ' .k-grid-add').addClass('showRevenueEdit');

            $(_gridId + ' .k-grid-add').unbind('click').on('click', function (e) {
                _action = 'new';
            });

            $('.showRevenueEdit').unbind('click').on('click', function (e) {
                _action = 'edit';
            });

            // new/edit/view owner payout dialog 
            var caption = 'Edit Owner Payout';
            var account = $account.val().substring(0, $account.val().indexOf('@'));
            DojoWeb.Plugin.initFormDialog({
                selector: '.showRevenueEdit',
                caption: kendo.format('{0} for {1}', caption, account),
                width: 890,
                url: '/OwnerPayout/EditRevenue',
                formId: 'OwnerPayoutEntryForm',
                initEvent: DojoWeb.OwnerPayoutForm.init,
                modal: false,
                closeEvent: null
            });

            // new reservation dialog
            var caption = 'Add Reservation';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showReservationNew',
                caption: kendo.format('{0} for Owner Payout', caption),
                width: 890,
                url: '/OwnerPayout/AddReservation',
                formId: 'ReservationEntryForm',
                initEvent: DojoWeb.NewReservationForm.init,
                modal: false,
                closeEvent: null
            });

            // new resolution dialog
            var caption = 'Add Resolution';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showResolutionNew',
                caption: kendo.format('{0} for Owner Payout', caption),
                width: 890,
                url: '/Resolution/NewRevenue',
                formId: 'ResolutionEntryForm',
                initEvent: DojoWeb.NewResolutionForm.init,
                modal: false,
                closeEvent: null // TODO: refresh the grid
            });

            DojoWeb.MoneyEditor.init();

            // prevent combobox to scroll the page while it is scrolling
            var widget = $account.data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        onAccountDataBound = function (e) {
            var accounts = $account.data('kendoComboBox');
            var legendCount = _.countBy(accounts.dataSource.data(), function (p) {
                if (p.Count == 0)
                    return 'inactive';
                else
                    return 'active';
            });
            // add count to legend
            $('#legend-active').html('Active Accounts (' + (legendCount.active == undefined ? 0 : legendCount.active) + ')');
            $('#legend-inactive').html('Inactive Accounts (' + (legendCount.inactive == undefined ? 0 : legendCount.inactive) + ')');
        },

        initChildEvents = function () {
            // property
            DojoWeb.Plugin.initFormDialog({
                selector: '.showPropertyInfo',
                caption: 'View Property Information',
                width: 1200,
                url: '/Property/ModalDetails',
                modal: true,
            });

            // reservation
            DojoWeb.Plugin.initFormDialog({
                selector: '.showReservationInfo',
                caption: 'View Reservation Information',
                width: 900,
                url: '/Reservation/DetailInfo',
                modal: true,
            });

            // reservation
            DojoWeb.Plugin.initFormDialog({
                selector: '.showResolutionInfo',
                caption: 'View Reservation Information',
                width: 900,
                url: '/Reservation/DetailInfoByConfirmationCode',
                modal: true,
            });
        },

        getAccount = function () {
            return $account ? $account.val() : '';
        },

        getOwnerPayouts = function (selectedId) {
            var selectedMonth = $month.data('kendoDatePicker').value();
            var lastMonth = (new Date()).addMonths(-1);
            //_canEdit = _allowEdit == false ? _allowEdit : kendo.toString(selectedMonth, 'yyyy-MM-dd') >= kendo.toString(lastMonth, 'yyyy-MM-01');
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/OwnerPayout/RevenueView',
                data: { month: month, source: $account.val() },
                success: function (data) {
                    DojoWeb.Busy.hide();
                    if ($.isArray(data)) {
                        showMatchedPayout(true);
                        emptyGrid();
                        var gridParent = initParentGrid();
                        var dataGrid = $grid.kendoGrid(gridParent).data('kendoGrid');

                        dataGrid.bind('dataBound', function (e) {
                            initGridEvents();
                            setCount();
                            setMatchedBackground(e.sender.items());

                            // this will expand the first row
                            //this.expandRow(this.tbody.find("tr.k-master-row").first());

                            var id = DojoWeb.OwnerPayoutForm.getId();
                            var msgTemplate = '{0} of owner payout for account "{0}" is successful.';
                            var message = '';
                            if (_action == 'edit') {
                                message = kendo.format(msgTemplate, 'Update');
                            }
                            else if (id != undefined && _action == 'new') {
                                message = kendo.format(msgTemplate, 'Creation');
                            }
                            else if (_action == 'delete') {
                                message = kendo.format(msgTemplate, 'Deletion');
                            }
                            if (message != '') DojoWeb.Notification.show(message);

                            _action = undefined;

                            if (selectedId != undefined) expandParentById(selectedId);

                            if (!_canEdit) {
                                dataGrid.hideColumn(0);
                                dataGrid.hideColumn(1);
                                dataGrid.hideColumn(2);
                                dataGrid.hideColumn(3);
                                DojoWeb.MoneyEditor.disable();
                            }
                        });
                        var revenueSource = setupDataSource(data);
                        dataGrid.setDataSource(revenueSource);

                        // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                        // we remove the 'filter' text ad-hoc here
                        $(_gridId + ' span.k-filter').text('');

                        if (!_canEdit) {
                            dataGrid.hideColumn(0); // edit
                            dataGrid.hideColumn(1); // delete
                            dataGrid.hideColumn(2); // reserve
                            dataGrid.hideColumn(3); // resolve
                        }

                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                }
            });
        },

        reload = function (ownerPayoutId) {
            getOwnerPayouts(ownerPayoutId);
        },

        initParentGrid = function () {
            var account = $account.val().substring(0, $account.val().indexOf('@'));
            var height = $(window).height() - 300;
            return {
                batch: false,
                height: height,
                resizable: true,
                scrollable: true,
                filterable: true,
                sortable: false,
                reorderable: false,
                selectable: false,
                pageable: false,
                pageSize: 20,
                detailInit: initChildGrid,
                detailExpand: function (e) { // collapse previous row and expand current row
                    _saveExpandedColor = e.masterRow.css('background-color');
                    this.collapseRow(this.tbody.find(' > tr.k-master-row').not(e.masterRow));
                    e.masterRow.css('background-color', '#e0f1fc'); // '#fceae0' brownish, '#e5f9e3' greendish;
                    initChildEvents();
                },
                detailCollapse: function (e) {
                    e.masterRow.css('background-color', _saveExpandedColor);
                },
                toolbar: _canEdit ? [{ name: 'create', text: 'New Owner Payout' }] : null,
                columns: [
                    // action buttons
                    { field: 'edit', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.OwnerPayoutRevenue.renderAction(data.OwnerPayoutId, data.PayoutDate, data.Source, 'edit')#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'delete', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.OwnerPayoutRevenue.renderAction(data.OwnerPayoutId, data.PayoutDate, data.Source, 'delete')#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'reserve', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.OwnerPayoutRevenue.renderAction(data.OwnerPayoutId, data.PayoutDate, data.Source, 'reserve')#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'resolve', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.OwnerPayoutRevenue.renderAction(data.OwnerPayoutId, data.PayoutDate, data.Source, 'resolve')#", attributes: { class: 'grid-cell-align-center' } },
                    // grid data fields
                    { field: 'Source', title: 'Source', width: '250px' },
                    { field: 'PayoutDate', title: 'Payout Date', width: '120px', format: '{0:MM/dd/yyyy}', sortable: false },
                    { field: 'PayToAccount', title: 'PayTo Account', width: '200px' },
                    { field: 'PayoutAmount', title: 'Payout Amount', width: '150px', template: "#= DojoWeb.Template.money(PayoutAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'ReservationTotal', title: 'Reservation Total', width: '150px', template: "#= DojoWeb.Template.money(ReservationTotal) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'ResolutionTotal', title: 'Resolution Total', width: '150px', template: "#= DojoWeb.Template.money(ResolutionTotal) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'DiscrepancyAmount', title: 'Discrepancy Amount', width: '180px', template: "#= DojoWeb.Template.money(DiscrepancyAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'IsAmountMatched', title: 'Amount Matched?', width: '150px', hidden: true, template: "#= DojoWeb.OwnerPayoutRevenue.renderCheckBox(data.OwnerPayoutId, data.IsAmountMatched) #", attributes: { class: 'grid-cell-align-center' } },
                    // identity fields
                    //{ field: 'ReservationId', title: 'Reservation ID', hidden: true },
                    { field: 'OwnerPayoutId', title: 'Owner Payout ID', hidden: true },
                    { field: 'InputSource', title: 'Input Source', hidden: true },
                ],
                //editable: 'inline',
                cancel: function () {
                    $grid.data('kendoGrid').dataSource.cancelChanges();
                }
            }
        },

        initChildGrid = function (e) {
            $('<div/>').appendTo(e.detailCell).kendoGrid({
                dataSource: {
                    data: e.data.Children,
                    schema: {
                        model: {
                            id: 'ChildId',
                            fields: {
                                ChildId: { type: 'number', editable: false, nullable: true },
                                PropertyCode: { type: 'string', editable: false, nullable: false },
                                ConfirmationCode: { type: 'string', editable: false, nullable: false },
                                CheckinDate: { type: 'date', editable: false, nullable: false },
                                Nights: { type: 'number', editable: false, nullable: false },
                                Amount: { type: 'number', editable: false, nullable: false },
                            }
                        }
                    }
                },
                scrollable: false,
                sortable: true,
                pageable: false,
                columns: [
                    { field: "RevenueType", title: "Revenue Type", width: "150px" },
                    { field: "ConfirmationCode", title: "Confirmation Code", width: "150px", template: "#= DojoWeb.OwnerPayoutRevenue.reservationLink(data.ChildId, data.RevenueType, data.ConfirmationCode) #" },
                    { field: "CheckinDate", title: "Checkin Date", width: "120px", format: '{0:MM/dd/yyyy}', },
                    { field: "Nights", title: "Nights", width: "100px" },
                    { field: "PropertyCode", title: "Property Code", width: "120px", template: "#= DojoWeb.OwnerPayoutRevenue.propertyLink(data.ChildId, data.PropertyCode) #" },
                    { field: "Amount", title: "Amount", template: "#= DojoWeb.Template.money(Amount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'ChildId', title: 'Reservation/Resolution ID', hidden: true },
                ]
            });
        },

        setupDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<model>
                schema: {
                    model: {
                        id: 'OwnerPayoutId',
                        fields: {
                            OwnerPayoutId: { type: 'number', editable: false, nullable: false },
                            PayoutDate: { type: 'date', editable: true, nullable: false },
                            Source: { type: 'string', editable: false, nullable: true },
                            PayoutAmount: { type: 'number', editable: true, nullable: false },
                            DiscrepancyAmount: { type: 'number', editable: false, nullable: false },
                            IsAmountMatched: { type: 'boolean', editable: false, nullable: false },
                            InputSource: { type: 'string', editable: false, nullable: true },
                        }
                    }
                },
                error: function (e) {
                    if (e.xhr.responseJSON == undefined)
                        DojoWeb.ActionAlert.fail('revenue-alert', e.xhr.responseText);
                    else
                        DojoWeb.ActionAlert.fail('revenue-alert', e.xhr.responseJSON);
                    var dataGrid = $grid.data('kendoGrid');
                    if (dataGrid != undefined) dataGrid.cancelChanges();
                }
            });
        },

        renderAction = function (id, date, source, action) {
            if (action == 'edit') {
                if (_canEdit)
                    return "<div id='edit-id-" + id + "' class='showRevenueEdit gridcell-btn' title='Edit Owner Payout' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-wrench'></i></div></div>";
                else
                    return "<div id='view-id-" + id + "' class='showRevenueEdit gridcell-btn' title='View Owner Payout' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-eye'></i></div></div>";
            }
            else if (action == 'delete') {
                if (_canEdit)
                    return "<div id='delete-id-" + id + "' class='gridcell-btn' title='Delete Owner Payout' onClick='DojoWeb.OwnerPayoutRevenue.renderDelete(" + '"' + id + '"' + ");'><div class='btn dojo-center'><i class='fa fa-trash-o'></i></div></div>";
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-trash-o'></i></div></div>";
            }
            else if (action == 'reserve') {
                if (_canEdit)
                    return kendo.format("<div id='reserve-id-{0}' class='showReservationNew gridcell-btn' title='Add Reservation' data-id='{0}' data-date='{1}' data-account='{2}'><div class='btn dojo-center'><i class='fa fa-plane'></i></div></div>", id, kendo.toString(date, 'MM/dd/yyyy'), source);
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-plane'></i></div></div>";
            }
            else if (action == 'resolve') {
                if (_canEdit)
                    return kendo.format("<div id='resolve-id-{0}' class='showResolutionNew Edit gridcell-btn' title='Add Resolution' data-id='{0}' data-date='{1}' data-account='{2}'><div class='btn dojo-center'><i class='fa fa-gavel'></i></div></div>", id, kendo.toString(date, 'MM/dd/yyyy'), source);
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-gavel'></i></div></div>";
            }
        },

        renderDelete = function (id) {
            DojoWeb.Confirmation.confirmDiscard({
                id: 'confirmation-dialog',
                caption: 'Delete Owner Payout Confirmation',
                message: 'The selected Owner Payout will be deleted. Please confirm.',
                ok: function () {
                    $.ajax({
                        type: 'POST',
                        url: '/OwnerPayout/DeleteRevenue/?id=' + id,
                        success: function (result) {
                            if (result == 'success') {
                                _action = 'delete';
                                getOwnerPayouts(); // refresh the grid to remove the deleted row
                            }
                            else {
                                DojoWeb.Notification.show('There was an error deleting the Owner Payout.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (status == 'error') {
                                alert('There was an error deleting the Owner Payout.');
                            }
                        }
                    });
                }
            });
        },

        renderCheckBox = function (id, data) {
            var checked = '';
            if (data != 0) checked = 'checked="checked"';
            return kendo.format('<input type="checkbox" data-id="{0}" {1} disabled="true" />', id, checked);
        },

        reservationLink = function (id, revenueType, confirmationCode) {
            if (revenueType == 'Reservation') {
                return '<div class="showReservationInfo" data-id="' + id + '" style="text-align:center;">' + confirmationCode + '</div>';
            }
            else {
                return '<div class="showResolutionInfo" data-id="' + id + '" data-link="' + confirmationCode + '" style="text-align:center;">' + confirmationCode + '</div>';
            }
        },

        propertyLink = function (id, propertyCode) {
            return '<div class="showPropertyInfo" data-id="' + propertyCode + '" style="text-align:center;">' + propertyCode + '</div>';
        },

        requiredSelected = function () {
            return $account.val() != '' && $month.val() != '';
        },

        rebindComboBox = function () {
            $account.data('kendoComboBox').dataSource.transport.options.read.url =
                '/Property/GetOwnerPayoutAccounts?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $account.data('kendoComboBox').dataSource.read();
        },

        emptyGrid = function () {
            $grid.empty(); // empty grid content
        },

        expandParentById = function (id) {
            var grid = $grid.data('kendoGrid');
            var dataView = grid.dataSource.view();
            // grid row is identified by uid; so need to match id then get it from data source
            for (var i = 0; i < dataView.length; i++) {
                if (dataView[i].id == id) {
                    grid.expandRow($grid.find('tr[data-uid="' + dataView[i].uid + '"]'));
                    break;
                }
            }
        },

        showMismatchePayouts = function () {
            $account.data('kendoComboBox').value(''); // remove dropdown account selection
            getOwnerPayouts();
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var revenueSum = 0;
                $.each($view, function (i, r) {
                    revenueSum += r.PayoutAmount;
                });
                var htmlContent = '';
                htmlContent += '(Total Count = ' + count;
                htmlContent += ',  Total Monthly Payout = ' + DojoWeb.Template.money(revenueSum) + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        },

        setMatchedBackground = function (items) {
            var grid = $grid.data("kendoGrid");
            items.each(function (index) {
                var dataItem = grid.dataItem(this);
                if (dataItem.IsAmountMatched == 0) {
                    this.className += " red";
                }
            })
        },

        showMatchedPayout = function (show) {
            show ? $('#discrepancyPayout').removeClass('hide') : $('#discrepancyPayout').addClass('hide');
        }

    return {
        init: init,
        reload: reload,
        getAccount: getAccount,
        renderAction: renderAction,
        renderDelete: renderDelete,
        renderCheckBox: renderCheckBox,
        reservationLink: reservationLink,
        propertyLink: propertyLink
    }
}();

DojoWeb.MoneyEditor = function () {
    var _meneyTemplate = '<div id="owner-payout-edit-money-{0}">{1}</div>',

        _showTemplate = '<div class="revenue-left revenue-edit-money revenue-acton-icon" data-id="{0}" data-money="{2}"><i class="fa fa-pencil green"></i></div>' +
                        '<div class="revenue-right"><span class="revenue-money-width">{1}</span></div>' +
                        '<div class="clearfix"></div>',

        _editTemplate = '<div class="revenue-save-money revenue-left revenue-acton-icon" data-id="{0}" data-money="{2}"><i class="fa fa-save green"></i></div>' +
                        '<div class="revenue-left revenue-cancel-money revenue-acton-icon" data-id="{0}" data-money="{2}"><i class="fa fa-times red"></i></div>' +
                        '<div class="revenue-right"><input id="revenue-money-{0}" type="number" name="revenue-money-{0}" data-id="{0}" value="{2}" class="revenue-money-textbox" /></div>' +
                        '<div class="clearfix"></div>',

        _moneyEditBase = '#owner-payout-edit-money-',
        _newMoneyBase = '#revenue-money-',

        _callback = undefined,

        init = function (callback) {
            initEvents();
            _callback = callback;
        },

        initEvents = function (id) {
            // install edit/save/cancel events
            $('.revenue-edit-money').unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                editMoney(id, $(this).data('money'));
                initEvents(id);
            });

            $('.revenue-save-money').unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                var data = $(_newMoneyBase + id).val();
                var url = kendo.format('/OwnerPayout/SavePayoutAmount?id={0}&amount={1}', id, data);
                if ($.isNumeric(data)) {
                    $.ajax({
                        type: 'POST',
                        url: url,
                        success: function (result) {
                            if (result != '') {
                                var money = DojoWeb.Template.money(parseFloat(data));
                                var moneyHtml = kendo.format(_showTemplate, id, money, data);
                                var $editMoneyDiv = $(_moneyEditBase + id);
                                $editMoneyDiv.html(moneyHtml);
                            }
                            else {
                                DojoWeb.Notification.show('There was an error saving Owner Payout amount.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (status == 'error') {
                                alert('There was an error saving Owner Payout amount.');
                            }
                        }
                    });
                }
                else {
                    // html 5 feature: invalid number will be rejected by 'number' type if <input> tag
                }
                initEvents();
            });

            $('.revenue-cancel-money').unbind('click').on('click', function (e) {
                restoreMoney($(this).data('id'), $(this).data('money'));
                initEvents();
            });

            if (id != undefined) {
                $(_newMoneyBase + id).unbind('keydown').on('keydown', function (e) {
                    if (e.keyCode == 13) {
                        e.preventDefault();
                        $('.revenue-save-money').click();
                    }
                });
            }
        },

        disable = function () {
            $('.revenue-edit-money').addClass('hide');
        },

        renderMoney = function (id, data) {
            var money = DojoWeb.Template.money(data);
            var innerHtml = kendo.format(_showTemplate, id, money, data);
            return kendo.format(_meneyTemplate, id, innerHtml);
        },

        editMoney = function (id, data) {
            var money = DojoWeb.Template.money(data);
            var moneyHtml = kendo.format(_editTemplate, id, money, data);
            var $editMoneyDiv = $(_moneyEditBase + id);
            $editMoneyDiv.html(moneyHtml);
        },

        restoreMoney = function (id, data) {
            var money = DojoWeb.Template.money(data);
            var moneyHtml = kendo.format(_showTemplate, id, money, data);
            var $editMoneyDiv = $(_moneyEditBase + id);
            $editMoneyDiv.html(moneyHtml);
        }

    return {
        init: init,
        disable: disable,
        renderMoney: renderMoney,
        editMoney: editMoney,
        restoreMoney: restoreMoney
}
}();

DojoWeb.OwnerPayoutForm = function () {
    var $form = undefined,
        _currentId = undefined,

        init = function (formId) {
            $form = $('#' + formId);
            _currentId = undefined;
            installControls();
        },

        installControls = function () {
            DojoWeb.Plugin.initDatePicker('.app-simple-datepicker');

            $('#RevenueSave').unbind('click').on('click', function (e) {
                e.preventDefault();
                saveRevenue();
            });

            $('#RevenueCancel').unbind('click').on('click', function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
            });

            $('#Source').val(DojoWeb.OwnerPayoutRevenue.getAccount());
            $('#Source').prop('readonly', 'true');
        },

        getId = function () {
            return _currentId;
        },

        saveRevenue = function () {
            if (validate() > 0) {
                DojoWeb.Notification.show('We found some input errors.');
            }
            else {
                if ($form.valid()) {
                    _currentId = $('#OwnerPayoutId').val();
                    var formData = $form.serialize(); // this is a query string format; not json format
                    $.ajax({
                        type: 'POST',
                        url: '/OwnerPayout/SaveRevenue',
                        data: formData,
                        success: function (result) {
                            if (result != '') {
                                DojoWeb.Plugin.closeFormDialog();
                                _currentId = parseInt(result);
                                DojoWeb.OwnerPayoutRevenue.reload(_currentId);
                            }
                            else {
                                DojoWeb.Notification.show('Dojo encounters error while saving the Owner Payout.', 'error');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (jqXHR.status == 409) { // conflict
                                var message = 'The Owner Payout for property "' + $('#PropertyCode').val() + '" already exists.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 403) { // access denied
                                var message = "You don't have permission to perform this task.";
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 500) { // internal server error
                                var message = 'The Owner Payout cannot be saved to database. If this problem persists, please contact Dojo support team.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (status == 'error') {
                                var message = 'There was an error saving your Owner Payout to the database.';
                                DojoWeb.Notification.show(message);
                            }
                        }
                    });
                }
            }
        },

        validate = function () {
            // required validation
            var invalidCount = $form.valid() ? 0 : 1;
            invalidCount += DojoWeb.Validation.validateInputGroup('#PayoutAmount', 'Payout Amount is required.');
            if (invalidCount == 0) {
                invalidCount += DojoWeb.Validation.validateDecimal('#PayoutAmount', 'Payout Amount is an invalid number.');
            }
            invalidCount += DojoWeb.Validation.validateTextBox('#Source', 'Source is required.');
            invalidCount += DojoWeb.Validation.validateDate('#PayoutDate', 'Payout Date is required.');
            invalidCount += DojoWeb.Validation.validateTextBox('#PayToAccount', 'PayTo Account is required.');
            // data validation
            return invalidCount;
        },

        refresh = function () {
            // for partial view; not used
        },

        serverError = function () {
            // for partial view; not used
        }

    return {
        init: init,
        getId: getId,
        saveRevenue: saveRevenue,
        refresh: refresh,
        serverError: serverError
    }
}();

DojoWeb.NewReservationForm = function () {
    var $form = undefined,
        _ownerPayoutId = undefined,

        init = function (formId, id) {
            $form = $('#' + formId);
            _ownerPayoutId = id;
            installControls();
        },

        installControls = function () {
            DojoWeb.Plugin.initDatePicker('.app-simple-datepicker');

            $('#ReservationSave').unbind('click').on('click', function (e) {
                e.preventDefault();
                saveReservation();
            });

            $('#ReservationCancel').unbind('click').on('click', function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
            });

            var account = $('#reserve-id-' + _ownerPayoutId).data('account');

            DojoWeb.Helpers.preventBackspaceForDropdown('.noBackspace select');

            $('#ConfirmationCode').kendoComboBox({
                height: 400,
                placeholder: 'Type text to search...',
                filter: 'contains',
                dataTextField: 'Text',
                dataValueField: 'Value',
                dataSource: {
                    type: 'json',
                    transport: {
                        read: {
                            url: '/Reservation/GetConfirmationCode?account=' + account,
                        }
                    }
                },
                change: onConfirmationCodeChange,
            });

            var date = $('#reserve-id-' + _ownerPayoutId).data('date');
            if (date != undefined) {
                $('#PayoutDate').val(date);
                $('#PayoutDate').data('kendoDatePicker').readonly();
            }
        },

        onConfirmationCodeChange = function (e) {
            var comboText = this.text();
            if (comboText.indexOf(' | ') > 0) {
                var substrings = comboText.split(' | ');
                if (substrings.length > 1) $('#PropertyCode').val(substrings[1]);
            }
        },

        getId = function () {
            return _ownerPayoutId;
        },

        saveReservation = function () {
            if (validate() > 0) {
                DojoWeb.Notification.show('We found some input errors.');
            }
            else {
                if ($form.valid()) {
                    var formData = $form.serialize(); // this is a query string format; not json format
                    $.ajax({
                        type: 'POST',
                        url: '/OwnerPayout/SaveReservation',
                        data: formData,
                        success: function (result) {
                            if (result != '') {
                                DojoWeb.Plugin.closeFormDialog();
                                DojoWeb.OwnerPayoutRevenue.reload(_ownerPayoutId);
                            }
                            else {
                                DojoWeb.Notification.show('Dojo encounters error while saving the reservation.', 'error');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (jqXHR.status == 409) { // conflict
                                var message = 'The reservation for property "' + $('#PropertyCode').val() + '" already exists.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 403) { // access denied
                                var message = "You don't have permission to perform this task.";
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 500) { // internal server error
                                var message = 'The reservation cannot be saved to database. If this problem persists, please contact Dojo support team.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (status == 'error') {
                                var message = 'There was an error saving your reservation to the database.';
                                DojoWeb.Notification.show(message);
                            }
                        }
                    });
                }
            }
        },

        validate = function () {
            // required validation
            var invalidCount = $form.valid() ? 0 : 1;
            var nightCount = DojoWeb.Validation.validateTextBox('#Nights', 'Nights is required.');
            var totalCount = DojoWeb.Validation.validateInputGroup('#TotalRevenue', 'Total Revenue is required.');
            invalidCount += nightCount + totalCount;
            if (nightCount == 0) {
                invalidCount += DojoWeb.Validation.validatePositiveNumber('#Nights', 'Nights is an invalid number.');
            }
            if (totalCount == 0) {
                invalidCount += DojoWeb.Validation.validateDecimal('#TotalRevenue', 'Total Revenue is an invalid number.');
            }
            invalidCount += DojoWeb.Validation.validateTextBox('#PropertyCode', 'Property Code is required.');
            invalidCount += DojoWeb.Validation.showComboboxError('#ConfirmationCode', 'Confirmation Code is required.');
            invalidCount += DojoWeb.Validation.validateDate('#CheckinDate', 'Checkin Date is required.');
            invalidCount += DojoWeb.Validation.validateTextBox('#GuestName', 'Guest Name is required.');
            invalidCount += DojoWeb.Validation.validateDropdown('#Channel', 'Channel is required.');
            // data validation
            return invalidCount;
        },

        refresh = function () {
            // for partial view; not used
        },

        serverError = function () {
            // for partial view; not used
        }

    return {
        init: init,
        getId: getId,
        saveReservation: saveReservation,
        refresh: refresh,
        serverError: serverError
    }
}();

DojoWeb.NewResolutionForm = function () {
    var $form = undefined,
        _ownerPayoutId = undefined,

        init = function (formId, id) {
            $form = $('#' + formId);
            _ownerPayoutId = id;
            installControls();
        },

        installControls = function () {
            DojoWeb.Plugin.initDatePicker('.app-simple-datepicker');

            $('#ResolutionSave').unbind('click').on('click', function (e) {
                e.preventDefault();
                saveRevenue();
            });

            $('#ResolutionCancel').unbind('click').on('click', function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
            });

            var date = $('#resolve-id-' + _ownerPayoutId).data('date');
            if (date != undefined) {
                $('#ResolutionDate').val(date);
                $('#ResolutionDate').data('kendoDatePicker').readonly();
            }

            var account = $('#resolve-id-' + _ownerPayoutId).data('account');
            if (account != undefined) {
                $('#Source').val(account);
            }

            $('#ConfirmationCode').kendoComboBox({
                height: 400,
                placeholder: 'Search/Enter a confirmation or property code...',
                filter: 'contains',
                dataTextField: 'Text',
                dataValueField: 'Value',
                dataSource: {
                    type: 'json',
                    //serverFiltering: true,
                    transport: {
                        read: {
                            url: '/Reservation/GetConfirmationCode?account=' + $('#Source').val(),
                        }
                    }
                },
                change: onConfirmationCodeChange,
            });
        },

        onConfirmationCodeChange = function (e) {
            var comboText = this.text();
            var substrings = comboText.split('|');
            if (substrings.length == 1) {
                $('#PropertyCode').val(substrings[0].trim());
            }
            else if (substrings.length > 1)
                $('#PropertyCode').val(substrings[1].trim());
        },

        getId = function () {
            return _ownerPayoutId;
        },

        saveRevenue = function () {
            if (validate() > 0) {
                DojoWeb.Notification.show('We found some input errors.');
            }
            else {
                if ($form.valid()) {
                    var formData = $form.serialize(); // this is a query string format; not json format
                    $.ajax({
                        type: 'POST',
                        url: '/Resolution/SaveRevenue',
                        data: formData,
                        success: function (result) {
                            if (result != '') {
                                DojoWeb.Plugin.closeFormDialog();
                                DojoWeb.OwnerPayoutRevenue.reload(_ownerPayoutId);
                            }
                            else {
                                DojoWeb.Notification.show('Dojo encounters error while saving the resolution.', 'error');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (jqXHR.status == 409) { // conflict
                                var message = 'The resolution for property "' + $('#PropertyCode').val() + '" already exists.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 403) { // access denied
                                var message = "You don't have permission to perform this task.";
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 500) { // internal server error
                                var message = 'The resolution cannot be saved to database. If this problem persists, please contact Dojo support team.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (status == 'error') {
                                var message = 'There was an error saving your resolution to the database.';
                                DojoWeb.Notification.show(message);
                            }
                        }
                    });
                }
            }
        },

        validate = function () {
            // required validation
            var invalidCount = $form.valid() ? 0 : 1;
            invalidCount += DojoWeb.Validation.validateInputGroup('#ResolutionAmount', 'Resolution Amount is required.');
            if (invalidCount == 0) {
                invalidCount += DojoWeb.Validation.validateDecimal('#ResolutionAmount', 'Resolution Amount is an invalid number.');
            }
            invalidCount += DojoWeb.Validation.showComboboxError('#ConfirmationCode', 'Confirmation Code is required.');
            invalidCount += DojoWeb.Validation.validateTextBox('#PropertyCode', 'Property Code is required.');
            invalidCount += DojoWeb.Validation.validateTextBox('#ResolutionType', 'Resolution Type is required.');
            invalidCount += DojoWeb.Validation.validateTextBox('#ResolutionDescription', 'Resolution Description is required.');
            invalidCount += DojoWeb.Validation.validateDate('#ResolutionDate', 'Resolution Date is required.');
            invalidCount += DojoWeb.Validation.validateDropdown('#Impact', 'Impact is required.');
            // data validation
            return invalidCount;
        },

        refresh = function () {
            // for partial view; not used
        },

        serverError = function () {
            // for partial view; not used
        }

    return {
        init: init,
        getId: getId,
        saveRevenue: saveRevenue,
        refresh: refresh,
        serverError: serverError
    }
}();