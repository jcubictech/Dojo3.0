"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.LookupEditor = function () {
    var _gridId = undefined,

        init = function (id, type) {
            if (_gridId != undefined) {
                if ($(_gridId).data("kendoGrid") != null) clear();
                _gridId = id;
            }
            else {
                _gridId = '#' + id;
            }

            rebind(type);
        },

        configureGrid = function (type) {
            if (type == 'Vertical')
                type = 'Product'; // label changed to 'Product' in create button
            else if (type == 'AbbreviatedState')
                type = 'State'; // label changed to 'State' in create button
            else if (type == 'PaymentMethod')
                type = 'Payment Method'; // label changed to 'Payment Method' in create button

            return {
                batch: false,
                pageable: false,
                editable: 'inline',
                filterable: true,
                sortable: true,
                toolbar: [{ name: 'create', text: 'Add New ' + type }],
                columns: [
                            { field: 'Id', hidden: true },
                            { field: 'Type', hidden: true },
                            { field: 'Name', title: type + ' Name' },
                            {
                                command: [{ name: 'edit', text: { edit: 'Edit', update: 'Save', cancel: 'Cancel' } }, 'destroy'],
                                title: 'Action',
                                width: '200px'
                            }
                ],
            };
        },

        configureDataSource = function (type) {
            return new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/Lookup/Retrieve?type=' + type,
                        dataType: 'json',
                        complete: function (jqXHR, textStatus) {
                            if (textStatus == 'success') {
                                // inject artificial scollbar column to work around Kendo grid header and data column alignment issue
                                var dataGrid = $(_gridId).data('kendoGrid');
                                if (dataGrid.dataSource.view().length > 9) {
                                    DojoWeb.Helpers.injectDummyHeaderColumn(_gridId, 16);
                                }
                            }
                        }
                    },
                    update: {
                        url: '/Lookup/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/Lookup/Delete',
                        type: 'post',
                        dataType: 'json',
                        
                    },
                    create: {
                        url: '/Lookup/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') {
                                options.models[0].Id = 0;
                                options.models[0].Type = type;
                            }
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') {
                                options.Id = 0;
                                options.Type = type;
                            }
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                schema: {
                    model: {
                        id: 'Id',
                        fields: {
                            Id: { type: 'number', editable: false, nullable: false },
                            Type: { type: 'string', editable: false, nullable: false },
                            Name: { type: 'string', nullable: false }
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('ss-user-alert', e.errors);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        clear = function () {
            // ensure there is only one grid
            var dataGrid = $(_gridId).data("kendoGrid");
            if (dataGrid) {
                dataGrid.destroy();
                $(_gridId).empty();
            }
        },

        rebind = function (type) {
            clear();
            var dataGrid = $(_gridId).kendoGrid(configureGrid(type)).data('kendoGrid');
            dataGrid.setDataSource(configureDataSource(type));

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');
        }

    return {
        init: init,
        rebind: rebind
    }
}();
