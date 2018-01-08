"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.AirbnbImport = function () {
    var
        init = function () {
            initEvents();
            highlightCount();
        },

        initEvents = function() {
            $('#onViewImportLog').kendoButton({
                //spriteCssClass: 'fa fa-database orange ',
                click: showImportLog,
            });

            $('.import-data-calendar').removeClass('hide');
        },

        highlightCount = function () {
            var missingCount = $('td.import-account-col div.red').length;
            if (missingCount != undefined) $('#missingCount').text(missingCount + '');
        },

        highlight3Peats = function () {
            $('tr.import-account-row').each(function () {
                var noData1 = $($(this).find('td').eq(1).children()[0]).hasClass('no-count');
                var noData2 = $($(this).find('td').eq(2).children()[0]).hasClass('no-count');
                var noData3 = $($(this).find('td').eq(3).children()[0]).hasClass('no-count');
                if (noData1 && noData2 && noData3) {
                    $(this).find('td').eq(0).css('color', 'red').css('font-weight', 'bold');
                }
                else if (noData1 && noData2) {
                    $(this).find('td').eq(0).css('color', '#c2c201').css('font-weight', 'bold');
                }
            });
        },

        // show import log on a separate browser tab
        showImportLog = function () {
            $.ajax({
                type: 'POST',
                url: '/Import/AirbnbImportLog',
                success: function (logView) {
                    var newtab = window.open();
                    newtab.document.write(logView);
                    newtab.document.close();
                },
                error: function (jqXHR, status, errorThrown) {
                }
            });
        }

    return {
        init: init,
        showImportLog: showImportLog
    }
}();

DojoWeb.AirbnbImportLog = function () {
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
                height: height,
                filterable: true,
                sortable: true,
                scrollable: true,
                pageable: false,
                editable: false,
                columns: [
                            { field:'CreatedTime', title:'Import Time', filterable:false, width:'200px', template:"#= kendo.toString(kendo.parseDate(CreatedTime, 'yyyy-MM-dd'), 'MM/dd/yyyy hh:mm:ss tt') #" },
                            { field:'InputSource', title:'Input Source', filterable:{ multi: true }, width:'300px' },
                            { field:'Section', title:'Section', filterable:false, sortable:false, width:'180px' },
                            { field:'Message', title:'Import Message', filterable:false, sortable:false, template: '#= DojoWeb.AirbnbImportLog.highlightMessage(data.Message) #' }
                ],
            });

            // for some reason, Kendo 2016/June version has 'filter' text in the background of default filter icon.
            // we remove the 'filter' text ad-hoc here
            $(_gridId + ' span.k-filter').text('');
        },

        setupDataSource = function () {
            _dataSource = new kendo.data.DataSource({
                transport: {
                    read: {
                        url: '/Import/RetrieveImportLog',
                        type: 'post',
                        dataType: 'json'
                    },
                },
                schema: {
                    model: {
                        fields: {
                            CreatedTime: { type: 'datetime' },
                            InputSource: { type: 'string' },
                            Section: { type: 'string' },
                            Message: { type: 'string' }
                        }
                    }
                },
                error: function (e) {
                    DojoWeb.ActionAlert.fail('import-alert', e.xhr.responseJSON);
                    var grid = $(_gridId).data('kendoGrid');
                }
            });
        },

        highlightMessage = function (message) {
            if (message.indexOf('Error') == 0)
                return '<div style="color:red;">' + message + '</div>';
            else
                return message;

        }

    return {
        init: init,
        highlightMessage: highlightMessage
    }
}();

