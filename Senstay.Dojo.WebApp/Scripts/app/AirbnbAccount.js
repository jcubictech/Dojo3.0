"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.AirbnbAccount = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _dataSource = undefined,
        _height = 600,
        _marketFilters = undefined,
        _statusFilters = undefined,
        _verticalFilters = undefined,
        _highlightRow = undefined,
        _action = undefined,
        _canEdit = false,

        init = function (gridId, height, allowCreate) {
            _gridId = '#' + gridId;
            _height = height != undefined ? height : _height;
            DojoWeb.Helpers.injectDummyHeaderColumn(_gridId, 16);
            _marketFilters = undefined;
            _statusFilters = undefined;
            _verticalFilters = undefined;
            _action = undefined;
            _highlightRow = undefined;
            _canEdit = $('.app-grid-edit').length > 0; // app-grid-edit class indicate the grid is editabe

            render(); // show the grid

            DojoWeb.Notification.init('accountNotification');
        },

        installEvents = function () {
            $('#actionBarAddNew').addClass('showAirbnbAccountNew');

            $('#actionBarAddNew').unbind('click').on('click', function (e) {
                _dataGrid.clearSelection();
                _action = 'new';
            });

            $('.showAirbnbAccountEdit').unbind('click').on('click', function (e) {
                _action = 'edit';
            });

            initDialog('new');
            initDialog('edit');

            DojoWeb.Plugin.noHorizontalScroll();

            DojoWeb.AccountActionBar.attachEvent(getAirbnbAccounts);

            // event to scroll fixed grid header
            $(window).scroll(function () {
                $(_gridId + ' .k-grid-header').css('margin-left', -$(this).scrollLeft());
                //$(_gridId + ' .k-grid-header').css('left', originalLeft - $(this).scrollLeft());
            });

            // excel export
            //$('#actionBarExport').on('click', function (e) {
            //    DojoWeb.ExcelExport.download(_gridId);
            //});

            // when 'edit' button is clicked, it replaces with built-in 'update' and 'cancel' button with text.
            // this event removes the 'update' and 'cancel' text and replace icon with bootstrap's
            //$(_gridId).unbind('click').on("click", ".k-grid-edit", function () {
            //    $(".k-grid-update").html("<span class='k-icon k-update'></span>").css("min-width", "16px").removeClass("k-button-icontext");
            //    $(".k-grid-cancel").html("<span class='k-icon k-cancel'></span>").css("min-width", "16px").removeClass("k-button-icontext");
            //});

        },

        initDialog = function (action) {
            if (action == 'new') {
                // Airbnb Account new form
                DojoWeb.Plugin.initFormDialog({
                    selector: '.showAirbnbAccountNew',
                    caption: 'New Airbnb Account',
                    width: 1200,
                    url: '/AirbnbAccount/New',
                    formId: 'AirbnbAccountNewForm',
                    initEvent: DojoWeb.AirbnbAccountForm.init,
                    closeEvent: unselectRow,
                });
            }
            else if (action == 'edit') {
                // Airbnb Account edit form
                var caption = _canEdit ? 'Edit Airbnb Account' : 'View Airbnb Account';
                DojoWeb.Plugin.initFormDialog({
                    selector: '.showAirbnbAccountEdit',
                    caption: 'Edit Airbnb Account',
                    width: 1200,
                    url: '/AirbnbAccount/ModalEdit',
                    formId: 'AirbnbAccountEditForm',
                    initEvent: DojoWeb.AirbnbAccountForm.init,
                    closeEvent: unselectRow,
                });
            }
        },

        render = function () {
            var dateRange = DojoWeb.AccountActionBar.getDateRange();
            getAirbnbAccounts(dateRange.beginDate, dateRange.endDate);
        },

        getAirbnbAccounts = function (beginDate, endDate, hasFilter) {
            DojoWeb.Busy.show(); // wait animation is a globally available function

            $.get('/AirbnbAccount/Retrieve',
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
                        installEvents();
                        adjustColumnWidths();
                        readjustHeaderWidths();
                        setCount();
                        var id = DojoWeb.AirbnbAccountForm.getId();
                        if (needHighlightRow()) {
                            DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                            if (_action == 'edit') {
                                DojoWeb.Notification.show('Update of the Airbnb account "' + id + '" is successful.');
                            }
                        }
                        else if (id != undefined && _action == 'new') {
                            _highlightRow = id;
                            DojoWeb.GridHelper.selectRow($(_gridId), _highlightRow);
                            DojoWeb.Notification.show('Creation of the Airbnb account "' + id + '" is successful.');
                        }
                        else if (_action == 'delete') {
                            DojoWeb.Notification.show('Deletion of Airbnb account "' + id + '" is successful.');
                        }
                        _action = undefined;
                    });
                    // set data source and trigger grid display
                    _dataGrid.setDataSource(configureDataSource(data));

                    if (data == '') {
                        DojoWeb.Busy.hide();
                        DojoWeb.Notification.show('There is no Airbnb account data available for the given date range.');
                        return;
                    }

                    // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
                    // we remove the 'filter' text ad-hoc here
                    $(_gridId + ' span.k-filter').text('');
                })
                .error(function (errData) {
                    clear();
                    DojoWeb.Busy.hide();
                    alert('There was an error retrieving AirbnbAccount data. Please try refreshing this page. If the issue persists please contact the tool administrator.',
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
                sortable: false,
                editable: false,
                reorderable: true,
                selectable: true,
                // this is for editing by dialog
                //{
                //    mode: 'popup',
                //    template: $("#propertyEditorTemplate").html(),
                //    update: true,
                //    destroy: true,
                //    confirmation: "So you want to remove this AirbnbAccount. Confirm you really want to do it."
                //},
                toolbar: null, //_canEdit ? [{ name: 'create', text: 'New AirbnbAccount' }] : null,
                columns: [
                            {
                                field: 'edit',
                                title: ' ',
                                filterable: false,
                                template: "#= DojoWeb.AirbnbAccount.renderAction(data.Id, 'edit')#",
                                lockable: true,
                                hidden: false
                            },
                            {
                                field: 'delete',
                                title: ' ',
                                filterable: false,
                                template: "#= DojoWeb.AirbnbAccount.renderAction(data.Id, 'delete')#",
                                locked: true,
                                hidden: !_canEdit
                            },
                            { field: 'Id', title: 'ID', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'Email', title: 'Email', filterable: DojoWeb.Template.textSearch() },
                            { field: 'Password', title: 'Password', filterable: DojoWeb.Template.textSearch(), hidden: true },
                            { field: 'Gmailpassword', title: 'Gmail Password', filterable: DojoWeb.Template.textSearch(), hidden: true },
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

        renderAction = function (id, action) {
            if (_canEdit) {
                if (action == 'edit') {
                    return "<div id='edit-id-" + id + "' class='showAirbnbAccountEdit gridcell-btn dojo-center' title='Edit Airbnb Account' data-id='" + id + "'><div class='btn'><i class='fa fa-wrench'></i></div></div>";
                }
                else if (action == 'delete') {
                    return "<div id='delete-id-" + id + "' class='gridcell-btn dojo-center' title='Delete Airbnb Account' onClick='DojoWeb.AirbnbAccount.renderDelete(" + '"' + id + '"' + ");'><div class='btn'><i class='fa fa-trash-o'></i></div></div>";
                }
            }
            else {
                if (action == 'edit') {
                    return "<div id='edit-id-" + id + "' class='showAirbnbAccountEdit gridcell-btn dojo-center' title='Edit Airbnb Account' data-id='" + id + "'><div class='btn'><i class='fa fa-eye'></i></div></div>";
                }
            }
            return '';
        },

        renderDelete = function (id) {
            DojoWeb.Confirmation.confirmDiscard({
                id: 'confirmation-dialog',
                caption: 'Delete Airbnb Account Confirmation',
                message: 'The selected Airbnb Account will be deleted. Please confirm.',
                ok: function () {
                    $.ajax({
                        type: 'POST',
                        url: '/AirbnbAccount/Delete/?id=' + id,
                        success: function (result) {
                            if (result == 'success') {
                                _action = 'delete';
                                setHighlightRow(undefined);
                                render();
                            }
                            else {
                                DojoWeb.Notification.show('There was an error deleting the Airbnb account.');
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            if (status == 'error') {
                                var message = 'There was an error deleting the Airbnb account.';
                                alert(message);
                            }
                        }
                    });
                }
            });
        },

        adjustColumnWidths = function () {
            // try to make grid header text appear cleanly
            $(_gridId + ' th[data-index="0"]').css('min-width', '22px');
            $(_gridId + ' th[data-index="1"]').css('min-width', '20.5px');
            $(_gridId + ' th[data-field="Email"]').css('min-width', '221.5px');
            $(_gridId + ' th[data-field="Status"]').css('min-width', '60px');
            $(_gridId + ' th[data-field="DateAdded"]').css('min-width', '100px');
            $(_gridId + ' th[data-field="SecondaryAccountEmail"]').css('min-width', '250px');
            $(_gridId + ' th[data-field="AccountAdmin"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="Owner_Company"]').css('min-width', '130px');
            $(_gridId + ' th[data-field="Name"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="PhoneNumber1"]').css('min-width', '120px');
            $(_gridId + ' th[data-field="PhoneNumberOwner"]').css('min-width', '168px');
            $(_gridId + ' th[data-field="Payout_Method"]').css('min-width', '154px');
            $(_gridId + ' th[data-field="PointofContact"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="PhoneNumber2"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="EmailAddress"]').css('min-width', '193px');
            $(_gridId + ' th[data-field="ActiveListings"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="Pending_Onboarding"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="In_activeListings"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="ofListingsinLAMarket"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="ofListingsinNYCMarket"]').css('min-width', '160px');
            $(_gridId + ' th[data-field="ProxyIP"]').css('min-width', '140px');
            $(_gridId + ' th[data-field="C2ndProxyIP"]').css('min-width', '140px');

            //Adjust field width
            //$(_gridId + ' th[data-field="Address"]').css('min-width', '160px');
            //$(_gridId + ' th[data-field="BedsDescription"]').css('min-width', '160px');
            //$(_gridId + ' th[data-field="StreamlineHomeName"]').css('min-width', '160px'); 
            //$(_gridId + ' th[data-field="ListingStartDate"]').css('min-width', '100px');
            //$(_gridId + ' th[data-field="CheckInType"]').css('min-width', '100px'); 
            //$(_gridId + ' th[data-field="Account"]').css('min-width', '100px');
            //$(_gridId + ' th[data-field="BookingGuidelines"]').css('min-width', '250px');
            //$(_gridId + ' th[data-field="Amenities"]').css('min-width', '100px');
            //$(_gridId + ' th[data-field="HomeAway"]').css('min-width', '120px');
            //$(_gridId + ' th[data-field="BeltDesignation"]').css('min-width', '120px');

            // need to set minimum width for each td column as the header is fixed to top and cannot be used for content columns by kendo
            $(_gridId + ' tr td:nth-child(4)').css('min-width', '221.5px');   // Email
            $(_gridId + ' tr td:nth-child(7)').css('min-width', '60px');   // Status
            $(_gridId + ' tr td:nth-child(8)').css('min-width', '100.5px');   // Date Added
            $(_gridId + ' tr td:nth-child(9)').css('min-width', '250px');   // 2nd Email
            $(_gridId + ' tr td:nth-child(10)').css('min-width', '120px');   // AccountAdmin
            $(_gridId + ' tr td:nth-child(11)').css('min-width', '79.5px');   // Vertical
            $(_gridId + ' tr td:nth-child(12)').css('min-width', '130px');   // Owner_Company
            $(_gridId + ' tr td:nth-child(13)').css('min-width', '120px');   // Name
            $(_gridId + ' tr td:nth-child(14)').css('min-width', '120px');   // PhoneNumber1
            $(_gridId + ' tr td:nth-child(15)').css('min-width', '168px');   // PhoneNumberOwner
            $(_gridId + ' tr td:nth-child(16)').css('min-width', '80px');   // DOB1
            $(_gridId + ' tr td:nth-child(17)').css('min-width', '153.5px');   // Payout_Method
            $(_gridId + ' tr td:nth-child(18)').css('min-width', '140px');   // PointofContact
            $(_gridId + ' tr td:nth-child(19)').css('min-width', '140px');   // PhoneNumber2
            $(_gridId + ' tr td:nth-child(20)').css('min-width', '80px');   // DOB2
            $(_gridId + ' tr td:nth-child(21)').css('min-width', '192.5px');   // EmailAddress
            $(_gridId + ' tr td:nth-child(22)').css('min-width', '140px');   // ActiveListings
            $(_gridId + ' tr td:nth-child(23)').css('min-width', '160px');   // Pending_Onboarding
            $(_gridId + ' tr td:nth-child(24)').css('min-width', '140px');   // In_activeListings
            $(_gridId + ' tr td:nth-child(25)').css('min-width', '160px');   // ofListingsinLAMarket
            $(_gridId + ' tr td:nth-child(26)').css('min-width', '160px');   // ofListingsinNYCMarket
            $(_gridId + ' tr td:nth-child(27)').css('min-width', '132px');   // ProxyIP
            $(_gridId + ' tr td:nth-child(28)').css('min-width', '132px');   // C2ndProxyIP
        },

        readjustHeaderWidths = function () {
            for (var i = 3; i <= 28; i++) {
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

        AirbnbAccountView = function (id) {
            window.location.href = '/AirbnbAccount/info/?id=' + id;
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
        getAirbnbAccounts: getAirbnbAccounts,
        renderAction: renderAction,
        renderDelete: renderDelete,
        updateRow: updateRow,
        unselectRow: unselectRow
    }
}();

DojoWeb.AirbnbAccountForm = function () {
    var _formId = undefined,
        _currentId = undefined,

        init = function (formId) {
            _formId = formId;
            _currentId = undefined;
            installControls();
            initDate();
        },

        getId = function () {
            return _currentId;
        },

        installControls = function () {
            if ($('.app-form-view').length == 0) { // editing mode
                DojoWeb.Plugin.initDatePicker('.app-simple-datepicker');
            }
            else { // disble ui control editing for view-only mode
                $('.app-form-view').prop('readonly', true);
                $('.app-form-view :not(:selected)').prop('disabled', true);
            }

            //$('#PhoneNumber1').kendoMaskedTextBox({ mask: "(999) 000-0000" });
            //$('#PhoneNumber2').kendoMaskedTextBox({ mask: "(999) 000-0000" });

            $('#AirbnbAccountSave').click(function (e) {
                e.preventDefault();
                save();
            });

            $('#AirbnbAccountCancel').click(function (e) {
                e.preventDefault();
                DojoWeb.Plugin.closeFormDialog();
                DojoWeb.AirbnbAccount.unselectRow();
            });
        },

        initDate = function () {
            var datePicker = $('#DateAdded').data('kendoDatePicker');
            if (!datePicker.value()) {
                var todayDate = new Date();
                datePicker.value(todayDate);
                datePicker.trigger('change');
            }
            datePicker.readonly();
        },

        save = function () {
            var $form = $('#' + _formId);
            if ($form.valid()) {
                var formData = $form.serialize(); // this is a query string format; not json format
                $.ajax({
                    type: 'POST',
                    url: '/AirbnbAccount/ModalEdit',
                    data: formData,
                    success: function (result) {
                        DojoWeb.Plugin.closeFormDialog();
                        if (result != '') {
                            _currentId = parseInt(result);
                            // if the property date range is not up to today, the newly added property won't show up in the grid
                            var dateRange = DojoWeb.AccountActionBar.getDateRange();
                            if (dateRange.endDate < Date.today()) {
                                DojoWeb.Notification.show('Please change "To" date textbox to today to see the newly added Airbnb account.');
                                // change end date to today so that the added property can be seen
                                //DojoWeb.PropertyActionBar.setDateRange(dateRange.beginDate, Date.today());
                            }
                            DojoWeb.AirbnbAccount.updateRow(_currentId);
                        }
                        else {
                            DojoWeb.Notification.show('There was an error saving the Airbnb account.');
                        }
                    },
                    error: function (jqXHR, status, errorThrown) {
                        if (status == 'error') {
                            var message = 'There was an error saving your Airbnb account.';
                            alert(message);
                            //DojoWeb.ActionAlert.fail('dojo-alert', message);
                        }
                    }
                });
            }
        },

        refresh = function () {
            // for partial view; not used
        },

        serverError = function () {
            // for partial view; not used
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

DojoWeb.AccountActionBar = function () {
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
