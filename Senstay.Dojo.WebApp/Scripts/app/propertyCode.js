"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.PropertyCodeEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _availablePayoutMethods = [],
        _availablePayoutEntities = [],
        _availablePropertyOwners = [],
        _inlineEditIndex = 1,

        init = function (id) {
            _gridId = '#' + id;
            getAvailablePayoutMethods();
            getAvailablePayoutEntities();
            getAvailablePropertyOwners();
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
                        e.model.set('PropertyCode', 'PropertyPlaceholder');
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
                toolbar: null,
                columns: [
                    {
                        command: [
                            {
                                name: 'edit',
                                template: '<a class="k-button k-grid-edit" href="" style="min-width:16px;"><span class="fa fa-pencil blue"></span></a>',
                                text: { edit: 'Edit', update: '', cancel: '' },
                            },
                        ],
                        title: 'Edit',
                        width: '90px !important',
                        hidden: false,
                    },
                    { field: 'PropertyCode', title: 'Property Code', editable: false, width: '150px', template: '#= DojoWeb.PropertyCodeEditor.highlight(data) #' },
                    { field: 'CityTax', title: 'City Tax', editable: false, width: '100px', template: '#= DojoWeb.Template.decimal(data.CityTax, 4) #', attributes: { class: 'align-right' } },
                    { field: 'DamageWaiver', title: 'Damage Waiver', editable: false, width: '150px', template: '#= DojoWeb.Template.money(data.DamageWaiver, false) #', attributes: { class: 'align-right' } },
                    { field: 'ManagementFee', title: 'Management Fee', editable: false, width: '160px', template: '#= DojoWeb.Template.decimal(data.ManagementFee, 3) #', attributes: { class: 'align-right' } },
                    { field: 'PropertyOwner', title: 'Property Owner', width: '240px', editor: propertyOwnerEditor },
                    { field: 'PayoutMethod', title: 'Payout Method', editor: payoutMethodEditor },
                    { field: 'PayoutEntity', title: 'Payout Entity', width: '300px', editor: payoutEntityEditor },
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
                        url: '/PropertyCode/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    create: {
                        url: '/PropertyCode/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    update: {
                        url: '/PropertyCode/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].PropertyCode = 'PropertyPlaceholder';
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.PropertyCode = 'PropertyPlaceholder';
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<type>
                schema: {
                    model: {
                        id: 'PropertyCode',
                        fields: {
                            PropertyCode: { type: 'string', editable: false, nullable: false },
                            CityTax: { type: 'number', editable: false, nullable: false },
                            DamageWaiver: { type: 'number', editable: false, nullable: false },
                            ManagementFee: { type: 'number', editable: false, nullable: false },
                            PropertyOwner: { type: 'string', editable: true, nullable: false },
                            PayoutMethod: { type: 'string', editable: true, nullable: false },
                            PayoutEntity: { type: 'string', editable: true, nullable: false },
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

        getAvailablePropertyOwners = function () {
            $.ajax({
                url: '/PropertyAccount/PropertyOwnerList',
                dataType: 'json',
                success: function (result) {
                    _availablePropertyOwners = result;
                }
            });
        },

        getAvailablePayoutEntities = function () {
            $.ajax({
                url: '/PropertyEntity/PayoutEntityList',
                dataType: 'json',
                success: function (result) {
                    _availablePayoutEntities = result;
                }
            });
        },

        getAvailablePayoutMethods = function () {
            $.ajax({
                url: '/PayoutMethod/PayoutMethodNames',
                dataType: 'json',
                success: function (result) {
                    _availablePayoutMethods = result;
                }
            });
        },

        payoutMethodEditor = function (container, options) {
            $('<input id="propertyMethodEditor" data-bind="value:' + options.field + '"/>')
            .appendTo(container)
            .kendoDropDownList({
                autoWidth: true,
                //optionLabel: ' ',
                // these options are for combobox
                //placeholder: 'Select a payout method...',
                //filter: 'contains',
                //dataTextField: 'Text',
                //dataValueField: 'Value',
                dataSource: _availablePayoutMethods,
            });
            // prevent grid to scroll the page while it is scrolling
            var widget = $('#propertyMethodEditor').data('kendoDropDownList');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        payoutEntityEditor = function (container, options) {
            $('<input id="propertyEntityEditor" data-bind="value:' + options.field + '"/>')
            .appendTo(container)
            .kendoDropDownList({
                autoWidth: true,
                //optionLabel: ' ',
                // these options are for combobox
                //placeholder: 'Select a payout entity...',
                //filter: 'contains',
                //dataTextField: 'Text',
                //dataValueField: 'Value',
                dataSource: _availablePayoutEntities
            });
            // prevent grid to scroll the page while it is scrolling
            var widget = $('#propertyEntityEditor').data('kendoDropDownList');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        propertyOwnerEditor = function (container, options) {
            $('<input id="propertyOwnerEditor" data-bind="value:' + options.field + '"/>')
            .appendTo(container)
            .kendoDropDownList({
                autoWidth: true,
                //optionLabel: ' ',
                // these options are for combobox
                //placeholder: 'Select a property owner...',
                //filter: 'contains',
                //dataTextField: 'Text',
                //dataValueField: 'Value',
                dataSource: _availablePropertyOwners
            });
            // prevent grid to scroll the page while it is scrolling
            var widget = $('#propertyOwnerEditor').data('kendoDropDownList');
            DojoWeb.GridHelper.controlScroll(widget.ul.parent());
        },

        highlight = function (data) {
            if (data.PropertyOwner == undefined && data.PayoutMethod == undefined && data.PayoutEntity == undefined) {
                return '<div style="color:red;font-weight:bold;">' + data.PropertyCode + '</div>';
            }
            else if (data.PropertyOwner == undefined && (data.PayoutMethod == undefined || data.PayoutEntity == undefined)) {
                return '<div style="color:#c2c201;font-weight:bold;">' + data.PropertyCode + '</div>';
            }
            else {
                return data.PropertyCode;
            }
        }

    return {
        init: init,
        highlight: highlight
    }
}();