DojoWeb.AirbnbImportForm = function () {
    var _completedField = 'CompletedGoogleFileList',
        _futureField = 'FutureGoogleFileList',
        _completedFiles = [],
        _futureFiles = [],
        _processNote = 'Depending on how many import files you have selected, It may take a while to complete. Please sit back and relax until the completion...',

        init = function () {
            $(window).scrollTop();
            initEvents();
            initMultiFileSelect(1, 'completedFileList', 'completedCount', '');
            setType('completed'); // default date is set in view page
            //$('#ReportDate').data('kendoDatePicker').value('');
        },

        initEvents = function () {
            $('#airbnbImport').unbind('click').on('click', function (e) {
                doImport();
            });

            $('#importLogViewer').unbind('click').on('click', function (e) {
                DojoWeb.AirbnbImport.showImportLog();
            });

            $('#ReportDate').kendoDatePicker({
                format: 'MM/dd/yyyy',
                open: function (e) {
                    //toggleDateTyping(true); // hack: no direct date typing
                },
                close: function (e) {
                },
                change: function (e) {
                    var type = $('input[name="transaction-type"]:checked').val();
                    setType(type);
                }
            }).data('kendoDatePicker');
        },

        doImport = function () {
            var formData = new FormData();
            // add the field pairs
            formData.append('ReportDate', $('#ReportDate').val());
            formData.append('CompletedTransactionFiles', stringifyMultiselect('completedFileList'));
            formData.append('FutureTransactionFiles', stringifyMultiselect('futureFileList'));
            formData.append('GrossTransactionFiles', stringifyMultiselect('grossFileList'));
            formData.append('TransactionFileType', $('#TransactionFileType').val());

            $('#longProcessNote').html(kendo.toString(new Date(), 'G') + ': ' + _processNote);
            $('#notificationMessage').addClass('hide');

            DojoWeb.Busy.show();

            $.ajax({
                type: 'POST',
                url: '/Import/ImportAirbnb',
                contentType: false, // Not to set any content header if FormData() is used
                processData: false, // Not to process data if FormData() is used
                // canot use contentType and dataType for FormData type
                //contentType: 'application/json;charset=utf-8',
                //dataType: 'json',
                data: formData,
                success: function (result) {
                    if (result.Count != -1) {
                        var message = kendo.format('{0}: Airbnb transactions import result: total = {1}, skipped = {2}, successful = {3}, failed = {4} files',
                                                    kendo.toString(new Date(), 'G'), result.Count, result.Skip, result.Good, result.Bad);
                        if (result.Bad > 0)
                            DojoWeb.ActionAlert.fail('import-alert', message);
                        else
                            DojoWeb.ActionAlert.success('import-alert', message);
                    }
                    else {
                        DojoWeb.Notification.show(kendo.format('{0}: {1}', kendo.toString(new Date(), 'G'), result.Message));
                    }
                    DojoWeb.Busy.hide();
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error importing Airbnb Transactions.';
                        alert(message);
                    }
                }
            });
        },

        initMultiFileSelect = function (type, id, countId, reportDate) {
            DojoWeb.MultiSelect.install({
                'url': '/Import/GetImportFileList?reportDate=' + reportDate + '&reportType=' + type,
                'id': id,
                'buttonClass': 'form-control airbnb-import-dropdown input-sm',
                'default': [],
                'rightCaret': true,
                'includeMarker': true,
                'includeAll': true,
                'selectAllText': 'Select All',
                'enableFilter': true,
                'viewOnly': false,
                'callback': function () { },
                'countId': countId,
            });
        },

        setType = function (type) {
            if ($('#ReportDate').val() != '') {
                if (type == 'future') {
                    $('#completedFilesContainer').addClass('hide');
                    $('#grossFilesContainer').addClass('hide');
                    $('#futureFilesContainer').removeClass('hide');
                    $('#TransactionFileType').val('2');
                }
                else if (type == 'completed' || type == 'log') {
                    $('#completedFilesContainer').removeClass('hide');
                    $('#futureFilesContainer').addClass('hide');
                    $('#grossFilesContainer').addClass('hide');
                    if (type == 'completed')
                        $('#TransactionFileType').val('1');
                    else
                        $('#TransactionFileType').val('6');
                }
                else {
                    $('#completedFilesContainer').addClass('hide');
                    $('#futureFilesContainer').addClass('hide');
                    $('#grossFilesContainer').removeClass('hide');
                    $('#TransactionFileType').val('3');
                }

            }
            showFileSelect(type);
        },

        showFileSelect = function (type) {
            var folderDate = $('#ReportDate').data('kendoDatePicker').value();
            var reportDate = kendo.toString(folderDate, 'MM/dd/yyyy');

            if (type == 'future') {
                $('#futureFolderName').html(' for <b>' + kendo.toString(folderDate, 'MMMM d yyyy') + '</b>: ');
                DojoWeb.MultiSelect.destroy('futureFileList');
                initMultiFileSelect(2, 'futureFileList', 'futureCount', reportDate);
            }
            else if (type == 'completed'){
                $('#completedFolderName').html(' for <b>' + kendo.toString(folderDate, 'MMMM d yyyy') + '</b>: ');
                DojoWeb.MultiSelect.destroy('completedFileList');
                initMultiFileSelect(1, 'completedFileList', 'completedCount', reportDate);
            }
            else {
                $('#grossFolderName').html(' for <b>' + kendo.toString(folderDate, 'MMMM d yyyy') + '</b>: ');
                DojoWeb.MultiSelect.destroy('grossFileList');
                initMultiFileSelect(3, 'grossFileList', 'grossCount', reportDate);

            }
        },

        stringifyMultiselect = function (id) {
            var $id = $('#' + id);
            var selected = $id.val();
            if (selected && selected.length > 0) {
                var items = [];
                $.each(selected, function (i, item) {
                    var tokens = item.split(';'); // the separator for option item's value
                    items.push({ name: tokens[0], id: tokens[1] });
                });
                return JSON.stringify(items);
            }
            else
                return JSON.stringify([]);
        },

        toggleDateTyping = function (disabled) {
            // enable/disable typing directly into datepicker
            $('.k-datepicker input').prop('readonly', disabled);
        },

        refreshForm = function () {
        },

        serverError = function () {
        }

    return {
        init: init,
        setType: setType,
        refreshForm: refreshForm,
        serverError: serverError
    }
}();

