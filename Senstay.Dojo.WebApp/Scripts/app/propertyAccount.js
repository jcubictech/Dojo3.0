"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.PropertyAccount = function () {
    var
        _gridWrapperClass = '.property-account-editor',
        _accountEditorId = 'propertyAccount',
        _payoutEditorId = 'payoutMethod',
        _entityEditorId = 'propertyEntity',
        _feeEditorId = 'propertyFee',
        _titleEditorId = 'propertyTitle',
        _availablePropertyCodes = [],

        init = function () {
            getAvailablePropertyCodes();
            DojoWeb.PayoutMethodEditor.init(_payoutEditorId);
            DojoWeb.PropertyAccountEditor.init(_accountEditorId);
            DojoWeb.PropertyEntityEditor.init(_entityEditorId);
            DojoWeb.PropertyFeeEditor.init(_feeEditorId);
            DojoWeb.PropertyTitleEditor.init(_titleEditorId);
            showGrid(_payoutEditorId); // default
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

        availablePropertyCodes = function() {
            return _availablePropertyCodes;
        },

        show = function (which) {
            var id = _payoutEditorId; // default
            switch (which) {
                case 'Account': id = _accountEditorId; break;
                case 'Entity': id = _entityEditorId; break;
                case 'Fee': id = _feeEditorId; break;
                case 'Title': id = _titleEditorId; break;
                case 'Payout':
                default: id = _payoutEditorId; break;
            }

            showGrid(id);
        },

        showGrid = function (id) {
            $(_gridWrapperClass).addClass('hide');
            $('#' + id).removeClass('hide');

            // adjust property code search box position
            if (id == _payoutEditorId)
                $('.property-search').css('left', '670px');
            else
                $('.property-search').css('left', '380px');
        },
        
        toggleScrollbar = function (e) {
            var gridWrapper = e.sender.wrapper;
            var gridDataTable = e.sender.table;
            var gridDataArea = gridDataTable.closest('.k-grid-content');
            gridWrapper.toggleClass('no-scrollbar', gridDataTable[0].offsetHeight < gridDataArea[0].offsetHeight);
        }

    return {
        init: init,
        show: show,
        getAvailablePropertyCodes: getAvailablePropertyCodes,
        availablePropertyCodes: availablePropertyCodes,
        toggleScrollbar: toggleScrollbar
    }
}();

