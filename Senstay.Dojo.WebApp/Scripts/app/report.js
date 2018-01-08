"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.PropertyReport = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _height = 600,

        init = function (gridId) {
            _gridId = '#' + gridId;
            DojoWeb.Notification.init('reportNotification');
            render(); // show the grid
        },

        installEvents = function () {
            DojoWeb.ReportActionBar.attachEvent(getReport);

            // excel export
            $('#actionBarExport').on('click', function (e) {
                e.preventDefault();
                DojoWeb.ExcelExport.download(_gridId);
            });

            // database export
            $('#actionBarDownload').on('click', function (e) {
                window.location.href = '/ExportDb/Download?db=property';
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
            var dateRange = DojoWeb.ReportActionBar.getDateRange();
            getReport(dateRange.beginDate, dateRange.endDate);
        },

        getReport = function (beginDate, endDate, hasFilter) {
            DojoWeb.Busy.show(); // wait animation is a globally available function

            $.get('/Report/RetrieveProperties/',
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
                    // set data source and trigger grid display
                    _dataGrid.setDataSource(configureDataSource(data));
                    installEvents();

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.ActionAlert.fail('dojo-alert', 'There is no property data available for the given date range.', 5000);
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
                dataSource: [],
                batch: false,
                pageable: false,
                resizable: true,
                scrollable: false,
                filterable: true,
                sortable: false,
                editable: false,
                reorderable: true,
                // this does not work in AWS
                //excelExport: function(e) {
                //    e.workbook.fileName = 'Dojo_Property_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx';
                //},
                // this also works
                excel: {
                    fileName: 'Dojo_Property_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx',
                    proxyURL: "/proxy/save",
                    forceProxy: true
                },
                columns: [
                            { field: 'CreatedDate', title: 'Created Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'AirBnBHomeName', title: 'Airbnb Listing Title', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PropertyCode', title: 'Property Code', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PropertyStatus', title: 'Property Status', filterable: { multi: true }, template: "#= DojoWeb.Template.active(data.PropertyStatus) #" },
                            { field: 'Vertical', title: 'Vertical', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Vertical) #" },
                            { field: 'Owner', title: 'Owner Contact', filterable: true },
                            { field: 'NeedsOwnerApproval', title: 'Owner Approval?', filterable: { multi: true }, template: "#= DojoWeb.Template.boolean(data.NeedsOwnerApproval) #" },
                            { field: 'ListingStartDate', title: 'Listing Start', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'StreamlineHomeName', title: 'Streamline Listing Title', filterable: DojoWeb.Template.textSearch() },
                            { field: 'StreamlineUnitID', title: 'Streamline Home ID', filterable: DojoWeb.Template.textSearch() },

                            { field: 'Account', title: 'Airbnb Account', filterable: DojoWeb.Template.textSearch() },
                            { field: 'City', title: 'City', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.City) #" },
                            { field: 'Market', title: 'Market', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Market) #" },
                            { field: 'State', title: 'State', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.State) #" },
                            { field: 'Area', title: 'Area', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Area) #" },
                            { field: 'Neighborhood', title: 'Neighborhood', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Neighborhood) #" },
                            { field: 'BookingGuidelines', title: 'Booking Guidelines', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.BookingGuidelines) #" },
                            { field: 'Floor', title: 'Floor', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Floor) #" },
                            { field: 'Bedrooms', title: 'Bedrooms', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Bedrooms) #" },
                            { field: 'Bathrooms', title: 'Bathrooms', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Bathrooms) #" },

                            { field: 'BedsDescription', title: 'Beds Description', filterable: true },
                            { field: 'MaxOcc', title: 'Max Occ', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.MaxOcc) #" },
                            { field: 'StdOcc', title: 'Std Occ', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.StdOcc) #" },
                            { field: 'Elevator', title: 'Elevator', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Elevator) #" },
                            { field: 'A_C', title: 'A/C', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.A_C) #" },
                            { field: 'Parking', title: 'Parking', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Parking) #" },
                            { field: 'Pool', title: 'Pool', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Pool) #" },
                            { field: 'WiFiNetwork', title: 'Wi-Fi Network', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.WiFiNetwork) #" },
                            { field: 'WiFiPassword', title: 'Wi-Fi Password', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.WiFiPassword) #" },
                            { field: 'Ownership', title: 'Ownership', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Ownership) #" },

                            { field: 'MonthlyRent', title: 'Monthly Rent', filterable: true },
                            { field: 'DailyRent', title: 'Daily Rent', filterable: true },
                            { field: 'CleaningFees', title: 'Cleaning Fees', filterable: true },
                            { field: 'Currency', title: 'Currency', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Currency) #" },
                            { field: 'AIrBnBID', title: 'AirBnb ID', filterable: true, template: DojoWeb.Template.textSearch()},
                            { field: 'AirbnbiCalexportlink', title: 'Operations Contact', filterable: false },
                            { field: 'Amenities', title: 'Checklist', filterable: false, template: "#= DojoWeb.Template.link(data.Amenities, 'Checklist Link') #" },
                            { field: 'Zipcode', title: 'Zipcode', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Zipcode) #" },
                            { field: 'Address', title: 'Address', filterable: DojoWeb.Template.textSearch() },
                            { field: 'CheckInType', title: 'CheckIn Type', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.CheckInType) #" },

                            { field: 'OldListingTitle', title: 'Old Listing Title', filterable: true },
                            { field: 'GoogleDrivePicturesLink', title: 'Copy Document', filterable: false, template: "#= DojoWeb.Template.link(data.GoogleDrivePicturesLink, 'Picture Link') #" },
                            { field: 'SquareFootage', title: 'Square Footage', filterable: true },
                            { field: 'SecurityDeposit', title: 'Security Deposit', filterable: true },
                            { field: 'AirBnb', title: 'AirBnb Post', filterable: false, template: "#= DojoWeb.Template.makelink(data.AIrBnBID, 'AirBnb Link', 'https://www.airbnb.com/rooms/') #" },
                            { field: 'Inactive', title: 'Inactive', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'Dead', title: 'Dead', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'BeltDesignation', title: 'Belt Designation', filterable: { multi: true }, template: "#= DojoWeb.Template.belt(data.BeltDesignation) #" },

                            //===========================================================================================
                            // These fields are obsolete; to be removed after they are deleted from schema.
                            //===========================================================================================
                            //{ field: 'OutstandingBalance', title: 'Outstanding Balance', filterable: true },
                            //{ field: 'OwnerEntity', title: 'Owner Entity', filterable: DojoWeb.Template.textSearch() },
                            //{ field: 'OwnerPayout', title: 'Owner Payout', filterable: DojoWeb.Template.textSearch() },
                            //{ field: 'PaymentEmail', title: 'Payment Email', filterable: DojoWeb.Template.textSearch() },

                            //{ field: 'Password', title: 'Password', filterable: true, hidden: true},
                            //{ field: 'PendingContractDate', title: 'Pending Contract Date', filterable: true, hidden: true },
                            //{ field: 'PendingOnboardingDate', title: 'Pending Onboarding Date', filterable: true, hidden: true },
                            //{ field: 'HomeAway', title: 'HomeAway Post', filterable: false, template: "#= DojoWeb.Template.link(data.HomeAway, 'HomeAway Link') #", hidden: true },
                            //{ field: 'FlipKey', title: 'FlipKey Link', filterable: false, hidden: true },
                            //{ field: 'Expedia', title: 'Expedia Link', filterable: false, hidden: true },
                ],
            };
        },

        configureDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                schema: {
                    model: {
                        id: 'PropertyCode',
                        fields: {
                            PropertyCode: { type: 'string', editable: false, nullable: true },
                            //NeedsOwnerApproval: { type: 'number', editable: false, nullable: true },
                            ListingStartDate: { type: 'date', editable: false, nullable: true },
                            //Bedrooms: { type: 'number', editable: false, nullable: true },
                            //Bathrooms: { type: 'number', editable: false, nullable: true },
                            //MaxOcc: { type: 'number', editable: false, nullable: true },
                            //StdOcc: { type: 'number', editable: false, nullable: true },
                            //Ownership: { type: 'number', editable: false, nullable: true },
                            //MonthlyRent: { type: 'number', editable: false, nullable: true },
                            //DailyRent: { type: 'number', editable: false, nullable: true },
                            //CleaningFees: { type: 'number', editable: false, nullable: true },
                            //SquareFootage: { type: 'number', editable: false, nullable: true },
                            //InquiryLeadApproval: { type: 'number', editable: false, nullable: true },
                            //RevTeam2xApproval: { type: 'number', editable: false, nullable: true },
                            PendingContractDate: { type: 'date', editable: false, nullable: true },
                            PendingOnboardingDate: { type: 'date', editable: false, nullable: true },
                            Inactive: { type: 'date', editable: false, nullable: true },
                            Dead: { type: 'date', editable: false, nullable: true },
                            CreatedDate: { type: 'date', editable: false, nullable: true },
                        }
                    }
                }
            });
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-field="CreatedDate"]').css('min-width', '100px').css('max-width', '100px');
            $(_gridId + ' th[data-field="AirBnBHomeName"]').css('min-width', '220px').css('max-width', '220px');
            $(_gridId + ' th[data-field="PropertyCode"]').css('min-width', '110px').css('max-width', '110px');
            $(_gridId + ' th[data-field="PropertyStatus"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Vertical"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Owner"]').css('min-width', '120px').css('max-width', '120px');
            $(_gridId + ' th[data-field="NeedsOwnerApproval"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="ListingStartDate"]').css('min-width', '100px').css('max-width', '100px');
            $(_gridId + ' th[data-field="StreamlineHomeName"]').css('min-width', '200px').css('max-width', '200px');
            $(_gridId + ' th[data-field="StreamlineUnitID"]').css('min-width', '100px').css('max-width', '100px');

            $(_gridId + ' th[data-field="Account"]').css('min-width', '240px').css('max-width', '240px');
            $(_gridId + ' th[data-field="City"]').css('min-width', '90px').css('max-width', '90px');
            $(_gridId + ' th[data-field="Market"]').css('min-width', '90px').css('max-width', '90px');
            $(_gridId + ' th[data-field="State"]').css('min-width', '90px').css('max-width', '90px');
            $(_gridId + ' th[data-field="Area"]').css('min-width', '90px').css('max-width', '90px');
            $(_gridId + ' th[data-field="Neighborhood"]').css('min-width', '110px').css('max-width', '110px');
            $(_gridId + ' th[data-field="BookingGuidelines"]').css('min-width', '250px').css('max-width', '250px');
            $(_gridId + ' th[data-field="Floor"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Bedrooms"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Bathrooms"]').css('min-width', '80px').css('max-width', '80px');

            $(_gridId + ' th[data-field="BedsDescription"]').css('min-width', '180px').css('max-width', '180px');
            $(_gridId + ' th[data-field="MaxOcc"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="StdOcc"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Elevator"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="A_C"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Parking"]').css('min-width', '120px').css('max-width', '120px');
            $(_gridId + ' th[data-field="Pool"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="WiFiNetwork"]').css('min-width', '150px').css('max-width', '150px');
            $(_gridId + ' th[data-field="WiFiPassword"]').css('min-width', '150px').css('max-width', '150px');
            $(_gridId + ' th[data-field="Ownership"]').css('min-width', '80px').css('max-width', '80px');

            $(_gridId + ' th[data-field="MonthlyRent"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="DailyRent"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="CleaningFees"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Currency"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="AIrBnBID"]').css('min-width', '120px').css('max-width', '120px');
            $(_gridId + ' th[data-field="AirbnbiCalexportlink"]').css('min-width', '330px').css('max-width', '330px');
            $(_gridId + ' th[data-field="Amenities"]').css('min-width', '100px').css('max-width', '100px');
            $(_gridId + ' th[data-field="Zipcode"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Address"]').css('min-width', '180px').css('max-width', '180px');
            $(_gridId + ' th[data-field="CheckInType"]').css('min-width', '180px').css('max-width', '180px');

            $(_gridId + ' th[data-field="OldListingTitle"]').css('min-width', '240px').css('max-width', '240px');
            $(_gridId + ' th[data-field="GoogleDrivePicturesLink"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="SquareFootage"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="SecurityDeposit"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="AirBnb"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Inactive"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="Dead"]').css('min-width', '80px').css('max-width', '80px');
            $(_gridId + ' th[data-field="BeltDesignation"]').css('min-width', '120px').css('max-width', '120px');
            //===========================================================================================
            // These fields are obsolete; to be removed after they are deleted from schema.
            //===========================================================================================
            //$(_gridId + ' th[data-field="OutstandingBalance"]').css('min-width', '120px').css('max-width', '120px');
            //$(_gridId + ' th[data-field="OwnerEntity"]').css('min-width', '120px').css('max-width', '120px');
            //$(_gridId + ' th[data-field="OwnerPayout"]').css('min-width', '120px').css('max-width', '120px');
            //$(_gridId + ' th[data-field="PaymentEmail"]').css('min-width', '120px').css('max-width', '120px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            var i = 1;
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '117px').css('max-width', '117px');   // CreatedDate
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '220px').css('max-width', '220px');    // AirBnBHomeName
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '110px').css('max-width', '110px');   // PropertyCode
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // PropertyStatus
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Vertical
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // Owner
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // NeedsOwnerApproval
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '98px').css('max-width', '98px');   // ListingStartDate
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '200px').css('max-width', '200px');   // StreamlineHomeName
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '100px').css('max-width', '100px');   // StreamlineUnitID

            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '240px').css('max-width', '240px');   // Account
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '90px').css('max-width', '90px');   // City
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '90px').css('max-width', '90px');   // Market
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '90px').css('max-width', '90px');   // State
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '90px').css('max-width', '90px');   // Area
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '110px').css('max-width', '110px');   // Neighborhood
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '250px').css('max-width', '250px');   // BookingGuidelines
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Floor
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Bathrooms
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Bedrooms

            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '180px');   // BedsDescription
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // MaxOcc
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // StdOcc
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Elevator
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // A_C
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // Parking
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Pool
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '150px').css('max-width', '150px');   // WiFiNetwork
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '150px').css('max-width', '150px');   // WiFiPassword
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Ownership

            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // MonthlyRent
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // DailyRent
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // CleaningFees
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Currency
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // AIrBnBID
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '330px').css('max-width', '330px');   // AirbnbiCalexportlink
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '100px').css('max-width', '100px');   // Amenities
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Zipcode
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '180px').css('max-width', '180px');   // Address
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '180px').css('max-width', '180px');   // CheckInType

            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '240px').css('max-width', '240px');   // OldListingTitle
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // GoogleDrivePicturesLink
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // SquareFootage
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // SecurityDeposit
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // AirBnb
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Inactive
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '80px').css('max-width', '80px');   // Dead
            $(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // BeltDesignation
            //===========================================================================================
            // These fields are obsolete; to be removed after they are deleted from schema.
            //===========================================================================================
            //$(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // OutstandingBalance
            //$(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // OwnerEntity
            //$(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // OwnerPayout
            //$(_gridId + ' tr td:nth-child(' + (i++) + ')').css('min-width', '120px').css('max-width', '120px');   // PaymentEmail
        },

        setCount = function () {
            var count = $(_gridId).data('kendoGrid').dataSource.view().length;
            if (count > 0)
                $('#propertyCount').html('(' + count + ')');
            else
                $('#propertyCount').html('');

        }

    return {
        init: init,
        getReport: getReport,
    }
}();

