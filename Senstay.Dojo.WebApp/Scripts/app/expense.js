"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.Expense = function () {
    var _gridId = undefined,
        $grid = undefined,
        $property = $('#revenuePropertyCode'),
        $month = $('#revenueMonth'),
        $dataCount = $('#dataCount'),
        _checkboxCallbackClass = 'revenueCheckboxUpdate', // class to trigger callback to save data
        _newExpenseSelector = 'a.k-newExpense',
        _combineExpensesSelector = 'a.k-combineExpenses',
        _availableReservations = [],
        _availableCategories = [],
        _reviewAll = 'Set Review All',
        _approveAll = 'Set Approve All',
        _removeReviewAll = 'Remove All Reviewed',
        _removeApproveAll = 'Remove All Approved',
        _reviewAllSelector = 'a.k-reviewedAll',
        _removeReviewAllSelector = 'a.k-removeReviewedAll',
        _approveAllSelector = 'a.k-approvedAll',
        _removeApproveAllSelector = 'a.k-removeApprovedAll',
        _allowEdit = false,
        _canEdit = false,
        _canReview = false,
        _canApprove = false,
        _canDelete = false,
        _action = undefined,
        _saveExpandedColor = '#ffffff',

        init = function (id) {
            $grid = $('#' + id);
            _gridId = '#' + id;
            _allowEdit = $('.revenue-field-readonly').length == 0;
            _canReview = $('.revenue-grid-reviewer').length > 0;
            _canApprove = $('.revenue-grid-approver').length > 0;
            _canDelete = $('.revenue-grid-remover').length > 0;
            _canEdit = _allowEdit;
            installEvents();
            DojoWeb.RevenueWorkflow.install(moveTo); // initialize 3-state approval workflow with access control           
            DojoWeb.Notification.init('actionAlert', 3000); // install ajax action response messaging
        },

        installEvents = function () {
            // month picker
            $month.kendoDatePicker({
                start: 'year', // defines the start view
                depth: 'year', // defines when the calendar should return date            
                format: 'MMMM yyyy', // display month and year in the input
                dateInput: true // specifies that DateInput is used for masking the input element
            });

            // key input monitor to start query for reservations
            $property.unbind('change').on('change', function (e) {
                if (requiredSelected()) getData();
            });

            $month.unbind('change').on('change', function (e) {
                rebindComboBox();
                if (requiredSelected()) getData();
            });

            // searchable dropdown with color coded properties
            var height = $(window).height() - 300;
            $property.kendoComboBox({
                height: height,
                width: 500,
                placeholder: 'Select a property...',
                filter: 'startswith',
                dataTextField: 'PropertyCodeAndAddress',
                dataValueField: 'PropertyCode',
                dataSource: {
                    type: 'json',
                    //serverFiltering: true,
                    transport: {
                        read: {
                            url: '/Expense/GetPropertyCodeWithAddress?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy'),
                        }
                    }
                },
                template: DojoWeb.Template.propertyColorLegend(),
                dataBound: onPropertyDataBound
            });
            // prevent combobox to scroll the page while it is scrolling
            var widget = $property.data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        initGridEvents = function () {
            $('.' + _checkboxCallbackClass).unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                var field = $(this).data('field');
                var included = $(this).prop('checked') == true ? 1 : 0;
                saveFieldStatus(id, field, included);
            });

            $(_combineExpensesSelector).unbind('click').on('click', function (e) {
                e.preventDefault();
                //DojoWeb.CombineExpenses.showDialog($grid.data('kendoGrid').dataSource.view());
                DojoWeb.CombineExpenses.showDialog($month, $property);
            });

            $(_reviewAllSelector).unbind('click').on('click', function (e) {
                e.preventDefault();
                var state = DojoWeb.RevenueWorkflow.states().reviewed;
                moveToAll(state, 1);
            });

            $(_approveAllSelector).unbind('click').on('click', function (e) {
                e.preventDefault();
                var state = DojoWeb.RevenueWorkflow.states().approved;
                moveToAll(state, 1);
            });

            $(_removeReviewAllSelector).unbind('click').on('click', function (e) {
                e.preventDefault();
                var state = DojoWeb.RevenueWorkflow.states().reviewed;
                moveToAll(state, -1);
            });

            $(_removeApproveAllSelector).unbind('click').on('click', function (e) {
                e.preventDefault();
                var state = DojoWeb.RevenueWorkflow.states().approved;
                moveToAll(state, -1);
            });

            enableWorkflowButtons();

            // prevent combobox to scroll the page while it is scrolling
            var widget = $property.data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
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
                        getExpenses();
                    }
                    else {
                        _canEdit = _allowEdit ? true : false;
                        getExpenses();
                    }
                },
                error: function (jqXHR, status, errorThrown) { // ignore the error
                    getExpenses();
                }
            });
        },

        onPropertyDataBound = function (e) {
            var properties = $property.data('kendoComboBox');
            var legendCount = _.countBy(properties.dataSource.data(), function (p) {
                if (p.Empty == 1)
                    return 'noData';
                else if (p.AllApproved == 1)
                    return 'approved';
                else if (p.AllReviewed == 1)
                    return 'reviewed';
                else
                    return 'hasData';
            });
            // add count to legend
            $('#legend-no-data').html('No Data (' + (legendCount.noData == undefined ? 0 : legendCount.noData) + ')');
            $('#legend-approved').html('Approved (' + (legendCount.approved == undefined ? 0 : legendCount.approved) + ')');
            $('#legend-reviewed').html('Reviewed (' + (legendCount.reviewed == undefined ? 0 : legendCount.reviewed) + ')');
            $('#legend-has-data').html('To-Do (' + (legendCount.hasData == undefined ? 0 : legendCount.hasData) + ')');
        },

        getPropertyCode = function () {
            return $property ? $property.val() : '';
        },

        getExpenses = function () {
            DojoWeb.Busy.show();

            var selectedMonth = $month.data('kendoDatePicker').value();
            var lastMonth = (new Date()).addMonths(-1);
            //_canEdit = _allowEdit == false ? _allowEdit : kendo.toString(selectedMonth, 'yyyy-MM-dd') >= kendo.toString(lastMonth, 'yyyy-MM-01');

            if ($grid.data('kendoGrid') != undefined) { // reload data source to avoid stack up kendo grid events
                var month = kendo.toString(selectedMonth, 'MM/dd/yyyy');
                var propertyCode = $property.val();
                var options = {
                    type: 'GET',
                    url: kendo.format('/Expense/Retrieve?month={0}&propertyCode={1}', month, propertyCode)
                };
                $.ajax(options).done(function (result) {
                    DojoWeb.RevenueWorkflow.disable(!_canEdit);
                    $grid.data('kendoGrid').dataSource.data(result);
                    setCategoryList();
                    setReservationList();
                    setCount();
                    rebindComboBox();
                    ensureAllowDelete();
                    DojoWeb.Busy.hide();

                    var dataGrid = $grid.data('kendoGrid')
                    if (!_canEdit) {
                        dataGrid.hideColumn(0);
                    }
                    else {
                        dataGrid.showColumn(0);
                    }
                });
            }
            else { // set up grid and load first data soruce
                var dataSource = setupDataSource();
                var gridOptions = initParentGrid(dataSource);
                var dataGrid = $grid.kendoGrid(gridOptions).data('kendoGrid');
                dataGrid.bind('dataBound', function (e) {
                    initGridEvents();
                    setCategoryList();
                    setReservationList();
                    setCount();
                    ensureAllowDelete();
                    DojoWeb.Busy.hide();

                    if (!_canEdit) {
                        dataGrid.hideColumn(0);
                    }
                });
            }

        },

        setCategoryList = function () {
            if (_availableCategories.length > 0) return;

            var options = {
                type: 'GET',
                url: '/Expense/GetCategoryList'
            };

            $.ajax(options).done(function (result) {
                _availableCategories = result;
                _availableCategories.unshift('');
            });
        },

        setReservationList = function () {
            if (_availableReservations.length > 0) return;

            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            var propertyCode = $property.val();
            var options = {
                type: 'GET',
                url: kendo.format('/Expense/GetConfirmationCodeList?month={0}&propertyCode={1}', month, propertyCode)
            };

            $.ajax(options).done(function (result) {
                _availableReservations = result;
                _availableReservations.unshift('');
            });
        },

        initParentGrid = function (ds) {
            var height = $(window).height() - 300;
            return {
                dataSource: ds,
                height: height,
                filterable: true,
                sortable: true,
                pageable: false,
                editable: 'inline',
                batch: false,
                beforeEdit: function (e) {
                    // this does not get fired
                    if (!e.model.isNew() && e.model.Children.length > 0) {
                        alert('Combined expense item is not editable.');
                        e.preventDefault();
                    }
                },
                edit: function (e) {
                    if (!e.model.isNew() && e.model.Children.length > 0) {
                        var numeric = e.container.find('input[name=ExpenseAmount]').data('kendoNumericTextBox');
                        if (numeric !== undefined) numeric.enable(false); // disable expense amount editing

                        // prevent cancel button to bring up delete button if the user can't do delete
                        $(_gridId + ' .k-grid-cancel').unbind('click').on('click', function () {
                            setTimeout(function () {
                                $grid.data('kendoGrid').trigger('dataBound');
                            });
                        })
                    }
                    else if (e.model.isNew()) {
                        e.model.set('IncludeOnStatement', true);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(2)');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                save: function (e) {
                    //if (!e.model.isNew() && e.model.Children.length > 0) {
                    //    alert('Combined expense item is not editable.');
                    //    e.preventDefault();
                    //    $grid.data('kendoGrid').cancelChanges();
                    //}
                },
                remove: function(e) {
                    if (!e.model.isNew() && e.model.Children.length > 0) {
                        alert('Combined expense item cannot not be deleted.');
                        e.preventDefault();
                        $grid.data('kendoGrid').cancelChanges();
                    }
                },
                cancel: function(e) {
                    //alert('cancellig edit.');
                },
                detailInit: childInit,
                detailExpand: function (e) { // collapse previous row and expand current row
                    _saveExpandedColor = e.masterRow.css('background-color');
                    this.collapseRow(this.tbody.find(' > tr.k-master-row').not(e.masterRow));
                    e.masterRow.css('background-color', '#e0f1fc'); // '#fceae0' brownish, '#e5f9e3' greendish;
                },
                detailCollapse: function (e) {
                    e.masterRow.css('background-color', _saveExpandedColor);
                },
                toolbar: _canEdit ? [{ name: 'create', text: ' New Expense ', className: 'k-newExpense' },
                                     { name: 'CombineExpenses', text: ' Combine Expenses ', className: 'k-combineExpenses' },
                                     { name: 'ReviewedAll', text: _reviewAll, imageClass: '', className: 'k-reviewedAll', iconClass: '' },
                                     { name: '_removeReviewAll', text: _removeReviewAll, imageClass: '', className: 'k-removeReviewedAll', iconClass: '' },
                                     { name: 'ApprovedAll', text: _approveAll, imageClass: '', className: 'k-approvedAll', iconClass: '' },
                                     { name: '_removeApproveAll', text: _removeApproveAll, imageClass: '', className: 'k-removeApprovedAll', iconClass: '' }]
                         : null,
                columns: [
                    // action buttons; use template to display font awesome icon with color for edit/delete
                    // need to use template to display font awesome icons with color for save/cancel in 'edit' event above
                    {
                        command: [
                            {
                                name: 'edit',
                                template: '<a class="k-button k-grid-edit" href="" style="min-width:20px;"><span class="fa fa-pencil blue"></span></a>',
                                text: { edit: 'Edit', update: '', cancel: '' },
                            },
                            {
                                name: 'destroy',
                                template: '<a class="k-button k-grid-delete" href="" style="min-width:20px;"><span class="fa fa-trash"></span></a>',
                            },
                        ],
                        title: 'Action',
                        width: '90px !important', // wide enough to hold 2 font awesome icons
                        hidden: !_canEdit
                    },
                    // grid data fields
                    { field: 'ExpenseDate', title: 'Expense Date', width: '150px', filterable: { multi: true }, format: '{0:MM/dd/yyyy}', required: true, template: '#= DojoWeb.Template.dateUS(ExpenseDate) #' },
                    { field: 'ConfirmationCode', title: 'Reservation', width: '200px', editor: reservationEditor },
                    { field: 'Category', title: 'Category', filterable: { multi: true }, width: '350px', editor: categoryEditor, required: true },
                    { field: 'ExpenseAmount', title: 'Amount', width: '120px', required: true, template: "#= DojoWeb.Template.money(ExpenseAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'IncludeOnStatement', title: 'Included?', width: '110px', filterable: { multi: true }, template: "#= DojoWeb.Expense.renderCheckBox(data.ExpenseId, data.IncludeOnStatement)#", attributes: { class: 'grid-cell-align-center' } },
                    // workflow fields
                    { field: 'Reviewed', title: 'Review', width: '100px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.ExpenseId, 1, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'Approved', title: 'Approve', width: '100px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.ExpenseId, 2, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'ApprovedNote', title: 'Approval Note' },
                    { field: 'ApprovalStatus', title: 'Approval Status', hidden: true }, // occupy the rest of grid width
                    // identity fields
                    { field: 'ExpenseId', title: 'Expense ID', hidden: true },
                    { field: 'ParentId', title: 'Parent ID', hidden: true },
                    { field: 'ReservationId', title: 'Reservation ID', hidden: true },
                    { field: 'PropertyCode', title: 'Property Code', hidden: true },
                ],
            }
        },

        childInit = function (e) {
            $('<div/>').appendTo(e.detailCell).kendoGrid({
                dataSource: {
                    data: e.data.Children,
                    schema: {
                        model: {
                            id: 'ChildId',
                            fields: {
                                ChildId: { type: 'number', editable: false, nullable: true },
                                ExpenseDate: { type: 'date', editable: true, nullable: false },
                                ConfirmationCode: { type: 'string', editable: true, nullable: false },
                                Category: { type: 'string', editable: true, nullable: false },
                                ExpenseAmount: { type: 'number', editable: true, nullable: false },
                                ExpenseId: { type: 'number', editable: false, nullable: false },
                                ParentId: { type: 'number', editable: false, nullable: false },
                                ReservationId: { type: 'number', editable: false, nullable: false },
                                PropertyCode: { type: 'string', editable: false, nullable: false },
                                Memo: { type: 'string', editable: false, nullable: false },
                            }
                        }
                    }
                },
                scrollable: false,
                sortable: true,
                pageable: false,
                columns: [
                    // grid data fields
                    { field: 'ExpenseDate', title: 'Expense Date', width: '100px', required: true, format: '{0:MM/dd/yyyy}' },
                    { field: 'ConfirmationCode', title: 'Reservation', width: '100px', editor: reservationEditor },
                    { field: 'Category', title: 'Category', width: '400px', editor: categoryEditor, required: true },
                    { field: 'ExpenseAmount', title: 'Amount', width: '100px', required: true, template: "#= DojoWeb.Template.money(ExpenseAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'Memo', title: 'Memo', width: '500px' },
                    // identity fields
                    { field: 'ChildId', title: 'Reservation/Resolution ID', hidden: true },
                    { field: 'ExpenseId', title: 'Expense ID', hidden: true },
                    { field: 'ParentId', title: 'Parent ID', hidden: true },
                    { field: 'ReservationId', title: 'Reservation ID', hidden: true },
                    { field: 'PropertyCode', title: 'Property Code', hidden: true },
                ]
            });
        },

        setupDataSource = function () {
            return new kendo.data.DataSource({
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<model>
                transport: {
                    read: {
                        url: kendo.format('/Expense/Retrieve?month={0}&propertyCode={1}',
                                          kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy'),
                                          $property.val()),
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/Expense/Create',
                        type: 'Post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/Expense/Update',
                        type: 'Post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/Expense/Delete',
                        type: 'Post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].ExpenseId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') {
                                options.ExpenseId = 0;
                                options.ParentId = 0;
                                options.ReservationId = 0;
                                options.PropertyCode = getPropertyCode();
                                options.ApprovalStatus = 0;
                                options.Reviewed = false;
                                options.Approved = false;
                            }
                            else if (operation === 'update') {
                                options.PropertyCode = getPropertyCode();
                            }
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                sync: function (e) {
                    // post-processing after record is saved
                    //alert("sync complete");
                },
                schema: {
                    model: {
                        id: 'ExpenseId',
                        fields: {
                            ExpenseId: { type: 'number', editable: false, nullable: false },
                            ParentId: { type: 'number', editable: false, nullable: false },
                            ExpenseDate: { type: 'date', editable: true, nullable: false },
                            ConfirmationCode: { type: 'string', editable: true, nullable: false },
                            Category: { type: 'string', editable: true, nullable: false },
                            ExpenseAmount: { type: 'number', editable: true, nullable: false },
                            IncludeOnStatement: { type: 'boolean', editable: true, nullable: false },
                            ApprovedNote: { type: 'string', editable: true, nullable: false },
                            ReservationId: { type: 'number', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: false, nullable: false },
                            ApprovalStatus: { type: 'number', editable: false, nullable: false },
                            Reviewed: { type: 'boolean', editable: false, nullable: true },
                            Approved: { type: 'boolean', editable: false, nullable: true },
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

        renderCheckBox = function (id, data) {
            var checked = data != 0 ? 'checked="checked"' : '';
            var readonly = _canEdit ? '' : readonly = 'disabled="true"';
            return kendo.format('<input type="checkbox" class="{0}" data-field="IncludeOnStatement" data-id="{1}" {2} {3} />',
                                _checkboxCallbackClass, id, checked, readonly);
        },

        rebindComboBox = function () {
            $property.data('kendoComboBox').dataSource.transport.options.read.url =
                '/Expense/GetPropertyCodeWithAddress?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $property.data('kendoComboBox').dataSource.read();
        },

        renderComamnd = function (data) {
            var html = '';
            if (data.Children && data.Children.length == 0) {
                html = '<a class="k-button k-button-icontext k-grid-edit" href="#"><span class=k-icon k-edit"></span>Edit</a>' +
                       '<a class="k-button k-button-icontext k-grid-delete" href="#"><span class=k-icon k-delete"></span>Delete</a>';
            }
            return html;
        },

        requiredSelected = function () {
            return $property.val() != '' && $month.val() != '';
        },

        emptyGrid = function () {
            //$grid.data('kendoGrid').destroy(); not supported in kendo 2016 version
            //$grid.remove();
            $grid.empty(); // empty grid content
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var expenseSum = 0;
                $.each($view, function (i, r) {
                    expenseSum += r.ExpenseAmount;
                });
                var htmlContent = '';
                htmlContent += '(Count = ' + count;
                htmlContent += ',  Monthly Total = ' + (expenseSum == 0 ? '$0' : DojoWeb.Template.money(expenseSum)) + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        },

        moveTo = function (id, state, direction) {
            var url = kendo.format('/Expense/UpdateWorkflow?id={0}&state={1}&direction={2}', id, state, direction);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result >= 0) {
                        //DojoWeb.Notification.show('Workflow is updated successfully.');
                        if (result > state) result = result - 1;
                        updateGridDataSource(id, result);
                        enableWorkflowButtons();
                    }
                    else {
                        DojoWeb.Notification.show('Dojo encounters error while saving the workflow. Please refresh the page.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    if (jqXHR.status == 403) { // access denied
                        var message = "You don't have permission to perform this action.";
                        DojoWeb.Notification.show(message);
                    }
                    else if (jqXHR.status == 500) { // internal server error
                        var message = 'The workflow cannot be updated in database. If this problem persists, please contact Dojo support team.';
                        DojoWeb.Notification.show(message);
                    }
                    else if (status == 'error') {
                        var message = 'There was an error saving your workflow to the database.';
                        DojoWeb.Notification.show(message);
                    }
                }
            });
        },

        moveToAll = function (state, direction) {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            var propertyCode = $property.val();
            var url = kendo.format('/Expense/UpdateWorkflowAll?month={0}&propertyCode={1}&state={2}&direction={3}',
                                   month, propertyCode, state, direction);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result >= 0) {
                        getExpenses();
                    }
                    else {
                        DojoWeb.Notification.show('Dojo encounters error while saving the workflow. Please refresh the page.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    if (jqXHR.status == 403) { // access denied
                        var message = "You don't have permission to perform this action.";
                        DojoWeb.Notification.show(message);
                    }
                    else if (jqXHR.status == 500) { // internal server error
                        var message = 'The workflow cannot be updated in database. If this problem persists, please contact Dojo support team.';
                        DojoWeb.Notification.show(message);
                    }
                    else if (status == 'error') {
                        var message = 'There was an error saving your workflow to the database.';
                        DojoWeb.Notification.show(message);
                    }
                }
            });
        },

        isWorkflowSelectAll = function (selector, selectAll) {
            var html = $(selector).html();
            return html.indexOf(selectAll) >= 0;
        },

        enableWorkflowButtons = function () {
            if (_canEdit) {
                $(_newExpenseSelector).removeClass('hide');
                $(_combineExpensesSelector).removeClass('hide');
                if ($grid.data('kendoGrid')) {
                    var ds = $grid.data('kendoGrid').dataSource.view();

                    var allReviewed = true;
                    var allApproved = true;
                    var someApproved = false;
                    $.each(ds, function (i, item) {
                        if (item.ApprovalStatus < 1) allReviewed = false;
                        if (item.ApprovalStatus < 2)
                            allApproved = false;
                        else
                            someApproved = true;
                    });

                    // hide all buttons first
                    $(_reviewAllSelector).addClass('hide');
                    $(_approveAllSelector).addClass('hide');
                    $(_removeReviewAllSelector).addClass('hide');
                    $(_removeApproveAllSelector).addClass('hide');

                    if (allReviewed && allApproved) { // show only Remove All Approved
                        if (_canApprove && ds.length > 0) $(_removeApproveAllSelector).removeClass('hide');
                    }
                    else if (allReviewed && !allApproved) { // show only Set Approve All
                        if (_canReview) $(_approveAllSelector).removeClass('hide');
                        if (_canReview && !someApproved) $(_removeReviewAllSelector).removeClass('hide');
                    }
                    else if (!allReviewed) { // none/partially reviewed
                        if (_canReview) $(_reviewAllSelector).removeClass('hide');
                    }
                    else if (!allApproved) { // none/partially approved
                        if (_canApprove) $(_approveAllSelector).removeClass('hide');
                        if (_canReview) $(_removeReviewAllSelector).removeClass('hide');
                    }
                }
            }
            else {
                $(_newExpenseSelector).addClass('hide');
                $(_combineExpensesSelector).addClass('hide');
                $(_reviewAllSelector).addClass('hide');
                $(_approveAllSelector).addClass('hide');
                $(_removeReviewAllSelector).addClass('hide');
                $(_removeApproveAllSelector).addClass('hide');
            }
        },

        saveFieldStatus = function (id, field, included) {
            var url = kendo.format('/Expense/UpdateFieldStatus?id={0}&field={1}&included={2}', id, field, included);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result != '') {
                        // update grid datasource
                        //var ds = $grid.data('kendoGrid').dataSource;
                        //var dataItem = ds.get(id);
                        //dataItem.set(field, included);
                        //dataItem.dirty = false;
                        //ds.read(); // sync the datasource and display
                    }
                    else {
                        DojoWeb.Notification.show('Dojo encounters error while saving ' + field + '. Please refresh the page.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    if (jqXHR.status == 403) { // access denied
                        var message = "You don't have permission to perform this action.";
                        DojoWeb.Notification.show(message);
                    }
                    else if (jqXHR.status == 500) { // internal server error
                        var message = 'The ' + field + ' cannot be updated in database. If this problem persists, please contact Dojo support team.';
                        DojoWeb.Notification.show(message);
                    }
                    else if (status == 'error') {
                        var message = 'There was an error saving the ' + field + ' to the database.';
                        DojoWeb.Notification.show(message);
                    }
                }
            });
        },

        updateGridDataSource = function (id, state) {
            var ds = $grid.data('kendoGrid').dataSource.view();
            if (ds) {
                $.each(ds, function (i, item) {
                    if (item.ExpenseId == id) {
                        item.ApprovalStatus = state;
                    }
                });
            }
        },

        reservationEditor = function (container, options) {
            $('<input id="reservationEditor" data-bind="value:' + options.field + '"/>')
            .appendTo(container)
            .kendoAutoComplete({
                autoWidth: true,
                dataSource: _availableReservations
             });
        },

        categoryEditor = function (container, options) {
            $('<input id="categoryEditor" data-bind="value:' + options.field + '"/>')
            .appendTo(container)
            .kendoComboBox({
                autoWidth: true,
                dataSource: _availableCategories
            });
        },

        ensureAllowDelete = function () {
            if (!_canDelete) {
                $(_gridId + ' tbody tr .k-grid-delete').each(function () {
                    var currentDataItem = $grid.data('kendoGrid').dataItem($(this).closest('tr'));
                    $(this).remove();
                });
            }
        },

        adjustEditorIcon = function (name) {
            $($($('input[name="' + name + '"]').parent().children('span')[0]).children()[0]).html('');
        }

    return {
        init: init,
        getData: getData,
        renderComamnd: renderComamnd,
        moveTo: moveTo,
        getPropertyCode: getPropertyCode,
        renderCheckBox: renderCheckBox
    }
}();

