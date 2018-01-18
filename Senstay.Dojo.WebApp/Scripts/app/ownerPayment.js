"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.PayoutMethodPayment = function () {
    var _gridId = undefined,
        $grid = undefined,
        $month = $('#paymentMonth'),
        $dataCount = $('#dataCount'),
        _payoutMethodPaymentEditor = 'a.k-payoutMethodPayments',
        _allowEdit = false,
        _canEdit = false,
        _canDelete = false,
        _action = undefined,
        _saveExpandedColor = '#ffffff',
        _preventBinding = false,
        _availablePayoutMethodType = ['Checking', 'Paypal'],
        _availableProperties = [],
        _propertyCodes = [],
        _inlineEditIndex = 2, // change this index if the inline edit column position has changed

        init = function (id) {
            $grid = $('#' + id);
            _gridId = '#' + id;
            _allowEdit = $('.revenue-field-readonly').length == 0;
            _canDelete = $('.revenue-grid-remover').length > 0;
            _canEdit = _allowEdit;
            installEvents();
            DojoWeb.Notification.init('actionAlert', 3000); // install ajax action response messaging
            getAvailableProperties();
            getData();
        },

        installEvents = function () {
            // month picker
            $month.kendoDatePicker({
                start: 'year', // defines the start view
                depth: 'year', // defines when the calendar should return date            
                format: 'MMMM yyyy', // display month and year in the input
                dateInput: true // specifies that DateInput is used for masking the input element
            });
            //$month.data('kendoDatePicker').enable(false);

            $month.unbind('change').on('change', function (e) {
                var selectedMonth = $month.data('kendoDatePicker').value();
                if (selectedMonth != '') {
                    var month = kendo.toString(selectedMonth, 'yyyy-MM-dd');
                    if (month < '2017-08-01') // allow update beginning balance after 08/2017
                        $('#updateBalances').addClass('hide'); 
                    else
                        $('#updateBalances').removeClass('hide');
                }

                if (requiredSelected()) {
                    $.ajax({
                        type: 'POST',
                        url: '/OwnerPayment/IsEditFreezed',
                        data: { month: kendo.toString(selectedMonth, 'MM/dd/yyyy') },
                        success: function (freeze) {
                            if (freeze == -1) { // does not exist in db
                                _canEdit = false;
                                $('#enableEditButton').addClass('hide');
                                $('#disableEditButton').addClass('hide');
                            }
                            else if (freeze == 1) { // currently not editable
                                _canEdit = false;
                                $('#enableEditButton').removeClass('hide');
                                $('#disableEditButton').addClass('hide');
                            }
                            else {
                                _canEdit = _allowEdit ? true : false;
                                $('#disableEditButton').removeClass('hide');
                                $('#enableEditButton').addClass('hide');
                            }
                            getData();
                        },
                        error: function (jqXHR, status, errorThrown) { // ignore the error
                            getData();
                        }
                    });
                }
            });

            $('#disableEdit').kendoButton({
                spriteCssClass: 'fa fa-lock red',
                click: disableEdit
            });

            $('#enableEdit').kendoButton({
                spriteCssClass: 'fa fa-unlock green',
                click: enableEdit
            });

            $('#updateBalances').kendoButton({
                spriteCssClass: 'fa fa-balance-scale orange',
                click: updateBalances
            });
        },

        initGridEvents = function () {
            // new Payout Method - add dialog class cue to actiate it
            $(_gridId + ' .k-grid-add').addClass('showPayoutMethodEdit');

            $(_gridId + ' .k-grid-add').unbind('click').on('click', function (e) {
                _action = 'new';
            });

            $(_payoutMethodPaymentEditor).unbind('click').on('click', function (e) {
                e.preventDefault();
                DojoWeb.PaymentEditor.showDialog($month.data('kendoDatePicker').value());
            });

            $('.showPayoutMethodEdit').unbind('click').on('click', function (e) {
                _action = 'edit';
            });

            // balance redistribution dialog 
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MMMM');
            var caption = 'Balance Redistribution from ' + month;
            DojoWeb.Plugin.initFormDialog({
                selector: '.showBalanceEdit',
                caption: caption,
                width: 700,
                url: '/OwnerPayment/RebalanceEditor',
                formId: 'RealanceEntryForm',
                initEvent: DojoWeb.RebalanceForm.init,
                modal: false,
                closeEvent: null
            });

            // new/edit/view owner payout dialog 
            //var caption = 'Edit Payout Method';
            //DojoWeb.Plugin.initFormDialog({
            //    selector: '.showPayoutMethodEdit',
            //    caption: caption,
            //    width: 890,
            //    url: '/OwnerPayment/EditPayoutMethod',
            //    formId: 'PayoutMethodEntryForm',
            //    initEvent: DojoWeb.PayoutMethodForm.init,
            //    modal: false,
            //    closeEvent: null
            //});

            // new payment dialog
            //var caption = 'Add Payment';
            //DojoWeb.Plugin.initFormDialog({
            //    selector: '.showPaymentNew',
            //    caption: caption,
            //    width: 890,
            //    url: '/OwnerPayment/AddPayment',
            //    formId: 'PaymentEntryForm',
            //    initEvent: DojoWeb.NewPaymentForm.init,
            //    modal: false,
            //    closeEvent: null
            //});
        },

        initChildEvents = function () {
        },

        getData = function (selectedId) {
            if ($month.data('kendoDatePicker').value() == '') return;

            var selectedMonth = $month.data('kendoDatePicker').value();
            var statementMonth = (new Date()).addMonths(-1);
            _canEdit = _allowEdit == false ? _allowEdit : _canEdit;
            var month = kendo.toString(selectedMonth, 'MM/dd/yyyy');

            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/OwnerPayment/RetrievePayoutMethodPayments',
                data: { month: month },
                success: function (data) {
                    DojoWeb.Busy.hide();
                    removeOverpayStatus();
                    if ($.isArray(data)) {
                        emptyGrid();
                        var gridParent = initParentGrid();
                        var dataGrid = $grid.kendoGrid(gridParent).data('kendoGrid');

                        dataGrid.bind('dataBound', function (e) {
                            initGridEvents();
                            setCount();

                            var id = undefined; //DojoWeb.OwnerPayoutForm.getId();
                            var msgTemplate = '{0} of payout method is successful.';
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

                            var lastMonth = statementMonth.addMonths(-1);
                            if (!_canEdit) {
                                $('#beginBalanceButton').addClass('hide');
                            }
                            else {
                                $('#beginBalanceButton').removeClass('hide');
                            }
                        });

                        var revenueSource = setupDataSource(data);
                        dataGrid.setDataSource(revenueSource);
                        
                        initFreezeEdit();

                        // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                        // we remove the 'filter' text ad-hoc here
                        $(_gridId + ' span.k-filter').text('');

                        ensureAllowDelete();

                        checkOverpayStatus(data);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                }
            });
        },

        getAvailableProperties = function () {
            $.ajax({
                url: '/Property/GetPropertyCodes',
                dataType: 'json',
                success: function (result) {
                    _availableProperties = result;
                    _propertyCodes = _.map(_availableProperties, 'Value');
                }
            });
        },

        reload = function (payoutMethodId) {
            getData(payoutMethodId);
        },

        initParentGrid = function (ds) {
            var height = $(window).height() - 300;
            var monthName = kendo.toString($month.data('kendoDatePicker').value(), 'MMMM');
            return {
                height: height,
                filterable: true,
                sortable: true,
                pageable: false,
                editable: 'inline',
                batch: false,
                dataBinding: function (e) {
                    if (_preventBinding) {
                        e.preventDefault();
                    }
                    _preventBinding = false;
                },
                edit: function (e) {
                    if (!e.model.isNew()) {
                        var readOnlyField = e.container.find('input[name=PayoutMethodName]'); //.data('kendoEditor');
                        if (readOnlyField !== undefined) readOnlyField.attr('readonly', true);

                        // prevent cancel button to bring up delete button if the user can't do delete
                        $(_gridId + ' .k-grid-cancel').unbind('click').on('click', function () {
                            setTimeout(function () {
                                $grid.data('kendoGrid').trigger('dataBound');
                            });
                        })
                    }
                    else if (e.model.isNew()) {
                        e.model.set('PayoutMethodId', 0);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(' + _inlineEditIndex + ')');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                remove: function (e) {
                    if (!e.model.isNew()) {
                        alert('Payout Method cannot not be deleted.');
                        e.preventDefault();
                        $grid.data('kendoGrid').cancelChanges();
                    }
                },
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
                toolbar: _canEdit ?
                        [
                            //{ name: 'create', text: ' New Payout Method', iconClass: 'fa fa-plus' },
                            { name: 'PayoutMethodPaymentEditor', text: ' Owner Payments for ' + monthName, className: 'k-payoutMethodPayments showPaymentEditor', iconClass: 'fa fa-credit-card' },
                        ]
                        : null,
                columns: [
                    // action buttons
                    //{ field: 'edit', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.PayoutMethodPayment.renderAction(data.PayoutMethodId, 'edit')#", attributes: { class: 'grid-cell-align-center' } },
                    //{ field: 'delete', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.PayoutMethodPayment.renderAction(data.PayoutMethodId, 'delete')#", attributes: { class: 'grid-cell-align-center' } },
                    //{ field: 'balance', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.PayoutMethodPayment.renderAction(data.PayoutMethodId, data.TotalBalance, 'balance')#", attributes: { class: 'grid-cell-align-center'} },
                    //{
                    //    command: [
                    //        {
                    //            name: 'edit',
                    //            template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                    //            text: { edit: 'Edit', update: '', cancel: '' },
                    //        },
                    //    ],
                    //    title: 'Edit',
                    //    width: '90px !important', // wide enough to hold 2 font awesome icons
                    //    hidden: !_canEdit
                    //},
                    //{ field: 'payment', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.PayoutMethodPayment.renderAction(data.PayoutMethodId, 'payment')#", attributes: { class: 'grid-cell-align-center' } },
                    // grid data fields
                    { field: 'PayoutMethodName', title: 'Payout Method', width: '270px', filterable: true, required: true },
                    { field: 'EffectiveDate', title: 'Effective Date', width: '150px', filterable: true, format: '{0:MM/dd/yyyy}', required: true, template: '#= DojoWeb.Template.dateUS(EffectiveDate) #' },
                    { field: 'PayoutAccount', title: 'Payout Account', width: '150px', filterable: true, required: true },
                    { field: 'PayoutMethodType', title: 'Payout Type', width: '120px', filterable: true, editor: payoutMethodTypeEditor, required: true },
                    { field: 'BeginBalance', title: 'Begin Balance', width: '100px', required: true, template: "#= DojoWeb.Template.money(data.BeginBalance, true) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'TotalBalance', title: 'Total Balance', width: '100px', template: "#= DojoWeb.Template.money(TotalBalance) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'PayoutTotal', title: 'Total Payments', width: '100px', template: "#= DojoWeb.PayoutMethodPayment.checkOverpayment(DojoWeb.Template.money(data.PayoutTotal), data) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'LinkedPropertyCodes', title: 'Property Codes', editor: propertyCodeEditor, template: propertyCodeDisplay, filterable: false },
                    { field: 'SelectedProperties', title: 'Properties', hidden: true },
                    // identity fields
                    { field: 'PayoutMethodId', title: 'Payout Method ID', hidden: true },
                ],
            }
        },

        initChildGrid = function (e) {
            var findByID = function (id) {
                return e.data.Children.find(function (item) {
                    return item.PayoutPaymentId == id;
                });
            };

            $('<div/>').appendTo(e.detailCell).kendoGrid({
                dataSource: {
                    transport: {
                        read: function (options) {
                            options.success(e.data.Children);
                        },
                        update: function (options) {
                            var data = options.data,
                                parentItem = findByID(data.PayoutPaymentId);
                            for (var field in data) {
                                if (!(field.indexOf("_") === 0)) {
                                    parentItem[field] = data[field];
                                }
                            }
                            e.data.dirty = true;
                            options.success();
                        },
                        destroy: function (options) {
                            var parentItem = findByID(options.data.PayoutPaymentId);
                            _preventBinding = true;
                            e.data.Children.remove(parentItem);
                            options.success();
                        },
                    },
                    schema: {
                        model: {
                            id: 'PayoutPaymentId',
                            fields: {
                                PayoutPaymentId: { type: 'number', editable: false, nullable: false },
                                PaymentDate: { type: 'date', editable: true, nullable: false },
                                PaymentAmount: { type: 'number', editable: true, nullable: false },
                                PaymentMonth: { type: 'number', editable: false, nullable: false },
                                PaymentYear: { type: 'number', editable: false, nullable: false },
                                PayoutMethodId: { type: 'number', editable: false, nullable: false },
                            }
                        }
                    }
                },
                scrollable: false,
                sortable: true,
                pageable: false,
                editable: 'inline',
                batch: false,
                edit: function (e) {
                    //if (!e.model.isNew()) {
                    //    // prevent cancel button to bring up delete button if the user can't do delete
                    //    $(_gridId + ' .k-detail-cell .k-grid-cancel').unbind('click').on('click', function () {
                    //        setTimeout(function () {
                    //            $grid.data('kendoGrid').trigger('dataBound');
                    //        });
                    //    })
                    //}

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(1)');
                    commandCell.html('<a class="k-button k-grid-update" role="button" href="#" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" role="button" href="#" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                remove: function (e) {
                    //if (!e.model.isNew()) {
                    //    alert('Payment item cannot not be deleted.');
                    //    e.preventDefault();
                    //    $grid.data('kendoGrid').cancelChanges();
                    //    expandParentById(e.model.PayoutMethodId);
                    //}
                },
                cancel: function (e) {
                    // need to cancel the change; otherwise the child row will be removed from view
                    $grid.data('kendoGrid').cancelChanges();
                    expandParentById(e.model.PayoutMethodId);

                },
                columns: [
                    // action buttons
                    //{ field: 'edit', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.PayoutMethodPayment.renderPaymentAction(data.PayoutPaymentId, 'edit')#", attributes: { class: 'grid-cell-align-center' } },
                    //{ field: 'delete', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.PayoutMethodPayment.renderPaymentAction(data.PayoutPaymentId, 'delete')#", attributes: { class: 'grid-cell-align-center' } },

                    // action buttons; use template to display font awesome icon with color for edit/delete
                    // need to use template to display font awesome icons with color for save/cancel in 'edit' event above
                    {
                        command: [
                            {
                                name: 'edit',
                                template: '<a class="k-button k-grid-edit" role="button" href="\\#" style="min-width:20px;"><span class="fa fa-pencil blue"></span></a>',
                                text: { edit: 'Edit', update: '', cancel: '' },
                            },
                            {
                                name: 'destroy',
                                template: '<a class="k-button k-grid-delete" role="button" href="\\#" style="min-width:20px;"><span class="fa fa-trash"></span></a>',
                            },
                        ],
                        title: 'Action',
                        width: '90px !important', // wide enough to hold 2 font awesome icons
                        hidden: !_canEdit
                    },
                    //{ command: ["edit", "destroy"], title: "&nbsp;" },
                    // grid data fields
                    { field: 'PaymentMonth', title: 'Payment Month', width: '120px' },
                    { field: 'PaymentYear', title: 'Payment Year', width: '120px' },
                    { field: 'PaymentDate', title: 'Payment Date', width: '150px', required: true, format: '{0:MM/dd/yyyy}' },
                    { field: 'PaymentAmount', title: 'Amount', required: true, template: "#= DojoWeb.Template.money(PaymentAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    // identity fields
                    { field: 'PayoutPaymentId', title: 'Payment ID', hidden: true },
                    { field: 'PayoutMethodId', title: 'Payout Method ID', hidden: true },
                ]
            });
        },

        setupDataSource = function (data) {
            return new kendo.data.DataSource({
                transport: {
                    //read: function (options) {
                    //    options.success(data);
                    //},
                    // in order by built-in CRUD operations to work in kendo grid, read must use URL as data source
                    read: {
                        url: kendo.format('/OwnerPayment/RetrievePayoutMethodPayments?month={0}',
                                          kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy')),
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/PayoutMethod/Create',
                        type: 'Post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/PayoutMethod/Update',
                        type: 'Post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/PayoutMethod/Delete',
                        type: 'Post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].PayoutMethodId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') {
                                options.PayoutMethodId = 0;
                            }
                            else if (operation === 'update') {
                            }
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<model>
                change: function (e) {
                    if (e.field && e.field.indexOf('Children') >= 0) {
                        _preventBinding = true;
                    }
                },
                schema: {
                    model: {
                        id: 'PayoutMethodId',
                        fields: {
                            PayoutMethodId: { type: 'number', editable: false, nullable: false },
                            PayoutMethodName: { type: 'string', editable: true, nullable: true },
                            EffectiveDate: { type: 'date', editable: true, nullable: true },
                            PayoutAccount: { type: 'string', editable: true, nullable: true },
                            PayoutMethodType: { type: 'string', editable: true, nullable: true },
                            BeginBalance: { type: 'number', editable: false, nullable: false },
                            TotalBalance: { type: 'number', editable: false, nullable: false },
                            PayoutTotal: { type: 'number', editable: false, nullable: false },
                            EndingBalance: { type: 'number', editable: false, nullable: false },
                            LinkedPropertyCodes: {},
                            SelectedProperties: { type: 'string', editable: false, nullable: true },
                            balance: { editable: false },
                        }
                    }
                },
                error: function (e) {
                    if (e.xhr.responseJSON == undefined)
                        DojoWeb.ActionAlert.fail('payment-alert', e.xhr.responseText);
                    else
                        DojoWeb.ActionAlert.fail('payment-alert', e.xhr.responseJSON);
                    var dataGrid = $grid.data('kendoGrid');
                    if (dataGrid != undefined) dataGrid.cancelChanges();
                }
            });
        },

        renderAction = function (id, totalBalance, action) {
            if (action == 'balance') {
                var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
                if (_canEdit && totalBalance < 0)
                    return "<div id='balance-id-" + id + "' class='showBalanceEdit gridcell-btn' title='Redistribute Balance' data-id='" + id + "' data-link='" + month + "'><div class='btn dojo-center'><i class='fa fa-balance-scale'></i></div></div>";
                else
                    return "<div> </div>"
            }
            else if (action == 'edit') {
                if (_canEdit)
                    return "<div id='edit-id-" + id + "' class='showPayoutMethodEdit gridcell-btn' title='Edit Payout Method' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-wrench'></i></div></div>";
                else
                    return "<div id='view-id-" + id + "' class='showPayoutMethodEdit gridcell-btn' title='View Payout Method' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-eye'></i></div></div>";
            }
            else if (action == 'delete') {
                if (_canEdit)
                    return "<div id='delete-id-" + id + "' class='gridcell-btn' title='Delete Payout Method' onClick='DojoWeb.PayoutMethodPayment.doDelete(" + '"' + id + '"' + ");'><div class='btn dojo-center'><i class='fa fa-trash-o'></i></div></div>";
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-trash-o'></i></div></div>";
            }
            else if (action == 'payment') {
                if (_canEdit)
                    return kendo.format("<div id='payment-id-{0}' class='showPaymentNew gridcell-btn' title='Add Payment' data-id='0' data-parent-id='{0}'><div class='btn dojo-center'><i class='fa fa-credit-card'></i></div></div>", id);
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-plane'></i></div></div>";
            }
        },

        renderPaymentAction = function (id, action) {
            if (action == 'edit') {
                if (_canEdit)
                    return "<div id='payment-edit-id-" + id + "' class='showPaymentEdit gridcell-btn' title='Edit Payment' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-pencil'></i></div></div>";
                else
                    return "<div id='payment-view-id-" + id + "' class='showPaymentEdit gridcell-btn' title='View Payment' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-eye'></i></div></div>";
            }
            else if (action == 'delete') {
                if (_canEdit)
                    return "<div id='payment-delete-id-" + id + "' class='gridcell-btn' title='Delete Payout Method' onClick='DojoWeb.PayoutMethodPayment.doPaymentDelete(" + '"' + id + '"' + ");'><div class='btn dojo-center'><i class='fa fa-trash-o'></i></div></div>";
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-trash-o'></i></div></div>";
            }
        },

        doDelete = function (id) {
            DojoWeb.Confirmation.confirmDiscard({
                id: 'confirmation-dialog',
                caption: 'Delete Owner Payout Confirmation',
                message: 'The selected Owner Payout will be deleted. Please confirm.',
                ok: function () {
                    $.ajax({
                        type: 'POST',
                        url: '/OwnerPayment/DeletePayoutMethod/?id=' + id,
                        success: function (result) {
                            if (result == 'success') {
                                _action = 'delete';
                                getData(); // refresh the grid to remove the deleted row
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

        requiredSelected = function () {
            return $month.val() != '';
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

        ensureAllowDelete = function () {
            if (!_canDelete) {
                $(_gridId + ' tbody tr .k-grid-delete').each(function () {
                    var currentDataItem = $grid.data('kendoGrid').dataItem($(this).closest('tr'));
                    $(this).remove();
                });
            }
        },

        checkOverpayment = function (payment, data) {
            if (data.TotalBalance > 0 && data.PayoutTotal > 0 && data.PayoutTotal - data.TotalBalance >= 0.01) {
                return '<span style="color:red;font-weight:bold;">' + payment + '</span>';
            }
            else if (data.TotalBalance > 0 && data.PayoutTotal > 0 && data.TotalBalance - data.PayoutTotal >= 0.01) {
                return '<span style="color:blue;font-weight:bold;">' + payment + '</span>';
            }
            else {
                return payment;
            }
        },

        checkOverpayStatus = function (data) {
            var payment = _.countBy(data, function (item) {
                if (item.TotalBalance > 0 && item.PayoutTotal > 0 && item.PayoutTotal - item.TotalBalance >= 0.01)
                    return 'overpaid';
                else if (item.TotalBalance > 0 && item.PayoutTotal > 0 && item.TotalBalance - item.PayoutTotal >= 0.01)
                    return 'underpaid';
                else
                    'exact';
            });

            var notice = '';
            if (payment.overpaid > 0) {
                notice += payment.overpaid > 1 ? 'There are ' + payment.overpaid + ' overpayments (displayed in red).  ' : 'There is 1 overpayment (displayed in red).  ';
            }
            if (payment.underpaid > 0) {
                notice += payment.underpaid > 1 ? 'There are ' + payment.underpaid + ' underpayments (displayed in blue).' : 'There is 1 underpayment (displayed in blue).';
            }

            if (notice != '') DojoWeb.ActionAlert.warn('payment-alert', notice);
        },

        removeOverpayStatus = function () {
            $('#payment-alert').html('');
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var htmlContent = '';
                htmlContent += '(Total Payout Methods = ' + count + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        },

        payoutMethodTypeEditor = function (container, options) {
            $('<input id="categoryEditor" data-bind="value:' + options.field + '"/>')
            .appendTo(container)
            .kendoComboBox({
                autoWidth: true,
                dataSource: _availablePayoutMethodType
            });
        },

        propertyCodeEditor = function (container, options) {
            $('<select multiple="multiple" data-bind="value:LinkedPropertyCodes"/>')
                .appendTo(container)
                .kendoMultiSelect({
                    dataTextField: 'Text',
                    dataValueField: 'Value',
                    dataSource: _availableProperties
                });
        },

        propertyCodeDisplay = function (data) {
            var result = [];
            $.each(data.LinkedPropertyCodes, function (i, item) {
                result.push(item.Text);
            });
            return result.join(', ');
        },

        disableEdit = function () {
            freezeEdit(true);
        },

        enableEdit = function () {
            freezeEdit(false);
        },

        freezeEdit = function (freeze) {
            if ($month.data('kendoDatePicker').value() == '') return;
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/OwnerPayment/FreezeEditing',
                data: { month: month, freeze: freeze },
                success: function (result) {
                    DojoWeb.Busy.hide();
                    if (result == '1') {
                        _canEdit = true;
                        getData();
                        $('#disableEditButton').removeClass('hide');
                        $('#enableEditButton').addClass('hide');
                        //DojoWeb.ActionAlert.success('payment-alert', 'Editing of Revenue/Statement/Payment is enabled.', 10000);
                        DojoWeb.Notification.show('Editing of Revenue/Statement/Payment is enabled.');
                    }
                    else if (result == '0') {
                        _canEdit = false;
                        getData();
                        $('#enableEditButton').removeClass('hide');
                        $('#disableEditButton').addClass('hide');
                        //DojoWeb.ActionAlert.success('payment-alert', 'Edting of Revenue/Statement/Payment is disabled.', 10000);
                        DojoWeb.Notification.show('Edting of Revenue/Statement/Payment is disabled.');
                    }
                    else { // data contains the error message
                        DojoWeb.ActionAlert.fail('payment-alert', result);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    if (status == 'error') {
                        DojoWeb.ActionAlert.fail('payment-alert', errorThrown);
                    }
                }
            })
        },

        initFreezeEdit = function () {
            var selectedMonth = $month.data('kendoDatePicker').value();
            if (selectedMonth != '') {
                var month = kendo.toString(selectedMonth, 'yyyy-MM-dd');
                if (month < '2017-08-01') // allow update beginning balance after 08/2017
                    $('#updateBalances').addClass('hide');
                else
                    $('#updateBalances').removeClass('hide');
            }

            if (requiredSelected()) {
                $.ajax({
                    type: 'POST',
                    url: '/OwnerPayment/IsEditFreezed',
                    data: { month: kendo.toString(selectedMonth, 'MM/dd/yyyy') },
                    success: function (freeze) {
                        if (freeze == -1) { // does not exist in db
                            _canEdit = false;
                            $('#enableEditButton').addClass('hide');
                            $('#disableEditButton').addClass('hide');
                        }
                        else if (freeze == 1) { // currently not editable
                            _canEdit = false;
                            $('#enableEditButton').removeClass('hide');
                            $('#disableEditButton').addClass('hide');
                        }
                        else {
                            _canEdit = _allowEdit ? true : false;
                            $('#disableEditButton').removeClass('hide');
                            $('#enableEditButton').addClass('hide');
                        }
                    },
                    error: function (jqXHR, status, errorThrown) { // ignore the error
                    }
                });
            }
        },

        updateBalances = function () {
            if ($month.data('kendoDatePicker').value() == '') return;
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/OwnerPayment/UpdateBalances',
                data: { month: month },
                success: function (result) {
                    DojoWeb.Busy.hide();
                    if (result == 'success') {
                        DojoWeb.ActionAlert.success('payment-alert', 'Ending balances to be carried over to next month are updated.', 10000);
                    }
                    else { // data contains the error message
                        DojoWeb.ActionAlert.fail('payment-alert', result);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    if (status == 'error') {
                        DojoWeb.ActionAlert.fail('payment-alert', errorThrown);
                    }
                }
            })
        },

        adjustEditorIcon = function (name) {
            $($($('input[name="' + name + '"]').parent().children('span')[0]).children()[0]).html('');
        }

    return {
        init: init,
        reload: reload,
        getData: getData,
        renderAction: renderAction,
        renderPaymentAction: renderPaymentAction,
        checkOverpayment: checkOverpayment,
        doDelete: doDelete,
    }
}();

DojoWeb.PaymentEditor = function () {
    var _$formDialog = undefined,
        _year = '2017',

        showDialog = function (month) {
            var url = kendo.format('/OwnerPayment/PaymentEditor?month={0}', kendo.toString(month, 'MM/dd/yyyy'));
            var caption = kendo.format('Owner Payments for {0}', kendo.toString(month, 'MMMM'));
            _$formDialog = undefined;
            DojoWeb.Busy.show();
            $.ajax({
                url: url,
                success: function (data) {
                    DojoWeb.Busy.hide();
                    _$formDialog = $('#formDialog');
                    if (_$formDialog.length > 0) {
                        $('.dialog-body').html(data);
                        if (!_$formDialog.data('kendoWindow')) {
                            _$formDialog.kendoWindow({
                                width: 1000,
                                title: caption,
                                actions: [],
                                visible: false,
                                resizable: false,
                                scrollable: true,
                                modal: false // modal dialog is not closed when clicking outside of it
                            });
                        }
                        _$formDialog.data('kendoWindow').open().center(); // open() needs to come before center()
                        _$formDialog.data('kendoWindow').title(caption);

                        // enable closing popup when clicking outside of it if modal = true
                        $(document).unbind('click').on('click', '.k-overlay', function (e) {
                            $('#formDialog').data('kendoWindow').close();
                            $('body').css('overflow', 'scroll');
                        });

                        $('body').css('overflow', 'hidden'); // disable background scrolling

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

        closeDialog = function () {
            if (_$formDialog && _$formDialog.length > 0) _$formDialog.data('kendoWindow').close();
            $('body').css('overflow', 'scroll'); // enable background scrolling
        },

        initEvents = function () {
            // save event
            $('#PaymentSave').unbind('click').on('click', function (e) {
                e.preventDefault();
                savePayments();
            });

            // cancel event
            $('#PaymentCancel').unbind('click').on('click', function (e) {
                e.preventDefault();
                closeDialog();
            });

            $('#suggestedPayments').kendoButton({
                spriteCssClass: 'fa fa-credit-card green',
                click: suggestedPayments
            });

            //installNumericEditors();
            DojoWeb.Plugin.initNumericEditor('.payment-row', '.payment-item');
        },

        savePayments = function () {
            var $dialog = $('.dialog-page-content');
            $dialog.css('cursor', 'wait');
            var payments = getAllPayments();
            //alert('payments count = ' + payments.length);
            $.ajax({
                type: 'POST',
                url: '/OwnerPayment/SavePayoutMethodPayments',
                contentType: 'application/json;charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(payments),
                success: function (data) {
                    $dialog.css('cursor', 'default');
                    if (data == 'success') {
                        closeDialog();
                        DojoWeb.PayoutMethodPayment.getData(); // refresh the payout method grid
                    }
                    else { // data contains the error message
                        DojoWeb.Notification.show(data);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    $dialog.css('cursor', 'default');
                    if (status == 'error') {
                    }
                }
            })
        },

        installNumericEditors = function () {
            var paymentRows = $('.payment-row');
            $.each(paymentRows, function (index, item) {
                var amounts = $(this).find('.payment-item');
                if (amounts.length > 0) {
                    if (amounts[0].id !== undefined) {
                        $('#' + amounts[0].id).inputmask('numeric', {
                            radixPoint: ".",
                            groupSeparator: ',',
                            digits: 2,
                            autoGroup: true, // will put , for every 3-digit
                            prefix: '$', // No Space, this will truncate the first character
                            rightAlign: false,
                            oncleared: function () { self != undefined ? self.Value('') : ''; }
                        });

                        // kendo seems to have limit on how many numeric textboxes it can create
                        //$('#payment-item-' + index).kendoNumericTextBox({
                        //    spinners: false,
                        //    format: 'c',
                        //    decimals: 2
                        //});
                    }
                }
            });
        },

        getAllPayments = function () {
            var payments = [];
            var paymentRows = $('.payment-row');
            $.each(paymentRows, function (item, index) {
                var amounts = $(this).find('.payment-item');
                var amount = $(amounts[0]).val().replace(/\$|,|\)/g, '').replace('(', '-'); // remove masked characters
                if (amount != '') {
                    var methodIds = $(this).find('.methodId');
                    var methodNames = $(this).find('.methodName');
                    if (methodIds.length > 0 && methodNames.length > 0) {
                        var months = $(this).find('.paymentMonth');
                        var years = $(this).find('.paymentYear');
                        var paymentIds = $(this).find('.paymentId');
                        payments.push({
                            PayoutPaymentId: $(paymentIds[0]).text(),
                            PayoutMethodId: $(methodIds[0]).text(),
                            PaymentAmount: amount,
                            PaymentMonth: $(months[0]).text(),
                            PaymentYear: $(years[0]).text(),
                            PaymentDate: null,

                        });
                    }
                }
            });
            return payments;
        },

        suggestedPayments = function () {
            var payments = [];
            var paymentRows = $('.payment-row');
            $.each(paymentRows, function (item, index) {
                var beginBalances = $(this).find('.payment-begin-balance');
                var totalBalances = $(this).find('.payment-total-balance');
                var amounts = $(this).find('.payment-item');
                var amount = $(amounts[0]).val().replace(/\$|,|\)/g, '').replace('(', '-'); // remove masked characters
                //var beginBalance = $(beginBalances[0]).text().replace(/\$|,|\)|\n/g, '').replace('(', '-'); // remove masked characters
                var totalBalance = $(totalBalances[0]).text().replace(/\$|,|\)|\n/g, '').replace('(', '-'); // remove masked characters
                if (totalBalance != '' && amount == '') {
                    //var suggestedAmount = Number(totalBalance.trim()) - Number(beginBalance.trim());
                    var suggestedAmount = Number(totalBalance.trim());
                    if (suggestedAmount != undefined && suggestedAmount > 0)
                    $(amounts[0]).val(suggestedAmount.toString());
                }
            });
        },

        refresh = function () {
            // for partial view; not used
        },

        serverError = function () {
            // for partial view; not used
        }

    return {
        savePayments: savePayments,
        showDialog: showDialog,
        refresh: refresh,
        serverError: serverError
    }
}();

DojoWeb.RebalanceForm = function () {
    var $form = undefined,
        _balances = [],
        _balanceTotal = 0,

        init = function (formId) {
            $form = $('#' + formId);
            installControls();

            // retrieve the balances into structure
            _balanceTotal = getTotalBalance(),
            _balances = DeserializeBalances();

        },

        installControls = function () {
            $('#RebalanceSave').unbind('click').on('click', function (e) {
                e.preventDefault();
                redistributeBalance();
            });

            $('#RebalanceCancel').unbind('click').on('click', function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
            });

            $('#defaultRebalance').kendoButton({
                spriteCssClass: 'fa fa-balance-scale green',
                click: defaultRebalancesForSameMonth //defaultRebalances
            });

            DojoWeb.Plugin.initNumericEditor('.rebalance-row', '.rebalance-item');
        },

        redistributeBalance = function () {
            var $dialog = $('.dialog-page-content');
            $dialog.css('cursor', 'wait');
            var balances = getAllBalances();
            $.ajax({
                type: 'POST',
                url: '/OwnerPayment/RedistributeBalances',
                contentType: 'application/json;charset=utf-8',
                dataType: 'json',
                data: JSON.stringify(balances),
                success: function (data) {
                    $dialog.css('cursor', 'default');
                    if (data == 'success') {
                        closeDialog();
                        DojoWeb.PayoutMethodPayment.getData(); // refresh the payout method grid
                    }
                    else { // data contains the error message
                        DojoWeb.Notification.show(data);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    $dialog.css('cursor', 'default');
                    if (status == 'error') {
                    }
                }
            })
        },

        defaultRebalances = function () {
            if (_balances.length > 0)
            {
                var negativeCount = _balances.length;
                // rule #1: if beginBalance is positive, set it to 0
                $.each(_balances, function (i, balance) {
                    if (balance.BeginningBalance >= 0) {
                        balance.AdjustedBalance = 0;
                        negativeCount--;
                    }
                });

                // rule #2: evenly distribute balance among properties
                if (negativeCount > 0 && _balances.length > negativeCount) {
                    var avergeBalance = _balanceTotal / negativeCount;
                    $.each(_balances, function (i, balance) {
                        if (balance.BeginningBalance < 0) {
                            balance.AdjustedBalance = avergeBalance;
                        }
                    });
                }
                // rule #3: if all negative, carery over begin balance to adjusted balance
                else if (_balances.length == negativeCount) {
                    $.each(_balances, function (i, balance) {
                        balance.AdjustedBalance = balance.BeginningBalance;
                    });
                }

                // show the rebalanced amounts
                var rebalanceRows = $('.rebalance-row');
                $.each(rebalanceRows, function (i, item) {
                    var adjustedBalances = $(this).find('.rebalance-item');
                    if (_balances[i].AdjustedBalance != 0) {
                        $(adjustedBalances[0]).val(_balances[i].AdjustedBalance.toString());
                    }
                    else {
                        $(adjustedBalances[0]).val('0');
                    }
                });
            }
        },

        defaultRebalancesForSameMonth = function () {
            if (_balances.length > 0) {
                var positiveCount = 0;
                var positiveTotal = 0;
                // rule #1: if beginBalance is positive, make adjustedBalance the same negative amount
                $.each(_balances, function (i, balance) {
                    if (balance.BeginningBalance >= 0) {
                        balance.AdjustedBalance = -balance.BeginningBalance;
                        positiveTotal += balance.BeginningBalance;
                        positiveCount++;
                    }
                });

                // rule #2: evenly distribute positive balance total among negative properties;
                //          but if positive after redistribution; set it 0 and rebalance it
                if (positiveCount > 0 && _balances.length > positiveCount) {
                    var redistributeAmount = 0;
                    var redistributeCount = 0;
                    var avergeBalance = positiveTotal * 2 / (_balances.length - positiveCount);
                    $.each(_balances, function (i, balance) {
                        if (balance.BeginningBalance < 0) {
                            balance.AdjustedBalance = balance.BeginningBalance + avergeBalance;
                            if (balance.AdjustedBalance < 0) {
                                redistributeCount++;
                            }
                            else {
                                redistributeAmount += balance.AdjustedBalance;
                                balance.AdjustedBalance = 0;
                            }
                        }
                    });
                    // redistribute distributed amount that is positive
                    if (redistributeAmount > 0 && redistributeCount > 0) {
                        var averageAmount = redistributeAmount / redistributeCount;
                        $.each(_balances, function (i, balance) {
                            if (balance.BeginningBalance + balance.AdjustedBalance != 0 && balance.AdjustedBalance != 0) {
                                balance.AdjustedBalance += averageAmount;
                            }
                        });
                    }
                }
                // rule #3: if all negative, carery over begin balance to adjusted balance
                else if (positiveCount == 0) {
                    $.each(_balances, function (i, balance) {
                        balance.AdjustedBalance = balance.BeginningBalance;
                    });
                }

                // show the rebalanced amounts
                var rebalanceRows = $('.rebalance-row');
                $.each(rebalanceRows, function (i, item) {
                    var adjustedBalances = $(this).find('.rebalance-item');
                    if (_balances[i].AdjustedBalance != 0) {
                        $(adjustedBalances[0]).val(_balances[i].AdjustedBalance.toString());
                    }
                    else {
                        $(adjustedBalances[0]).val('0');
                    }
                });
            }
        },

        getTotalBalance = function() {
            var amountText = $('div.balanceTotal span').text();
            if (amountText !== undefined) {
                var totalBalance = amountText.replace(/\$|,|\)/g, '').replace('(', '-'); // remove masked characters
                return Number(totalBalance.trim());
            }
            else
                return 0;
        },

        DeserializeBalances = function () {
            var rebalances = [];
            var rebalanceRows = $('.rebalance-row');
            $.each(rebalanceRows, function (item, index) {
                var adjustedBalances = $(this).find('.rebalance-item');
                var adjustedBalance = $(adjustedBalances[0]).val().replace(/\$|,|\)/g, '').replace('(', '-'); // remove masked characters
                if (adjustedBalance != '') {
                    var propertyCodes = $(this).find('.propertyCode');
                    var months = $(this).find('.rebalanceMonth');
                    var years = $(this).find('.rebalanceYear');
                    var beginBalances = $(this).find('.beginBalance');
                    rebalances.push({
                        PropertyCode: $(propertyCodes[0]).text(),
                        Month: Number($(months[0]).text()),
                        Year: Number($(years[0]).text()),
                        BeginningBalance: Number($(beginBalances[0]).text().replace(/\$|,|\)/g, '').replace('(', '-')),
                        AdjustedBalance: Number(adjustedBalance),
                    });
                }
            });
            return rebalances;
        },

        getAllBalances = function () {
            var rebalances =[];
            var rebalanceRows = $('.rebalance-row');
            $.each(rebalanceRows, function (item, index) {
                var adjustedBalances = $(this).find('.rebalance-item');
                var adjustedBalance = $(adjustedBalances[0]).val().replace(/\$|,|\)/g, '').replace('(', '-'); // remove masked characters
                if (adjustedBalance != '') {
                    var propertyCodes = $(this).find('.propertyCode');
                    var months = $(this).find('.rebalanceMonth');
                    var years = $(this).find('.rebalanceYear');
                    var beginBalances = $(this).find('.beginBalance');
                    rebalances.push({
                        PropertyCode: $(propertyCodes[0]).text(),
                        Month: $(months[0]).text(),
                        Year: $(years[0]).text(),
                        BeginningBalance: $(beginBalances[0]).text().replace(/\$|,|\)/g, '').replace('(', '-'),
                        AdjustedBalance: adjustedBalance,
                    });
                }
            });
            return rebalances;
        }

    return {
        init: init,
        redistributeBalance: redistributeBalance,
    }
}();
