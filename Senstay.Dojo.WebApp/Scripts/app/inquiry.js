"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.Inquiry = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _dataSource = undefined,
        _height = 600,
        _highlightRow = undefined,
        _action = undefined,
        _defaultFilter = 0,
        _canEdit = false,
        _canApprove = false,
        _userName = '', // only used for enabling delete icon; need to check code if for other purpose
        _approvers = [],
        _localStorageKey = 'inquiryGridFilters',
        _checkinDateField = 'Check_inDate',

        init = function (gridId, height) {
            _gridId = '#' + gridId;
            _height = height != undefined ? height : _height;
            DojoWeb.Helpers.injectDummyHeaderColumn(_gridId, 16);
            setDefaultFilter();
            _canEdit = $('.app-grid-edit').length > 0; // app-grid-edit class indicate the grid is editable
            _canApprove = $('.app-grid-approve').length > 0; // app-grid-edit class indicate the grid is approverable
            _userName = $('#UserName').val();
            
            initGridFilters();

            // custom filter events
            DojoWeb.InquiryActionBar.attachFilters(getInquiries,
                                                   filterByUnapproved,
                                                   filterByRecentlyApproved,
                                                   filterByCheckinToday,
                                                   filterByApprovalStatus);

            getApprovers();
            render(); // show the grid

            DojoWeb.Notification.init('inquiryNotification');
        },

        installEvents = function () {
            $('#actionBarAddNew').addClass('showInquiryNew');
            $('#actionBarAddNew').unbind('click').on('click', function (e) {
                _dataGrid.clearSelection();
                _action = 'new';
            });
            $('.showInquiryEdit').unbind('click').on('click', function (e) {
                _action = 'edit';
            });
            $('.showInquiryApprove').unbind('click').on('click', function (e) {
                _action = 'approve';
            });
            $('.showInquiryInfo').unbind('click').on('click', function (e) {
                _action = 'view';
            });
            $('.showPropertyInfo').unbind('click').on('click', function (e) {
                _action = 'property';
            });

            // info
            DojoWeb.Plugin.initFormDialog({
                selector: '.showInquiryInfo',
                caption: 'View Inquiry Information',
                width: 1200,
                url: '/Inquiry/Info',
                modal: true,
                closeEvent: unselectRow
            });

            // property
            DojoWeb.Plugin.initFormDialog({
                selector: '.showPropertyInfo',
                caption: 'View Property Information',
                width: 1200,
                url: '/Inquiry/Property',
                modal: true,
                closeEvent: unselectRow
            });

            // edit/view
            var caption = _canEdit ? 'Edit Inquiry' : 'View Inquiry';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showInquiryEdit',
                caption: caption,
                width: 1000,
                url: '/Inquiry/Edit',
                formId: 'InquiryEntryForm',
                initEvent: DojoWeb.InquiryForm.init,
                modal: false,
                closeEvent: unselectRow
            });

            // new
            DojoWeb.Plugin.initFormDialog({
                selector: '.showInquiryNew',
                caption: 'New Inquiry',
                width: 1000,
                url: '/Inquiry/New',
                formId: 'InquiryEntryForm',
                initEvent: DojoWeb.InquiryForm.init,
                modal: false,
                closeEvent: unselectRow
            });

            // approve
            caption = _canApprove ? 'Approve Inquiry' : 'View Approval Inquiry';
            DojoWeb.Plugin.initFormDialog({
                selector: '.showInquiryApprove',
                caption: caption,
                width: 500,
                formId: 'InquiryApproveForm',
                url: '/Inquiry/Approve',
                initEvent: DojoWeb.InquiryForm.init,
                modal: false,
                closeEvent: unselectRow
            });

            // custom filter events
            DojoWeb.InquiryActionBar.attachFilters(getInquiries,
                                                   filterByUnapproved,
                                                   filterByRecentlyApproved,
                                                   filterByCheckinToday,
                                                   filterByApprovalStatus);

            // event to scroll fixed grid header
            $(window).scroll(function () {
                $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
                //$(_gridId + ' .k-grid-header').css('left', originalLeft - $(this).scrollLeft());
            });

            // adjust grid first row position to work around Mac browser problem
            if (DojoWeb.Helpers.isMac()) { //DojoWeb.Helpers.isSafari()) {
                var height = $('#inquiryGrid .k-grid-header').css('height');
                if (height) $('#inquiryGrid').css('top', height);
            }

            // excel export
            //$('#actionBarExport').on('click', function (e) {
            //    DojoWeb.ExcelExport.download(_gridId);
            //});

            // when 'edit' button is clicked, it replaces with built-in 'update' and 'cancel' button with text.
            // this event removes the 'update' and 'cancel' text and replace icon with bootstrap's
            //$(_gridId).unbind('click').on('click', '.k-grid-edit', function () {
            //    $(".k-grid-update").html("<span class='k-icon k-update'></span>").css("min-width", "16px").removeClass("k-button-icontext");
            //    $(".k-grid-cancel").html("<span class='k-icon k-cancel'></span>").css("min-width", "16px").removeClass("k-button-icontext");
            //});
        },

        render = function () {
            var dateRange = DojoWeb.InquiryActionBar.getDateRange();
            getInquiries(dateRange.beginDate, dateRange.endDate);
        },

        getInquiries = function (beginDate, endDate) {
            DojoWeb.Busy.show(); // wait animation is a globally available function
            DojoWeb.InquiryActionBar.resetCustomFilters();
            saveGridFilters();

            $.get('/Inquiry/Retrieve',
                {
                    beginDate: kendo.toString(beginDate, 'MM/dd/yyyy'),
                    endDate: kendo.toString(endDate, 'MM/dd/yyyy')
                },
                function (data) {
                    clear();

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.Notification.show('There is no inquiry data available for the given date range.');
                        return;
                    }

                    // init the grid
                    _dataGrid = $(_gridId).kendoGrid(configureGrid()).data('kendoGrid');
                    // bind grid data
                    _dataGrid.bind('dataBound', function (e) {
                        DojoWeb.Busy.hide();
                        installEvents();
                        adjustColumnWidths();
                        readjustHeaderWidths();
                        setCount();
                        var id = DojoWeb.InquiryForm.getId();
                        var propertyCode = DojoWeb.InquiryForm.getProperty();
                        if (needHighlightRow()) {
                            DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                            if (_action == 'approve' || _action == 'edit') {
                                var message = _action == 'approve' ? 'Approval of inquiry ID "' + id + '" is successful.'
                                                                   : 'Update of inquiry for property "' + propertyCode + '"  is successful.';
                                DojoWeb.Notification.show(message);
                            }
                        }
                        else if (id != undefined && _action == 'new') {
                            _highlightRow = id;
                            DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                            DojoWeb.Notification.show('Creation of inquiry for property "' + propertyCode + '" is successful.');
                        }
                        else if (_action == 'delete') {
                            DojoWeb.Notification.show('Deletion of inquiry ID "' + id + '" is successful.');
                        }
                        _action = undefined;
                    });
                    // set data source and trigger grid display
                    _dataGrid.setDataSource(configureDataSource(data));
                    _dataSource = _dataGrid.dataSource;
                    // reload the saved grid filters
                    applyGridFilters();

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

        getApprovers = function () {
            $.get('/UserRoleManager/GetApprovers',
            function (data) {
                _approvers = data;
            });
        },

        initGridFilters = function () {
            localStorage.setItem(_localStorageKey, '');
        },

        saveGridFilters = function () {
            if (_dataGrid && _dataGrid.dataSource && _dataGrid.dataSource.filter()) {
                localStorage.setItem(_localStorageKey, kendo.stringify(_dataGrid.dataSource.filter()));
            }
            else
                initGridFilters();
        },

        applyGridFilters = function () {
            var filters = localStorage.getItem(_localStorageKey);
            if (filters != undefined && filters != '' && _dataGrid != undefined) {
                _dataGrid.dataSource.filter(JSON.parse(filters));
            }
        },

        applyCustomFilters = function (customFilter, filterNames) {
            DojoWeb.GridFilters.applyCustomFilter(_gridId, customFilter, filterNames);
        },

        filterByUnapproved = function (e) {
            var selected = $('#' + e.target.id).hasClass('custom-filter-selected');
            var ds = $(_gridId).data('kendoGrid').dataSource;
            var customFilters = { filters: [], logic: 'and' };
            if (selected) {
                // the filter is defined by these business rules:
                // 1. Belt designation = black or yellow
                // 2. price approval status is n/a
                // 3. sort by timestamp in accending order
                var beltFilter = {
                    logic: 'or',
                    filters: [
                        { field: 'BeltDesignation', operator: 'eq', value: 'Yellow belt' },
                        { field: 'BeltDesignation', operator: 'eq', value: 'Black belt' },
                    ]
                };

                var approverFilter = {
                    logic: 'or',
                    filters: [
                        { field: 'PricingApprover1', operator: 'eq', value: '' },
                        { field: 'PricingApprover1', operator: 'eq', value: 'N/A' },
                        { field: 'PricingApprover1', operator: 'eq', value: 'N\A' },
                    ]
                };

                customFilters.filters.push(beltFilter);
                customFilters.filters.push(approverFilter);
            }

            ds.filter(customFilters);
        },

        filterByRecentlyApproved = function (e) {
            var selected = $('#' + e.target.id).hasClass('custom-filter-selected');
            var ds = $(_gridId).data('kendoGrid').dataSource;
            var customFilters = { filters: [], logic: 'and' };
            if (selected) {
                // the filter is defined by these business rules:
                // 1. Belt designation = black or yellow
                // 2. price approver is one of the approver role
                // 3. sort by timestamp in accending order
                var beltFilter = {
                    logic: 'or',
                    filters: [
                        { field: 'BeltDesignation', operator: 'eq', value: 'Yellow belt' },
                        { field: 'BeltDesignation', operator: 'eq', value: 'Black belt' },
                    ]
                };

                var decisionFilter = {
                    logic: 'or',
                    filters: [
                        { field: 'PricingDecision1', operator: 'eq', value: 'Yes' },
                    ]
                };

                var recent = Date.today().addDays(-7); // last 7 days of approved inquiries
                var approverFilters = { filters: [], logic: 'and' };
                approverFilters.filters.push({ field: 'PricingApprover1', operator: 'neq', value: '' });
                approverFilters.filters.push({ field: 'InquiryCreatedTimestamp', operator: 'gt', value: recent });

                // TODO: enable this if approver role is used
                //$.each(_approvers, function (i, approver) {
                //    approverFilters.filters.push({ field: 'PricingApprover1', operator: 'eq', value: approver.Value });
                //});

                customFilters.filters.push(beltFilter);
                customFilters.filters.push(decisionFilter);
                customFilters.filters.push(approverFilters);
            }

            ds.filter(customFilters);
        },

        filterByCheckinToday = function (e) {
            var selected = $('#' + e.target.id).hasClass('custom-filter-selected');
            var ds = $(_gridId).data('kendoGrid').dataSource;
            var customFilters = { filters: [], logic: 'and' };
            if (selected) {
                var today = Date.today();
                //var checkinFilter = { field: 'Check_inDate', operator: 'eq', value: today };
                var tomorrow = Date.today().addDays(1);
                var dateRangeFilter = {
                    logic: 'and',
                    filters: [
                        { field: 'Check_inDate', operator: 'gte', value: today },
                        { field: 'Check_inDate', operator: 'lt', value: tomorrow },
                    ]
                };
                customFilters.filters.push(dateRangeFilter);
            }

            ds.filter(customFilters);
        },

        filterByApprovalStatus = function (e) {
            // get a list of favorite filters from css class, parse the id to get field and value pair
            var customFilters = { filters: [], logic: 'or' };
            var field = 'PricingDecision1';
            var equal = 'eq';
            $('.actionBar-approval-group').each(function () {
                if ($(this).is(':checked')) {
                    switch (this.id) {
                        case 'statusApproved':
                            customFilters.filters.push({ field: field, operator: equal, value: "Yes" });
                            break;
                        case 'statusDisapproved':
                            customFilters.filters.push({ field: field, operator: equal, value: "No" });
                            break;
                        case 'statusHold':
                            customFilters.filters.push({ field: field, operator: equal, value: "Hold" });
                            break;
                        case 'statusCounter':
                            customFilters.filters.push({ field: field, operator: equal, value: "Counter Offer" });
                            break;
                        case 'statusTetris':
                            customFilters.filters.push({ field: field, operator: equal, value: "Teris" });
                            break;
                        case 'statusTooFarOut':
                            customFilters.filters.push({ field: field, operator: equal, value: "Too Far Out" });
                            break;
                    }
                }
            });

            if (customFilters.filters.length <= 0)
                customFilters = { field: field, operator: 'neq', value: 'None' }; // something not a legal approval status

            DojoWeb.InquiryActionBar.resetCustomFilters();
            applyCustomFilters(customFilters, [field, _checkinDateField]);
        },

        clear = function () {
            $(_gridId).empty(); // empty grid content
        },

        configureGrid = function () {
            return {
                //height: _height, // comment out to display all records
                //dataSource: _dataSource,
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
                            {
                                field: 'edit',
                                title: ' ',
                                width: '40px',
                                filterable: false,
                                template: "#= DojoWeb.Inquiry.renderAction(data.Id, 'edit')#",
                                lockable: true,
                                hidden: false
                            },
                            {
                                field: 'approve',
                                title: ' ',
                                width: '40px',
                                filterable: false,
                                template: "#= DojoWeb.Inquiry.renderAction(data.Id, 'approve')#",
                                locked: true,
                                hidden: !_canApprove
                            },
                            {
                                field: 'view',
                                title: ' ',
                                width: '40px',
                                filterable: false,
                                template: "#= DojoWeb.Inquiry.renderAction(data.Id, 'view')#",
                                locked: true,
                                hidden: false
                            },
                            {
                                field: 'delete',
                                title: ' ',
                                width: '40px',
                                filterable: false,
                                template: "#= DojoWeb.Inquiry.renderAction(data.Id, 'delete', data.CreatedBy)#",
                                locked: true,
                                hidden: false
                            },
                            { field: 'Id', title: 'Id', width: '50px', filterable: DojoWeb.Template.numberSearch(), editable: false },
                            { field: 'GuestName', title: 'Guest Name', width: '80px', filterable: DojoWeb.Template.textSearch() },
                            { field: 'AirBnBListingTitle', title: 'Airbnb Listing Title', width: '200px', filterable: DojoWeb.Template.textSearch() },
                            { field: 'PropertyCode', title: 'Property Code', width: '80px', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Inquiry.propertyLink(data.Id, data.PropertyCode) #" },
                            { field: 'AdditionalInfo_StatusofInquiry', width: '200px', title: 'Summary', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.AdditionalInfo_StatusofInquiry) #" },
                            { field: 'BookingGuidelines', title: 'Booking Guidelines', width: '250px', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.BookingGuidelines) #" },
                            { field: 'Check_inDate', title: 'Check-in Date', width: '90px', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'Check_outDate', title: 'Check-out Date', width: '95px', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'PricingDecision1', title: 'Pricing Decision', width: '90px', filterable: false, template: "#= DojoWeb.Template.nullable(data.PricingDecision1) #" },
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
                filterMenuInit: function (e) {
                    //if (e.field == 'CreatedDate') {
                    //    var filterMultiCheck = this.thead.find("[data-field=" + e.field + "]").data("kendoFilterMultiCheck")
                    //    if (filterMultiCheck != null && filterMultiCheck.container != null) {
                    //        filterMultiCheck.container.empty();

                    //        // this is the work around for Telerik suggested code below
                    //        var sortedData = _.sortBy(filterMultiCheck.checkSource.data(), e.field);
                    //        filterMultiCheck.checkSource.data(sortedData);
                    //        filterMultiCheck.createCheckBoxes();

                    //        // the following Telerik suggested code to make multi-select in sorted order is missing some items
                    //        //filterMultiCheck.checkSource.sort({ field: e.field, dir: "asc" });
                    //        //filterMultiCheck.checkSource.data(filterMultiCheck.checkSource.view().toJSON());
                    //        //filterMultiCheck.createCheckBoxes();
                    //    }
                    //}
                },
            };
        },

        configureDataSource = function (data) {
            return new kendo.data.DataSource({
                data: data,
                filter: _defaultFilter,
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

        renderAction = function (id, action, createdBy) {
            if (action == 'view') {
                return "<div id='view-id-" + id + "' class='showInquiryInfo gridcell-btn dojo-center' title='View Inquiry' data-id='" + id + "'><div class='btn'><i class='fa fa-eye'></i></div></div>";
            }
            else if (_canEdit || _canApprove) {
                if (action == 'edit') {
                    return "<div id='edit-id-" + id + "' class='showInquiryEdit gridcell-btn dojo-center' title='Edit Inquiry' data-id='" + id + "'><div class='btn'><i class='fa fa-wrench'></i></div></div>";
                }
                else if (action == 'approve') {
                    return "<div id='approve-id-" + id + "' class='showInquiryApprove gridcell-btn dojo-center' title='Approve Inquiry' data-id='" + id + "'><div class='btn'><i class='fa fa-user-plus'></i></div></div>";
                }
                else if (action == 'delete') {
                    if ((createdBy == _userName || _userName == 'DelegateDeletion') && _userName != '') {
                        return "<div id='delete-id-" + id + "' class='gridcell-btn dojo-center' title='Delete Inquiry' onClick='DojoWeb.Inquiry.renderDelete(" + '"' + id + '"' + ");'><div class='btn'><i class='fa fa-trash-o'></i></div></div>";
                    }
                    else
                        return "<div class='gridcell-btn dojo-center'><div class='center faintGray'><i class='fa fa-trash-o'></i></div></div>";
                }
            }
            else {
                if (action == 'edit') { // edit button is clicked in view only mode
                    return "<div id='edit-id-" + id + "' class='showInquiryEdit gridcell-btn dojo-center' title='Edit Inquiry' data-id='" + id + "'><div class='btn'><i class='fa fa-edit'></i></div></div>";
                }
            }
            return '';
        },

        renderDelete = function (id) {
            DojoWeb.Confirmation.confirmDiscard({
                id: 'confirmation-dialog',
                caption: 'Delete Inquiry Confirmation',
                message: 'The selected inquiry will be deleted. Please confirm.',
                ok: function () {
                    $.ajax({
                        type: 'POST',
                        url: '/Inquiry/Delete/?id=' + id,
                        success: function (result) {
                            if (result == 'success') {
                                _action = 'delete';
                                setHighlightRow(undefined);
                                render();
                            }
                            else {
                                DojoWeb.Notification.show('There was an error deleting the inquiry.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (status == 'error') {
                                alert('There was an error deleting the inquiry.');
                            }
                        }
                    });
                }
            });
        },

        propertyLink = function (id, propertyCode) {
            return '<div class="showPropertyInfo" data-id="' + id + '" style="text-align:center;">' + propertyCode + '</div>';
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

        setDefaultFilter = function () {
            var inquiryId = parseInt($('#InquiryId').val());
            if (inquiryId != 0) {
                _defaultFilter = {
                    field: 'Id',
                    operator: 'eq',
                    value: inquiryId
                }
            }
            else {
                _defaultFilter = null;
            }
        },

        setCount = function () {
            var count = $(_gridId).data('kendoGrid').dataSource.view().length;
            if (count > 0)
                $('#gridDataCount').html('(' + count + ')');
            else
                $('#gridDataCount').html('');

        },

        setHighlightRow = function (id) {
            _highlightRow = id;
        },

        needHighlightRow = function () {
            return _highlightRow != undefined && _action != 'new';
        },

        updateRow = function (id) {
            setHighlightRow(id);
            render();

            // sample code; data source is observable meaning that the changes will reflect on the grid display automatically
            // performance issue for set call: each set call replenish data source. need to find a better way
            //var dataGrid = $(_gridId).data('kendoGrid');
            //var selectedRow = dataGrid.select(); // Access the row that is selected
            //var rowData = dataGrid.dataItem(selectedRow); // the row data
            //rowData.set('OwnerApprovalNeeded', newData.OwnerApprovalNeeded);
            //rowData.set('ApprovedbyOwner', newData.ApprovedbyOwner);
            //rowData.set('PricingApprover1', newData.PricingApprover1);
            //rowData.set('PricingApprover2', newData.PricingApprover2);
            //rowData.set('PricingDecision1', newData.PricingDecision1);
            //DojoWeb.GridHelper.redrawRow(dataGrid, selectedRow);
        },

        unselectRow = function() {
            if (_dataGrid) _dataGrid.clearSelection();
            _highlightRow = undefined;
        },

        adjustEditorIcon = function (name) {
            $($($('input[name="' + name + '"]').parent().children('span')[0]).children()[0]).html('');
        }

    return {
        init: init,
        getInquiries: getInquiries,
        filterByUnapproved: filterByUnapproved,
        filterByRecentlyApproved: filterByRecentlyApproved,
        filterByCheckinToday: filterByCheckinToday,
        filterByApprovalStatus: filterByApprovalStatus,
        propertyLink: propertyLink,
        renderAction: renderAction,
        renderDelete: renderDelete,
        updateRow: updateRow,
        unselectRow: unselectRow
    }
}();

DojoWeb.InquiryForm = function () {
    var _formId = undefined,
        _currentId = undefined,
        _currentProperty = undefined,
        _dateRangeHandle = undefined,

        init = function (formId) {
            _formId = formId;
            _currentId = undefined;
            installControls();
        },

        getId = function() {
            return _currentId;
        },

        getProperty = function () {
            return _currentProperty;
        },

        installControls = function () {
            if ($('.app-form-view').length == 0) { // editing mode
            }
            else { // disble ui control editing for view-only mode
            }

            $('.form-cancel').click(function () {
                $('#formDialog').data('kendoWindow').close();
            });

            $('#PropertyCode').unbind('change').on('change', function (e) {
                DojoWeb.Validation.clearParentMessage('PropertyCode');
            });

            $('#TotalPayout').unbind('change').on('change', function (e) {
                DojoWeb.Validation.clearParentMessage('TotalPayout');
            });
            
            var checkInDate, checkoutDate;
            if ($('#Check_inDate').val()) checkInDate = Date.parse($('#Check_inDate').val());
            if ($('#Check_outDate').val()) checkoutDate = Date.parse($('#Check_outDate').val());
            _dateRangeHandle = DojoWeb.DateRange.init('Check_inDate', 'Check_outDate', checkInDate, checkoutDate);
            DojoWeb.Plugin.initSearchableList('#PropertyCode', 'PropertyCodeSearchableSelect');
        },

        cancel = function () {
            $('#formDialog').data('kendoWindow').close();
            DojoWeb.Inquiry.unselectRow();
        },

        switchTabTo = function (tab) {
            $('.inquiry-tab-pane').addClass('hide');
            $('.inquiry-info-tab').removeClass('active-tab');
            $(tab).removeClass('hide');
        },

        saveApproveForm = function() {
            var $form = $('#' + _formId);
            if ($form.valid()) {
                var formData = {
                    Id: $('#Id').val(),
                    OwnerApprovalNeeded: $('#OwnerApprovalNeeded').val(),
                    ApprovedbyOwner: $('#ApprovedbyOwner').val(),
                    PricingApprover1: $('#PricingApprover1').val(),
                    PricingApprover2: $('#PricingApprover2').val(),
                    PricingDecision1: $('#PricingDecision1').val(),
                    PricingReason1: $('#PricingReason1').val(),
                }

                $.ajax({
                    type: 'POST',
                    url: '/Inquiry/SaveApproveStatus',
                    contentType: 'application/json;charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify(formData),
                    success: function (result) { // return inquiry Id
                        DojoWeb.Plugin.closeFormDialog();
                        if (result != '') {
                            _currentId = parseInt(result);
                            DojoWeb.Inquiry.updateRow(_currentId);
                        }
                        else {
                            DojoWeb.Notification.show('There was an error saving your approval status.');
                        }
                    },
                    error: function (jqXHR, status, errorThrown) {
                        if (status == 'error') {
                            var message = 'There was an error saving your approval status.';
                            alert(message);
                        }
                    }
                });
            }
        },

        saveEditForm = function() {
            var $form = $('#' + _formId);
            var invalidCount = $form.valid() ? 0 : 1;
            invalidCount += DojoWeb.Validation.validateSearchableDropdown('#PropertyCode', 'Property Code is required.');
            invalidCount += DojoWeb.Validation.validateDate('#Check_inDate', 'Check-in Date is required.');
            invalidCount += DojoWeb.Validation.validateDate('#Check_outDate', 'Check-out Date is required.');
            invalidCount += DojoWeb.Validation.validateInputGroup('#TotalPayout', 'Total Payout is required.');
            invalidCount += DojoWeb.Validation.validateTextBox('#Weekdayorphandays', 'Weekday Orphan Days is required.');
            invalidCount += validatePropertyCode('#PropertyCode');
            invalidCount += validateDecimal('#TotalPayout', 'Total Payout is an invalid number.');
            invalidCount += validateNaturalNumber('#Weekdayorphandays', 'Weekday Orphan Days is an invalid natural number.');
            if (invalidCount == 0) {
                _currentId = $('#Id').val();
                _currentProperty = $('#PropertyCode').val();
                var formData = {
                    Id: _currentId,
                    GuestName: $('#GuestName').val(),
                    InquiryTeam: $('#InquiryTeam').val(),
                    PropertyCode: _currentProperty,
                    Channel: $('#Channel').val(),
                    TotalPayout: $('#TotalPayout').val(),
                    Check_inDate: $('#Check_inDate').val(),
                    Check_outDate: $('#Check_outDate').val(),
                    Weekdayorphandays: $('#Weekdayorphandays').val(),
                    AdditionalInfo_StatusofInquiry: $('#AdditionalInfo_StatusofInquiry').val(),
                }
                $.ajax({
                    type: 'POST',
                    url: '/Inquiry/save',
                    contentType: 'application/json;charset=utf-8',
                    dataType: 'json',
                    data: JSON.stringify(formData),
                    success: function (result) {
                        if (result != '') { // return inquiry Id
                            DojoWeb.Plugin.closeFormDialog();
                            _currentId = parseInt(result);
                            // if the inquiry date range is not up to today, the newly added inquiry won't show up in the grid
                            var dateRange = DojoWeb.InquiryActionBar.getDateRange();
                            if (dateRange.endDate < Date.today()) {
                                DojoWeb.Notification.show('Please change "To" date textbox to today to see the newly added inquiry.');
                                // change end date to today so that the added inquiry can be seen
                                //DojoWeb.InquiryActionBar.setDateRange(dateRange.beginDate, Date.today());
                            }
                            DojoWeb.Inquiry.updateRow(_currentId);
                        }
                        else {
                            var message = 'There was an error saving your inquiry to the database. Please make sure the Property Code is selected from the list.';
                            DojoWeb.Notification.show(message);
                        }
                    },
                    error: function (jqXHR, status, errorThrown) {
                        if (jqXHR.status == 409) { // conflict
                            var message = 'The inquiry for property "' + formData.PropertyCode + '" already exists.';
                            DojoWeb.Notification.show(message);
                        }
                        else if (jqXHR.status == 500) { // internal server error
                            var message = 'The inquiry cannot be saved to database. If this problem persists, please contact Dojo support team.';
                            DojoWeb.Notification.show(message);
                        }
                        else if (status == 'error') {
                            var message = 'There was an error saving your inquiry to the database.';
                            DojoWeb.Notification.show(message);
                        }
                    }
                });
            }
        },

        validateNaturalNumber = function (id, message) {
            if ($(id).val() == '') return 0;
            var input = $(id).val().trim();
            var n = parseInt(input, 10);
            var result = (n >= 0 && n.toString() === input) ? 0 : 1;
            if (result != 0) DojoWeb.Validation.showStandardError(true, id, message);
            return result;
        },

        validateDecimal = function (id, message) {
            if ($(id).val() == '') return 0;
            var input = $(id).val().trim();
            if (input == '') { // all spaces
                DojoWeb.Validation.showStandardParentError(true, id, 'Total Payout is required.');
                return 1;
            }
            else {
                var invalid = isNaN(input);
                if (invalid) DojoWeb.Validation.showStandardParentError(true, id, message);
                return invalid ? 1 : 0;
            }
        },

        validatePropertyCode = function (id) {
            var combobox = $(id).data('kendoComboBox');
            var code = combobox.text();
            if (code == '') return 0;
            if (combobox.selectedIndex == -1) {
                DojoWeb.Validation.showStandardParentError(true, id, 'The Property Code does not exist.');
                return 1;
            }
            else
                return 0;
        },

        // not needed as the date range is guaranteed to be in order
        validateDateRange = function () {
            var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
            if (dateRange.beginDate && dateRange.endDate) {
                return DojoWeb.Validation.validateDateRange('#Check_inDate', '#Check_outDate', 'Check-in date cannot be after check-out date.');
            }
            return 0;
        }

    return {
        init: init,
        getId: getId,
        getProperty: getProperty,
        cancel: cancel,
        switchTabTo: switchTabTo,
        saveApproveForm: saveApproveForm,
        saveEditForm: saveEditForm
    }
}();

DojoWeb.InquiryActionBar = function () {
    var _dateRangeHandle = undefined,

        install = function (beginDate, endDate) {
            _dateRangeHandle = DojoWeb.DateRange.init('beginDatePicker', 'endDatePicker', beginDate, endDate);
            DojoWeb.DateRange.initValidator('actionBarDateRange'); // need html coded properly to engage kendo date range validator
        },
        
        attachFilters = function (goAction, unapprovedFilter, recentlyApprovedFilter, checkInTodayFilter, filterByStatus) {
            $('.actionBar-custom-group').unbind('click').on('click', function (e) {
                $('.actionBar-approval-group').prop('checked', false);
                var unselect = $('#' + e.target.id).hasClass('custom-filter-selected');
                $('.actionBar-custom-group').removeClass('custom-filter-selected');
                if (!unselect) $('#' + e.target.id).addClass('custom-filter-selected');
                switch (e.target.id) {
                    case 'unapproved': unapprovedFilter(e); break;
                    case 'recentlyApproved': recentlyApprovedFilter(e); break;
                    case 'checkinToday': checkInTodayFilter(e); break;
                    default: break;
                }
                return;
            });

            $('.actionBar-approval-group').unbind('click').on('click', function (e) {
                $('.actionBar-custom-group').removeClass('custom-filter-selected');
                if (e.target.id == 'statusOthers') {
                    $('.actionBar-approval-group').prop('checked', false);
                    $('#statusOthers').prop('checked', true);
                }
                else {
                    $('#statusOthers').prop('checked', false);
                }
                filterByStatus(e);
            });

            if (goAction != undefined) {
                $('#actionBarGo').unbind('click').on('click', function (e) {
                    var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
                    goAction(dateRange.beginDate, dateRange.endDate);
                });

                $('#actionBarPrev').unbind('click').on('click', function (e) {
                    var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
                    var endDate = new Date(+dateRange.beginDate);
                    var localBeginDate = new Date(dateRange.beginDate.valueOf());
                    var beginDate = localBeginDate.add(-7).days();
                    DojoWeb.DateRange.setRange(_dateRangeHandle, beginDate, endDate);
                    goAction(beginDate, endDate);
                    e.preventDefault();
                });

                $('#actionBarNext').unbind('click').on('click', function (e) {
                    var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
                    if (dateRange.endDate >= Date.today()) return;
                    var beginDate = new Date(+dateRange.endDate);
                    var localEndDate = new Date(dateRange.endDate.valueOf());
                    var endDate = localEndDate.add(7).days();
                    DojoWeb.DateRange.setRange(_dateRangeHandle, beginDate, endDate);
                    goAction(beginDate, endDate);
                    e.preventDefault();
                });
            }
        },

        resetCustomFilters = function () {
            $('.actionBar-custom-group').removeClass('custom-filter-selected');
            //$('.actionBar-approval-group').prop('checked', false);
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
        attachFilters: attachFilters,
        resetCustomFilters: resetCustomFilters,
        getDateRange: getDateRange,
        setDateRange: setDateRange,
        validateDateRange: validateDateRange
    }
}();