// popup dialog style airbnb transaction import; not tested yet
DojoWeb.AirbnbImportDialog = function () {
    var _$formDialog = undefined,

        showDialog = function () {
            _$formDialog = undefined;
            DojoWeb.Busy.show();
            $.ajax({
                url: '/Import/AirbnbImportForm',
                success: function (data) {
                    DojoWeb.Busy.hide();
                    _$formDialog = $('#formDialog');
                    if (_$formDialog.length > 0) {
                        $('.dialog-body').html(data);
                        if (!_$formDialog.data('kendoWindow')) {
                            _$formDialog.kendoWindow({
                                width: 950,
                                title: caption,
                                actions: [],
                                visible: false,
                                resizable: false,
                                scrollable: true,
                                modal: false // modal dialog is not closed when clicking outside of it
                            });
                        }
                        _$formDialog.data('kendoWindow').open().center(); // open() needs to come before center()
                        _$formDialog.data('kendoWindow').title(caption);

                        // enable closing popup when clicking outside of it if modal = true
                        $(document).unbind('click').on('click', '.k-overlay', function (e) {
                            $('#formDialog').data('kendoWindow').close();
                            $('body').css('overflow', 'scroll');
                        });

                        $('body').css('overflow', 'hidden'); // disable background scrolling

                        initEvents();
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                    }
                }
            })
        },

        closeDialog = function () {
            if (_$formDialog && _$formDialog.length > 0) _$formDialog.data('kendoWindow').close();
            $('body').css('overflow', 'scroll'); // enable background scrolling
        },

        initEvents = function () {
            // close event
            $('#ImportClose').unbind('click').on('click', function (e) {
                e.preventDefault();
                closeDialog();
            });
        },

        refresh = function () {
            // for partial view; not used
        },

        serverError = function () {
            // for partial view; not used
        }

    return {
        showDialog: showDialog,
        refresh: refresh,
        serverError: serverError
    }
}();
