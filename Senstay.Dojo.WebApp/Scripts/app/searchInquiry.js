"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.SearchInquiry = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _dataSource = undefined,
        _height = 600,

        init = function (gridId) {
            _gridId = '#' + gridId;
            installEvents();
            DojoWeb.Helpers.injectDummyHeaderColumn(_gridId, 16);         
        },

        installEvents = function () {
            $('#actionBarGo').unbind('click').on('click', function (e) {
                search();
            })
            // event to scroll fixed grid header
            $(window).scroll(function () {
                $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
            });
            $('#actionBarSearchID').unbind('click').on('click', function (e) {
                searchID();
            })
            // event to scroll fixed grid header
            $(window).scroll(function () {
                $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
            });

            // adjust grid first row position to work around Mac browser problem
            if (DojoWeb.Helpers.isMac()) { //DojoWeb.Helpers.isSafari()) {
                var height = $('#inquiryGrid .k-grid-header').css('height');
                if (height) $('#inquiryGrid').css('top', height);
            }
        },

        search = function () {
            DojoWeb.Busy.show(); // wait animation is a globally available function
            $.get('/SearchInquiry/Search?propertyCode=' + $('#PropertyCode').val(),
                function (data) {
                    clear();
                    // init the grid
                    _dataGrid = $(_gridId).kendoGrid(configureGrid()).data('kendoGrid');

                    _dataGrid.bind('dataBound', function (e) {
                        DojoWeb.Busy.hide();
                        adjustColumnWidths();
                        readjustHeaderWidths();
                        setCount();
                    });

                    // set data source and trigger grid display
                    _dataGrid.setDataSource(configureDataSource(data));

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.Notification.show('There is no inquiry data available for the given date range.');
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
        searchID = function () {
            DojoWeb.Busy.show(); // wait animation is a globally available function
            $.get('/SearchInquiry/SearchID?id=' + $('#ID').val(),
                function (data) {
                    clear();
                    // init the grid
                    _dataGrid = $(_gridId).kendoGrid(configureGrid()).data('kendoGrid');

                    _dataGrid.bind('dataBound', function (e) {
                        DojoWeb.Busy.hide();
                        adjustColumnWidths();
                        readjustHeaderWidths();
                        setCount();
                    });

                    // set data source and trigger grid display
                    _dataGrid.setDataSource(configureDataSource(data));

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.Notification.show('There is no inquiry data available for the given date range.');
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
            $(_gridId).empty(); // empty grid content
        },

        configureGrid = function () {
            return {
                //height: _height, // comment out to display all records
                dataSource: _dataSource,
                batch: false,
                pageable: false,
                resizable: true,
                scrollable: false,
                filterable: true,
                sortable: true,
                editable: false,
                reorderable: true,
                selectable: true,
                toolbar: null, //_canEdit ? [{ name: 'create', text: 'New inquiry' }] : null,
                columns: [
                            { field: 'Id', title: 'Id', width: '50px', filterable: DojoWeb.Template.numberSearch(), editable: false },
                            { field: 'GuestName', title: 'Guest Name', width: '80px', filterable: DojoWeb.Template.textSearch() },
                            { field: 'AirBnBListingTitle', title: 'Airbnb Listing Title', width: '200px', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PropertyCode', title: 'Property Code', width: '80px', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.SearchInquiry.propertyLink(data.Id, data.PropertyCode) #" },
                            { field: 'AdditionalInfo_StatusofInquiry', width: '200px', title: 'Summary', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.AdditionalInfo_StatusofInquiry) #" },
                            { field: 'BookingGuidelines', title: 'Booking Guidelines', width: '250px', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.BookingGuidelines) #" },
                            { field: 'Check_inDate', title: 'Check-in Date', width: '90px', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'Check_outDate', title: 'Check-out Date', width: '95px', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'PricingDecision1', title: 'Pricing Decision', width: '90px', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.PricingDecision1) #" },
                            { field: 'PricingReason1', title: 'Pricing Reason', width: '200px', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.PricingReason1) #" },
                            { field: 'PricingApprover1', title: 'Pricing Approver', width: '100px', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.PricingApprover1) #" },
                            { field: 'BeltDesignation', title: 'Belt', width: '50px', filterable: { multi: true }, template: "#= DojoWeb.Template.belt(data.BeltDesignation) #" },
                            { field: 'ApprovedbyOwner', title: 'Approved by Owner', width: '90px', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.ApprovedbyOwner) #" },
                            { field: 'InquiryTeam', title: 'Inquiry Team', width: '80px', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.InquiryTeam) #" },
                            { field: 'Channel', title: 'Channel', width: '80px', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Channel) #" },
                            { field: 'InquiryCreatedTimestamp', title: 'Timestamp (PST)', width: '150px', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy hh:mm tt}' },
                            { field: 'CreatedBy', title: 'Created By', hidden: true },
                            { field: 'ModifiedDate', title: 'Modified Date', hidden: true },
                            { field: 'Market', title: 'Market', width: '120px', hidden: true, filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Market) #" },

                            //{ field: 'Account', title: 'Airbnb Account', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            //{ field: 'TotalPayout', title: 'Total Payout', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'Check_InDay', title: 'Check-in Weekday', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Check_InDay), hidden: true #", hidden: true },
                            //{ field: 'Check_OutDay', title: 'Check-out Weekday', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Check_OutDay) #", hidden: true },
                            //{ field: 'DaysOut', title: 'Days Out', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'DaysOutPoints', title: 'DaysOut Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'NightlyRate', title: 'Nightly Rate', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'LengthofStay', title: 'Length of Stay', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'LengthofStayPoints', title: 'Length of Stay Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'Weekdayorphanday', title: 'Weekday Orphan Day', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'OpenWeekdaysPoints', title: 'Open Weekdays Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'TotalPoints', title: 'Total Points', filterable: DojoWeb.Template.numberSearch(), hidden: true },
                            //{ field: 'AdditionalInfo_StatusofInquiry', title: 'Inquiry Status', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            //{ field: 'OwnerApprovalNeeded', title: 'Owner Approval Needed', filterable: { multi: true }, template: "#= DojoWeb.Template.boolean(data.NeedsOwnerApproval) #", hidden: true },
                            //{ field: 'AirBnBURL', title: 'Airbnb URL', filterable: false, template: "#= DojoWeb.Template.link(data.AirBnBURL, 'AirBnb URL') #", hidden: true },
                            //{ field: 'InquiryDate', title: 'Inquiry Date', filterable: true, hidden: true },
                            //{ field: 'InquiryTime__PST_', title: 'Inquiry Time (PST)', filterable: true, hidden: true },
                            //{ field: 'PricingTeamTimeStamp', title: 'Pricing Team TimeStamp', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}', hidden: true },
                            //{ field: 'Cleaning_Fee', title: 'Cleaning Fee', hiden: true },
                            //{ field: 'Doesitrequire2pricingteampprovals', title: 'Need 2 approvals?', hidden: true },
                            //{ field: 'Approvedby2PricingTeamMember', title: 'Approved by 2 Pricing Team Member', hidden: true },
                            //{ field: 'PricingDecision2', title: 'Pricing Decision 2', hidden: true },
                            //{ field: 'PricingReason2', title: 'Pricing Reason 2', hidden: true },
                            //{ field: 'InquiryAge', title: 'Inquiry Age', hidden: true },
                            //{ field: 'Daystillcheckin', title: 'Days Till Check-in', hidden: true },
                ],
            };
        },

        propertyLink = function (id, propertyCode) {
            return '<div class="showPropertyInfo" data-id="' + id + '" style="text-align:center;">' + propertyCode + '</div>';
        },

        configureDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                schema: {
                    model: {
                        id: 'Id',
                        fields: {
                            Id: { type: 'number', editable: false },
                            InquiryCreatedTimestamp: { type: 'date', editable: false, nullable: true },
                            Check_inDate: { type: 'date', editable: false, nullable: true },
                            Check_outDate: { type: 'date', editable: false, nullable: true },
                            PricingTeamTimeStamp: { type: 'date', editable: false, nullable: true },
                            CreatedDate: { type: 'date', editable: false, nullable: true },
                            ModifiedDate: { type: 'date', editable: false, nullable: true },
                        }
                    }
                }
            });
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-index="0"]').css('min-width', '23px').css('max-width', '23px');
            $(_gridId + ' th[data-index="1"]').css('min-width', '23px').css('max-width', '23px');
            $(_gridId + ' th[data-index="2"]').css('min-width', '23px').css('max-width', '23px');
            $(_gridId + ' th[data-index="3"]').css('min-width', '24px').css('max-width', '24px');
            $(_gridId + ' th[data-field="Id"]').css('min-width', '50px');
            $(_gridId + ' th[data-field="GuestName"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="AirBnBListingTitle"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="PropertyCode"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="AdditionalInfo_StatusofInquiry"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="BookingGuidelines"]').css('min-width', '250px');
            $(_gridId + ' th[data-field="Check_inDate"]').css('min-width', '90px');
            $(_gridId + ' th[data-field="Check_outDate"]').css('min-width', '95px');
            $(_gridId + ' th[data-field="PricingDecision1"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="PricingReason1"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="PricingApprover1"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="BeltDesignation"]').css('min-width', '50px');
            $(_gridId + ' th[data-field="ApprovedbyOwner"]').css('min-width', '90px');
            $(_gridId + ' th[data-field="InquiryTeam"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Channel"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="InquiryCreatedTimestamp"]').css('min-width', '150px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            $(_gridId + ' tr td:nth-child(1)').css('min-width', '40px').css('max-width', '40px');   // edit
            $(_gridId + ' tr td:nth-child(2)').css('min-width', '40px').css('max-width', '40px');   // approval
            $(_gridId + ' tr td:nth-child(3)').css('min-width', '40px').css('max-width', '40px');   // view
            $(_gridId + ' tr td:nth-child(4)').css('min-width', '40px').css('max-width', '40px');   // delete
            $(_gridId + ' tr td:nth-child(5)').css('min-width', '50px').css('max-width', '50px');   // id
            $(_gridId + ' tr td:nth-child(6)').css('min-width', '80px').css('max-width', '80px');   // guest
            $(_gridId + ' tr td:nth-child(7)').css('min-width', '200px').css('max-width', '200px');  // listing
            $(_gridId + ' tr td:nth-child(8)').css('min-width', '80px').css('max-width', '80px');   // property
            $(_gridId + ' tr td:nth-child(9)').css('min-width', '200px').css('max-width', '200px'); // summary (AdditionalInfo_StatusofInquiry)
            $(_gridId + ' tr td:nth-child(10)').css('min-width', '250px').css('max-width', '250px'); // booking guidelines
            $(_gridId + ' tr td:nth-child(11)').css('min-width', '90px').css('max-width', '90px');  // check-in
            $(_gridId + ' tr td:nth-child(12)').css('min-width', '95px').css('max-width', '95px'); // check-out
            $(_gridId + ' tr td:nth-child(13)').css('min-width', '99px').css('max-width', '100px');  // pricing decision
            $(_gridId + ' tr td:nth-child(14)').css('min-width', '200px').css('max-width', '200px'); // pricing reason
            $(_gridId + ' tr td:nth-child(15)').css('min-width', '100px').css('max-width', '100px'); // pricing approver
            $(_gridId + ' tr td:nth-child(16)').css('min-width', '50px').css('max-width', '50px');  // belt
            $(_gridId + ' tr td:nth-child(17)').css('min-width', '80px').css('max-width', '90px');  // approved by owner
            $(_gridId + ' tr td:nth-child(18)').css('min-width', '80px').css('max-width', '80px');   // team
            $(_gridId + ' tr td:nth-child(19)').css('min-width', '80px').css('max-width', '80px'); // channel
            $(_gridId + ' tr td:nth-child(20)').css('min-width', '150px').css('max-width', '150px');  // timestamp
        },

        readjustHeaderWidths = function () {
            for (var i = 6; i <= 21; i++) {
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
                $('#gridDataCount').html('(' + count + ')');
            else
                $('#gridDataCount').html('');

        }

    return {
        init: init,
        propertyLink: propertyLink,
    }
}();



