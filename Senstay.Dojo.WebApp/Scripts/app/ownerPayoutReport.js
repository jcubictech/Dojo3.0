"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.OwnerPayoutReport = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _height = 600,

        init = function (gridId) {
            _gridId = '#' + gridId;
            DojoWeb.Notification.init('reportNotification');
            render(); // show the grid
        },

        installEvents = function () {
            DojoWeb.OwnerPayoutReportBar.attachEvent(getReport);

            // excel export
            $('#actionBarExport').on('click', function (e) {
                e.preventDefault();
                DojoWeb.ExcelExport.download(_gridId);
            });

            // database export
            $('#actionBarDownload').on('click', function (e) {
                window.location.href = '/ExportDb/Download?db=airbnbaccount';
            });

            // event to scroll fixed grid header
            $(window).scroll(function () {
                $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
                //$(_gridId + ' .k-grid-header').css('left', originalLeft - $(this).scrollLeft());
            });

            //$(window).scroll(function () {
            //    $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
            //    //$(_gridId + ' .k-grid-header').css('left', originalLeft - $(this).scrollLeft());
            //});
        },

        render = function () {
            var dateRange = DojoWeb.OwnerPayoutReportBar.getDateRange();
            getReport(dateRange.beginDate, dateRange.endDate);
        },

        getReport = function (beginDate, endDate, hasFilter) {
            DojoWeb.Busy.show(); // wait animation is a globally available function

            $.get('/OwnerPayout/Retrieve/',
                {
                    beginDate: kendo.toString(beginDate, 'MM/dd/yyyy'),
                    endDate: kendo.toString(endDate, 'MM/dd/yyyy')
                },
                function (data) {
                    clear();
                    // init the grid
                    _dataGrid = $(_gridId).kendoGrid(configureGrid()).data('kendoGrid');
                    _dataGrid.bind('dataBound', function (e) {
                        DojoWeb.Busy.hide();
                        adjustColumnWidths();
                        setCount();
                    });
                    setCount();
                    // set data source and trigger grid display
                    _dataGrid.setDataSource(configureDataSource(data));
                    installEvents();

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.ActionAlert.fail('dojo-alert', 'There is no Airbnb account data available for the given date range.', 5000);
                        return;
                    }

                    // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                    // we remove the 'filter' text ad-hoc here
                    $(_gridId + ' span.k-filter').text('');
                })
                .error(function (errData) {
                    clear();
                    DojoWeb.Busy.hide();
                    alert('There was an error retrieving property data. Please try refreshing this page. If the issue persists please contact the tool administrator.',
                          DojoWeb.Alert.alertTypes().error);
                });
        },

        clear = function () {
            $(_gridId).empty(); // empty grid content
        },

        configureGrid = function () {
            return {
                //height: _height, // comment out to display all records
                dataSource: [],
                batch: false,
                pageable: false,
                resizable: true,
                scrollable: false,
                filterable: true,
                sortable: true,
                editable: false,
                reorderable: true,
                // this does not work in AWS
                //excelExport: function (e) {
                //    e.workbook.fileName = 'Dojo_AirbnbAccount_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx';
                //},
                excel: {
                    fileName: 'Dojo_OwnerPayout_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx',
                    proxyURL: "/proxy/save",
                    forceProxy: true
                },
                columns: [
                            { field: 'PayoutDate', title: 'PayoutDate', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'PayoutAmount', title: 'PayoutAmount', filterable: DojoWeb.Template.numberSearch() },
                            { field: 'Source', title: 'Source', filterable: DojoWeb.Template.textSearch() },
                            { field: 'AccountNumber', title: 'AccountNumber', filterable: DojoWeb.Template.textSearch() },
                            { field: 'DiscrepancyAmount', title: 'DiscrepancyAmount', filterable: DojoWeb.Template.numberSearch() },
                            { field: 'IsAmountMatched', title: 'IsAmountMatched', filterable: true },
                            { field: 'CreatedDate', title: 'CreatedDate', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'ModifiedDate', title: 'ModifiedDate', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'InputSource', title: 'InputSource', filterable: false, hidden: true },
                            { field: 'OwnerPayoutId', title: 'OwnerPayoutId', filterable: false, hidden: true },
                ],
            };
        },

        configureDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                schema: {
                    model: {
                        id: 'OwnerPayoutId',
                        fields: {
                            OwnerPayoutId: { type: 'number', editable: false, nullable: false },
                            PayoutDate: { type: 'date', editable: false, nullable: true },
                            PayoutAmount: { type: 'number', editable: false, nullable: true },
                            Source: { type: 'string', editable: false, nullable: true },
                            DiscrepancyAmount: { type: 'number', editable: false, nullable: true },
                            isAmountMatched: { type: 'number', editable: false, nullable: true },
                            CreatedDate: { type: 'date', editable: false, nullable: true },
                            ModifiedDate: { type: 'date', editable: false, nullable: true },
                            InputSource: { type: 'string', editable: false, nullable: true },
                        }
                    }
                }
            });
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-field="PayoutDate"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="PayoutAmount"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="AccountNumber"]').css('min-width', '208px');
            $(_gridId + ' th[data-field="DiscrepancyAmount"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="IsAmountMatched"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="Source"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="CreatedDate"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="ModifiedDate"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="InputSource"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="OwnerPayoutId"]').css('min-width', '200px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            var i = 1;
            $(_gridId + ' tr td:nth-child(1)').css('min-width', '100px').css('max-width', '100px');   // OwnerPayoutId
            $(_gridId + ' tr td:nth-child(2)').css('min-width', '100px');   // PayoutAmount
            $(_gridId + ' tr td:nth-child(3)').css('min-width', '100px').css('max-width', '100px');   // PayoutDate
            $(_gridId + ' tr td:nth-child(4)').css('min-width', '100px').css('max-width', '100px');   // DiscrepancyAmount
            $(_gridId + ' tr td:nth-child(5)').css('min-width', '100px').css('max-width', '100px');   // CreatedDate
            $(_gridId + ' tr td:nth-child(6)').css('min-width', '100px').css('max-width', '100px');   // ModifiedDate
            $(_gridId + ' tr td:nth-child(7)').css('min-width', '100px').css('max-width', '100px');   // Source
            $(_gridId + ' tr td:nth-child(8)').css('min-width', '100px').css('max-width', '100px');   //AccountNumber
            $(_gridId + ' tr td:nth-child(9)').css('min-width', '100px').css('max-width', '100px');   //InputSource
            $(_gridId + ' tr td:nth-child(10)').css('min-width', '100px').css('max-width', '100px');   // IsAmountMatched

        },

        setCount = function () {
            var count = $(_gridId).data('kendoGrid').dataSource.view().length;
            if (count > 0)
                $('#gridDataCount').html('(' + count + ')');
            else
                $('#gridDataCount').html('');

        }

    return {
        init: init,
        getReport: getReport,
    }
}();