DojoWeb.PropertyAccountEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _availableAccounts = [],
        _availablePayoutMethods = [],
        _inlineEditIndex = 1,

        init = function (id) {
            _gridId = '#' + id;
            getAvailableAccounts();
            getAvailablePayoutMethods();
            setupDataSource();
            setupGrid();
            initEvents();
        },

        initEvents = function () {
            $('#payoutSearch').on('click', function (e) {
                if ($('#payoutSearchBox').val() != '') {
                    $(_gridId).data('kendoGrid').dataSource.filter({
                        field: 'joinedPayoutMethods',
                        operator: 'contains',
                        value: $('#payoutSearchBox').val()
                    });
                }
            });

            $('#payoutReset').on('click', function (e) {
                $('#payoutSearchBox').val('');
                $(_gridId).data('kendoGrid').dataSource.filter([]);
            });

            $('#payoutSearchBox').on('keydown', function (e) {
                if (e.keyCode == 13) {
                    e.preventDefault();
                    $(_gridId).data('kendoGrid').dataSource.filter({
                        field: 'joinedPayoutMethods',
                        operator: 'contains',
                        value: $('#payoutSearchBox').val()
                    });
                }
            });
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                height: height,
                editable: 'inline',
                pageable: false,
                filterable: true,
                sortable: true,
                //dataBound: DojoWeb.PropertyAccount.toggleScrollbar,
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
                        e.model.set('PropertyAccountId', 0);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(' + _inlineEditIndex + ')');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                remove: function (e) {
                    if (!e.model.isNew()) {
                        //alert('Property Account cannot not be deleted.');
                        //e.preventDefault();
                        //$(_gridId).data('kendoGrid').cancelChanges();
                    }
                },
                toolbar: [
                        { name: 'create', text: 'New Property Account' },
                        { template: kendo.template($('#payoutTemplate').html()) },
                ],
                columns: [
                        {
                            command: [
                                {
                                    name: 'edit',
                                    template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                                    text: { edit: 'Edit', update: '', cancel: '' },
                                },
                                {
                                    name: 'destroy',
                                    template: '<a class="k-button k-grid-delete" href="" style="min-width:20px;"><span class="fa fa-trash"></span></a>',
                                },
                            ],
                            title: 'Edit',
                            width: '90px !important', // wide enough to hold 2 font awesome icons
                            hidden: false,
                        },
                        { field: 'PropertyAccountId', hidden: true },
                        { field: 'LoginAccount', title: 'Login Account', width: '400px', editor: accountEditor },
                        { field: 'OwnerName', title: 'Owner Name *', width: '200px' },
                        { field: 'OwnerEmail', title: 'Owner Email', width: '280px' },
                        { field: 'SelectedPayoutMethods', title: 'Payout Methods', editor: payoutMethodEditor, template: payoutMethodDisplay, filterable: false },
                ],
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');

            // for some reason, the height of k-grid-content is not set for grid content to scroll as needed. 
            // we explicitly set the height here; compensate 70 pixel to account for the actionbar to allow the entire content to scroll up
            $(_gridId + ' .k-grid-content').css('height', (height - 70) + 'px');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/PropertyAccount/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/PropertyAccount/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/PropertyAccount/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/PropertyAccount/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].PropertyAccountId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.PropertyAccountId = 0;
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'PropertyAccountId',
                        fields: {
                            PropertyAccountId: { type: 'number', editable: false, nullable: false },
                            LoginAccount: { type: 'string', editable: true, nullable: false },
                            OwnerName: { type: 'string', editable: true, validation: { required: true }, nullable: false },
                            OwnerEmail: { type: 'string', editable: true, nullable: false },
                            SelectedPayoutMethods: {}
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('ss-account-alert', e.errorThrown);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        getAvailableAccounts = function () {
            $.ajax({
                url: '/Account/LoginAccounts',
                dataType: 'json',
                success: function (result) {
                    _availableAccounts = result;
                }
            });
        },

        getAvailablePayoutMethods = function () {
            $.ajax({
                url: '/PayoutMethod/PayoutMethodList',
                dataType: 'json',
                success: function (result) {
                    _availablePayoutMethods = result;
                }
            });
        },

        accountEditor = function (container, options) {
            $('<input id="accountEditor" data-bind="value:' + options.field + '"/>')
            .appendTo(container)
            .kendoComboBox({
                autoWidth: true,
                placeholder: 'Select an account...',
                filter: 'contains',
                dataTextField: 'Text',
                dataValueField: 'Value',
                dataSource: _availableAccounts,
            });

            // prevent grid to scroll the page while it is scrolling
            var widget = $('#accountEditor').data('kendoComboBox');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        onAccountChange = function(e) {
            var tokens = e.sender.text().split(' | ');
            if (tokens.length > 1) {
                var email = tokens[0];
                var name = tokens[1];
                //if (e.options.model.OwnerEmail == '') e.options.model.set('OwnerEmail', email);
                //if (e.options.model.OwnerName == '') e.options.model.set('OwnerName', name);
            }
        },

        payoutMethodEditor = function(container, options) {
            $('<select id="" multiple="multiple" data-bind="value:SelectedPayoutMethods" />')
                .appendTo(container)
                .kendoMultiSelect({
                    dataTextField: 'Text',
                    dataValueField: 'Value',
                    dataSource: _availablePayoutMethods
                });
        },

        payoutMethodDisplay = function (data) {
            var result = [];
            $.each(data.SelectedPayoutMethods, function (i, item) {
                result.push(item.Text);
            });

            var stringifyPayoutMethods = result.join(', ');
            data.joinedPayoutMethods = stringifyPayoutMethods;
            return stringifyPayoutMethods;
        }

    return {
        init: init,
    }
}();

DojoWeb.PayoutMethodEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _availablePayoutMethodType = ['Checking', 'Paypal'],
        _inlineEditIndex = 1,

        init = function (id) {
            _gridId = '#' + id;
            setupDataSource();
            setupGrid();
            initEvents();
        },

        initEvents = function () {
            $('#propertySearch').on('click', function (e) {
                if ($('#propertySearchBox').val() != '') {
                    $(_gridId).data('kendoGrid').dataSource.filter({
                        field: 'joinedPropertyCodes',
                        operator: 'contains',
                        value: $('#propertySearchBox').val()
                    });
                }
            });

            $('#propertyReset').on('click', function (e) {
                //getData();
                // clear this filter is hang a long time if reset is used more than once
                $('#propertySearchBox').val('');
                $(_gridId).data('kendoGrid').dataSource.filter([]);
            });

            $('#propertySearchBox').on('keydown', function (e) {
                if (e.keyCode == 13) {
                    e.preventDefault();
                    $(_gridId).data('kendoGrid').dataSource.filter({
                        field: 'joinedPropertyCodes',
                        operator: 'contains',
                        value: $('#propertySearchBox').val()
                    });
                }
            });
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                height: height,
                editable: 'inline',
                pageable: false,
                filterable: true,
                sortable: true,
                //dataBound: DojoWeb.PropertyAccount.toggleScrollbar,
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
                        e.model.set('PayoutMethodId', 0);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(' + _inlineEditIndex + ')');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                remove: function (e) {
                    if (!e.model.isNew()) {
                    }
                },
                toolbar: [
                        { name: 'create', text: 'New Payout Method' },
                        { template: kendo.template($('#propertyTemplate').html()) },
                ],
                columns: [
                    {
                        command: [
                            {
                                name: 'edit',
                                template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                                text: { edit: 'Edit', update: '', cancel: '' },
                            },
                            {
                                name: 'destroy',
                                template: '<a class="k-button k-grid-delete" href="" style="min-width:20px;"><span class="fa fa-trash"></span></a>',
                            },
                        ],
                        title: 'Edit',
                        width: '90px !important', // wide enough to hold 2 font awesome icons
                        hidden: false,
                    },
                    { field: 'PayoutMethodName', title: 'Payout Method *', width: '280px', filterable: true, required: true },
                    { field: 'EffectiveDate', title: 'Effective Date *', width: '140px', filterable: true, format: '{0:MM/dd/yyyy}', required: true, template: '#= DojoWeb.Template.dateUS(EffectiveDate) #' },
                    { field: 'ExpiryDate', title: 'Expiry Date *', width: '130px', filterable: true, format: '{0:MM/dd/yyyy}', required: true, template: '#= DojoWeb.Template.dateUS(ExpiryDate) #' },
                    { field: 'PayoutAccount', title: 'Payout Account', width: '200px', filterable: true },
                    { field: 'PayoutMethodType', title: 'Payout Type *', width: '130px', filterable: true, editor: payoutMethodTypeEditor, required: true },
                    { field: 'SelectedPropertyCodes', title: 'Property Codes', editor: propertyCodeEditor, template: propertyCodeDisplay, filterable: false },
                    // identity fields
                    { field: 'PayoutMethodId', title: 'Payout Method ID', hidden: true },
                ],
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');

            // for some reason, the height of k-grid-content is not set for grid content to scroll as needed. 
            // we explicitly set the height here; compensate 70 pixel to account for the actionbar to allow the entire content to scroll up
            $(_gridId + ' .k-grid-content').css('height', (height - 70) + 'px');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/PayoutMethod/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/PayoutMethod/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/PayoutMethod/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/PayoutMethod/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].PayoutMethodId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.PayoutMethodId = 0;
                            return { model: kendo.stringify(options) };
                        }
                        //else if (operation !== "read" && options.models) { // batch = true goes here
                        //    return { model: kendo.stringify(options.models) };
                        //}
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'PayoutMethodId',
                        fields: {
                            PayoutMethodId: { type: 'number', editable: false, nullable: false },
                            PayoutMethodName: { type: 'string', editable: true, validation: { required: true }, nullable: true },
                            EffectiveDate: { type: 'date', editable: true, validation: { required: true }, nullable: true },
                            ExpiryDate: { type: 'date', editable: true, validation: { required: true }, nullable: true },
                            PayoutAccount: { type: 'string', editable: true, nullable: true },
                            PayoutMethodType: { type: 'string', editable: true, validation: { required: true }, nullable: true },
                            SelectedPropertyCodes: {},
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('ss-account-alert', e.errorThrown);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        payoutMethodTypeEditor = function (container, options) {
            $('<input id="categoryEditor" name="PayoutType" data-bind="value:' + options.field + '" required="required"/>')
            .appendTo(container)
            .kendoComboBox({
                autoWidth: true,
                dataSource: _availablePayoutMethodType
            });
        },

        propertyCodeEditor = function (container, options) {
            $('<select multiple="multiple" data-bind="value:SelectedPropertyCodes" />')
                .appendTo(container)
                .kendoMultiSelect({
                    dataTextField: 'Text',
                    dataValueField: 'Value',
                    dataSource: DojoWeb.PropertyAccount.availablePropertyCodes()
                });
        },

        payoutMethodTypeDisplay = function (data) {
            if (data.PayoutMethodType == '1')
                return _availablePayoutMethodType[0];
            else if (data.PayoutMethodType == '2')
                return _availablePayoutMethodType[1];
            else
                return '';
        },

        propertyCodeDisplay = function (data) {
            var result = [];
            $.each(data.SelectedPropertyCodes, function (i, item) {
                result.push(item.Text);
            });
            var stringifyPropertyCodes = result.join(', ');
            data.joinedPropertyCodes = stringifyPropertyCodes;
            return stringifyPropertyCodes;
        }

    return {
        init: init,
    }
}();