DojoWeb.AirbnbAccountReport = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _height = 600,

        init = function (gridId) {
            _gridId = '#' + gridId;
            DojoWeb.Notification.init('reportNotification');
            render(); // show the grid
        },

        installEvents = function () {
            DojoWeb.ReportActionBar.attachEvent(getReport);

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
            var dateRange = DojoWeb.ReportActionBar.getDateRange();
            getReport(dateRange.beginDate, dateRange.endDate);
        },

        getReport = function (beginDate, endDate, hasFilter) {
            DojoWeb.Busy.show(); // wait animation is a globally available function

            $.get('/Report/RetrieveAirbnbAccounts/',
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
                    fileName: 'Dojo_AirbnbAccount_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx',
                    proxyURL: "/proxy/save",
                    forceProxy: true
                },
                columns: [
                            //{ field: 'Id', title: 'ID', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            //{ field: 'Password', title: 'Password', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            //{ field: 'Gmailpassword', title: 'Gmail Password', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'Email', title: 'Email', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.spacing(data.Email) #" },
                            { field: 'Status', title: 'Status', filterable: { multi: true }, template: "#= DojoWeb.Template.active(data.Status) #" },
                            { field: 'DateAdded', title: 'Date Added', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'SecondaryAccountEmail', title: 'Secondary Account Email', filterable: DojoWeb.Template.textSearch() },
                            { field: 'AccountAdmin', title: 'Account Admin', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.AccountAdmin) #" },
                            { field: 'Vertical', title: 'Vertical', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Vertical) #" },
                            { field: 'Owner_Company', title: 'Owner Company', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Name', title: 'Name', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PhoneNumber1', title: 'Phone Number 1', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PhoneNumberOwner', title: 'Phone Number Owner', filterable: DojoWeb.Template.textSearch() },
                            { field: 'DOB1', title: 'DOB1', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'Payout_Method', title: 'Payout Method', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PointofContact', title: 'Point Of Contact', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PhoneNumber2', title: 'Phone Number 2', filterable: DojoWeb.Template.textSearch() },
                            { field: 'DOB2', title: 'DOB2', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'EmailAddress', title: 'Email Address', filterable: DojoWeb.Template.textSearch() },
                            { field: 'ActiveListings', title: 'Active Listings', filterable: DojoWeb.Template.numberSearch() },
                            { field: 'Pending_Onboarding', title: 'Pending Onboarding', filterable: DojoWeb.Template.numberSearch() },
                            { field: 'In_activeListings', title: 'Inactive Listings', filterable: DojoWeb.Template.numberSearch() },
                            { field: 'ofListingsinLAMarket', title: 'Of Listing In LA Market', filterable: DojoWeb.Template.numberSearch() },
                            { field: 'ofListingsinNYCMarket', title: 'Of Listing In NY Market', filterable: DojoWeb.Template.numberSearch() },
                            { field: 'ProxyIP', title: 'Proxy IP', filterable: DojoWeb.Template.textSearch() },
                            { field: 'C2ndProxyIP', title: 'C 2nd Proxy IP', filterable: DojoWeb.Template.textSearch() },
                ],
            };
        },

        configureDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                schema: {
                    model: {
                        id: 'Id',
                        fields: {
                            Id: { type: 'number', editable: false, nullable: false },
                            DateAdded: { type: 'date', editable: false, nullable: true },
                            DOB1: { type: 'date', editable: false, nullable: true },
                            DOB2: { type: 'date', editable: false, nullable: true },
                            ActiveListings: { type: 'number', editable: false, nullable: true },
                            Pending_Onboarding: { type: 'number', editable: false, nullable: true },
                            In_activeListings: { type: 'number', editable: false, nullable: true },
                            ofListingsinLAMarket: { type: 'number', editable: false, nullable: true },
                            ofListingsinNYCMarket: { type: 'number', editable: false, nullable: true },
                        }
                    }
                }
            });
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-field="Email"]').css('min-width', '263px');
            $(_gridId + ' th[data-field="Status"]').css('min-width', '65px');
            $(_gridId + ' th[data-field="DateAdded"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="SecondaryAccountEmail"]').css('min-width', '250px');
            $(_gridId + ' th[data-field="AccountAdmin"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="Vertical"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="Owner_Company"]').css('min-width', '130px');
            $(_gridId + ' th[data-field="Name"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="PhoneNumber1"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="PhoneNumberOwner"]').css('min-width', '168px');
            $(_gridId + ' th[data-field="DOB1"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="Payout_Method"]').css('min-width', '154px');
            $(_gridId + ' th[data-field="PointofContact"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="PhoneNumber2"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="DOB2"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="EmailAddress"]').css('min-width', '193px');
            $(_gridId + ' th[data-field="ActiveListings"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="Pending_Onboarding"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="In_activeListings"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="ofListingsinLAMarket"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="ofListingsinNYCMarket"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="ProxyIP"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="C2ndProxyIP"]').css('min-width', '150px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            var i = 1;
            $(_gridId + ' tr td:nth-child(1)').css('min-width', '280px');   // Email
            $(_gridId + ' tr td:nth-child(2)').css('min-width', '65px');    // Status
            $(_gridId + ' tr td:nth-child(3)').css('min-width', '100px');   // Date Added
            $(_gridId + ' tr td:nth-child(4)').css('min-width', '250px');   // 2nd Email
            $(_gridId + ' tr td:nth-child(5)').css('min-width', '120px');   // AccountAdmin
            $(_gridId + ' tr td:nth-child(6)').css('min-width', '120px');   // Vertical
            $(_gridId + ' tr td:nth-child(7)').css('min-width', '130px');   // Owner_Company
            $(_gridId + ' tr td:nth-child(8)').css('min-width', '120px');   // Name
            $(_gridId + ' tr td:nth-child(9)').css('min-width', '120px');   // PhoneNumber1
            $(_gridId + ' tr td:nth-child(10)').css('min-width', '167px');   // PhoneNumberOwner
            $(_gridId + ' tr td:nth-child(11)').css('min-width', '120px');   // DOB1
            $(_gridId + ' tr td:nth-child(12)').css('min-width', '154px');   // Payout_Method
            $(_gridId + ' tr td:nth-child(13)').css('min-width', '140px');   // PointofContact
            $(_gridId + ' tr td:nth-child(14)').css('min-width', '140px');   // PhoneNumber2
            $(_gridId + ' tr td:nth-child(15)').css('min-width', '120px');   // DOB2
            $(_gridId + ' tr td:nth-child(16)').css('min-width', '193px');   // EmailAddress
            $(_gridId + ' tr td:nth-child(17)').css('min-width', '140px');   // ActiveListings
            $(_gridId + ' tr td:nth-child(18)').css('min-width', '160px');   // Pending_Onboarding
            $(_gridId + ' tr td:nth-child(19)').css('min-width', '140px');   // In_activeListings
            $(_gridId + ' tr td:nth-child(20)').css('min-width', '160px');   // ofListingsinLAMarket
            $(_gridId + ' tr td:nth-child(21)').css('min-width', '160px');   // ofListingsinNYCMarket
            $(_gridId + ' tr td:nth-child(22)').css('min-width', '150px');   // ProxyIP
            $(_gridId + ' tr td:nth-child(23)').css('min-width', '150px');   // C2ndProxyIP
        },

        setCount = function () {
            var count = $(_gridId).data('kendoGrid').dataSource.view().length;
            if (count > 0)
                $('#AirbnbAccountCount').html('(' + count + ')');
            else
                $('#AirbnbAccountCount').html('');

        }

    return {
        init: init,
        getReport: getReport,
    }
}();

