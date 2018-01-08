"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.OtherRevenue = function () {
    var _gridId = undefined,
        $grid = undefined,
        $property = $('#revenuePropertyCode'),
        $month = $('#revenueMonth'),
        $dataCount = $('#dataCount'),
        _checkboxCallbackClass = 'revenueCheckboxUpdate', // class to trigger callback to save data
        _allowEdit = false,
        _canEdit = false,
        _canDelete = false,
        _action = undefined,

        init = function (id) {
            $grid = $('#' + id);
            _gridId = '#' + id;
            _allowEdit = $('.revenue-field-readonly').length == 0;
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
                placeholder: 'Select a property...',
                filter: 'startswith',
                dataTextField: 'PropertyCodeAndAddress',
                dataValueField: 'PropertyCode',
                minLength: 1,
                dataSource: {
                    type: 'json',
                    transport: {
                        read: {
                            url: '/OtherRevenue/GetPropertyCodeWithAddress?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy'),
                        },
                    }
                },
                template: DojoWeb.Template.propertyColorLegend(),
                dataBound: onPropertyDataBound
            });

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
                        getOtherRevenue();
                    }
                    else {
                        _canEdit = _allowEdit ? true : false;
                        getOtherRevenue();
                    }
                },
                error: function (jqXHR, status, errorThrown) { // ignore the error
                    getOtherRevenue();
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

        initGridEvents = function () {
            $('.' + _checkboxCallbackClass).unbind('click').on('click', function (e) {
                var id = $(this).data('id');
                var field = $(this).data('field');
                var included = $(this).prop('checked') == true ? 1 : 0;
                saveFieldStatus(id, field, included);
            });
        },

        getPropertyCode = function () {
            return $property ? $property.val() : '';
        },

        getOtherRevenue = function () {
            DojoWeb.Busy.show();

            var selectedMonth = $month.data('kendoDatePicker').value();
            var lastMonth = (new Date()).addMonths(-1);
            //_canEdit = _allowEdit == false ? _allowEdit : kendo.toString(selectedMonth, 'yyyy-MM-dd') >= kendo.toString(lastMonth, 'yyyy-MM-01');

            if ($grid.data('kendoGrid') != undefined) { // reload data source only to avoid stack up kendo grid events
                var month = kendo.toString(selectedMonth, 'MM/dd/yyyy');
                var propertyCode = $property.val();
                var options = {
                    type: 'GET',
                    url: kendo.format('/OtherRevenue/Retrieve?month={0}&propertyCode={1}', month, propertyCode)
                };
                $.ajax(options).done(function (result) {
                    $grid.data('kendoGrid').dataSource.data(result);
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
                var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
                var propertyCode = $property.val();
                var dataSource = setupDataSource(month, propertyCode);
                var gridOptions = setupGridOptions(dataSource);
                var dataGrid = $grid.kendoGrid(gridOptions).data('kendoGrid');
                dataGrid.bind('dataBound', function (e) {
                    initGridEvents();
                    setCount();
                    rebindComboBox();
                    ensureAllowDelete();
                    DojoWeb.Busy.hide();

                    if (!_canEdit) {
                        dataGrid.hideColumn(0);
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
                editable: 'inline',
                edit: function (e) {
                    if (e.model.isNew()) e.model.set('IncludeOnStatement', true);

                    // prevent cancel button to bring up delete button if the user can't do delete
                    $(_gridId + ' .k-grid-cancel').unbind('click').on('click', function () {
                        setTimeout(function () {
                            $grid.data('kendoGrid').trigger('dataBound');
                        });
                    })

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:first');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                batch: false,
                toolbar: _canEdit ? [{ name: 'create', text: 'New Revenue', className: 'k-newOtherRevenue' }] : null,
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
                    { field: 'OtherRevenueDate', title: 'Date', width: '150px', filterable: { multi: true }, format: '{0:MM/dd/yyyy}', required: true, template: '#= DojoWeb.Template.dateUS(OtherRevenueDate) #' },
                    { field: 'OtherRevenueDescription', title: 'Description', width: '400px',required: true },
                    { field: 'OtherRevenueAmount', title: 'Amount', width: '150px', required: true, template: "#= DojoWeb.Template.money(OtherRevenueAmount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'IncludeOnStatement', title: 'Included?', width: '110px', filterable: { multi: true }, template: "#= DojoWeb.OtherRevenue.renderCheckBox(data.OtherRevenueId, data.IncludeOnStatement)#", attributes: { class: 'grid-cell-align-center' } },
                    // workflow fields
                    { field: 'Reviewed', title: 'Review', width: '100px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.OtherRevenueId, 1, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'Approved', title: 'Approve', width: '100px', filterable: false, template: "#= DojoWeb.RevenueWorkflow.init(data.OtherRevenueId, 2, data.ApprovalStatus)#", attributes: { class: 'grid-cell-align-center' } },
                    { field: 'ApprovedNote', title: 'Approval Note' },
                    { field: 'ApprovalStatus', title: 'Approval Status', hidden: true },
                    // identity fields
                    { field: 'OtherRevenueId', title: 'Other Revenue ID', hidden: true },
                    { field: 'PropertyCode', title: 'Property Code', hidden: true },
                ],
            }
        },

        setupDataSource = function (month, propertyCode) {
            return new kendo.data.DataSource({
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<model>
                transport: {
                    read: {
                        url: kendo.format('/OtherRevenue/Retrieve?month={0}&propertyCode={1}', month, propertyCode),
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/OtherRevenue/Create',
                        type: 'Post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/OtherRevenue/Update',
                        type: 'Post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/OtherRevenue/Delete',
                        type: 'Post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].OtherRevenueId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') {
                                options.PropertyCode = getPropertyCode();
                                options.OtherRevenueId = 0;
                                options.ApprovalStatus = 0;
                                options.Reviewed = false;
                                options.Approved = false;
                            }
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                schema: {
                    model: {
                        id: 'OtherRevenueId',
                        fields: {
                            OtherRevenueId: { type: 'number', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: false, nullable: false },
                            OtherRevenueDate: { type: 'date', editable: true, nullable: false },
                            OtherRevenueDescription: { type: 'string', editable: true, nullable: false },
                            OtherRevenueAmount: { type: 'number', editable: true, nullable: false },
                            IncludeOnStatement: { type: 'boolean', editable: true, nullable: false },
                            ApprovedNote: { type: 'string', editable: true, nullable: false },
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

        removeRevenue = function (e) {
            alert('remove this revenue?');
        },

        requiredSelected = function () {
            return $property.val() != '' && $month.val() != '';
        },

        rebindComboBox = function () {
            $property.data('kendoComboBox').dataSource.transport.options.read.url =
                '/OtherRevenue/GetPropertyCodeWithAddress?month=' + kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $property.data('kendoComboBox').dataSource.read();
        },

        emptyGrid = function () {
            $grid.empty(); // empty grid content
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var revenueSum = 0;
                $.each($view, function (i, r) {
                    revenueSum += r.OtherRevenueAmount;
                });
                var htmlContent = '';
                htmlContent += '(Count = ' + count;
                htmlContent += ',  Monthly Total = ' + (revenueSum == 0 ? '$0' : DojoWeb.Template.money(revenueSum)) + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        },

        moveTo = function (id, state, direction) {
            var url = kendo.format('/OtherRevenue/UpdateWorkflow?id={0}&state={1}&direction={2}', id, state, direction);
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

        saveFieldStatus = function (id, field, included) {
            var url = kendo.format('/OtherRevenue/UpdateFieldStatus?id={0}&field={1}&included={2}', id, field, included);
            $.ajax({
                type: 'POST',
                url: url,
                success: function (result) {
                    if (result != '') {
                        // update grid datasource
                        var ds = $grid.data('kendoGrid').dataSource;
                        var dataItem = ds.get(id);
                        dataItem.set(field, included);
                        dataItem.dirty = false;
                        ds.read(); // sync the datasource and display
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
        moveTo: moveTo,
        getPropertyCode: getPropertyCode,
        renderCheckBox: renderCheckBox,
        removeRevenue: removeRevenue
    }
}();
