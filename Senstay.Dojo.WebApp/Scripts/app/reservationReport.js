"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.ReservationReport = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _height = 600,

        init = function (gridId) {
            _gridId = '#' + gridId;
            DojoWeb.Notification.init('reportNotification');
            render(); // show the grid
        },

        installEvents = function () {
            DojoWeb.ReservationReportBar.attachEvent(getReport);

            // show count

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
            var dateRange = DojoWeb.ReservationReportBar.getDateRange();
            getReport(dateRange.beginDate, dateRange.endDate);
        },

        getReport = function (beginDate, endDate, hasFilter) {
            DojoWeb.Busy.show(); // wait animation is a globally available function

            $.get('/Reservation/Retrieve/',
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
                    fileName: 'Dojo_Reservation_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx',
                    proxyURL: "/proxy/save",
                    forceProxy: true
                },
                columns: [
                            //{ field: 'Password', title: 'Password', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            //{ field: 'Gmailpassword', title: 'Gmail Password', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'ReservationId', title: 'ReservationId', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'OwnerPayoutId', title: 'OwnerPayoutId', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'PropertyCode', title: 'Property Code', filterable: DojoWeb.Template.textSearch() },
                            { field: 'ConfirmationCode', title: 'Confirmation Code', filterable: DojoWeb.Template.textSearch() },
                            { field: 'TransactionDate', title: 'Transaction Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'TotalRevenue', title: 'Total Revenue', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Channel', title: 'Channel', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Source', title: 'Source', filterable: DojoWeb.Template.textSearch() },
                            { field: 'GuestName', title: 'Guest Name', filterable: DojoWeb.Template.textSearch() },
                            { field: 'CheckinDate', title: 'Checkin Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'CheckoutDate', title: 'Checkout Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'Nights', title: 'Nights', filterable: DojoWeb.Template.textSearch() },
                            { field: 'IncludeOnStatement', title: 'Include On Statement', filterable: DojoWeb.Template.textSearch() },
                            { field: 'IsFutureBooking', title: 'Is Future Booking', filterable: DojoWeb.Template.textSearch() },
                            { field: 'ApprovalStatus', title: 'Approval Status', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Currency', title: 'Currency', filterable: DojoWeb.Template.textSearch() },
                            { field: 'CreatedDate', title: 'Created Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'CreatedBy', title: 'Created By', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'ModifiedDate', title: 'Modified Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'ModifiedBy', title: 'Modified By', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'InputSource', title: 'Input Source', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'LocalTax', title: 'Local Tax', filterable: DojoWeb.Template.textSearch() },
                            { field: 'DamageWaiver', title: 'Damage Waiver', filterable: DojoWeb.Template.textSearch() },
                            { field: 'AdminFee', title: 'Admin Fee', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PlatformFee', title: 'Platform Fee', filterable: DojoWeb.Template.textSearch() },
                            { field: 'TaxRate', title: 'Tax Rate', filterable: DojoWeb.Template.textSearch() },
                            { field: 'ListingTitle', title: 'Listing Title', filterable: DojoWeb.Template.textSearch() },

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
                            TransactionDate: { type: 'date', editable: false, nullable: true },
                            CheckinDate: { type: 'date', editable: false, nullable: true },
                            CheckoutDate: { type: 'date', editable: false, nullable: true },
                            CreatedDate: { type: 'date', editable: false, nullable: true },
                            ModifiedDate: { type: 'date', editable: false, nullable: true },
                        }
                    }
                }
            });
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-field="ReservationId"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="OwnerPayoutId"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="PropertyCode"]').css('min-width', '118px');
            $(_gridId + ' th[data-field="ConfirmationCode"]').css('min-width', '145px');
            $(_gridId + ' th[data-field="TransactionDate"]').css('min-width', '136px');
            $(_gridId + ' th[data-field="TotalRevenue"]').css('min-width', '118px');
            $(_gridId + ' th[data-field="Channel"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Source"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="GuestName"]').css('min-width', '110px');
            $(_gridId + ' th[data-field="CheckinDate"]').css('min-width', '110px');
            $(_gridId + ' th[data-field="CheckoutDate"]').css('min-width', '118px');
            $(_gridId + ' th[data-field="Nights"]').css('min-width', '75px');
            $(_gridId + ' th[data-field="IncludeOnStatement"]').css('min-width', '170px');
            $(_gridId + ' th[data-field="IsFutureBooking"]').css('min-width', '145px');
            $(_gridId + ' th[data-field="ApprovalStatus"]').css('min-width', '125px');
            $(_gridId + ' th[data-field="Currency"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="CreatedDate"]').css('min-width', '110px');
            $(_gridId + ' th[data-field="CreatedBy"]').css('min-width', '110px');
            $(_gridId + ' th[data-field="ModifiedDate"]').css('min-width', '115px');
            $(_gridId + ' th[data-field="ModifiedBy"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="InputSource"]').css('min-width', '400px');
            $(_gridId + ' th[data-field="LocalTax"]').css('min-width', '90px');
            $(_gridId + ' th[data-field="DamageWaiver"]').css('min-width', '127px');
            $(_gridId + ' th[data-field="AdminFee"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="PlatformFee"]').css('min-width', '112px');
            $(_gridId + ' th[data-field="TaxRate"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="ListingTitle"]').css('min-width', '180px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            var i = 1;
            $(_gridId + ' tr td:nth-child(1)').css('min-width', '100px').css('max-width', '100px');   // ReservationId
            $(_gridId + ' tr td:nth-child(2)').css('min-width', '100px').css('max-width', '100px');   // OwnerPayoutId
            $(_gridId + ' tr td:nth-child(3)').css('min-width', '118px').css('max-width', '118px');   // PropertyCode
            $(_gridId + ' tr td:nth-child(4)').css('min-width', '145px').css('max-width', '145px');   // ConfirmationCode
            $(_gridId + ' tr td:nth-child(5)').css('min-width', '136px').css('max-width', '136px');   // TransactionDate
            $(_gridId + ' tr td:nth-child(6)').css('min-width', '118px').css('max-width', '118px');   // TotalRevenue
            $(_gridId + ' tr td:nth-child(7)').css('min-width', '80px').css('max-width', '80px');   // Channel
            $(_gridId + ' tr td:nth-child(8)').css('min-width', '200px').css('max-width', '200px');   // Source
            $(_gridId + ' tr td:nth-child(9)').css('min-width', '110px').css('max-width', '110px');   // GuestName
            $(_gridId + ' tr td:nth-child(10)').css('min-width', '110px').css('max-width', '110px');   // CheckinDate
            $(_gridId + ' tr td:nth-child(11)').css('min-width', '118px').css('max-width', '118px');   // CheckoutDate
            $(_gridId + ' tr td:nth-child(12)').css('min-width', '75px').css('max-width', '75px');   // Nights
            $(_gridId + ' tr td:nth-child(13)').css('min-width', '170px').css('max-width', '170px');   // IncludeOnStatement
            $(_gridId + ' tr td:nth-child(14)').css('min-width', '145px').css('max-width', '145px');   // IsFutureBooking
            $(_gridId + ' tr td:nth-child(15)').css('min-width', '125px').css('max-width', '125px');   // ApprovalStatus
            $(_gridId + ' tr td:nth-child(16)').css('min-width', '81px').css('max-width', '81px');   // Currency
            $(_gridId + ' tr td:nth-child(17)').css('min-width', '110px').css('max-width', '110px');   // CreatedDate
            $(_gridId + ' tr td:nth-child(18)').css('min-width', '110px').css('max-width', '110px');   // CreatedBy
            $(_gridId + ' tr td:nth-child(19)').css('min-width', '115px').css('max-width', '115px');   // ModifiedDate
            $(_gridId + ' tr td:nth-child(20)').css('min-width', '110px').css('max-width', '110px');   // ModifiedBy
            $(_gridId + ' tr td:nth-child(21)').css('min-width', '400px').css('max-width', '400px');   // InputSource
            $(_gridId + ' tr td:nth-child(22)').css('min-width', '90px').css('max-width', '90px');   // LocalTax
            $(_gridId + ' tr td:nth-child(23)').css('min-width', '127px').css('max-width', '127px');   // DamageWaiver
            $(_gridId + ' tr td:nth-child(24)').css('min-width', '100px').css('max-width', '100px');   // AdminFee
            $(_gridId + ' tr td:nth-child(25)').css('min-width', '112px').css('max-width', '112px');   // PlatformFee
            $(_gridId + ' tr td:nth-child(26)').css('min-width', '80px').css('max-width', '80px');   // TaxRate
            $(_gridId + ' tr td:nth-child(27)').css('min-width', '180px').css('max-width', '180px');   // ListingTitle

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
DojoWeb.ReservationReportBar = function () {
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