DojoWeb.InquiryReport = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _height = 600,

        init = function (gridId) {
            _gridId = '#' + gridId;
            DojoWeb.Notification.init('reportNotification');
            var beginDate = (7).days().ago();
            var endDate = Date.today();
            DojoWeb.ReportActionBar.setDateRange(beginDate, endDate);

            // show the grid
            render();
        },

        installEvents = function () {
            DojoWeb.ReportActionBar.attachEvent(getReport);

            // excel export
            $('#actionBarExport').on('click', function (e) {
                e.preventDefault();
                DojoWeb.ExcelExport.download(_gridId);
            });

            // database export
            $('#actionBarDownload').on('click', function (e) {
                window.location.href = '/ExportDb/Download?db=inquiry';
                //$.ajax({
                //    type: 'POST',
                //    url: '/ExportDb/Download',
                //    data: { db: 'inquiry' },
                //    success: function (result) {
                //    },
                //    error: function (jqXHR, status, errorThrown) {
                //        if (status == 'error') {
                //            DojoWeb.ActionAlert.fail('dojo-alert', 'There was an error downloading Airbnb Account database to csv file.');
                //        }
                //    }
                //});
            });

            // event to scroll fixed grid header
            $(window).scroll(function () {
                $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
                //$(_gridId + ' .k-grid-header').css('left', originalLeft - $(this).scrollLeft());
            });
        },

        render = function () {
            var dateRange = DojoWeb.ReportActionBar.getDateRange();
            getReport(dateRange.beginDate, dateRange.endDate);
        },

        getReport = function (beginDate, endDate, hasFilter) {
            DojoWeb.Busy.show(); // wait animation is a globally available function

            $.get('/Report/RetrieveInquiries/',
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
                        toggleDashboard(true);
                        adjustColumnWidths();
                        //readjustHeaderWidths();
                        setCount();
                    });
                    // set data source and trigger grid display
                    _dataGrid.setDataSource(configureDataSource(data));
                    installEvents();

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.ActionAlert.fail('dojo-alert', 'There is no property data available for the given date range.', 5000);
                        return;
                    }

                    // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                    // we remove the 'filter' text ad-hoc here
                    $(_gridId + ' span.k-filter').text('');
                })
                .error(function (errData) {
                    clear();
                    DojoWeb.Busy.hide();
                    alert('There was an error retrieving inquiry data. Please try refreshing this page. If the issue persists please contact the tool administrator.',
                          DojoWeb.Alert.alertTypes().error);
                });
        },

        clear = function () {
            toggleDashboard(false);
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
                //    e.workbook.fileName = 'Dojo_Inquiry_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx';
                //},
                excel: {
                    fileName: 'Dojo_Inquiry_Export_' + (new Date()).toString('mm-dd-yyyy-hh-mm-ss') + '.xlsx',
                    proxyURL: "/proxy/save",
                    forceProxy: true
                },
                columns: [
                            { field: 'GuestName', title: 'Guest Name', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.spacing(data.GuestName) #" },
                            { field: 'InquiryTeam', title: 'Inquiry Team', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.InquiryTeam) #" },
                            { field: 'AirBnBListingTitle', title: 'Airbnb Listing Title', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PropertyCode', title: 'Property Code', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Channel', title: 'Channel', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Channel) #" },
                            { field: 'BookingGuidelines', title: 'Booking Guidelines', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.BookingGuidelines) #" },
                            //{ field: 'AdditionalInfo_StatusofInquiry', title: 'Inquiry Status', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            //{ field: 'AirBnBURL', title: 'Airbnb URL', filterable: false, template: "#= DojoWeb.Template.link(data.AirBnBURL, 'AirBnb URL') #", hidden: true },
                            { field: 'Bedrooms', title: 'Bedrooms', filterable: DojoWeb.Template.textSearch() },
                            //{ field: 'Account', title: 'Airbnb Account', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'InquiryCreatedTimestamp', title: 'Created Timestamp', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy hh:mm tt}' },
                            { field: 'Check_inDate', title: 'Check-in Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            //{ field: 'Check_InDay', title: 'Check-in Weekday', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Check_InDay) #", hidden: true },
                            { field: 'Check_outDate', title: 'Check-out Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            //{ field: 'Check_OutDay', title: 'Check-out Weekday', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Check_OutDay) #", hidden: true },
                            //{ field: 'TotalPayout', title: 'Total Payout', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'Weekdayorphanday', title: 'Weekday Orphan Day', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'DaysOut', title: 'Days Out', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'DaysOutPoints', title: 'DaysOut Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'LengthofStay', title: 'Length of Stay', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'LengthofStayPoints', title: 'Length of Stay Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'OpenWeekdaysPoints', title: 'Open Weekdays Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'NightlyRate', title: 'Nightly Rate', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'TotalPoints', title: 'Total Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            { field: 'OwnerApprovalNeeded', title: 'Owner Approval Needed', filterable: { multi: true }, template: "#= DojoWeb.Template.boolean(data.NeedsOwnerApproval) #", hidden: true },
                            { field: 'ApprovedbyOwner', title: 'Approved by Owner', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.ApprovedbyOwner) #" },
                            { field: 'PricingApprover1', title: 'Pricing Approver', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.ApprovedbyOwner) #" },
                            { field: 'PricingDecision1', title: 'Pricing Decision', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.ApprovedbyOwner) #" },
                            { field: 'PricingReason1', title: 'Pricing Reason', filterable: DojoWeb.Template.textSearch() },
                            //{ field: 'InquiryAge', title: 'Inquiry Age', hidden: true },
                            //{ field: 'Daystillcheckin', title: 'Days Till Check-in', hidden: true },

                            //{ field: 'Market', title: 'Market', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Market) #" },
                            //{ field: 'PricingApprover2', title: 'Pricing Approver 2', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.ApprovedbyOwner) #" },                         
                            //{ field: 'InquiryDate', title: 'Inquiry Date', filterable: true, hidden: true },
                            //{ field: 'InquiryTime__PST_', title: 'Inquiry Time (PST)', filterable: true, hidden: true },
                            //{ field: 'PricingTeamTimeStamp', title: 'Pricing Team TimeStamp', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}', hidden: true },
                            //{ field: 'Cleaning_Fee', title: 'Cleaning Fee', hiden: true },
                            //{ field: 'Doesitrequire2pricingteampprovals', title: 'Need 2 approvals?', hidden: true },
                            //{ field: 'Approvedby2PricingTeamMember', title: 'Approved by 2 Pricing Team Member', hidden: true },
                            //{ field: 'PricingDecision2', title: 'Pricing Decision 2', hidden: true },
                            //{ field: 'PricingReason2', title: 'Pricing Reason 2', hidden: true },
                ],
            };
        },

        configureDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                //filter: { // default filter
                //    field: 'Market',
                //    operator: 'eq',
                //    value: 'New York'
                //},
                change: function (e) { // will catch filter event
                    // render the dashboard with the data displayed in current grid
                    //renderDashboard($(_gridId).data().kendoGrid.dataSource.view());
                },
                schema: {
                    model: {
                        id: 'Id',
                        fields: {
                            Id: { type: 'int', editable: false, hidden: true },
                            InquiryCreatedTimestamp: { type: 'date', editable: false, nullable: true },
                            Check_inDate: { type: 'date', editable: false, nullable: true },
                            Check_outDate: { type: 'date', editable: false, nullable: true },
                            PricingTeamTimeStamp: { type: 'date', editable: false, nullable: true },
                        }
                    }
                }
            });
        },

        renderDashboard = function (data) {
            toggleDashboard(true);
            renderWeekDashboard(data);
        },

        renderWeekDashboard = function (data) {
            var groups = _.groupBy(data, function (item) {
                return item.Market;
            });

            var chartData = _.sortBy(_.map(groups, function (group) {
                return {
                    market: group[0].Market,
                    payout: _.pluck(group, 'TotalPayout'),
                }
            }), 'market');

            // markets for x-axis
            var markets = _.map(_.pluck(chartData, 'market'), function (item) { return item; });
            var payouts = _.pluck(chartData, 'payout');

            DojoWeb.StackChart.marketChart('marketPayout', 'Payout ($1000)', 'Payout', false, payouts, markets, marketChartClick);
            DojoWeb.StackChart.marketChart('marketRentedUnit', 'Rented Units', 'RentedUnit', false, payouts, markets, marketChartClick);
        },

        toggleDashboard = function (show) {
            if (show)
                $('#dojoReportDashboard').removeClass('hide');
            else
                $('#dojoReportDashboard').addClass('hide');
        },

        marketChartClick = function (e) {
            chartClick(e, 'Market');
        },

        chartClick = function (e, field, opr) {
            var category = e.category;
            if (opr == undefined) opr = 'eq';

            var ds = $(_gridId).data('kendoGrid').dataSource; // kendo data source
            var filter = ds.filter(); // current grid filters

            // create chart filter that kendo understands
            var chartFilter = { filters: [{ field: field, operator: opr, value: category }], logic: 'or' };

            // combine grid filters and chart filters
            if (filter) {
                if (filter.logic != 'and') { // if the filters are 'or' together, we 'and' it with chart filter
                    var combinedFilters = { filters: [], logic: 'and' };
                    combinedFilters.filters.push(filter);
                    combinedFilters.filters.push(chartFilter);
                    ds.filter(combinedFilters);
                } else { // if grid filters are already 'and' together, we just add chart filter to grid filter group
                    filter.filters.push(chartFilter);
                    ds.filter(filter);
                }
            }
            else { // chart fiter is the only filter
                ds.filter(chartFilter);
            }

            //renderDashboard($(_gridId).data().kendoGrid.dataSource.view()); // update the dashboard
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-field="GuestName"]').css('min-width', '110px');
            $(_gridId + ' th[data-field="InquiryTeam"]').css('min-width', '110px');
            $(_gridId + ' th[data-field="AirBnBListingTitle"]').css('min-width', '250px');
            $(_gridId + ' th[data-field="PropertyCode"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Channel"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="BookingGuidelines"]').css('min-width', '250px');
            $(_gridId + ' th[data-field="Bedrooms"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="InquiryCreatedTimestamp"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="Check_inDate"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="Check_outDate"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="OwnerApprovalNeeded"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="ApprovedbyOwner"]').css('min-width', '87px');
            $(_gridId + ' th[data-field="PricingApprover1"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="PricingDecision1"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="PricingReason1"]').css('min-width', '200px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            $(_gridId + ' tr td:nth-child(1)').css('min-width', '127px');   // GuestName
            $(_gridId + ' tr td:nth-child(2)').css('min-width', '110px');  // InquiryTeam
            $(_gridId + ' tr td:nth-child(3)').css('min-width', '250px');   // AirBnBListingTitle
            $(_gridId + ' tr td:nth-child(4)').css('min-width', '80px');   // PropertyCode
            $(_gridId + ' tr td:nth-child(5)').css('min-width', '100px');  // Channel
            $(_gridId + ' tr td:nth-child(6)').css('min-width', '250px');   // BookingGuidelines
            $(_gridId + ' tr td:nth-child(7)').css('min-width', '90px');  // Bedrooms
            $(_gridId + ' tr td:nth-child(8)').css('min-width', '150px'); // InquiryCreatedTimestamp
            $(_gridId + ' tr td:nth-child(9)').css('min-width', '100px');  // Check_inDate
            $(_gridId + ' tr td:nth-child(10)').css('min-width', '120px'); // Check_outDate
            $(_gridId + ' tr td:nth-child(11)').css('min-width', '100px');  // OwnerApprovalNeeded
            $(_gridId + ' tr td:nth-child(12)').css('min-width', '86px'); // ApprovedbyOwner
            $(_gridId + ' tr td:nth-child(13)').css('min-width', '100px'); // PricingApprover1
            $(_gridId + ' tr td:nth-child(14)').css('min-width', '100px');  // PricingDecision1
            $(_gridId + ' tr td:nth-child(15)').css('min-width', '200px'); // PricingReason1
        },

        readjustHeaderWidths = function () {
            for (var i = 1; i <= 15; i++) {
                var $td = $(_gridId + ' tr td:nth-child(' + i + ')');
                var $th = $(_gridId + ' tr th:nth-child(' + i + ')');
                var tdWidth = $td.width();
                var thWidth = $th.width();
                if (tdWidth - thWidth >= 1) {
                    $th.css('min-width', tdWidth + 'px');
                    $th.css('width', tdWidth + 'px');
                }
                else if (thWidth - tdWidth >= 1) {
                    $td.css('min-width', thWidth + 'px');
                    $td.css('width', thWidth + 'px');
                }
            }
        },

        setCount = function () {
            var count = $(_gridId).data('kendoGrid').dataSource.view().length;
            if (count > 0)
                $('#inquiryCount').html('(' + count + ')');
            else
                $('#inquiryCount').html('');

        }

    return {
        init: init,
        getReport: getReport,
    }
}();

