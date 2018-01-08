"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.Property = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _dataSource = undefined,
        _height = 600,
        _allowCreate = true,
        _highlightRow = undefined,
        _action = undefined,
        _canEdit = false,
        _originalFilter = undefined,
        _localStorageKey = 'propertyGridFilters',

        init = function (gridId, height) {
            _gridId = '#' + gridId;
            _height = height != undefined ? height : _height;
            DojoWeb.Helpers.injectDummyHeaderColumn(_gridId, 16);
            _highlightRow = undefined;
            _action = undefined;
            _highlightRow = undefined;
            _canEdit = $('.app-grid-edit').length > 0; // app-grid-edit class indicate the grid is editable

            initGridFilters();
            render(); // show the grid

            DojoWeb.Notification.init('propertyNotification');
        },

        installEvents = function () {
            $('#actionBarAddNew').addClass('showPropertyNew');
            $('#actionBarAddNew').unbind('click').on('click', function (e) {
                _dataGrid.clearSelection();
                _action = 'new';
            });
            $('.showPropertyEdit').on('click', function (e) {
                _action = 'edit';
            });
            $('.showPropertyDetails').unbind('click').on('click', function (e) {
                _action = 'view';
            });

            // adjust some styling
            $('.app-grid.k-grid td:nth-child(2)').css('padding', '0 5px 0 5px');
            $('.app-grid.k-grid td:nth-child(3)').css('padding', '0 5px 0 5px');

            initDialog('new');
            initDialog('edit');
            initDialog('view');

            // regisgter events to trigger filtering and query from action bar
            DojoWeb.PropertyActionBar.attachFilters(getProperties, filterByMarket, filterByStatus, filterByVertical);

            DojoWeb.Plugin.noHorizontalScroll();

            // event to scroll fixed grid header
            $(window).scroll(function () {
                $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
                //$(_gridId + ' .k-grid-header').css('left', originalLeft - $(this).scrollLeft());
            });

            // not completed yet
            // pass overrideEvent:true into the filter object in order to avoid triggering this event
            //e.g.  dataSource.filter({ "field": "myField",  "operator" : "lt",  "value": '50', overrideEvent: true});
            //$(document).on('syncFilter', function(event, params, dataSource) {
            //    if(params !== null && params.hasOwnProperty('overrideEvent') && params.overrideEvent === true) {
            //        //TODO: check the filter box in action bar
            //    }
            //    else if (params !== null && $.isEmptyObject(params) === false) { // a filter was applied
            //        //filterNotifier.open();
            //    }
            //    else if (params === null || $.isEmptyObject(params) === true) { //a filter was cleared
            //        //filterNotifier.close();
            //    }
            //});

            // adjust grid first row position to work around Mac browser problem
            if (DojoWeb.Helpers.isMac()) { //DojoWeb.Helpers.isSafari()) {
                var height = $('#propertyGrid .k-grid-header').css('height');
                if (height) $('#propertyGrid').css('top', height);
            }
        },

        initDialog = function (action) {
            if (action == 'new') {
                // porperty new form
                DojoWeb.Plugin.initFormDialog({
                    selector: '.showPropertyNew',
                    caption: 'New Property',
                    width: 1200,
                    url: '/Property/New',
                    formId: 'PropertyEntryForm',
                    modal: false,
                    initEvent: DojoWeb.PropertyForm.init,
                    closeEvent: unselectRow
                });
            }
            else if (action == 'edit') {
                // porperty edit form
                var caption = _canEdit ? 'Edit Property' : 'View Property';
                DojoWeb.Plugin.initFormDialog({
                    selector: '.showPropertyEdit',
                    caption: caption,
                    width: 1200,
                    url: '/Property/ModalEdit',
                    formId: 'PropertyEntryForm',
                    modal: false,
                    initEvent: DojoWeb.PropertyForm.init,
                    closeEvent: unselectRow
                });
            }
            else if (action == 'view') {
                // porperty details form
                DojoWeb.Plugin.initFormDialog({
                    selector: '.showPropertyDetails',
                    caption: 'Property Details',
                    width: 1200,
                    url: '/Property/ModalDetails',
                    modal: true
                });
            }
        },

        render = function () {
            var dateRange = DojoWeb.PropertyActionBar.getDateRange();
            getProperties(dateRange.beginDate, dateRange.endDate);
        },

        getProperties = function (beginDate, endDate, isActive, isPending, isDead) {
            DojoWeb.Busy.show();
            saveGridFilters();

            $.get('/Property/Retrieve',
                {
                    beginDate: kendo.toString(beginDate, 'MM/dd/yyyy'),
                    endDate: kendo.toString(endDate, 'MM/dd/yyyy'),
                    isActive: isActive,
                    isPending: isPending,
                    isDead: isDead
                },
                function (data) {
                    clear(); // empty the grid

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.Notification.show('There is no property data available for the given date range.');
                        return;
                    }

                    // round to 2 decimals
                    $.each(data, function (i, item) {
                        item.Ownership = Math.round(item.Ownership*100) / 100;
                    });

                    // init the grid
                    _dataGrid = $(_gridId).kendoGrid(configureGrid()).data('kendoGrid');
                    // bind grid data
                    _dataGrid.bind('dataBound', function (e) {
                        DojoWeb.Busy.hide();
                        installEvents();
                        adjustColumnWidths();
                        setCount();
                        var id = DojoWeb.PropertyForm.getId();
                        if (needHighlightRow()) {
                            DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                            if (_action == 'edit') {
                                DojoWeb.Notification.show('Update of the property "' + id + '" is successful.');
                            }
                        }
                        else if (id != undefined && _action == 'new') {
                            _highlightRow = id;
                            DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                            DojoWeb.Notification.show('Creation of the property "' + id + '" is successful.');
                        }

                        _action = undefined;
                    });
                    // set up data source and trigger grid display
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
                    alert('There was an error retrieving property data. Please try refreshing this page. If the issue persists please contact the tool administrator.',
                          DojoWeb.Alert.alertTypes().error);
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

        filterByMarket = function (e) { // this filter is deprecated to make room for property status filter
            var selected = $('#' + e.target.id).hasClass('custom-filter-selected');
            if (selected) {
                var market = e.target.innerText;
                var field = $('#' + e.target.id).attr('data-field');
                var customFilter = { field: field, operator: 'eq', value: market };
                applyCustomFilters(customFilter, [field]);
            }
        },

        filterByStatus = function (e) {
            // hack a bit here
            var field = 'PropertyStatus';
            var customFilters = { filters: [], logic: 'or' };
            var checked = $('#CustomStatusActive').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'startswith', value: 'Active' });
            checked = $('#CustomStatusContract').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'eq', value: 'Pending-Contract' });
            checked = $('#CustomStatusOnboard').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'contains', value: 'Pending-Onboard' });
            checked = $('#CustomStatusInactive').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'eq', value: 'Inactive' });
            checked = $('#CustomStatusDead').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'contains', value: 'Dead' });

            if (customFilters.filters.length <= 0)
                customFilters = { field: field, operator: 'neq', value: 'None' }; // something not a legal status

            applyCustomFilters(customFilters, ['PropertyStatus']);
        },

        filterByVertical = function (e) {
            // hack a bit here
            var field = 'Vertical';
            var customFilters = { filters: [], logic: 'or' };
            var checked = $('#CustomVerticalFS').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'eq', value: 'FS' });
            checked = $('#CustomVerticalRS').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'eq', value: 'RS' });
            checked = $('#CustomVerticalSenStay').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'eq', value: 'CO' });
            checked = $('#CustomVerticalHotels').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'eq', value: 'HO' });
            checked = $('#CustomVerticalJV').is(':checked');
            if (checked) customFilters.filters.push({ field: field, operator: 'eq', value: 'JV' });

            if (customFilters.filters.length <= 0)
                customFilters = { field: field, operator: 'neq', value: 'None' }; // something not a legal vertical

            applyCustomFilters(customFilters, ['Vertical']);
        },

        // TODO: catch filter event 
        syncFilterWithActionbar = function () {
            if (_originalFilter != undefined) {
                kendo.data.DataSource.fn.filter = function (e) {
                    if (arguments.length > 0) {
                        $.event.trigger('syncFilter', [e, $(this)]);
                    }
                    return _originalFilter.apply(this, arguments);
                };
            }
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
                sortable: false,
                editable: false,
                reorderable: true,
                selectable: true,
                toolbar: null, //_canEdit ? [{ name: 'create', text: 'New Property' }] : null,
                columns: [
                            {
                                field: 'edit',
                                title: ' ',
                                filterable: false,
                                template: "#= DojoWeb.Property.renderAction(data.PropertyCode, 'edit')#",
                                lockable: true,
                                hidden: false,
                            },
                            { field: 'PropertyStatus', title: 'Status', filterable: false, template: "#= DojoWeb.Template.active(data.PropertyStatus) #" },
                            { field: 'Vertical', title: 'Product', filterable: false, template: "#= DojoWeb.Template.nullable(data.Vertical) #" },
                            { field: 'PropertyCode', title: 'Property Code', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Property.propertyLink(data.PropertyCode) #" },
                            { field: 'AirbnbiCalexportlink', title: 'Operations Contact', filterable: false, template: "#= DojoWeb.Template.linkOrText(data.AirbnbiCalexportlink, 'Contact Link') #" },
                            { field: 'AirBnBHomeName', title: 'Airbnb Listing Title', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Address', title: 'Address', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.googleMap(data.Address, data.City, data.State, data.Zipcode) #" },
                            { field: 'Market', title: 'Market', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Market) #" },
                            { field: 'Bedrooms', title: 'Bedrooms', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Bedrooms) #" },
                            { field: 'BedsDescription', title: 'Beds Description', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Floor', title: 'Fl', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Floor) #" },
                            { field: 'Elevator', title: 'Elevator', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Elevator) #" },
                            { field: 'MaxOcc', title: 'Max Occ', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.MaxOcc) #" },
                            { field: 'Parking', title: 'Parking', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Parking) #" },
                            { field: 'Pool', title: 'Pool', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Pool) #" },
                            { field: 'CheckInType', title: 'CheckIn Type', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.CheckInType) #" },
                            { field: 'AirBnb', title: 'AirBnb Link', filterable: false, template: "#= DojoWeb.Template.makelink(data.AIrBnBID, 'AirBnb Link', 'https://www.airbnb.com/rooms/') #" },
                            { field: 'Amenities', title: 'Checklist', filterable: false, template: "#= DojoWeb.Template.link(data.Amenities, 'Checklist Link') #" },
                            { field: 'BookingGuidelines', title: 'Booking Guidelines', filterable: DojoWeb.Template.textSearch(), template: "#= DojoWeb.Template.nullable(data.BookingGuidelines) #" },
                            { field: 'Owner', title: 'Owner Contact', filterable: DojoWeb.Template.textSearch() },
                            { field: 'NeedsOwnerApproval', title: 'Owner Approval?', filterable: { multi: true }, template: "#= DojoWeb.Template.boolean(data.NeedsOwnerApproval) #" },
                            { field: 'ListingStartDate', title: 'Listing Start', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'StreamlineHomeName', title: 'Streamline Listing Title', filterable: DojoWeb.Template.textSearch() },
                            { field: 'StreamlineUnitID', title: 'Streamline Home Link', filterable: false, template: "#= DojoWeb.Template.makelink(data.StreamlineUnitID, 'Streamline Link', 'https://admin.streamlinevrs.com/edit_home.html?home_id=') #" },
                            { field: 'Account', title: 'Airbnb Account', filterable: DojoWeb.Template.textSearch() },
                            { field: 'City', title: 'City', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.City) #" },
                            { field: 'State', title: 'State', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.State) #" },
                            { field: 'Area', title: 'Area', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Area) #" },
                            { field: 'Neighborhood', title: 'Neighborhood', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Neighborhood) #" },
                            { field: 'Bathrooms', title: 'Bathrooms', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Bathrooms) #" },
                            { field: 'StdOcc', title: 'Std Occ', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.StdOcc) #" },
                            { field: 'A_C', title: 'A/C', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.A_C) #" },
                            { field: 'WiFiNetwork', title: 'Wi-Fi Network', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.WiFiNetwork) #" },
                            { field: 'WiFiPassword', title: 'Wi-Fi Password', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.WiFiPassword) #" },
                            { field: 'Ownership', title: 'Mgmt Fee', filterable: { multi: true }, template: "#= DojoWeb.Template.percentage(data.Ownership) #" },
                            { field: 'MonthlyRent', title: 'Monthly Rent', filterable: true },
                            { field: 'DailyRent', title: 'Daily Rent', filterable: true },
                            { field: 'CleaningFees', title: 'Cleaning Fees', filterable: true },
                            { field: 'Currency', title: 'Currency', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Currency) #" },
                            { field: 'Zipcode', title: 'Zipcode', filterable: { multi: true }, template: "#= DojoWeb.Template.nullable(data.Zipcode) #" },
                            { field: 'OldListingTitle', title: 'Old Listing Title', filterable: DojoWeb.Template.textSearch() },
                            { field: 'GoogleDrivePicturesLink', title: 'Copy Document', filterable: false, template: "#= DojoWeb.Template.link(data.GoogleDrivePicturesLink, 'Picture Link') #" },
                            { field: 'SquareFootage', title: 'Square Footage', filterable: true },
                            { field: 'SecurityDeposit', title: 'Security Deposit', filterable: true },
                            { field: 'HomeAway', title: 'HomeAway Property Link', filterable: false, template: "#= DojoWeb.Template.makelink(data.HomeAway, 'HomeAway Link', 'https://www.homeaway.com/vacation-rental/p') #" },
                            { field: 'BeltDesignation', title: 'Belt Designation', filterable: { multi: true }, template: "#= DojoWeb.Template.belt(data.BeltDesignation) #" },
                            { field: 'Inactive', title: 'Inactive', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'CreatedDate', title: 'Created Date', filterable: DojoWeb.Template.dateSearch(), format: '{0:MM/dd/yyyy}' },
                            { field: 'PendingContractDate', title: 'Pending Contract Date', filterable: true, hidden: true },
                            { field: 'PendingOnboardingDate', title: 'Pending Onboarding Date', filterable: true, hidden: true },
                            { field: 'FlipKey', title: 'FlipKey Link', filterable: false, hidden: true },
                            { field: 'Expedia', title: 'Expedia Link', filterable: false, hidden: true },
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
                schema: {
                    model: {
                        id: 'PropertyCode',
                        fields: {
                            PropertyCode: { type: 'string', editable: false, nullable: true },
                            NeedsOwnerApproval: { type: 'number', editable: false, nullable: true },
                            ListingStartDate: { type: 'date', editable: false, nullable: true },
                            Bedrooms: { type: 'number', editable: false, nullable: true },
                            Bathrooms: { type: 'number', editable: false, nullable: true },
                            MaxOcc: { type: 'number', editable: false, nullable: true },
                            StdOcc: { type: 'number', editable: false, nullable: true },
                            Ownership: { type: 'number', editable: false, nullable: true },
                            MonthlyRent: { type: 'number', editable: false, nullable: true },
                            DailyRent: { type: 'number', editable: false, nullable: true },
                            CleaningFees: { type: 'number', editable: false, nullable: true },
                            SquareFootage: { type: 'number', editable: false, nullable: true },
                            InquiryLeadApproval: { type: 'number', editable: false, nullable: true },
                            RevTeam2xApproval: { type: 'number', editable: false, nullable: true },
                            PendingContractDate: { type: 'date', editable: false, nullable: true },
                            PendingOnboardingDate: { type: 'date', editable: false, nullable: true },
                            Inactive: { type: 'date', editable: false, nullable: true },
                            CreatedDate: { type: 'date', editable: false, nullable: true },
                        }
                    }
                }
            });
        },

        renderAction = function (id, action) {
            if (_canEdit) {
                if (action == 'edit') {
                    return "<div id='edit-id-" + id + "' class='showPropertyEdit gridcell-btn dojo-center' title='Edit Property' data-id='" + id + "'><div class='btn'><i class='fa fa-wrench'></i></div></div>";
                }
            }
            else {
                if (action == 'edit') {
                    return "<div id='edit-id-" + id + "' class='showPropertyEdit gridcell-btn dojo-center' title='Edit Property' data-id='" + id + "'><div class='btn'><i class='fa fa-eye'></i></div></div>";
                }
            }
            return '';
        },

        propertyLink = function (propertyCode) {
            return propertyCode;
            //return '<div class="showPropertyDetails" data-id="' + propertyCode + '" style="text-align:center;">' + propertyCode + '</div>';
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-index="0"]').css('min-width', '24px');
            $(_gridId + ' th[data-field="PropertyStatus"]').css('min-width', '60px').css('max-width', '60px');
            $(_gridId + ' th[data-field="Vertical"]').css('min-width', '70px');
            $(_gridId + ' th[data-field="PropertyCode"]').css('min-width', '110px');
            $(_gridId + ' th[data-field="AirbnbiCalexportlink"]').css('min-width', '250px');
            $(_gridId + ' th[data-field="AirBnBHomeName"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="Address"]').css('min-width', '180px');
            $(_gridId + ' th[data-field="Market"]').css('min-width', '90px');
            $(_gridId + ' th[data-field="Bedrooms"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="BedsDescription"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="Floor"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Elevator"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="MaxOcc"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Parking"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="Pool"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="CheckInType"]').css('min-width', '170px');
            $(_gridId + ' th[data-field="AirBnb"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Amenities"]').css('min-width', '100px');

            $(_gridId + ' th[data-field="BookingGuidelines"]').css('min-width', '250px');
            $(_gridId + ' th[data-field="Owner"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="NeedsOwnerApproval"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="ListingStartDate"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="StreamlineHomeName"]').css('min-width', '200px');
            $(_gridId + ' th[data-field="StreamlineUnitID"]').css('min-width', '90px');
            $(_gridId + ' th[data-field="Account"]').css('min-width', '240px');
            $(_gridId + ' th[data-field="City"]').css('min-width', '90px');
            $(_gridId + ' th[data-field="State"]').css('min-width', '90px');

            $(_gridId + ' th[data-field="Area"]').css('min-width', '90px');
            $(_gridId + ' th[data-field="Neighborhood"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="Bathrooms"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="StdOcc"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="A_C"]').css('min-width', '80px');

            $(_gridId + ' th[data-field="WiFiNetwork"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="WiFiPassword"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="Ownership"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="MonthlyRent"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="DailyRent"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="CleaningFees"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Currency"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="Zipcode"]').css('min-width', '80px');

            $(_gridId + ' th[data-field="OldListingTitle"]').css('min-width', '150px');
            $(_gridId + ' th[data-field="GoogleDrivePicturesLink"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="SquareFootage"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="SecurityDeposit"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="HomeAway"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="BeltDesignation"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="Inactive"]').css('min-width', '80px');
            $(_gridId + ' th[data-field="CreatedDate"]').css('min-width', '100px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            $(_gridId + ' tr td:nth-child(1)').css('min-width', '40px');   // edit
            $(_gridId + ' tr td:nth-child(2)').css('min-width', '67px').css('max-width', '67px');   // PropertyStatus
            $(_gridId + ' tr td:nth-child(3)').css('min-width', '78px').css('max-width', '78px');   // Prod (Vertical)
            $(_gridId + ' tr td:nth-child(4)').css('min-width', '110px').css('max-width', '110px');   // PropertyCode
            $(_gridId + ' tr td:nth-child(5)').css('min-width', '250px').css('max-width', '250px');   // Operations Contact (AirbnbiCalexportlink)
            $(_gridId + ' tr td:nth-child(6)').css('min-width', '200px').css('max-width', '200px');  // AirBnBHomeName
            $(_gridId + ' tr td:nth-child(7)').css('min-width', '180px').css('max-width', '180px');   // Address
            $(_gridId + ' tr td:nth-child(8)').css('min-width', '90px').css('max-width', '90px');   // Market
            $(_gridId + ' tr td:nth-child(9)').css('min-width', '80px').css('max-width', '80px');   // Bedrooms
            $(_gridId + ' tr td:nth-child(10)').css('min-width', '160px').css('max-width', '160px');   // BedsDescription
            $(_gridId + ' tr td:nth-child(11)').css('min-width', '80px').css('max-width', '80px');   // Floor
            $(_gridId + ' tr td:nth-child(12)').css('min-width', '80px').css('max-width', '80px');   // Elevator
            $(_gridId + ' tr td:nth-child(13)').css('min-width', '80px').css('max-width', '80px');   // MaxOcc
            $(_gridId + ' tr td:nth-child(14)').css('min-width', '120px').css('max-width', '170px');   // Parking
            $(_gridId + ' tr td:nth-child(15)').css('min-width', '80px').css('max-width', '80px');   // Pool
            $(_gridId + ' tr td:nth-child(16)').css('min-width', '170px').css('max-width', '170px');   // CheckInType
            $(_gridId + ' tr td:nth-child(17)').css('min-width', '80px').css('max-width', '80px');   // AirBnb
            $(_gridId + ' tr td:nth-child(18)').css('min-width', '100px').css('max-width', '100px');   // Amenities

            $(_gridId + ' tr td:nth-child(19)').css('min-width', '250px').css('max-width', '250px');   // BookingGuidelines
            $(_gridId + ' tr td:nth-child(20)').css('min-width', '120px').css('max-width', '120px');   // Owner
            $(_gridId + ' tr td:nth-child(21)').css('min-width', '80px').css('max-width', '80px');   // NeedsOwnerApproval
            $(_gridId + ' tr td:nth-child(22)').css('min-width', '100px').css('max-width', '100px');   // ListingStartDate
            $(_gridId + ' tr td:nth-child(23)').css('min-width', '200px').css('max-width', '200px');   // StreamlineHomeName
            $(_gridId + ' tr td:nth-child(24)').css('min-width', '90px').css('max-width', '90px');   // StreamlineUnitID
            $(_gridId + ' tr td:nth-child(25)').css('min-width', '240px').css('max-width', '240px');   // Account
            $(_gridId + ' tr td:nth-child(26)').css('min-width', '90px').css('max-width', '90px');   // City
            $(_gridId + ' tr td:nth-child(27)').css('min-width', '90px').css('max-width', '90px');   // State

            $(_gridId + ' tr td:nth-child(28)').css('min-width', '90px').css('max-width', '90px');   // Area
            $(_gridId + ' tr td:nth-child(29)').css('min-width', '120px').css('max-width', '120px');   // Neighborhood
            $(_gridId + ' tr td:nth-child(30)').css('min-width', '80px').css('max-width', '80px');   // Bathrooms
            $(_gridId + ' tr td:nth-child(31)').css('min-width', '80px').css('max-width', '80px');   // StdOcc
            $(_gridId + ' tr td:nth-child(32)').css('min-width', '80px').css('max-width', '80px');   // A_C

            $(_gridId + ' tr td:nth-child(33)').css('min-width', '160px').css('max-width', '160px');   // WiFiNetwork
            $(_gridId + ' tr td:nth-child(34)').css('min-width', '150px').css('max-width', '150px');   // WiFiPassword
            $(_gridId + ' tr td:nth-child(35)').css('min-width', '80px').css('max-width', '80px');   // Ownership
            $(_gridId + ' tr td:nth-child(36)').css('min-width', '80px').css('max-width', '80px');   // MonthlyRent
            $(_gridId + ' tr td:nth-child(37)').css('min-width', '80px').css('max-width', '80px');   // DailyRent
            $(_gridId + ' tr td:nth-child(38)').css('min-width', '80px').css('max-width', '80px');   // CleaningFees
            $(_gridId + ' tr td:nth-child(39)').css('min-width', '80px').css('max-width', '80px');   // Currency
            $(_gridId + ' tr td:nth-child(40)').css('min-width', '80px').css('max-width', '80px');   // Zipcode

            $(_gridId + ' tr td:nth-child(41)').css('min-width', '150px').css('max-width', '150px');;   // OldListingTitle
            $(_gridId + ' tr td:nth-child(42)').css('min-width', '80px').css('max-width', '80px');   // GoogleDrivePicturesLink
            $(_gridId + ' tr td:nth-child(43)').css('min-width', '80px').css('max-width', '80px');   // SquareFootage
            $(_gridId + ' tr td:nth-child(44)').css('min-width', '80px').css('max-width', '80px');   // SecurityDeposit
            $(_gridId + ' tr td:nth-child(45)').css('min-width', '120px').css('max-width', '120px');   // HomeAway
            $(_gridId + ' tr td:nth-child(46)').css('min-width', '120px').css('max-width', '120px');   // BeltDesignation
            $(_gridId + ' tr td:nth-child(47)').css('min-width', '80px').css('max-width', '80px');   // Inactive
            $(_gridId + ' tr td:nth-child(48)').css('min-width', '110px').css('max-width', '110px');   // CreatedDate
        },

        readjustHeaderWidths = function () {
            for (var i = 2; i <= 48; i++) {
                var $td = $(_gridId + ' tr td:nth-child(' + i + ')');
                var $th = $(_gridId + ' tr th:nth-child(' + i + ')');
                var tdWidth = $td.width();
                var thWidth = $th.width();
                if (tdWidth - thWidth >= 1) {
                    //$th.css('min-width', tdWidth + 'px');
                    $th.css('width', tdWidth + 'px');
                }
                else if (thWidth - tdWidth >= 1) {
                    //$td.css('min-width', thWidth + 'px');
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
        },

        unselectRow = function () {
            if (_dataGrid) _dataGrid.clearSelection();
            _highlightRow = undefined;
        },

        adjustEditorIcon = function (name) {
            $($($('input[name="' + name + '"]').parent().children('span')[0]).children()[0]).html('');
        }

    return {
        init: init,
        propertyLink: propertyLink,
        getProperties: getProperties,
        filterByMarket: filterByMarket,
        filterByStatus: filterByStatus,
        filterByVertical: filterByVertical,
        renderAction: renderAction,
        updateRow: updateRow,
        unselectRow: unselectRow
    }
}();

DojoWeb.PropertyForm = function () {
    var _formId = undefined,
        _currentId = undefined,

        init = function (formId) {
            _formId = formId;
            _currentId = undefined;
            installControls();
        },

        getId = function () {
            return _currentId;
        },

        installControls = function () {
            if ($('.app-form-view').length == 0) { // editing mode
            }
            else { // disable ui control editing for view-only mode
                $('.app-form-view').prop('readonly', true);
                $('.app-form-view :not(:selected)').prop('disabled', true);
            }

            $('#propertySave').click(function (e) {
                e.preventDefault();
                save();
            });

            $('#propertyCancel').click(function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
            });

            // when this class is set, it indicates the boolean field is null
            if ($('.no-boolean-selection').length > 0) $('#NeedsOwnerApproval').val('');
        },

        save = function () {
            if (validate()) {
                var $form = $('#' + _formId);
                if ($form.valid()) {
                    DojoWeb.Busy.show();
                    var formData = $form.serialize(); // this is a query string format; not json format
                    $.ajax({
                        type: 'POST',
                        url: '/Property/ModalEdit',
                        data: formData,
                        success: function (result) {
                            DojoWeb.Busy.hide();
                            DojoWeb.Plugin.closeFormDialog();
                            if (result != '') {
                                _currentId = result;
                                // if the property date range is not up to today, the newly added property won't show up in the grid
                                var dateRange = DojoWeb.PropertyActionBar.getDateRange();
                                if (dateRange.endDate < Date.today()) {
                                    DojoWeb.Notification.show('Please change "To" date textbox to today to see the newly added property.');
                                    // change end date to today so that the added property can be seen
                                    //DojoWeb.PropertyActionBar.setDateRange(dateRange.beginDate, Date.today());
                                }
                                DojoWeb.Property.updateRow(_currentId);
                            }
                            else {
                                DojoWeb.Notification.show('There was an error saving the property.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            DojoWeb.Busy.hide();
                            if (status == 'error') {
                                alert('There was an error saving the property.');
                            }
                        }
                    });
                }
            }
        },

        cancel = function () {
            $('#formDialog').data('kendoWindow').close();
            DojoWeb.Property.unselectRow();
        },

        refresh = function () {
        },

        validate = function () {
            $('#InputErrorMessage').html('');
            var validated = $('#AirBnBHomeName').val().indexOf(',') == -1; // no , in the property title
            if (!validated) {
                $('#InputErrorMessage').html('Airbnb Listing Title cannot contain comma character.');
            }
            return validated;
        },

        serverError = function () {
            alert('There is server error.');
        },

        dateMarker = function (d1) {
            var date1 = new Date(d1);
            var daterez = Math.floor((Date.now() - date1) / 86400000);
            if (daterez < 30) { return "New" } else { return "" }
        }

    return {
        init: init,
        getId: getId,
        refresh: refresh,
        serverError: serverError,
        dateMarker: dateMarker
    }
}();

DojoWeb.PropertyActionBar = function () {
    var _dateRangeHandle = undefined,
        _cachedFilters = [],

        install = function (beginDate, endDate) {
            _dateRangeHandle = DojoWeb.DateRange.init('beginDatePicker', 'endDatePicker', beginDate, endDate);
            DojoWeb.DateRange.initValidator('actionBarDateRange'); // need html coded properly to engage kendo date range validator
            initStatus();
        },

        initStatus = function () {
            $('#GoActive').prop('checked', true);
            $('#GoPending').prop('checked', true);
            $('#GoInactiveDead').prop('checked', false);
        },

        attachFilters = function (goAction, marketFilters, statusFilters, VerticalFilters) {
            if (marketFilters != undefined && $('.actionBar-market-group').length > 0) {
                $('.actionBar-market-group').unbind('click').on('click', function (e) {
                    var unselect = $('#' + e.target.id).hasClass('custom-filter-selected');
                    $('.actionBar-market-group').removeClass('custom-filter-selected');
                    if (!unselect) $('#' + e.target.id).addClass('custom-filter-selected');
                    marketFilters(e);
                });
            }

            if (statusFilters != undefined && $('.actionBar-status-group').length > 0) {
                $('.actionBar-status-group').unbind('click').on('click', function (e) {
                    var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
                    statusFilters(e);
                });
            }

            if (VerticalFilters != undefined && $('.actionBar-vertical-group').length > 0) {
                $('.actionBar-vertical-group').unbind('click').on('click', function (e) {
                    var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
                    VerticalFilters(e);
                });
            }

            if (goAction != undefined) {
                $('#actionBarGo').unbind('click').on('click', function (e) {
                    var dateRange = DojoWeb.DateRange.getRange(_dateRangeHandle);
                    var isActive = $('#GoActive').is(":checked");
                    var isPending = $('#GoPending').is(':checked');
                    var isDead = $('#GoInactiveDead').is(':checked');
                    goAction(dateRange.beginDate, dateRange.endDate, isActive, isPending, isDead);
                });
            }
        },

        cacheFilters = function () {
            _cachedFilters = [];

            if ($('#CustomStatusActive').is(':checked')) _cachedFilters.push({ id: '#CustomStatusActive', selected: true });
            if ($('#CustomStatusContract').is(':checked')) _cachedFilters.push({ id: '#CustomStatusContract', selected: true });
            if ($('#CustomStatusOnboard').is(':checked')) _cachedFilters.push({ id: '#CustomStatusOnboard', selected: true });
            if ($('#CustomStatusInactive').is(':checked')) _cachedFilters.push({ id: '#CustomStatusInactive', selected: true });
            if ($('#CustomStatusDead').is(':checked')) _cachedFilters.push({ id: '#CustomStatusDead', selected: true });

            if ($('#CustomStatusActive').is(':checked')) _cachedFilters.push({ id:'#CustomStatusActive', selected: true });
            if ($('#CustomStatusContract').is(':checked')) _cachedFilters.push({ id: '#CustomStatusContract', selected: true });
            if ($('#CustomStatusOnboard').is(':checked')) _cachedFilters.push({ id: '#CustomStatusOnboard', selected: true });
            if ($('#CustomStatusInactive').is(':checked')) _cachedFilters.push({ id: '#CustomStatusInactive', selected: true });
            if ($('#CustomStatusDead').is(':checked')) _cachedFilters.push({ id: '#CustomStatusDead', selected: true });

            if ($('#GoActive').is(':checked')) _cachedFilters.push({ id: '#GoActive', selected: true });
            if ($('#GoPending').is(':checked')) _cachedFilters.push({ id: '#GoPending', selected: true });
            if ($('#GoInactiveDead').is(':checked')) _cachedFilters.push({ id: '#GoInactiveDead', selected: true });
        },

        setCachedFilters = function () {
            $.each(_cachedFilters, function(i, cache) {
                if (cache.seleced) $(cache.id).prop('checked', false);
            });
        },

        clearFilters = function () {
            $('.actionBar-market-group').removeClass('custom-filter-selected');
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
        cacheFilters: cacheFilters,
        setCachedFilters: setCachedFilters,
        clearFilters: clearFilters,
        getDateRange: getDateRange,
        setDateRange: setDateRange,
        validateDateRange: validateDateRange
    }
}();