DojoWeb.CombineExpenses = function () {
    var _expenseTree = 'expenseTree',
        $expenseTree = undefined,
        $dataTree = undefined,
        _hasChanged = false,

        showDialog = function ($month, $property) {
            DojoWeb.Busy.show();
            $.ajax({
                url: '/Expense/Combine',
                success: function (data) {
                    DojoWeb.Busy.hide();
                    var $formDialog = $('#formDialog');
                    if ($formDialog.length > 0) {
                        $('.dialog-body').html(data);
                        if (!$formDialog.data('kendoWindow')) {
                            $formDialog.kendoWindow({
                                width: 700,
                                title: 'Combine or Disassociate Expenses',
                                actions: ['Close'],
                                visible: false,
                                resizable: false,
                                modal: true
                            });
                        }
                        $formDialog.data('kendoWindow').open().center(); // open() needs to come before center()

                        $formDialog.data('kendoWindow').bind('close', function (e) {
                            DojoWeb.Expense.getExpenses(); // refresh expense grid
                            //e.preventDefault(); // will prevent kendo window to be closed
                        });

                        // enable closing popup when clicking outside of it if modal = true
                        $(document).unbind('click').on('click', '.k-overlay', function (e) {
                            $('#formDialog').data('kendoWindow').close();
                        });

                        getExpenses($month, $property);
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
        },

        getData = function ($month, $property) {
            DojoWeb.Busy.show();
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            var propertyCode = $property.val();
            $.ajax({
                url: kendo.format('/Expense/GetCombinedExpenseTree?month={0}&propertyCode={1}', month, propertyCode),
                success: function (data) {
                    DojoWeb.Busy.hide();
                    $expenseTree = $('#' + _expenseTree);
                    populateTreeView(data);
                    initEvents();
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                    }
                }
            });
        },

        populateTreeView = function (data) {
            $dataTree = $expenseTree.kendoTreeView({
                dataSource: data,
                loadOnDemand: false,
                dragAndDrop: true,
                template: '#= item.text #', // allow html code to be rendered in tree node
                dragstart: function (e) {
                    // cannot use kendo tree traverse facility as we are using html encoded template.
                    // so use the good old fashion jquey dom manipulation
                    var node = e.sourceNode;
                    var children =  $('#' + node.id).closest('li').children('ul');
                    if (children.length > 0) {
                        e.preventDefault();
                    }
                },
                drop: function (e) {
                    // cannot use kendo tree traverse facility as we are using html encoded template.
                    // so use the good old fashion jquey dom manipulation
                    if (e.dropTarget != undefined) { // treeview node only
                        var noChild = true;
                        var isLevel3 = true;
                        var node = $('#' + e.dropTarget.id).closest('li');
                        if (node.length > 0) { // drop target is not a root node
                            var noChild = node.find('div[class="expense-tree-node"]').length == 1; // only self
                            var parent = node.parent('ul').length > 0 ? node.parent('ul').parent('li') : node.parent('ul');
                            if (parent.find('div[class="expense-tree-node"]').length > 1) {
                                var grandParent = parent.parent('ul').length > 0 ? parent.parent('ul').parent('li') : parent.parent('ul');
                                isLevel3 = grandParent.find('div[class="expense-tree-node"]').length > 1;
                            }
                        }

                        if (noChild && isLevel3) {
                            e.setValid(false);
                            e.preventDefault();
                        }
                        else {
                            var source = $('#' + e.sourceNode.id).find('div[class="expense-tree-node"]');
                            if (source.length > 0) {
                                parts = source[0].id.split('-');
                                var sourceId = parseInt(parts[parts.length - 1]);

                                var parts = e.dropTarget.id.split('-');
                                var targetId = parseInt(parts[parts.length - 1]);
                                if (targetId == 0) targetId = sourceId;

                                updateExpense(sourceId, targetId);
                            }
                            else {
                                e.setValid(false);
                                e.preventDefault();
                            }
                        }
                    }
                }
            }).data('kendoTreeView');

            //$dataTree.expand($('.k-item:first')); // expand expense tree root
            $dataTree.expand($('.k-item')); // expand all
        },

        updateExpense = function (sourceId, targetId) {
            var $dialog = $('.dialog-page-content');
            $dialog.css('cursor', 'wait');
            $.ajax({
                url: kendo.format('/Expense/UpdateCombinedExpense?sourceId={0}&targetId={1}', sourceId, targetId),
                type: 'POST',
                success: function (data) {
                    $dialog.css('cursor', 'default');
                    _hasChanged = true;
                },
                error: function (jqXHR, status, errorThrown) {
                    $dialog.css('cursor', 'default');
                    if (status == 'error') {
                    }
                }
            })
        }

    return {
        showDialog: showDialog
    }
}();

// not working correctly; not use
DojoWeb.CombineExpensesByGrid = function () {
    var _fromGrid = 'expenseFromGrid',
        _toGrid = 'expenseToGrid',
        $fromGrid = undefined,
        $toGrid = undefined,
        $fromDataGrid = undefined,
        $toDataGrid = undefined,
        _fromDataSource = undefined,
        _toDataSource = undefined,

        init = function (ds) {
            $fromGrid = $('#' + _fromGrid);
            $toGrid = $('#' + _toGrid);
            if ($fromGrid.length > 0) {
                var fromGridOptions = initGridOptions();
                $fromDataGrid = $fromGrid.kendoGrid(fromGridOptions).data('kendoGrid');
                $fromDataGrid.bind('dataBound', function (e) {
                    initFromEvents();
                });
                _fromDataSource = initDataSource(ds);
                $fromDataGrid.setDataSource(_fromDataSource);
            }

            if ($toGrid.length > 0) {
                var toGridOptions = initGridOptions();
                $toDataGrid = $toGrid.kendoGrid(toGridOptions).data('kendoGrid');
                $toDataGrid.bind('dataBound', function (e) {
                    initToEvents();
                });
                _toDataSource = initDataSource(ds);
                $toDataGrid.setDataSource(_toDataSource);
            }
        },

        initFromEvents = function () {
            $($fromDataGrid.element).kendoDraggable({
                filter: 'tr',
                hint: function (e) {
                    var item = $('<div class="k-grid k-widget" style="background-color:green;color:white;"><table><tbody><tr>' + e.html() + '</tr></table></div>');
                    return item;
                },
                group: 'expenseFromGrid'
            });

            $fromDataGrid.table.kendoDropTarget({
                drop: function (e) {
                    var dataItem = _toDataSource.getByUid(e.draggable.currentTarget.data('uid'));
                    _toDataSource.remove(dataItem);
                    _fromDataSource.add(dataItem);
                    // TODO: group item...
                },
                group: 'expenseToGrid'
            });
        },

        initToEvents = function () {
            $($toDataGrid.element).kendoDraggable({
                filter: 'tr',
                hint: function (e) {
                    var item = $('<div class="k-grid k-widget" style="background-color:DarkOrange;color:white;"><table><tbody><tr>' + e.html() + '</tr></table></div>');
                    return item;
                },
                group: 'expenseToGrid'
            });

            $toDataGrid.table.kendoDropTarget({
                drop: function (e) {
                    var dataItem = _fromDataSource.getByUid(e.draggable.currentTarget.data('uid'));
                    _fromDataSource.remove(dataItem);
                    _toDataSource.add(dataItem);
                    // TODO: group item...
                },
                group: 'expenseFromGrid'
            });
        },

        initGridOptions = function () {
            return {
                //height: auto,
                filterable: false,
                sortable: false,
                pageable: false,
                batch: false,
                columns: [
                    // grid data fields
                    { field: 'ExpenseDate', title: 'Expense Date', width: '100px', format: '{0:MM/dd/yyyy}' },
                    { field: 'ConfirmationCode', title: 'Reservation', width: '120px', hidden: true },
                    { field: 'Category', title: 'Category' },
                    { field: 'ExpenseAmount', title: 'Amount', width: '100px', template: "#= DojoWeb.Template.money(ExpenseAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    // workflow fields
                    { field: 'Reviewed', title: 'Review', hidden: true },
                    { field: 'Approved', title: 'Approve',hidden: true },
                    { field: 'ApprovalStatus', title: 'Approval Status', hidden: true },
                    // identity fields
                    { field: 'ExpenseId', title: 'Expense ID', hidden: true },
                    { field: 'ParentId', title: 'Parent ID', hidden: true },
                    { field: 'ReservationId', title: 'Reservation ID', hidden: true },
                    { field: 'PropertyCode', title: 'Property Code', hidden: true },
                ],
            }
        },

        initDataSource = function (ds) {
            return new kendo.data.DataSource({
                batch: false,
                data: ds,
                schema: {
                    model: {
                        id: 'ExpenseId',
                        fields: {
                            ExpenseId: { type: 'number', editable: false, nullable: false },
                            ParentId: { type: 'number', editable: false, nullable: false },
                            ExpenseDate: { type: 'date', editable: true, nullable: false },
                            ConfirmationCode: { type: 'string', editable: true, nullable: false },
                            Category: { type: 'string', editable: true, nullable: false },
                            ExpenseAmount: { type: 'number', editable: true, nullable: false },
                            ReservationId: { type: 'number', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: false, nullable: false },
                            ApprovalStatus: { type: 'number', editable: false, nullable: false },
                            Reviewed: { type: 'boolean', editable: false, nullable: true },
                            Approved: { type: 'boolean', editable: false, nullable: true },
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

        showDialog = function (ds) {
            DojoWeb.Busy.show();
            $.ajax({
                url: '/Expense/Combine',
                success: function (data) {
                    DojoWeb.Busy.hide();
                    var $formDialog = $('#formDialog');
                    if ($formDialog.length > 0) {
                        $('.dialog-body').html(data);
                        if (!$formDialog.data('kendoWindow')) {
                            $formDialog.kendoWindow({
                                width: 1200,
                                title: 'Combine Expenses',
                                actions: ['Close'],
                                visible: false,
                                resizable: false,
                            });
                        }
                        $formDialog.data('kendoWindow').open().center(); // open() needs to come before center()

                        init(ds);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                    }
                }
            })
        }

    return {
        init: init,
        showDialog: showDialog
    }
}();
