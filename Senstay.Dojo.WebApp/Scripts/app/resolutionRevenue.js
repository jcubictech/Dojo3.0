"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.ResolutionRevenue = function () {
    var _gridId = undefined,
        $grid = undefined,
        $property = $('#revenuePropertyCode'),
        $month = $('#revenueMonth'),
        $dataCount = $('#dataCount'),
        _checkboxCallbackClass = 'revenueCheckboxUpdate', // class to trigger callback to save data
        _impactList = [ 'Client Expense', 'Adjustment', 'SenStay Expense' ],
        __confirmationPicker = undefined,
        _confirmationCode,
        _allowEdit = false,
        _canEdit = false,
        _canDelete = false,
        _canReview = false,
        _canApprove = false,
        _reviewAll = 'Set Review All',
        _approveAll = 'Set Approve All',
        _removeReviewAll = 'Remove All Reviewed',
        _removeApproveAll = 'Remove All Approved',
        _reviewAllSelector = 'a.k-reviewedAll',
        _removeReviewAllSelector = 'a.k-removeReviewedAll',
        _approveAllSelector = 'a.k-approvedAll',
        _removeApproveAllSelector = 'a.k-removeApprovedAll',

        init = function (id) {
            $grid = $('#' + id);
            _gridId = '#' + id;
            _allowEdit = $('.revenue-field-readonly').length == 0;
            _canDelete = $('.revenue-grid-remover').length > 0;
            _canApprove = $('.revenue-grid-approver').length > 0;
            _canReview = $('.revenue-grid-reviewer').length > 0;
            _canDelete = $('.revenue-grid-remover').length > 0;
            $property.val('PropertyPlaceholder');
            _canEdit = _allowEdit;
            installEvents();
            DojoWeb.RevenueWorkflow.install(moveTo); // initialize 3-state approval workflow with access control           
            DojoWeb.Notification.init('actionAlert', 3000); // install ajax action response messaging
            $month.change();
        },

        installEvents = function () {
            $('.showRevenueEdit').unbind('click').on('click', function (e) {
                _action = 'edit';
            });

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
                if (requiredSelected()) getData();
            });

        },
        
        installGridEvents = function () {
            $('.' + _checkboxCallbackClass).unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                var field = $(this).data('field');
                var included = $(this).prop('checked') == true ? 1 : 0;
                saveFieldStatus(id, field, included);
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

            // new/edit/view dialog; must be after all grid settings
            var caption = 'Edit Resolution';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showRevenueEdit',
                caption: caption,
                width: 890,
                url: '/Resolution/EditRevenue',
                formId: 'ResolutionEntryForm',
                initEvent: DojoWeb.ResolutionForm.init,
                modal: false,
                closeEvent: null
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
                        getResolutions();
                    }
                    else {
                        _canEdit = _allowEdit ? true : false;
                        getResolutions();
                    }
                },
                error: function (jqXHR, status, errorThrown) { // ignore the error
                    getResolutions();
                }
            });
        },

        getPropertyCode = function () {
            return $property ? $property.val() : '';
        },

        getResolutions = function () {
            DojoWeb.Busy.show();

            var selectedMonth = $month.data('kendoDatePicker').value();
            var lastMonth = (new Date()).addMonths(-1);
            //_canEdit = _allowEdit == false ? _allowEdit : kendo.toString(selectedMonth, 'yyyy-MM-dd') >= kendo.toString(lastMonth, 'yyyy-MM-01');

            if ($grid.data('kendoGrid') != undefined) { // reload data source to avoid stack up kendo grid events
                var month = kendo.toString(selectedMonth, 'MM/dd/yyyy');
                var propertyCode = $property.val();
                var options = {
                    type: 'GET',
                    url: kendo.format('/Resolution/RetrieveView?month={0}&propertyCode={1}', month, propertyCode)
                };
                $.ajax(options).done(function (result) {
                    DojoWeb.RevenueWorkflow.disable(!_canEdit);
                    $grid.data('kendoGrid').dataSource.data(result);
                    setCount();
                    ensureAllowDelete();
                    DojoWeb.Busy.hide();

                    showOrphanCount();

                    var dataGrid = $grid.data('kendoGrid')
                    if (!_canEdit) {
                        dataGrid.hideColumn(0); // edit
                        dataGrid.hideColumn(1); // delete
                    }
                    else { // need to set action button state as the grid is not re-initialized
                        dataGrid.showColumn(0); // edit
                        dataGrid.showColumn(1); // delete
                    }
                });
            }
            else { // set up grid and load first data soruce
                var month = kendo.toString(selectedMonth, 'MM/dd/yyyy');
                var propertyCode = $property.val();
                DojoWeb.RevenueWorkflow.disable(!_canEdit);
                var dataSource = setupDataSource(month, propertyCode);
                var gridOptions = setupGridOptions(dataSource);
                var dataGrid = $grid.kendoGrid(gridOptions).data('kendoGrid');
                dataGrid.bind('dataBound', function (e) {
                    installGridEvents();
                    setCount();
                    ensureAllowDelete();
                    DojoWeb.Busy.hide();

                    showOrphanCount();

                    var dataGrid = $grid.data('kendoGrid')
                    if (!_canEdit) {
                        dataGrid.hideColumn(0); // edit
                        dataGrid.hideColumn(1); // delete
                    }
                });
            }
        },

        setupGridOptions = function (ds) {
            var height = $(window).height() - 300;
            return {
                dataSource: ds,
                height: height,
                filterable: true,
                sortable: true,
                pageable: false,
                batch: false,
                toolbar: _canEdit ? [{ name: 'ReviewedAll', text: _reviewAll, imageClass: '', className: 'k-reviewedAll', iconClass: '' },
                                     { name: '_removeReviewAll', text: _removeReviewAll, imageClass: '', className: 'k-removeReviewedAll', iconClass: '' },
                                     { name: 'ApprovedAll', text: _approveAll, imageClass: '', className: 'k-approvedAll', iconClass: '' },
                                     { name: '_removeApproveAll', text: _removeApproveAll, imageClass: '', className: 'k-removeApprovedAll', iconClass: '' }]
                         : null,
                columns: [
                    // action buttons
                    { field: 'edit', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.ResolutionRevenue.renderAction(data.ResolutionId, 'edit')#", hidden: !_canEdit },
                    { field: 'delete', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.ResolutionRevenue.renderAction(data.ResolutionId, 'delete')#", hidden: !_canEdit },

                    // grid data fields
                    { field: 'ResolutionDate', title: 'Resolution Date', width: '110px', filterable: { multi: true }, format: '{0:MM/dd/yyyy}', required: true, template: "#= DojoWeb.Template.linkDate(data.Source, data.ResolutionDate, data.OwnerPayoutId) #" },
                    { field: 'ConfirmationCode', title: 'Reservation', width: '180px', template: "#= DojoWeb.ResolutionRevenue.confirmationAndPropertyCode(data.ConfirmationCode, data.PropertyCode, data.CanStatementSeeIt) #" },
                    { field: 'ResolutionType', title: 'Type', width: '100px', filterable: { multi: true } },
                    { field: 'ResolutionDescription', title: 'Description', width: '220px', filterable: false },
                    { field: 'Impact', title: 'Impact', width: '120px', filterable: { multi: true } },
                    { field: 'Cause', title: 'Cause', width: '140px', filterable: { multi: true } },
                    { field: 'Product', title: 'Product', width: '100px', filterable: { multi: true } },
                    { field: 'PayToAccount', title: 'PayTo Account', width: '110px', filterable: { multi: true } },
                    { field: 'ResolutionAmount', title: 'Amount', width: '90px', filterable: false, required: true, template: "#= DojoWeb.Template.money(ResolutionAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'IncludeOnStatement', title: 'IS', width: '60px', filterable: { multi: true }, template: "#= DojoWeb.ResolutionRevenue.renderCheckBox(data.ResolutionId, data.IncludeOnStatement)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'Source', title: 'Source', width: '130px', filterable: { multi: true }, template: "#= DojoWeb.ResolutionRevenue.showSource(Source) #" },
                    // workflow fields
                    { field: 'Reviewed', title: 'Review', width: '100px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.ResolutionId, 1, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'Approved', title: 'Approve', width: '100px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.ResolutionId, 2, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'ApprovedNote', title: 'Approval Note', filterable: false },
                    // identity fields
                    { field: 'ApprovalStatus', title: 'Approval Status', hidden: true },
                    { field: 'CanStatementSeeIt', title: 'Is Orphan', hidden: true },
                    { field: 'PropertyCode', title: 'Property Code', hidden: true },
                    { field: 'ResolutionId', title: 'Resolution ID', hidden: true },
                    { field: 'OwnerPayoutId', title: 'OwnerPayout ID', hidden: true },
                ],
            }
        },

        setupDataSource = function (month, propertyCode) {
            return new kendo.data.DataSource({
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<model>
                transport: {
                    read: {
                        url: kendo.format('/Resolution/RetrieveView?month={0}&propertyCode={1}', month, propertyCode),
                        type: 'Get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/Resolution/Create',
                        type: 'Post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/Resolution/Update',
                        type: 'Post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/Resolution/Delete',
                        type: 'Post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].ResolutionId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') {
                                options.ResolutionId = 0;
                                options.OwnerPayoutId = 0;
                                options.PropertyCode = getPropertyCode();
                                options.ApprovalStatus = 0;
                                options.Reviewed = false;
                                options.Approved = false;
                            }
                            else if (operation === 'update') {
                                //options.PropertyCode = getPropertyCode();
                            }
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                schema: {
                    model: {
                        id: 'ResolutionId',
                        fields: {
                            ResolutionId: { type: 'number', editable: false, nullable: false },
                            OwnerPayoutId: { type: 'number', editable: false, nullable: false },
                            ResolutionDate: { type: 'date', editable: true, nullable: false },
                            ConfirmationCode: { type: 'string', editable: true, nullable: false },
                            ResolutionType: { type: 'string', editable: true, nullable: false },
                            ResolutionDescription: { type: 'string', editable: true, nullable: false },
                            Impact: { type: 'string', editable: true, nullable: false },
                            Cause: { type: 'string', editable: true, nullable: false },
                            ResolutionAmount: { type: 'number', editable: true, nullable: false },
                            IncludeOnStatement: { type: 'boolean', editable: true, nullable: false },
                            ApprovedNote: { type: 'string', editable: true, nullable: false },
                            Source: { type: 'string', editable: false, nullable: false },
                            ApprovalStatus: { type: 'number', editable: false, nullable: false },
                            Reviewed: { type: 'boolean', editable: false, nullable: true },
                            Approved: { type: 'boolean', editable: false, nullable: true },
                            PropertyCode: { type: 'string', editable: false, nullable: false },
                            CanStatementSeeIt: { type: 'boolean', editable: false, nullable: false },
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

        renderAction = function (id, action) {
            if (action == 'edit') {
                if (_canEdit)
                    return "<div id='edit-id-" + id + "' class='showRevenueEdit gridcell-btn' title='Edit Resolution' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-wrench'></i></div></div>";
                else
                    return "<div id='view-id-" + id + "' class='showRevenueEdit gridcell-btn' title='View Resolution' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-eye'></i></div></div>";
            }
            else if (action == 'delete') {
                if (_canEdit)
                    return "<div id='delete-id-" + id + "' class='gridcell-btn' title='Delete Resolution' onClick='DojoWeb.ResolutionRevenue.renderDelete(" + '"' + id + '"' + ");'><div class='btn dojo-center'><i class='fa fa-trash-o'></i></div></div>";
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-trash-o'></i></div></div>";
            }
        },

        renderDelete = function (id) {
            DojoWeb.Confirmation.confirmDiscard({
                id: 'confirmation-dialog',
                caption: 'Delete Resolution Confirmation',
                message: 'The selected Resolution will be deleted. Please confirm.',
                ok: function () {
                    $.ajax({
                        type: 'POST',
                        url: '/Resolution/DeleteRevenue/?id=' + id,
                        success: function (result) {
                            if (result == 'success') {
                                getResolutions(); // refresh the grid to remove the deleted row
                            }
                            else {
                                DojoWeb.Notification.show('There was an error deleting the Resolution.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (status == 'error') {
                                alert('There was an error deleting the Resolution.');
                            }
                        }
                    });
                }
            });
        },

        renderCheckBox = function (id, data) {
            var checked = data != 0 ? 'checked="checked"' : '';
            var readonly = _canEdit ? '' : readonly = 'disabled="true"';
            return kendo.format('<input type="checkbox" class="{0}" data-field="IncludeOnStatement" data-id="{1}" {2} {3} />',
                                _checkboxCallbackClass, id, checked, readonly);
        },

        requiredSelected = function () {
            return $property.val() != '' && $month.val() != '';
        },

        emptyGrid = function () {
            $grid.empty(); // empty grid content
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var ResolutionSum = 0;
                $.each($view, function (i, r) {
                    ResolutionSum += r.ResolutionAmount;
                });
                var htmlContent = '';
                htmlContent += '(Count = ' + count;
                htmlContent += ',  Monthly Total = ' + (ResolutionSum == 0 ? '$0' : DojoWeb.Template.money(ResolutionSum)) + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        },

        moveTo = function (id, state, direction) {
            var url = kendo.format('/Resolution/UpdateWorkflow?id={0}&state={1}&direction={2}', id, state, direction);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result >= 0) {
                        //DojoWeb.Notification.show('Workflow is updated successfully.');
                        if (result > state) result = result - 1;
                        updateGridDataSource(id, result);
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
            DojoWeb.Busy.show();
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            var url = kendo.format('/Resolution/UpdateWorkflowAll?month={0}&state={1}&direction={2}',
                                   month, state, direction);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result >= 0) {
                        getResolutions();
                    }
                    else {
                        DojoWeb.Notification.show('Dojo encounters error while saving the workflow. Please refresh the page.');
                    }
                    DojoWeb.Busy.hide();
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
                    DojoWeb.Busy.hide();
                }
            });
        },

        enableWorkflowButtons = function () {
            if (_canEdit) {
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
                $(_reviewAllSelector).addClass('hide');
                $(_approveAllSelector).addClass('hide');
                $(_removeReviewAllSelector).addClass('hide');
                $(_removeApproveAllSelector).addClass('hide');
            }
        },

        saveFieldStatus = function (id, field, included) {
            var url = kendo.format('/Resolution/UpdateFieldStatus?id={0}&field={1}&included={2}', id, field, included);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result != '') {
                        // update grid datasource
                        getResolutions();
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

        onReservationSelected = function(e) {
            var selected = this.dataItem(e.item.index());
            _confirmationCode = selected.ConfirmationCode;
        },

        showSource = function (source) {
            return source.split('@')[0];
        },

        updateGridDataSource = function (id, state) {
            var ds = $grid.data('kendoGrid').dataSource.view();
            if (ds) {
                $.each(ds, function (i, item) {
                    if (item.ResolutionId == id) {
                        item.ApprovalStatus = state;
                    }
                });
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

        removeTextFromActionButtons = function () {
            // remove text from action button
            if ($('.k-grid-delete').length > 0) {
                var deleteIconContent = $('.k-grid-delete').html().replace('Delete', '').replace('Cancel', '');
                $('.k-grid-delete').html(deleteIconContent);
            }
            if ($('.k-grid-edit').length > 0) {
                var editIconContent = $('.k-grid-edit').html().replace('Edit', '');
                $('.k-grid-edit').html(editIconContent);
            }
            if ($('.k-grid-update').length > 0) {
                var saveIconContent = $('.k-grid-update').html().replace('Save', '');
                $('.k-grid-update').html(saveIconContent);
            }
            if ($('.k-grid-cancel').length > 0) {
                var cancelIconContent = $('.k-grid-cancel').html().replace('Cancel', '');
                $('.k-grid-cancel').html(cancelIconContent);
            }
        },

        disableAction = function (actionClass, disabled) {
            if (disabled)
                $('#' +_gridId + ' .' +actionClass).addClass('disabled');
            else
                $('#' +_gridId + ' .' +actionClass).removeClass('disabled');
        },

        setReadOnly = function (isReadOnly) {
            if (isReadOnly) {
                disableAction('k-grid-add', true);

                // in case inline editing is still on, we remove it
                if ($('#' + _gridId).find('.k-grid-cancel').length > 0)
                    $('#' + _gridId).find('.k-grid-cancel').click();

                //disableAction('k-grid-update', true);
                //disableAction('k-grid-cancel', true);
                disableAction('k-grid-edit', true);
                disableAction('k-grid-delete', true);
                disableAction('add-blog-disabled', true);
            }
            else {
                disableAction('k-grid-add', false);
                //disableAction('k-grid-update', false);
                //disableAction('k-grid-cancel', false);
                disableAction('k-grid-edit', false);
                disableAction('k-grid-delete', false);
                disableAction('add-blog-disabled', false);
            }
        },

        confirmationAndPropertyCode = function (confirmation, property, orphan) {
            if (confirmation === null) confirmation = '';
            if (property === null) property = '';
            if (confirmation != '' || property != '') {
                if (orphan == 0)
                    return '<span style="color:red;">' + confirmation + ' | ' + property + '</span>';
                else
                    return confirmation + ' | ' + property;
            }
            else
                return '';
        },

        showOrphanCount = function () {
            var gridData = $grid.data('kendoGrid').dataSource.data();

            var status = _.countBy(gridData, function (item) {
                return item.Approved ? 'approved' : (item.Reviewed ? 'reviewed' : 'na');
            });
            var statusNotice = kendo.format('<span><span style="color:green">Approved ({0})</span>&nbsp;&nbsp;&nbsp;<span style="color:blue">Reviewed ({1})</span>&nbsp;&nbsp;&nbsp;To-Do ({2})</span>&nbsp;&nbsp;&nbsp;',
                                            status.approved == undefined ? 0 : status.approved,
                                            status.reviewed == undefined ? 0 : status.reviewed,
                                            status.na == undefined ? 0 : status.na);

            var orphan = _.countBy(gridData, function (item) {
                return item.CanStatementSeeIt ? 'good' : 'bad';
            });

            $('#orphanNotice').removeClass('hide');
            if (orphan.bad > 0) {
                $('#orphanNotice span').html(statusNotice + '<span style="color:red;">Invalid Confirmation Code (' + orphan.bad + ')</span>');
            }
            else {
                $('#orphanNotice span').html(statusNotice);
            }
        },

        adjustEditorIcon = function (name) {
            $($($('input[name="' + name + '"]').parent().children('span')[0]).children()[0]).html('');
        }

    return {
        init: init,
        getResolutions: getResolutions,
        moveTo: moveTo,
        getPropertyCode: getPropertyCode,
        renderCheckBox: renderCheckBox,
        renderAction: renderAction,
        renderDelete: renderDelete,
        showSource: showSource,
        confirmationAndPropertyCode: confirmationAndPropertyCode
    }
}();

DojoWeb.ResolutionForm = function () {
    var $form = undefined,
        _currentId = undefined, // this is the owner payout id

        init = function (formId, id) {
            $form = $('#' + formId);
            _currentId = id;
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

            var date = $('#resolve-id-' + _currentId).data('date');
            if (date != undefined) {
                $('#ResolutionDate').val(date);
                $('#ResolutionDate').data('kendoDatePicker').readonly();
            }

            $('#ConfirmationCode').kendoComboBox({
                height: 400,
                placeholder: 'Search a confirmation or property code...',
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

            if ($('#ConfirmationCode').val() == '') {
                $('#ConfirmationCode').data('kendoComboBox').value($('#PropertyCode').val());
            }
        },

        onConfirmationCodeChange = function (e) {
            var comboText = this.text();
            var substrings = comboText.split('|');
            if (substrings.length == 1)
                $('#PropertyCode').val(substrings[0].trim());
            else if (substrings.length > 1)
                $('#PropertyCode').val(substrings[1].trim());
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
                    var formData = $form.serialize(); // this is a query string format; not json format
                    $.ajax({
                        type: 'POST',
                        url: '/Resolution/SaveRevenue',
                        data: formData,
                        success: function (result) {
                            if (result != '') {
                                DojoWeb.Plugin.closeFormDialog();
                                _currentId = parseInt(result);
                                DojoWeb.ResolutionRevenue.getResolutions();
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
            invalidCount += DojoWeb.Validation.validateTextBox('#ResolutionType', 'Resolution Type is required.');
            invalidCount += DojoWeb.Validation.validateDate('#ResolutionDate', 'Resolution Date is required.');
            //invalidCount += DojoWeb.Validation.validateTextBox('#PropertyCode', 'Property Code is required.');
            //invalidCount += DojoWeb.Validation.validateTextBox('#ResolutionDescription', 'Resolution Description is required.');
            //invalidCount += DojoWeb.Validation.validateDropdown('#Impact', 'Impact is required.');
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
