"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.StatementReport = function () {
    var _gridId = undefined,
        $grid = undefined,
        $title = $('#reportTitle'),
        $dataCount = $('#dataCount'),
        $property = $('#revenuePropertyCode'),
        $month = $('#revenueMonth'),
        $type = $('input[name=reportType]'),
        $export = $('#exportReport'),
        $statementReportView = $('#reportGrid'),
        _selectedType = 'Journal',

        init = function (id) {
            _gridId = '#' + id;
            $grid = $(_gridId);
            installEvents();
            $(window).scrollTop();
            DojoWeb.Notification.init('actionAlert', 3000);
            ensureRequiredSelected();
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

            $month.unbind('change').on('change', function (e) {
                ensureRequiredSelected();
            });

            // key input monitor to start query for reservations
            $property.unbind('change').on('change', function (e) {
                ensureRequiredSelected();
            });

            $type.unbind('change').on('change', function (e) {
                if (this.checked) {
                    _selectedType = $(this).val();
                    ensureRequiredSelected();
                }
            });

            // searchable dropdown for properties
            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            $property.kendoComboBox({
                height: 400,
                placeholder: 'Select a property...',
                filter: 'contains',
                dataTextField: 'PropertyCodeWithPayoutMethod',
                dataValueField: 'PropertyCode',
                dataSource: {
                    type: 'json',
                    transport: {
                        read: {
                            url: '/Property/GetOwnerStatementPropertyList?month=' + month,
                        }
                    }
                },
            });

            $export.kendoButton({
                spriteCssClass: 'fa fa-print green',
                click: exportReport
            });

            // prevent combobox to scroll the page while it is scrolling
            var widget = $property.data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        installGridEvents = function () {
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
                    { field: 'CustomerJob', title: 'Customer Job', width: '300px' },
                    { field: 'ClassName', title: 'Class', width: '150px' },
                    { field: 'ReportDate', title: 'Report Date', width: '120px', format: '{0:MM/dd/yyyy}' },
                    { field: 'InvoiceNumber', title: 'Invoice #', width: '110px', attributes: { class: 'grid-cell-align-right' } },
                    { field: 'ItemName', title: 'Item' },
                    { field: 'Quantity', title: 'Quantity', width: '100px', attributes: { class: 'grid-cell-align-right' } },
                    { field: 'Rate', title: 'Rate', width: '100px', template: "#= DojoWeb.Template.money(Rate) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'Amount', title: 'Amount', width: '110px', template: "#= DojoWeb.Template.money(Amount) #", attributes: { class: 'grid-cell-align-right' } },
                    { field: 'PropertyCode', title: 'Property Code', width: '140px', attributes: { class: 'grid-cell-align-right' } },
                    { field: 'Vertical', title: 'Product', width: '100px', attributes: { class: 'grid-cell-align-right' } },
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
                            CustomerJob: { type: 'string', editable: false, nullable: false },
                            ClassName: { type: 'string', editable: false, nullable: false },
                            ReportDate: { type: 'date', editable: false, nullable: false },
                            InvoiceNumber: { type: 'number', editable: false, nullable: false },
                            ItemName: { type: 'string', editable: false, nullable: false },
                            Quantity: { type: 'number', editable: false, nullable: false },
                            Rate: { type: 'number', editable: false, nullable: false },
                            Amount: { type: 'number', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: false, nullable: false },
                            Vertical: { type: 'string', editable: false, nullable: false },
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

        getStatementReport = function () {
            DojoWeb.Busy.show();

            var month = kendo.toString($month.data('kendoDatePicker').value(), 'MM/dd/yyyy');
            var type = _selectedType;

            $.ajax({
                type: 'POST',
                url: '/StatementReport/Retrieve',
                data: { month: month, type: type },
                success: function (data) {
                    DojoWeb.Busy.hide();
                    if ($.isArray(data)) {
                        emptyGrid();
                        var gridOptions = setupGrid();
                        var dataGrid = $grid.kendoGrid(gridOptions).data('kendoGrid');

                        dataGrid.bind('dataBound', function (e) {
                            installGridEvents();
                            setCount();
                        });
                        var revenueSource = setupDataSource(data);
                        dataGrid.setDataSource(revenueSource);

                        // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                        // we remove the 'filter' text ad-hoc here
                        $(_gridId + ' span.k-filter').text('');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                }
            });
        },

        exportReport = function (e) {
            e.preventDefault();
            var columnsToHide = ['PropertyCode', 'Vertical'];
            DojoWeb.ExcelExport.customDownload(_gridId, [], columnsToHide);
        },

        emptyGrid = function () {
            $grid.empty(); // empty grid content
        },

        setCount = function () {
            var $view = $grid.data('kendoGrid').dataSource.view();
            var count = $view.length;
            if (count > 0) {
                var htmlContent = '(Total Report Records = ' + count + ')';
                $dataCount.html(htmlContent);
            }
            else
                $dataCount.html('');
        },

        ensureRequiredSelected = function () {
            if ($month.val() != '') { // && $property.val() != '') {
                $title.html(_selectedType + ' Report');
                $export.data('kendoButton').enable(true);
                getStatementReport();
            }
            else {
                $title.html('Statement Report');
                $export.data('kendoButton').enable(false);
            }
        }

    return {
        init: init,
    }
}();