DojoWeb.ExcelExport = function () {
    var _exportIndicator = false,
        $gridData = undefined,

        download = function (gridSelector) {
            customDownload(gridSelector);
        },

        customDownload = function (gridSelector, columnsToShow, columnsToHide) {
            if (DojoWeb.Helpers.isEdge()) {
                alert('Excel Export does not currently support Edge browser. Please use other browsers.');
                return;
            }
            $gridData = $(gridSelector).data('kendoGrid');
            if (columnsToShow != undefined) {
                $.each(columnsToShow, function (index, name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.showColumn(col);
                });
            }

            if (columnsToHide != undefined) {
                $.each(columnsToHide, function (index, name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.hideColumn(col);
                });
            }

            $gridData.saveAsExcel();
            //var dataURI = "data:text/plain;base64," + kendo.util.encodeBase64($gridData);
            //kendo.saveAs({
            //    dataURI: dataURI,
            //    fileName: filename,
            //    proxyURL: "/proxy/save",
            //    forceProxy: true
            //});

            if (columnsToShow != undefined) {
                $.each(columnsToShow, function (index, name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.hideColumn(col);
                });
            }

            if (columnsToHide != undefined) {
                $.each(columnsToHide, function (index, name) {
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
DojoWeb.ReportActionBar = function () {
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