DojoWeb.ExcelExport = function () {
    var _exportIndicator = false,
        $gridData = undefined,

        download = function (gridId, columnsToShow, columnsToHide) {
            customDownload(gridId, columnsToShow, columnsToHide);
        },

        customDownload = function (gridId, columnsToShow, columnsToHide) {
            if (columnsToShow != undefined) {
                $.each(columnsToShow, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.showColumn(col);
                });
            }

            if (columnsToHide != undefined) {
                $.each(columnsToHide, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.hideColumn(col);
                });
            }

            $gridData = $(gridId).data('kendoGrid');
            $gridData.saveAsExcel();
            //var dataURI = "data:text/plain;base64," + kendo.util.encodeBase64($gridData);
            //kendo.saveAs({
            //    dataURI: dataURI,
            //    fileName: filename,
            //    proxyURL: "/proxy/save",
            //    forceProxy: true
            //});

            if (columnsToShow != undefined) {
                $.each(columnsToShow, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.hideColumn(col);
                });
            }

            if (columnsToHide != undefined) {
                $.each(columnsToHide, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.showColumn(col);
                });
            }
        }

    return {
        download: download,
        customDownload: customDownload
    }
}();

// TODO: share with AirbnbAccount; they have the same code
DojoWeb.OwnerPayoutReportBar = function () {
    var _dateRangeHandle = undefined,

        install = function (beginDate, endDate) {
            _dateRangeHandle = DojoWeb.DateRange.init('beginDatePicker', 'endDatePicker', beginDate, endDate);
            DojoWeb.DateRange.initValidator('actionBarDateRange'); // need html coded properly to engage kendo date range validator
        },
        
        attachEvent = function (goAction) {
            if (goAction != undefined) {
                $('#actionBarGo').unbind('click').on('click', function (e) {
                    var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
                    goAction(dateRange.beginDate, dateRange.endDate);
                });
            }
        },

        getDateRange = function () {
            return DojoWeb.DateRange.getRange(_dateRangeHandle);
        },

        setDateRange = function (beginDate, endDate) {
            DojoWeb.DateRange.setRange(_dateRangeHandle, beginDate, endDate);
        },

        validateDateRange = function () {
            var rangeValidator = $("#actionBarDateRange").data("kendoValidator");
            return rangeValidator != undefined && rangeValidator.validate();
        }

    return {
        install: install,
        attachEvent: attachEvent,
        getDateRange: getDateRange,
        setDateRange: setDateRange,
        validateDateRange: validateDateRange
    }
}();
