"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.ReservationRevenue = function () {
    var _gridId = undefined,
        $grid = undefined,
        $property = $('#revenuePropertyCode'),
        $month = $('#revenueMonth'),
        $dataCount = $('#dataCount'),
        _checkboxCallbackClass = 'revenueCheckboxUpdate', // class to trigger callback to save data
        _editableCheckboxCue = 'manual',
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
        _highlightRow = undefined,
        _approvalNoteColumnWidth = 500,
        _availablePropertyCodes = [],

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

            DojoWeb.DuplicateReservations.init('duplicateGrid');

            // diable for now
            //getAvailablePropertyCodes();
            //DojoWeb.MissingPropertyCodes.init('missingGrid', $month);
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
                if (requiredSelected()) {
                    getData();
                }
            });

            $month.unbind('change').on('change', function (e) {
                rebindComboBox();
                if (requiredSelected()) {
                    getData();
                }
            });

            $('#duplicateReservation').kendoButton({
                spriteCssClass: 'fa fa-object-ungroup red',
                click: duplicate
            });

            $('#missingPropertyCode').kendoButton({
                spriteCssClass: 'fa fa-bug red',
                click: missing
            });

            // searchable dropdown with color coded properties
            var height = $(window).height() - 300;
            $property.kendoComboBox({
                height: height,
                placeholder: 'Select a property...',
                filter: 'contains',
                dataTextField: 'PropertyCodeAndAddress',
                dataValueField: 'PropertyCode',
                dataSource: {
                    type: 'json',
                    //serverFiltering: true,
                    transport: {
                        read: {
                            url: '/Reservation/GetPropertyCodeWithAddress?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy'),
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
        
        installGridEvents = function () {
            $('.showRevenueEdit').unbind('click').on('click', function (e) {
                _action = 'edit';
            });

            // new reservation - add dialog class cue to actiate it
            $(_gridId + ' .k-grid-add').addClass('showRevenueEdit');

            $(_gridId + ' .k-grid-add').unbind('click').on('click', function (e) {
                _action = 'new';
            });

            $('.' + _checkboxCallbackClass).unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                var field = $(this).data('field');
                var taxrate = $(this).data('taxrate');
                var included = $(this).prop('checked') == true ? 1 : 0;
                saveFieldStatus(id, field, included, taxrate);
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

            // new/edit/view edit dialog; must be after all grid settings
            var caption = 'Edit Reservation';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showRevenueEdit',
                caption: kendo.format('{0} for Property {1}', caption, $property.val()),
                width: 890,
                url: '/Reservation/EditRevenue',
                formId: 'ReservationEntryForm',
                initEvent: DojoWeb.ReservationForm.init,
                modal: false,
                closeEvent: unselectRow
            });

            // new/edit/view tetris dialog; must be after all grid settings
            var caption = 'Tetrising Reservation';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showTetrisEdit',
                caption: caption,
                width: 600,
                url: '/Reservation/TetrisRevenue',
                formId: 'TetrisReservationEntryForm',
                initEvent: DojoWeb.ReservationTetrisForm.init,
                modal: false,
                closeEvent: unselectRow
            });

            // new/edit/view split dialog; must be after all grid settings
            var caption = 'Splitting Reservation';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showSplitEdit',
                caption: caption,
                width: 600,
                url: '/Reservation/SplitRevenue',
                formId: 'SplitReservationEntryForm',
                initEvent: DojoWeb.ReservationSplitForm.init,
                modal: false,
                closeEvent: unselectRow
            });

            DojoWeb.ApprovalNoteEditor.init();

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
                        getReservations();
                    }
                    else {
                        _canEdit = _allowEdit ? true : false;
                        getReservations();
                    }
                },
                error: function (jqXHR, status, errorThrown) { // ignore the error
                    getReservations();
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

        getReservations = function () {
            var selectedMonth = $month.data('kendoDatePicker').value();
            var lastMonth = (new Date()).addMonths(-1);
            //_canEdit = _allowEdit == false ? _allowEdit : kendo.toString(selectedMonth, 'yyyy-MM-dd') >= kendo.toString(lastMonth, 'yyyy-MM-01');
            var month = kendo.toString(selectedMonth, 'MM/dd/yyyy');
            DojoWeb.Busy.show();
            $.ajax({
                type: 'POST',
                url: '/Reservation/RevenueView',
                data: { month: month, propertyCode: $property.val() },
                success: function (data) {
                    DojoWeb.Busy.hide();
                    if ($.isArray(data)) {
                        DojoWeb.DuplicateReservations.toggleGrid($property, true); // show resevation grid
                        emptyGrid();
                        DojoWeb.RevenueWorkflow.disable(!_canEdit); // disable workflow action if not editable
                        var gridOptions = setupGridOptions();
                        var dataGrid = $grid.kendoGrid(gridOptions).data('kendoGrid');

                        dataGrid.bind('dataBound', function (e) {
                            installGridEvents();
                            setCount();
                            rebindComboBox();
                            var id = DojoWeb.ReservationForm.getId();
                            var msgTemplate = '{0} of reservation for property "{0}" is successful.';
                            var message = '';
                            if (needHighlightRow()) {
                                DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                                if (_action == 'edit') kendo.format(msgTemplate, 'Update');
                            }
                            else if (id != undefined && _action == 'new') {
                                _highlightRow = id;
                                DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                                kendo.format(msgTemplate, 'Creation');
                            }
                            else if (_action == 'delete') {
                                kendo.format(msgTemplate, 'Deletion');
                            }
                            if (message != '') DojoWeb.Notification.show(message);
                            _action = undefined;

                            if (!_canEdit) {
                                dataGrid.hideColumn(0); // edit
                                dataGrid.hideColumn(1); // delete
                                dataGrid.hideColumn(2); // tetrising
                                dataGrid.hideColumn(3); // split
                                dataGrid.hideColumn(4); // convert
                                DojoWeb.ApprovalNoteEditor.disable();
                            }
                        });
                        var revenueSource = setupDataSource(data);
                        dataGrid.setDataSource(revenueSource);

                        // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                        // we remove the 'filter' text ad-hoc here
                        $(_gridId + ' span.k-filter').text('');

                        // set the approval note width
                        _approvalNoteColumnWidth = getApprovalNoteColumnWidth();
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                }
            });
        },

        getAvailablePropertyCodes = function () {
            $.ajax({
                url: '/Property/GetPropertyCodes',
                dataType: 'json',
                success: function (result) {
                    _availablePropertyCodes = result;
                }
            });
        },

        setupGridOptions = function () {
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
                toolbar: _canEdit ? [//{ name: 'create', text: 'New Reservation' }, // move creation to owner payout so that reservation can relate to it
                                     { name: 'ReviewedAll', text: _reviewAll, imageClass: '', className: 'k-reviewedAll', iconClass: '' },
                                     { name: '_removeReviewAll', text: _removeReviewAll, imageClass: '', className: 'k-removeReviewedAll', iconClass: '' },
                                     { name: 'ApprovedAll', text: _approveAll, imageClass: '', className: 'k-approvedAll', iconClass: '' },
                                     { name: '_removeApproveAll', text: _removeApproveAll, imageClass: '', className: 'k-removeApprovedAll', iconClass: '' }]
                         : null,
                columns: [
                    // action buttons
                    { field: 'edit', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.ReservationRevenue.renderAction(data.ReservationId, 'edit')#" },
                    { field: 'delete', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.ReservationRevenue.renderAction(data.ReservationId, 'delete')#" },
                    { field: 'tetris', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.ReservationRevenue.renderAction(data.ReservationId, 'tetris')#", hidden: !_canEdit },
                    { field: 'split', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.ReservationRevenue.renderAction(data.ReservationId, 'split')#", hidden: !_canEdit },
                    { field: 'convert', title: ' ', width: '40px', filterable: false, template: "#= DojoWeb.ReservationRevenue.renderAction(data.ReservationId, 'convert')#", hidden: !_canEdit },
                    // grid data fields
                    { field: 'PayoutDate', title: 'Payout Date', width: '120px', template: "#= DojoWeb.Template.linkDate(data.Source, data.PayoutDate, data.OwnerPayoutId) #", filterable: { multi: true }, format: '{0:MM/dd/yyyy}', sortable: false },
                    { field: 'ConfirmationCode', title: 'Confirmation Code', width: '150px' },
                    { field: 'Channel', title: 'Channel', width: '110px', filterable: { multi: true } },
                    { field: 'CheckinDate', title: 'Checkin Date', width: '120px', template: "#= DojoWeb.Template.highlightDate(data.CheckinDate, data.OverlapColor) #", filterable: { multi: true }, format: '{0:MM/dd/yyyy}' },
                    { field: 'Nights', title: 'Nights', width: '80px', filterable: { multi: true } },
                    { field: 'GuestName', title: 'Guest Name', width: '150px' },
                    { field: 'TotalRevenue', title: 'Total Revenue', width: '120px', template: "#= DojoWeb.Template.money(TotalRevenue) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'IsTaxed', title: 'Taxed?', width: '80px', filterable: { multi: true }, template: "#= DojoWeb.ReservationRevenue.renderCheckBox(data.ReservationId, data.IsTaxed, data.Channel, 'manual', 'IsTaxed', data.TaxRate)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'TaxCollected', title: 'Tax Collected', width: '120px', template: "#= DojoWeb.Template.money(TaxCollected) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'IncludeOnStatement', title: 'Included?', width: '100px', filterable: { multi: true }, template: "#= DojoWeb.ReservationRevenue.renderCheckBox(data.ReservationId, data.IncludeOnStatement, data.Channel, 'manual', 'IncludeOnStatement', data.TaxRate)#", attributes: { class: 'grid-cell-align-center' } },
                    // workflow fields
                    { field: 'Reviewed', title: 'Review', width: '90px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.ReservationId, 1, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'Approved', title: 'Approve', width: '100px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.ReservationId, 2, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'ApprovedNote', title: 'Workflow Note', template: "#= DojoWeb.ApprovalNoteEditor.renderNote(data.ReservationId, data.ApprovedNote, data.ApproveStatus) #" },
                    { field: 'ApprovalStatus', title: 'Approval Status', hidden: true },
                    // identity fields
                    { field: 'ReservationId', title: 'Reservation ID', hidden: true },
                    { field: 'OwnerPayoutId', title: 'Owner Payout ID', hidden: true },
                    { field: 'InputSource', title: 'Input Source', hidden: true },
                    { field: 'Source', title: 'Source', hidden: true },
                    { field: 'TaxRate', title: 'TaxRate', hidden: true },
                ],
                pageable: false,
                pageSize: 20,
                //editable: 'inline',
                cancel: function () {
                    $grid.data('kendoGrid').dataSource.cancelChanges(); 
                }        
            }
        },

        setupDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<model>
                schema: {
                    model: {
                        id: 'ReservationId',
                        fields: {
                            ReservationId: { type: 'number', editable: false, nullable: false },
                            PayoutDate: { type: 'date', editable: true, nullable: false },
                            ConfirmationCode: { type: 'string', editable: false, nullable: true },
                            Channel: { type: 'string', editable: false, nullable: true },
                            CheckinDate: { type: 'date', editable: false, nullable: true },
                            Nights: { type: 'number', editable: false, nullable: true },
                            GuestName: { type: 'string', editable: false, nullable: true },
                            TotalRevenue: { type: 'number', editable: true, nullable: false },
                            IsTaxed: { type: 'boolean', editable: true, nullable: false },
                            IncludeOnStatement: { type: 'boolean', editable: true, nullable: false },
                            ApprovalStatus: { type: 'number', editable: false, nullable: false },
                            Reviewed: { type: 'boolean', editable: true, nullable: true },
                            Approved: { type: 'boolean', editable: true, nullable: true },
                            ApprovedNote: { type: 'string', editable: true, nullable: true },
                            OwnerPayoutId: { type: 'number', editable: false, nullable: true },
                            TaxRate: { type: 'number', editable: false, nullable: false },
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
                    return "<div id='edit-id-" + id + "' class='showRevenueEdit gridcell-btn' title='Edit Reservation' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-wrench'></i></div></div>";
                else
                    return "<div id='view-id-" + id + "' class='showRevenueEdit gridcell-btn' title='View Reservation' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-eye'></i></div></div>";
            }
            else if (action == 'delete') {
                if (_canEdit)
                    return "<div id='delete-id-" + id + "' class='gridcell-btn' title='Delete Reservation' onClick='DojoWeb.ReservationRevenue.renderDelete(" + '"' + id + '"' + ");'><div class='btn dojo-center'><i class='fa fa-trash-o'></i></div></div>";
                else
                    return "<div class='gridcell-btn'><div class='center faintGray'><i class='fa fa-trash-o'></i></div></div>";
            }
            else if (action == 'tetris') {
                return "<div id='tetris-id-" + id + "' class='showTetrisEdit gridcell-btn' title='Tetrising' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-refresh'></i></div></div>";
            }
            else if (action == 'split') {
                return "<div id='split-id-" + id + "' class='showSplitEdit gridcell-btn' title='Split Reservation' data-id='" + id + "'><div class='btn dojo-center'><i class='fa fa-share-alt'></i></div></div>";
            }
            else if (action == 'convert') {
                return "<div id='convert-id-" + id + "' class='gridcell-btn' title='Convert Reservation to Resolution' onClick='DojoWeb.ReservationRevenue.renderConvert(" + '"' + id + '"' + ");'><div class='btn dojo-center'><i class='fa fa-share'></i></div></div>";
            }
        },

        renderDelete = function (id) {
            DojoWeb.Confirmation.confirmDiscard({
                id: 'confirmation-dialog',
                caption: 'Delete Reservation Confirmation',
                message: 'The selected Reservation will be deleted. Please confirm.',
                ok: function () {
                    $.ajax({
                        type: 'POST',
                        url: '/Reservation/DeleteRevenue/?id=' + id,
                        success: function (result) {
                            if (result == 'success') {
                                _action = 'delete';
                                setHighlightRow(undefined);
                                getReservations(); // refresh the grid to remove the deleted row
                            }
                            else {
                                DojoWeb.Notification.show('There was an error deleting the Reservation.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (status == 'error') {
                                alert('There was an error deleting the Reservation.');
                            }
                        }
                    });
                }
            });
        },

        renderConvert = function (id) {
            DojoWeb.Confirmation.confirmDiscard({
                id: 'confirmation-dialog',
                caption: 'Convert Reservation Confirmation',
                message: 'The selected Reservation will be converted to Resolution. Please confirm.',
                width: '560px',
                ok: function () {
                    $.ajax({
                        type: 'POST',
                        url: '/Reservation/ConvertRevenue/?id=' + id,
                        success: function (result) {
                            if (result == 'success') {
                                _action = 'delete';
                                setHighlightRow(undefined);
                                getReservations(); // refresh the grid to remove the converted reservation
                            }
                            else {
                                DojoWeb.Notification.show('There was an error conerting the Reservation.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (status == 'error') {
                                alert('There was an error converting the Reservation.');
                            }
                        }
                    });
                }
            });
        },

        renderCheckBox = function (id, data, channel, inputSource, fieldName, taxrate) {
            var readonly = '';
            var checked = '';
            var callbackClass = _checkboxCallbackClass;
            if (inputSource != _editableCheckboxCue || !_canEdit || (channel == 'Airbnb' && fieldName == 'IsTaxed')) {
                readonly = 'disabled="true"';
            }
            if (data != 0) checked = 'checked="checked"';
            if (taxrate == null || taxrate == '') taxrate = 0;
            return kendo.format('<input type="checkbox" class="{0}" data-id="{1}" data-field="{2}" data-taxrate="{3}" {4} {5} />',
                                callbackClass, id, fieldName, taxrate, checked, readonly);
        },

        requiredSelected = function () {
            return $property.val() != '' && $month.val() != '';
        },

        rebindComboBox = function () {
            $property.data('kendoComboBox').dataSource.transport.options.read.url =
                '/Reservation/GetPropertyCodeWithAddress?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $property.data('kendoComboBox').dataSource.read();
        },

        emptyGrid = function () {
            $grid.empty(); // empty grid content
        },

        moveToAll = function(state, direction) {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            var propertyCode = $property.val();
            var url = kendo.format('/Reservation/UpdateWorkflowAll?month={0}&propertyCode={1}&state={2}&direction={3}', 
                                   month, propertyCode, state, direction);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result >= 0) {
                        getReservations();
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

        duplicate = function () {
            DojoWeb.DuplicateReservations.getDuplicates($month, $property);
        },

        missing = function () {
            DojoWeb.MissingPropertyCodes.toggleGrid($property);
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
        },

        // TODO: does not work as intended to align the buttons to be on top of the corresponding columns
        alignCustomWorkflowButtons = function () {
            var reviewedCol = $('.k-grid-header thead tr th.k-header[data-field="Reviewed"]');
            var offset = reviewedCol.offset();
            if (offset) {
                $(_reviewAllSelector).css('margin-left', offset.left + 'px !important');
                $(_approveAllSelector).css('margin-left', '50px !important');
            }
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var revenueSum = 0;
                $.each($view, function (i, r) {
                    revenueSum += r.TotalRevenue;
                });
                var htmlContent = '';
                htmlContent += '(Reservations = ' + count;
                htmlContent += ',  Total Monthly Revenue = ' + DojoWeb.Template.money(revenueSum) + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        },

        setHighlightRow = function (id) {
            _highlightRow = id;
        },

        needHighlightRow = function () {
            return _highlightRow != undefined && _action != 'new';
        },

        updateRow = function (id) {
            setHighlightRow(id);
            getReservations();
        },

        unselectRow = function () {
            try {
                var $dataGrid = $grid.data('kendoGrid');
                if ($dataGrid && $dataGrid.selectable) $dataGrid.clearSelection();
                _highlightRow = undefined;
            }
            catch (e) {
            }
        },

        moveTo = function (id, state, direction) {
            var url = kendo.format('/Reservation/UpdateWorkflow?id={0}&state={1}&direction={2}', id, state, direction);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result >= 0) {
                        //DojoWeb.Notification.show('Workflow is updated successfully.');
                        if (result >= DojoWeb.RevenueWorkflow.states().approved) {
                            DojoWeb.ApprovalNoteEditor.renderNote(id, '', result);
                        }

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

        updateGridDataSource = function (id, state) {
            var ds = $grid.data('kendoGrid').dataSource.view();
            if (ds) {
                $.each(ds, function (i, item) {
                    if (item.ReservationId == id) {
                        item.ApprovalStatus = state;
                    }
                });
            }
        },

        saveFieldStatus = function (id, field, included, taxrate) {
            var url = kendo.format('/Reservation/UpdateFieldStatus?id={0}&field={1}&included={2}&taxrate={3}', id, field, included, taxrate);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result != '') {
                        // refresh to update tax amount if the field is IsTaxed
                        if (field == 'IsTaxed') getReservations();

                        //DojoWeb.Notification.show(kendo.format('{0} is set to {1} successfully.',
                        //                                        field, (included == 1 ? 'Yes' : 'No')));
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

        getApprovalNoteColumnWidth = function () {
            var grid = $grid.data('kendoGrid');
            var columnWidth =  500;
            var fieldname = 'ApprovedNote';
            $.each(grid.columns, function (i, col) {
                if (col.field == fieldname) {
                    if (grid.table[0].rows.length > 0) {
                        columnWidth = grid.table[0].rows[0].cells[i].offsetWidth;
                    }
                }
            });
            return columnWidth;
        },

        approvalNoteColumnWidth = function() {
            return _approvalNoteColumnWidth;
        },

        availablePropertyCodes = function () {
            return _availablePropertyCodes;
        },

        currentMonth = function() {
            return kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
        },

        adjustEditorIcon = function (name) {
            $($($('input[name="' + name + '"]').parent().children('span')[0]).children()[0]).html('');
        }

    return {
        init: init,
        getPropertyCode: getPropertyCode,
        getReservations: getReservations,
        availablePropertyCodes: availablePropertyCodes,
        renderAction: renderAction,
        renderDelete: renderDelete,
        renderConvert: renderConvert,
        renderCheckBox: renderCheckBox,
        moveTo: moveTo,
        updateRow: updateRow,
        unselectRow: unselectRow,
        approvalNoteColumnWidth: approvalNoteColumnWidth,
        currentMonth: currentMonth
    }
}();

DojoWeb.ApprovalNoteEditor = function () {
    var _noteTemplate = '<div id="reservation-edit-note-{0}">{1}</div>',

        _showTemplate = '<div class="revenue-left revenue-edit-note revenue-acton-icon" data-id="{0}" data-note="{2}"><i class="fa fa-pencil green"></i></div>' +
                        '<div class="revenue-left"><span class="revenue-note-label">{1}</span></div>' +
                        '<div class="clearfix"></div>',

        _editTemplate = '<div class="revenue-save-note revenue-left revenue-acton-icon" data-id="{0}" data-note="{2}"><i class="fa fa-save green"></i></div>' +
                        '<div class="revenue-left revenue-cancel-note revenue-acton-icon" data-id="{0}" data-note="{2}"><i class="fa fa-times red"></i></div>' +
                        '<div class="revenue-left"><input id="revenue-note-{0}" type="text" name="revenue-note-{0}" data-id="{0}" value="{2}" class="revenue-note-textbox" style="width:{3}" /></div>' +
                        '<div class="clearfix"></div>',

        _noteEditBase = '#reservation-edit-note-',
        _newNoteBase = '#revenue-note-',

        _callback = undefined,

        init = function (callback) {
            initEvents();
            _callback = callback;
        },

        initEvents = function (id) {
            // install edit/save/cancel events
            $('.revenue-edit-note').unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                editNote(id, $(this).data('note'));
                initEvents(id);
            });

            $('.revenue-save-note').unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                var data = $(_newNoteBase + id).val();
                var url = kendo.format('/Reservation/SaveNote?id={0}&note={1}', id, encodeURIComponent(data));
                    $.ajax({
                        type: 'POST',
                        url: url,
                        success: function (result) {
                            if (result != '') {
                                var note = data.replace(/"/g, '""');
                                var noteHtml = kendo.format(_showTemplate, id, note, data);
                                var $editNoteDiv = $(_noteEditBase + id);
                                $editNoteDiv.html(noteHtml);
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
                initEvents();
            });

            $('.revenue-cancel-note').unbind('click').on('click', function (e) {
                restoreNote($(this).data('id'), $(this).data('note'));
                initEvents();
            });

            if (id != undefined) {
                $(_newNoteBase + id).unbind('keydown').on('keydown', function (e) {
                    if (e.keyCode == 13) {
                        e.preventDefault();
                        $('.revenue-save-note').click();
                    }
                });
            }
        },

        disable = function () {
            $('.revenue-edit-note').addClass('hide');
        },

        renderNote = function (id, data, status) {
            //if (status > DojoWeb.RevenueWorkflow.states().approved) {
            if (data == null || data == undefined) data = '';
            var note = data.replace(/"/g, '""');
            var innerHtml = kendo.format(_showTemplate, id, note, data);
            return kendo.format(_noteTemplate, id, innerHtml);
            //}
            //else
            //    return '';
        },

        editNote = function (id, data) {
            var width = DojoWeb.ReservationRevenue.approvalNoteColumnWidth() - 100;
            var note = data.replace(/"/g, '""');
            var noteHtml = kendo.format(_editTemplate, id, note, data, width + 'px');
            var $editNoteDiv = $(_noteEditBase + id);
            $editNoteDiv.html(noteHtml);
        },

        restoreNote = function (id, data) {
            var note = data.replace(/"/g, '""');
            var noteHtml = kendo.format(_showTemplate, id, note, data);
            var $editNoteDiv = $(_noteEditBase + id);
            $editNoteDiv.html(noteHtml);
        }

    return {
        init: init,
        disable: disable,
        renderNote: renderNote,
        editNote: editNote,
        restoreNote: restoreNote
    }
}();

DojoWeb.ReservationForm = function () {
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
                DojoWeb.ReservationRevenue.unselectRow();
            });

            // disable fields that are not intended to be edited
            $('#PropertyCode').val(DojoWeb.ReservationRevenue.getPropertyCode());
            $('#ConfirmationCode').prop('readonly', 'true');

            if ($('#Channel').val() == 'Airbnb') $('#IsTaxed').prop('disabled', 'disabled');
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
                    _currentId = $('#ReservationId').val();
                    var formData = $form.serialize(); // this is a query string format; not json format
                    $.ajax({
                        type: 'POST',
                        url: '/Reservation/SaveRevenue',
                        data: formData,
                        success: function (result) {
                            if (result != '') {
                                DojoWeb.Plugin.closeFormDialog();
                                _currentId = parseInt(result);
                                DojoWeb.ReservationRevenue.updateRow(_currentId);
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
            invalidCount += DojoWeb.Validation.validateTextBox('#Nights', 'Nights is required.');
            invalidCount += DojoWeb.Validation.validateInputGroup('#TotalRevenue', 'Total Revenue is required.');
            if (invalidCount == 0) {
                invalidCount += DojoWeb.Validation.validateDecimal('#TotalRevenue', 'Total Revenue is an invalid number.');
                invalidCount += DojoWeb.Validation.validatePositiveNumber('#Nights', 'Nights is an invalid number.');
            }
            invalidCount += DojoWeb.Validation.validateDate('#CheckinDate', 'Checkin Date is required.');
            invalidCount += DojoWeb.Validation.validateDate('#PayoutDate', 'Payout Date is required.');
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
        saveRevenue: saveRevenue,
        refresh: refresh,
        serverError: serverError
    }
}();

DojoWeb.ReservationTetrisForm = function () {
    var $form = undefined,

        init = function (formId) {
            $form = $('#' + formId);
            installControls();
        },

        installControls = function () {
            $('#RevenueChange').unbind('click').on('click', function (e) {
                e.preventDefault();
                changeReservation();
            });

            $('#RevenueCancel').unbind('click').on('click', function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
                DojoWeb.ReservationRevenue.unselectRow();
            });

            // searchable dropdown
            $('#NewPropertyCode').kendoComboBox({
                height: 400,
                placeholder: 'Select a property...',
                filter: 'contains',
                dataTextField: 'PropertyCodeAndAddress',
                dataValueField: 'PropertyCode',
                dataSource: {
                    type: 'json',
                    //serverFiltering: true,
                    transport: {
                        read: {
                            url: '/Reservation/GetPropertyCodeWithAddress?month=' + DojoWeb.ReservationRevenue.currentMonth(),
                        }
                    }
                },
            });

            // prevent combobox to scroll the page while it is scrolling
            var widget = $('#NewPropertyCode').data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        changeReservation = function () {
            if (validate() > 0) {
                DojoWeb.Notification.show('We found some input errors.');
            }
            else {
                if ($form.valid()) {
                    var formData = $form.serialize(); // this is a query string format; not json format
                    $.ajax({
                        type: 'POST',
                        url: '/Reservation/ChangePropertyCode',
                        data: formData,
                        success: function (result) {
                            if (result != '') {
                                DojoWeb.Plugin.closeFormDialog();
                                DojoWeb.ReservationRevenue.getReservations();
                            }
                            else {
                                DojoWeb.Notification.show('Dojo encounters error while tetrising the reservation.', 'error');
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
                                var message = 'The reservation cannot be changed to database. If this problem persists, please contact Dojo support team.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (status == 'error') {
                                var message = 'There was an error changing your reservation to the database.';
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
            return invalidCount;
        }

    return {
        init: init,
        changeReservation: changeReservation,
    }
}();

DojoWeb.ReservationSplitForm = function () {
    var $form = undefined,

        init = function (formId) {
            $form = $('#' + formId);
            installControls();
        },

        installControls = function () {
            $('#RevenueChange').unbind('click').on('click', function (e) {
                e.preventDefault();
                splitReservation();
            });

            $('#RevenueCancel').unbind('click').on('click', function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
                DojoWeb.ReservationRevenue.unselectRow();
            });

            $('#splittedProperties').kendoMultiSelect({
                height: 360,
                autoBind: false, // bind list item object (not string)
                placeholder: 'Select one or more property...',
                filter: 'contains',
                dataTextField: 'Text',
                dataValueField: 'Value',
                value: [], // no initial selection
            });
        },

        splitReservation = function () {
            if (validate() > 0) {
                DojoWeb.Notification.show('We found some input errors.');
            }
            else {
                if ($form.valid()) {
                    var selectedProeprties = $('#splittedProperties').data('kendoMultiSelect').value();
                    var formData = {
                        ReservationId: $('#ReservationId').val(),
                        PropertyCode: $('#PropertyCode').val(),
                        ConfirmationCode: $('#ConfirmationCode').val(),
                        ReservationAmount: $('#ReservationAmount').val().replace(/\$|,|\)/g, '').replace('(', '-'),
                        TargetProperties: selectedProeprties
                    }
                    $.ajax({
                        type: 'POST',
                        url: '/Reservation/SplitReservation',
                        data: formData,
                        success: function (result) {
                            if (result != '') {
                                DojoWeb.Plugin.closeFormDialog();
                                DojoWeb.ReservationRevenue.getReservations();
                            }
                            else {
                                DojoWeb.Notification.show('Dojo encounters error while splitting the reservation.', 'error');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (jqXHR.status == 409) { // conflict
                                var message = 'The reservation for property "' + $('#PropertyCode').val() + '" does not exist.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 403) { // access denied
                                var message = "You don't have permission to perform this task.";
                                DojoWeb.Notification.show(message);
                            }
                            else if (jqXHR.status == 500) { // internal server error
                                var message = 'The reservation cannot be splitted. If this problem persists, please contact Dojo support team.';
                                DojoWeb.Notification.show(message);
                            }
                            else if (status == 'error') {
                                var message = 'There was an error splitting your reservation.';
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
            return invalidCount;
        }

    return {
        init: init,
        splitReservation: splitReservation,
    }
}();

DojoWeb.DuplicateReservations = function () {
    var _gridId = undefined,
        $grid = undefined,

        init = function (id) {
            _gridId = '#' + id;
            $grid = $(_gridId);
            installEvents();
        },

        installEvents = function () {
        },

        installGridEvents = function () {
        },

        getDuplicates = function ($month, $property) {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $.ajax({
                type: 'POST',
                url: '/Reservation/GetDuplicateReservations',
                data: { month: month },
                success: function (model) { // return duplicate reservation model
                    if ($.isArray(model)) {
                        toggleGrid($property, false); // show duplicate grid
                        emptyGrid();
                        var gridOptions = setupGrid();
                        var dataGrid = $grid.kendoGrid(gridOptions).data('kendoGrid');

                        dataGrid.bind('dataBound', function (e) {
                            installGridEvents();
                            //setCount();
                        });
                        var revenueSource = setupDataSource(model);
                        dataGrid.setDataSource(revenueSource);

                        // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                        // we remove the 'filter' text ad-hoc here
                        $(_gridId + ' span.k-filter').text('');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                }
            });
        },

        setupGrid = function (ds) {
            var height = $(window).height() - 300;
            return {
                height: height,
                batch: false,
                pageable: false,
                filterable: true,
                sortable: true,
                columns: [
                    { field: 'PropertyCode', title: 'Property Code', width: '150px' },
                    { field: 'ConfirmationCode', title: 'Confirmation Code', width: '200px' },
                    { field: 'GuestName', title: 'Guest Name' },
                    { field: 'CheckinDate', title: 'Checkin Date', width: '130px', format: '{0:MM/dd/yyyy}' },
                    { field: 'Nights', title: 'Nights', width: '90px', attributes: { class: 'grid-cell-align-right' } },
                    { field: 'TotalRevenue', title: 'Total Revenue', width: '140px', template: "#= DojoWeb.DuplicateReservations.duplicateColor(data.IsSameTransactionDate, DojoWeb.Template.money(TotalRevenue)) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'TransactionDate', title: 'Transaction Date', width: '150px', format: '{0:MM/dd/yyyy}' }, //, template: "#= DojoWeb.DuplicateReservations.duplicateColor(data.IsSameTransactionDate, data.TransactionDate) #" },
                    { field: 'Channel', title: 'Channel', width: '100px' },
                    { field: 'ReservationId', title: 'ReservationId', hidden: true },
                    { field: 'OwnerPayoutId', title: 'OwnerPayoutId', hidden: true },
                    { field: 'IsSameTransactionDate', title: 'IsSameTransactionDate', hidden: true },
                ],
            };
        },

        setupDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        fields: {
                            ReservationId: { type: 'number', editable: false, nullable: false },
                            OwnerPayoutId: { type: 'number', editable: false, nullable: false },
                            TransactionDate: { type: 'date', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: false, nullable: false },
                            ConfirmationCode: { type: 'string', editable: false, nullable: false },
                            GuestName: { type: 'string', editable: false, nullable: false },
                            CheckinDate: { type: 'date', editable: false, nullable: false },
                            Nights: { type: 'number', editable: false, nullable: false },
                            TotalRevenue: { type: 'number', editable: false, nullable: false },
                            Channel: { type: 'string', editable: false, nullable: false },
                            IsSameTransactionDate: { type: 'string', editable: false, nullable: false },
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('report-alert', e.errorThrown);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        emptyGrid = function () {
            $grid.empty(); // empty grid content
        },

        toggleGrid = function ($property, showReservation) {
            if (showReservation) {
                $('#duplicateReservations').addClass('hide');
                $('#missingPropertyCodes').addClass('hide');
                $('#propertyReservations').removeClass('hide');
            }
            else {
                $('#propertyReservations').addClass('hide');
                $('#missingPropertyCodes').addClass('hide');
                $('#duplicateReservations').removeClass('hide');
                $property.data('kendoComboBox').value(''); // remove property selection
            }
        },

        duplicateColor = function (flag, text) {
            if (flag == 'true' || flag == 'True') {
                return kendo.format('<span style="color:{0};">{1}</span>', 'red', text);
            }
            else {
                return kendo.format('<span style="color:{0};">{1}</span>', 'green', text);
            }
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var htmlContent = '(Total Duplciate Reservations = ' + count + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        }

    return {
        init: init,
        getDuplicates: getDuplicates,
        toggleGrid: toggleGrid,
        duplicateColor: duplicateColor
    }
}();

DojoWeb.MissingPropertyCodes = function () {
    var _gridId = undefined,
        $grid = undefined,
        _dataSource = undefined,

        init = function (id, $month) {
            _gridId = '#' + id;
            $grid = $(_gridId);
            setupDataSource($month);
            setupGrid();
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                height: height,
                batch: false,
                pageable: false,
                filterable: true,
                sortable: true,
                editable: 'inline',
                edit: function (e) {
                    if (!e.model.isNew()) {
                        // prevent cancel button to bring up delete button if the user can't do delete
                        $(_gridId + ' .k-grid-cancel').unbind('click').on('click', function () {
                            setTimeout(function () {
                                $grid.data('kendoGrid').trigger('dataBound');
                            });
                        })
                    }
                    else if (e.model.isNew()) {
                        e.model.set('ReservationId', 0);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(1)');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                columns: [
                    {
                        command: [
                            {
                                name: 'edit',
                                template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                                text: { edit: 'Edit', update: '', cancel: '' },
                            },
                        ],
                        title: 'Edit',
                        width: '90px !important',
                        hidden: false,
                    },
                    { field: 'PropertyCode', title: 'Property Code', width: '200px', editor: propertyCodeSelector },
                    { field: 'ConfirmationCode', title: 'Confirmation Code', width: '180px' },
                    { field: 'ListingTitle', title: 'Property Title' },
                    { field: 'TransactionDate', title: 'Transaction Date', width: '150px', format: '{0:MM/dd/yyyy}' },
                    { field: 'GuestName', title: 'Guest Name', width: '150px' },
                    { field: 'CheckinDate', title: 'Checkin Date', width: '150px', format: '{0:MM/dd/yyyy}' },
                    { field: 'Nights', title: 'Nights', width: '100px', attributes: { class: 'grid-cell-align-right' } },
                    { field: 'TotalRevenue', title: 'Total Revenue', width: '150px', template: '#= DojoWeb.Template.money(TotalRevenue) #', attributes: { class: 'grid-cell-align-right' } },
                    { field: 'ReservationId', title: 'ReservationId', hidden: true },
                ],
            });
        },

        setupDataSource = function ($month) {
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            _dataSource = new kendo.data.DataSource({
                transport: {
                    // does not work for kendo native CRUD operations using transport
                    //read: function (options) {
                    //    options.success(data);
                    //},
                    read: {
                        url: '/Reservation/GetMissingPropertyCodes?month=' + month,
                        type: 'get',
                        dataType: 'json'
                    },
                    update: {
                        url: '/Reservation/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].ReservationId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.ReservationId = 0;
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'ReservationId',
                        fields: {
                            ReservationId: { type: 'number', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: true, nullable: false },
                            ConfirmationCode: { type: 'string', editable: false, nullable: false },
                            ListingTitle: { type: 'string', editable: false, nullable: false },
                            TransactionDate: { type: 'date', editable: false, nullable: false },
                            GuestName: { type: 'string', editable: false, nullable: false },
                            CheckinDate: { type: 'date', editable: false, nullable: false },
                            Nights: { type: 'number', editable: false, nullable: false },
                            TotalRevenue: { type: 'number', editable: false, nullable: false },
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('report-alert', e.errorThrown);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        toggleGrid = function ($property, showReservation) {
            if (showReservation) {
                $('#missingPropertyCodes').addClass('hide');
                $('#duplicateReservations').addClass('hide');
                $('#propertyReservations').removeClass('hide');
            }
            else {
                $('#propertyReservations').addClass('hide');
                $('#duplicateReservations').addClass('hide');
                $('#missingPropertyCodes').removeClass('hide');
                $property.data('kendoComboBox').value(''); // remove property selection
            }
        },

        propertyCodeSelector = function (container, options) {
            $('<input id="propertyCodeSelector" data-bind="value:' + options.field + '" />')
            .appendTo(container)
            .kendoComboBox({
                autoWidth: true,
                placeholder: 'Select a property...',
                filter: 'contains',
                dataTextField: 'Text',
                dataValueField: 'Value',
                dataSource: DojoWeb.ReservationRevenue.availablePropertyCodes()
            });
        }

    return {
        init: init,
        toggleGrid: toggleGrid,
    }
}();