DojoWeb.PropertyEntityEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _inlineEditIndex = 1,

        init = function (id) {
            _gridId = '#' + id;
            setupDataSource();
            setupGrid();
            initEvents();
        },

        initEvents = function () {
            $('#propertySearch').on('click', function (e) {
                if ($('#propertySearchBox').val() != '') {
                    $(_gridId).data('kendoGrid').dataSource.filter({
                        field: 'joinedPropertyCodes',
                        operator: 'contains',
                        value: $('#propertySearchBox').val()
                    });
                }
            });

            $('#propertyReset').on('click', function (e) {
                //getData();
                // clear this filter is hang a long time if reset is used more than once
                $('#propertySearchBox').val('');
                $(_gridId).data('kendoGrid').dataSource.filter([]);
            });

            $('#propertySearchBox').on('keydown', function (e) {
                if (e.keyCode == 13) {
                    e.preventDefault();
                    $(_gridId).data('kendoGrid').dataSource.filter({
                        field: 'joinedPropertyCodes',
                        operator: 'contains',
                        value: $('#propertySearchBox').val()
                    });
                }
            });
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                height: height,
                editable: 'inline',
                pageable: false,
                filterable: true,
                sortable: true,
                //dataBound: DojoWeb.PropertyAccount.toggleScrollbar,
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
                        e.model.set('PropertyEntityId', 0);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(' + _inlineEditIndex + ')');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                remove: function (e) {
                    if (!e.model.isNew()) {
                        //alert('Property Entity cannot not be deleted.');
                        //e.preventDefault();
                        //$(_gridId).data('kendoGrid').cancelChanges();
                    }
                },
                toolbar: [
                        { name: 'create', text: 'New Property Entity' },
                        { template: kendo.template($('#propertyTemplate').html()) },
                ],
                columns: [
                        {
                            command: [
                                {
                                    name: 'edit',
                                    template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                                    text: { edit: 'Edit', update: '', cancel: '' },
                                },
                                {
                                    name: 'destroy',
                                    template: '<a class="k-button k-grid-delete" href="" style="min-width:20px;"><span class="fa fa-trash"></span></a>',
                                },
                            ],
                            title: 'Edit',
                            width: '90px !important', // wide enough to hold 2 font awesome icons
                            hidden: false,
                        },
                        { field: 'PropertyEntityId', hidden: true },
                        { field: 'EntityName', title: 'Entity Name *', width: '300px' },
                        { field: 'EffectiveDate', title: 'Effective Date *', width: '150px', format: '{0:MM/dd/yyyy}', sortable: false },
                        { field: 'SelectedPropertyCodes', title: 'Property Codes', editor: propertyCodeEditor, template: propertyCodeDisplay, filterable: false },
                ],
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');

            // for some reason, the height of k-grid-content is not set for grid content to scroll as needed. 
            // we explicitly set the height here; compensate 70 pixel to account for the actionbar to allow the entire content to scroll up
            $(_gridId + ' .k-grid-content').css('height', (height - 70) + 'px');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/PropertyEntity/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/PropertyEntity/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/PropertyEntity/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/PropertyEntity/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].PropertyEntityId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.PropertyEntityId = 0;
                            return { model: kendo.stringify(options) };
                        }
                        //else if (operation !== "read" && options.models) { // batch = true goes here
                        //    return { model: kendo.stringify(options.models) };
                        //}
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'PropertyEntityId',
                        fields: {
                            PropertyEntityId: { type: 'number', editable: false, nullable: false },
                            EntityName: { type: 'string', editable: true, validation: { required: true }, nullable: false },
                            EffectiveDate: { type: 'date', editable: true, validation: { required: true }, nullable: false },
                            SelectedPropertyCodes: {}
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('ss-account-alert', e.errorThrown);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        propertyCodeEditor = function (container, options) {
            $('<select multiple="multiple" data-bind="value:SelectedPropertyCodes" />')
                .appendTo(container)
                .kendoMultiSelect({
                    dataTextField: 'Text',
                    dataValueField: 'Value',
                    dataSource: DojoWeb.PropertyAccount.availablePropertyCodes()
                });
        },

        propertyCodeDisplay = function (data) {
            var result = [];
            $.each(data.SelectedPropertyCodes, function (i, item) {
                result.push(item.Text);
            });
            var stringifyPropertyCodes = result.join(', ');
            data.joinedPropertyCodes = stringifyPropertyCodes;
            return stringifyPropertyCodes;
        }

    return {
        init: init,
    }
}();

