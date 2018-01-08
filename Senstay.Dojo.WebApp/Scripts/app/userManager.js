"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.UserManagementEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,

        init = function (id) {
            _gridId = '#' + id;
            setupDataSource();
            setupGrid();
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                pageable: false,
                height: height,
                //toolbar: [{ name: 'create', text: 'Add New Role' }],
                edit: function (e) {
                },
                columns: [
                            { field: 'UserId', hidden: true },
                            { field: 'UserName', title: 'User Name', width: '200px' },
                            { field: 'Email', title: 'Email', width: '250px' },
                            {
                                command: ['destroy'],
                                title: 'Action',
                                width: '100px'
                            }
                ],
                editable: 'inline',
                filterable: true,
                sortable: true
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/UserManager/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    update: {
                        url: '/UserManager/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/UserManager/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    create: {
                        url: '/UserManager/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].UserId = '';
                            return { models: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.UserId = '';
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'UserId',
                        fields: {
                            UserId: { type: 'string', editable: false, nullable: false },
                            Email: { type: 'string', editable: false, nullable: false },
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('ss-user-alert', e.errors);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        }

    return {
        init: init
    }
}();

DojoWeb.RoleManagementEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,

        init = function (id) {
            _gridId = '#' + id;
            setupDataSource();
            setupGrid();
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                pageable: false,
                height: height,
                toolbar: [{ name: 'create', text: 'Add New Role' }],
                edit: function (e) {
                },
                columns: [
                            { field: 'RoleId', hidden: true },
                            { field: 'RoleName', title: 'Role Name', width: '100px' },
                            {
                                command: [{ name: 'edit', text: { edit: 'Edit', update: 'Save', cancel: 'Cancel' } }, 'destroy'],
                                title: 'Action',
                                width: '150px'
                            }
                ],
                editable: 'inline',
                filterable: true,
                sortable: true
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/RoleManager/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    update: {
                        url: '/RoleManager/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/RoleManager/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    create: {
                        url: '/RoleManager/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].RoleId = '';
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.RoleId = '';
                            return { model: kendo.stringify(options) };
                        }
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<CustomEventDate>
                schema: {
                    model: {
                        id: 'RoleId',
                        fields: {
                            RoleId: { type: 'string', editable: false, nullable: false },
                            RoleName: { type: 'string', nullable: false }
                        }
                    }
                },
                error: function (e) {
                    if (e.xhr.responseJSON == undefined)
                        DojoWeb.ActionAlert.fail('ss-user-alert', e.xhr.responseText);
                    else
                        DojoWeb.ActionAlert.fail('ss-user-alert', e.xhr.responseJSON);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        }

    return {
        init: init,
    }
}();

DojoWeb.UserRoleManagerEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _availableRoles = [],

        init = function (id) {
            _gridId = '#' + id;
            getAvailableRoles();
            setupDataSource();
            setupGrid();
        },

        setupGrid = function () {
            var height = $(window).height() - 300;
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                pageable: false,
                height: height,
                columns: [
                            { field: 'UserId', hidden: true },
                            { field: 'UserName', title: 'User Name', width: '200px' },
                            {
                                field: 'UserRoles',
                                title: 'Roles',
                                editor: roleEditor,
                                template: roleDisplay,
                                width: '400px'
                            },
                            {
                                command: [{ name: 'edit', text: { edit: 'Edit', update: 'Save', cancel: 'Cancel' } }],
                                title: 'Action',
                                width: '200px'
                            }
                ],
                editable: 'inline',
                filterable: true,
                sortable: true
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/UserRoleManager/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    update: {
                        url: '/UserRoleManager/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/UserRoleManager/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].UserId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.UserId = 0;
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
                        id: 'UserId',
                        fields: {
                            UserId: { type: 'string', editable: false, nullable: false },
                            UserName: { type: 'string', editable: false, nullable: false },
                            UserRoles: { }
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

        getAvailableRoles = function () {
            $.ajax({
                url: '/UserRoleManager/AvailableRoles',
                dataType: 'json',
                success: function (result) {
                    _availableRoles = result;
                }
            });
        },

        roleEditor = function(container, options) {
            $('<select multiple="multiple" data-bind="value:UserRoles"/>')
                .appendTo(container)
                .kendoMultiSelect({
                    dataTextField: 'Text',
                    dataValueField: 'Id',
                    dataSource: _availableRoles
                });
        },

        roleDisplay = function (data) {
            var result = [];
            $.each(data.UserRoles, function (i, item) {
                result.push(item.Text);
            });
            return result.join(', ');
        }

    return {
        init: init,
        getAvailableRoles: getAvailableRoles
    }
}();

DojoWeb.UserInvitationEditor = function () {
    var _gridId = undefined,
        _dataSource = undefined,
        _availableRoles = [],

        init = function (id) {
            _gridId = '#' + id;
            getAvailableRoles();
            setupDataSource();
            setupGrid();
            $(_gridId).css('min-height', '150px');
        },

        setupGrid = function () {
            $(_gridId).kendoGrid({
                dataSource: _dataSource,
                pageable: false,
                toolbar: [{ name: 'create', text: 'New User to Invite' }],
                edit: function (e) {
                    // initialize new row cell data
                    if (e.model.isNew() && !e.model.dirty) {
                        e.container
                         .find('input[name=InvitationCode]') // get the input element for the field
                         .val(DojoWeb.UUID.newGuid()) // set the value
                         .change(); // trigger change in order to notify the model binding

                        e.container
                         .find('input[name=ExpirationDate]')
                         .val(kendo.toString(Date.today().addDays(7), 'MM/dd/yyyy'))
                         .change();

                        $('input[name=UserId]').val('0');
                        $('input[name=Password]').val('');
                        $('input[name=ConfirmPassword]').val('');
                        $('input[name=MobilePhone]').val('');
                    }
                    else if (!e.model.isNew()) {
                        $('input[name=InvitationCode]').attr('readonly', true);
                    }
                },
                columns: [
                            { field: 'UserName', title: 'User Name *', width: '150px' },
                            { field: 'UserEmail', title: 'User Email *', width: '200px' },
                            { field: 'InvitationCode', title: 'Invitation Code *', width: '300px' },
                            { field: 'ExpirationDate', title: 'Expiration Date *', width: '160px', format: '{0:MM/dd/yyyy}' },
                            { field: 'UserId', hidden: true },
                            { field: 'Password', hidden: true },
                            { field: 'ConfirmPassword', hidden: true },
                            { field: 'MobilePhone', hidden: true },
                            { field: 'UserRoles', title: 'Roles *', editor: roleEditor, template: roleDisplay, width: '360px' },
                            {
                                command: [
                                    { name: 'edit', text: { edit: 'Edit', update: 'Invite', cancel: 'Cancel' } },
                                    { name: 'destroy' }
                                ],
                                title: 'Action',
                                width: '180px'
                            }
                ],
                editable: 'inline',
                filterable: false,
                sortable: false
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/UserInvitation/Retrieve',
                        type: 'get',
                        dataType: 'json'
                    },
                    update: {
                        url: '/UserInvitation/Update',
                        type: 'post',
                        dataType: 'json'
                    },
                    destroy: {
                        url: '/UserInvitation/Delete',
                        type: 'post',
                        dataType: 'json'
                    },
                    create: {
                        url: '/UserInvitation/Create',
                        type: 'post',
                        dataType: 'json'
                    },
                    parameterMap: function (options, operation) {
                        if (operation !== 'read' && options.models) { // batch = true goes here
                            if (operation === 'create') options.models[0].UserId = 0;
                            return { model: kendo.stringify(options.models) };
                        }
                        else if (operation !== 'read' && options.models == undefined) { // batch = false goes here
                            if (operation === 'create') options.UserId = 0;
                            return { model: kendo.stringify(options) };
                        }
                        //else if (operation !== "read" && options.models) { // batch = true goes here
                        //    return { model: kendo.stringify(options.models) };
                        //}
                    }
                },
                batch: false, // enable options.models above if this is set to true; need to change controller code to support IEnumerable<model>
                schema: {
                    model: {
                        id: 'UserId',
                        fields: {
                            UserId: { type: 'string', editable: false, nullable: false },
                            UserName: {
                                type: 'string',
                                validation: { 
                                    required: true,
                                    //usernamevalidation: function (input) {
                                    //    if (input.is("[name='UserName']") && input.val() != "") {
                                    //        input.attr("data-usernamevalidation-msg", "Allow only letters, spaces, and digits.");
                                    //        return /^[a-z 0-9]+$/i.test(input.val()); // alphanumeric and space only
                                    //    }
                                    //    return true;
                                    //}
                                },
                                editable: true,
                                nullable: false
                            },
                            UserEmail: { type: 'string', validation: { required: true }, editable: true, nullable: false },
                            InvitationCode: { type: 'string', editable: true, nullable: false },
                            ExpirationDate: { type: 'date', validation: { required: true, min: new Date() }, editable: true, nullable: false },
                            UserRoles: { validation: { required: true } },
                            Password: { type: 'string', editable: false, nullable: false },
                            ConfirmPassword: { type: 'string', editable: false, nullable: false },
                            MobilePhone: { type: 'string', editable: false, nullable: false },
                        }
                    }
                },
                error: function (e) {
                    if (e.xhr.responseJSON == undefined)
                        DojoWeb.ActionAlert.fail('ss-user-alert', e.xhr.responseText);
                    else
                        DojoWeb.ActionAlert.fail('ss-user-alert', e.xhr.responseJSON);
                    var grid = $(_gridId).data('kendoGrid');
                    if (grid != undefined) grid.cancelChanges();
                }
            });
        },

        getAvailableRoles = function () {
            $.ajax({
                url: '/UserRoleManager/AvailableRoles',
                dataType: 'json',
                success: function (result) {
                    _availableRoles = result;
                }
            });
        },

        roleEditor = function (container, options) {
            $('<select multiple="multiple" data-bind="value:UserRoles"/>')
                .appendTo(container)
                .kendoMultiSelect({
                    dataTextField: 'Text',
                    dataValueField: 'Id',
                    dataSource: _availableRoles
                });
        },

        roleDisplay = function (data) {
            var result = [];
            $.each(data.UserRoles, function (i, item) {
                result.push(item.Text);
            });
            return result.join(', ');
        }

    return {
        init: init
    }
}();

DojoWeb.AcceptInvite = function () {
    var _formId = undefined,
        _alertId = undefined,

        init = function (formId, alertId) {
            _formId = formId;
            _alertId = alertId;

            $('#app-page-container').css('top', 'auto');

            // this is a hack so that the form can get field data outside of it
            $('#SocialInvite #InvitationCode').val($('#InvitationCode').val());
            $('#SocialInvite #UserId').val($('#UserId').val());
            $('#SocialInvite #UserName').val($('#UserName').val());
            $('#SocialInvite #UserEmail').val($('#UserEmail').val());
            $('#SocialInvite #MobilePhone').val($('#MobilePhone').val());
            $('#AcceptInvite #InvitationCode').val($('#InvitationCode').val());
            $('#AcceptInvite #UserId').val($('#UserId').val());
            $('#AcceptInvite #UserName').val($('#UserName').val());
            $('#AcceptInvite #UserEmail').val($('#UserEmail').val());
            $('#AcceptInvite #MobilePhone').val($('#MobilePhone').val());
            $('#Provider').val('Google');

            installEvents();
        },

        installEvents = function () {
            $('.social-submit').on('click', function (e) {
                $('#SocialInvite #MobilePhone').val($('#MobilePhone').val());
                $('#Provider').val(e.taget.id);
            });

            $('#acceptSubmit').on('click', function (e) {
                $('#AcceptInvite #MobilePhone').val($('#MobilePhone').val());
                submit(e);
            });
        },

        submit = function (e) {
            // password validation
            var password = $('#Password').val();
            var confirmPassword = $('#ConfirmPassword').val();
            if (password == confirmPassword) { // make sure password is typed correctly
                $('#' + _formId).submit();
            }
            else if (password == '' && confirmPassword == '') {
                window.alerts.showAlert(
                    {
                        message: 'Password and ConfirmPassword are required.',
                        alertClass: 'alert-danger'
                    },
                    {
                        id: _alertId,
                        delay: 10000
                    }
                );
            }
            else {
                window.alerts.showAlert(
                    {
                        message: 'Password and ConfirmPassword does not match.',
                        alertClass: 'alert-danger'
                    },
                    {
                        id: _alertId,
                        delay: 10000
                    }
                );
            }
        }

    return {
        init: init
}
}();