DojoWeb.PropertyFeeEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _inlineEditIndex = 1,

        init = function (id) {
            _gridId = '#' + id;
            setupDataSource();
            setupGrid();
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                height: height,
                editable: 'inline',
                pageable: false,
                filterable: true,
                sortable: true,
                scrollable: true,
                //dataBound: DojoWeb.PropertyAccount.toggleScrollbar,
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
                        e.model.set('PropertyFeeId', 0);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(' + _inlineEditIndex + ')');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                remove: function (e) {
                    if (!e.model.isNew()) {
                        //alert('Property Fee cannot not be deleted.');
                        //e.preventDefault();
                        //$(_gridId).data('kendoGrid').cancelChanges();
                    }
                },
                toolbar: [ { name: 'create', text: ' New Property Fee', iconClass: 'fa fa-plus' } ],
                columns: [
                        {
                            command: [
                                {
                                    name: 'edit',
                                    template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                                    text: { edit: 'Edit', update: '', cancel: '' },
                                },
                                {
                                    name: 'destroy',
                                    template: '<a class="k-button k-grid-delete" href="" style="min-width:20px;"><span class="fa fa-trash"></span></a>',
                                },
                            ],
                            title: 'Edit',
                            width: '90px !important', // wide enough to hold 2 font awesome icons
                            hidden: false,
                        },
                        { field: 'PropertyFeeId', hidden: true },
                        { field: 'PropertyCode', title: 'Property Code *', editor: propertyCodeSelector },
                        { field: 'EffectiveDate', title: 'Effective Date *', width: '150px', format: '{0:MM/dd/yyyy}', sortable: false },
                        { field: 'CityTax', title: 'City Tax', width: '100px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.CityTax) #" },
                        { field: 'ManagementFee', title: 'Management Fee', width: '180px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.ManagementFee) #" },
                        { field: 'DamageWaiver', title: 'Damage Waiver', width: '150px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.DamageWaiver, 2) #" },
                        { field: 'Cleanings', title: 'Cleanings', width: '120px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.Cleanings, 2) #" },
                        { field: 'Laundry', title: 'Laundry', width: '100px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.Laundry, 2) #" },
                        { field: 'Consumables', title: 'Consumables', width: '130px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.Consumables, 2) #" },
                        { field: 'PoolService', title: 'Pool Service', width: '120px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.PoolService, 2) #" },
                        { field: 'Landscaping', title: 'Landscaping', width: '130px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.Landscaping, 2) #" },
                        { field: 'TrashService', title: 'Trash Service', width: '130px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.TrashService, 2) #" },
                        { field: 'PestService', title: 'Pest Control Service', width: '180px', editor: decimalEditor, template: "#= DojoWeb.PropertyFeeEditor.decimalDisplay(data.PestService, 2) #" },
                ],
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');

            // for some reason, the height of k-grid-content is not set for grid content to scroll as needed. 
            // we explicitly set the height here; compensate 70 pixel to account for the actionbar to allow the entire content to scroll up
            $(_gridId + ' .k-grid-content').css('height', (height - 70) + 'px');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/PropertyFee/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/PropertyFee/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/PropertyFee/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/PropertyFee/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].PropertyFeeId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.PropertyFeeId = 0;
                            return { model: kendo.stringify(options) };
                        }
                        //else if (operation !== "read" && options.models) { // batch = true goes here
                        //    return { model: kendo.stringify(options.models) };
                        //}
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'PropertyFeeId',
                        fields: {
                            PropertyFeeId: { type: 'number', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: true, validation: { required: true }, nullable: false },
                            EffectiveDate: { type: 'date', editable: true, validation: { required: true }, nullable: false },
                            CityTax: { type: 'number', editable: true, nullable: false },
                            ManagementFee: { type: 'number', editable: true, nullable: false },
                            DamageWaiver: { type: 'number', editable: true, nullable: false },
                            Cleanings: { type: 'number', editable: true, nullable: false },
                            Laundry: { type: 'number', editable: true, nullable: false },
                            Consumables: { type: 'number', editable: true, nullable: false },
                            PoolService: { type: 'number', editable: true, nullable: false },
                            Landscaping: { type: 'number', editable: true, nullable: false },
                            TrashService: { type: 'number', editable: true, nullable: false },
                            PestService: { type: 'number', editable: true, nullable: false },
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('ss-account-alert', e.errorThrown);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        propertyCodeSelector = function (container, options) {
            $('<input id="propertyCodeSelector" name="PropertyCode" data-bind="value:' + options.field + '" required="required"/>')
            .appendTo(container)
            .kendoComboBox({
                autoWidth: true,
                placeholder: 'Select a property...',
                filter: 'contains',
                dataTextField: 'Text',
                dataValueField: 'Value',
                dataSource: DojoWeb.PropertyAccount.availablePropertyCodes()
            });
        },

        decimalEditor = function (container, options) {
            var amount = options.model.CityTax;
            if (options.field == 'ManagementFee') amount = options.model.ManagementFee;
            var inputTag = kendo.format('<input id="deimal-{0}" data-bind="value:{0}"/>', options.field);
            $(inputTag).appendTo(container).kendoNumericTextBox({ format: 'n4', decimals: 4 });
        },

        decimalDisplay = function (data, decimal) {
            if (data >= 1 || (decimal != undefined && decimal == 2)) {
                return kendo.toString(data, 'n2').replace(/0+$/g, '').replace(/\.$/g, '');
            }
            else {
                return kendo.toString(data, 'n4').replace(/0+$/g, '').replace(/\.$/g, '');
            }
        }

    return {
        init: init,
        decimalDisplay: decimalDisplay
    }
}();

DojoWeb.PropertyTitleEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _inlineEditIndex = 1,

        init = function (id) {
            _gridId = '#' + id;
            setupDataSource();
            setupGrid();
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                height: height,
                editable: 'inline',
                pageable: false,
                filterable: true,
                sortable: true,
                scrollable: true,
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
                        e.model.set('PropertyTitleHistoryId', 0);
                    }

                    // customize action buttons using font awesome icon wth color for save/cancel
                    var commandCell = e.container.find('td:nth-child(' + _inlineEditIndex + ')');
                    commandCell.html('<a class="k-button k-grid-update" href="" style="min-width:20px;"><span class="fa fa-save green"></span></a><a class="k-button k-grid-cancel" href="" style="min-width:20px;"><span class="fa fa-close red"></span></a>');
                },
                remove: function (e) {
                    if (!e.model.isNew()) {
                        //e.preventDefault();
                    }
                },
                toolbar: [{ name: 'create', text: ' New Property Title', iconClass: 'fa fa-plus' }],
                columns: [
                        {
                            command: [
                                {
                                    name: 'edit',
                                    template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                                    text: { edit: 'Edit', update: '', cancel: '' },
                                },
                                {
                                    name: 'destroy',
                                    template: '<a class="k-button k-grid-delete" href="" style="min-width:20px;"><span class="fa fa-trash"></span></a>',
                                },
                            ],
                            title: 'Edit',
                            width: '90px !important', // wide enough to hold 2 font awesome icons
                            hidden: false,
                        },
                        { field: 'PropertyTitleHistoryId', hidden: true },
                        { field: 'PropertyCode', title: 'Property Code *', width: '200px', editor: propertyCodeSelector },
                        { field: 'EffectiveDate', title: 'Effective Date *', width: '150px', format: '{0:MM/dd/yyyy}', sortable: false },
                        { field: 'PropertyTitle', title: 'Property Title *' },
                ],
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');

            // for some reason, the height of k-grid-content is not set for grid content to scroll as needed. 
            // we explicitly set the height here; compensate 70 pixel to account for the actionbar to allow the entire content to scroll up
            $(_gridId + ' .k-grid-content').css('height', (height - 70) + 'px');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/PropertyTitle/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/PropertyTitle/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/PropertyTitle/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/PropertyTitle/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].PropertyTitleHistoryId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.PropertyTitleHistoryId = 0;
                            return { model: kendo.stringify(options) };
                        }
                        //else if (operation !== "read" && options.models) { // batch = true goes here
                        //    return { model: kendo.stringify(options.models) };
                        //}
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'PropertyTitleHistoryId',
                        fields: {
                            PropertyTitleHistoryId: { type: 'number', editable: false, nullable: false },
                            PropertyCode: { type: 'string', editable: true, validation: { required: true }, nullable: false },
                            EffectiveDate: { type: 'date', editable: true, validation: { required: true }, nullable: false },
                            PropertyTitle: { type: 'string', editable: true, validation: { required: true }, nullable: false },
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('ss-account-alert', e.errorThrown);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        propertyCodeSelector = function (container, options) {
            $('<input id="propertyCodeSelector" name="PropertyCode" data-bind="value:' + options.field + '" required="required"/>')
            .appendTo(container)
            .kendoComboBox({
                autoWidth: true,
                placeholder: 'Select a property...',
                filter: 'contains',
                dataTextField: 'Text',
                dataValueField: 'Value',
                dataSource: DojoWeb.PropertyAccount.availablePropertyCodes()
            });
        }

    return {
        init: init,
    }
}();
