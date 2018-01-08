"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.OwnerPayoutForm = function () {
    var _completedField = 'CompletedGoogleFileList',
        _futureField = 'FutureGoogleFileList',
        _completedFiles = [],
        _futureFiles = [],
        _processNote = 'Depending on how many import files you have selected, It may take a while to complete. Please sit back and relax until the completion...',

        init = function () {
            $('.google-file-list').hide();
            $(window).scrollTop();
            initEvents();
            $('#ReportDate').data('kendoDatePicker').value('');
            initMultiFileSelect(1, 'completedFileList', 'completedCount', '');
        },

        initEvents = function () {
            $('#ownerPayoutImport').unbind('click').on('click', function (e) {
                doImport();
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

            $('#longProcessNote').html(kendo.toString(new Date(), 'G') + ': ' + _processNote);
            $('#notificationMessage').addClass('hide');

            DojoWeb.Busy.show();

            $.ajax({
                type: 'POST',
                url: '/OwnerPayout/Import',
                contentType: false, // Not to set any content header if FormData() is used
                processData: false, // Not to process data if FormData() is used
                // canot use contentType and dataType for FormData type
                //contentType: 'application/json;charset=utf-8',
                //dataType: 'json',
                data: formData,
                success: function (result) {
                    if (result.OK !== -1) {
                        var message = kendo.format('{0}: Owner Payout import result: completed successfully = {1} files, failed = {2} files',
                                                    kendo.toString(new Date(), 'G'), result.OK, result.Error);
                        if (result.Error > 0)
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', message);
                        else
                            DojoWeb.ActionAlert.success('ownerPayout-alert', message);

                        if (result.Message !== '') {
                            $('#notificationMessage').removeClass('hide');
                            $('#notificationMessage').html(result.Message);
                        }
                    }
                    else {
                        DojoWeb.Notification.show(kendo.format('{0}: {1}', kendo.toString(new Date(), 'G'), result.Message));
                    }
                    DojoWeb.Busy.hide();
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error importing Airbnb Owner Payout Transactions.';
                        alert(message);
                    }
                }
            });
        },

        initMultiFileSelect = function (type, id, countId, reportDate) {
            DojoWeb.MultiSelect.install({
                'url': '/OwnerPayout/GetImportFileList?reportDate=' + reportDate + '&reportType=' + type,
                'id': id,
                'buttonClass': 'form-control owner-payout-dropdown input-sm',
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
                    $('#futureFilesContainer').removeClass('hide');
                }
                else {
                    $('#completedFilesContainer').removeClass('hide');
                    $('#futureFilesContainer').addClass('hide');
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
            else {
                $('#completedFolderName').html(' for <b>' + kendo.toString(folderDate, 'MMMM d yyyy') + '</b>: ');
                DojoWeb.MultiSelect.destroy('completedFileList');
                initMultiFileSelect(1, 'completedFileList', 'completedCount', reportDate);
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

DojoWeb.File = function () {
    var
        initUpload = function (options) {
            var clickId = options && options.clickId ? options.clickId : undefined;
            if (clickId == undefined) return;

            var $clickSelector = $('#' + clickId);
            var filenameId = options && options.filenameId ? options.filenameId : $clickSelector.attr('data-field');
            var uploadId = options && options.uploadId ? options.uploadId : $clickSelector.attr('data-upload');

            if (!filenameId || !uploadId) return;

            var $filenameSelector = $('#' + filenameId);
            var $uploadSelector = $('#' + uploadId);

            $clickSelector.unbind('click').on('click', function () {
                $uploadSelector.click(); // delegate to file select control
                $uploadSelector.change(); // force the change in case the file is the same
            });

            $uploadSelector.on('change', function () {
                var filename = $(this).val();
                if (filename != null) {
                    $filenameSelector.val(filename);
                    $filenameSelector.blur(); // allow validation to kick in wihout user interaction
                }
                else {
                    $filenameSelector.val('');
                }
            });
        },

        download = function (url) {
            //iOS devices do not support downloading. We have to inform user about this.
            if (/(iP)/g.test(navigator.userAgent)) {
                alert('Your device does not support files downloading. Please try again in desktop browser.');
                return false;
            }

            //If in Chrome or Safari - download via virtual link click
            if (isChrome || isSafari) {
                var link = document.createElement('a'); // Creating new link node.
                link.href = sUrl;

                if (link.download !== undefined) {
                    //Set HTML5 download attribute. This will prevent file from opening if supported.
                    var fileName = sUrl.substring(sUrl.lastIndexOf('/') + 1, sUrl.length);
                    link.download = fileName;
                }

                //Dispatching click event.
                if (document.createEvent) {
                    var e = document.createEvent('MouseEvents');
                    e.initEvent('click', true, true);
                    link.dispatchEvent(e);
                    return true;
                }
            }

            // Force file download (whether supported by server) for all other browsers
            if (sUrl.indexOf('?') === -1) {
                sUrl += '?download';
            }

            window.open(sUrl, '_self');
            return true;
        },

        isChrome = function () {
            return navigator.userAgent.toLowerCase().indexOf('chrome') > -1;
        },

        isSafari = function () {
            return navigator.userAgent.toLowerCase().indexOf('safari') > -1;
        }

    return {
        initUpload: initUpload,
        download: download
    }
}();

DojoWeb.OwnerPayoutUtil = function () {
    var
        init = function () {
            $('#ownerPayoutImport').on('click', function (e) {
                DojoWeb.Busy.show();
                $.ajax({
                    type: 'POST',
                    url: '/OwnerPayout/DoTestExcelImport?isCompleted=1',
                    success: function (result) {
                        DojoWeb.Busy.hide();
                        if (result.Status == 0)
                            DojoWeb.ActionAlert.warn('ownerPayout-alert', result.Message);
                        else if (result.Status == 1)
                            DojoWeb.ActionAlert.success('ownerPayout-alert', result.Message);
                        else
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', result.Message);
                    },
                    error: function (jqXHR, status, errorThrown) {
                        DojoWeb.Busy.hide();
                        if (status == 'error') {
                            var message = 'There was an intrna error in Owner Payout Excel import testing.';
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', message);
                        }
                    }
                });
            });

            $('#futureOwnerPayoutImport').on('click', function (e) {
                DojoWeb.Busy.show();
                $.ajax({
                    type: 'POST',
                    url: '/OwnerPayout/DoTestExcelImport?isCompleted=0',
                    success: function (result) {
                        DojoWeb.Busy.hide();
                        if (result.Status == 0)
                            DojoWeb.ActionAlert.warn('ownerPayout-alert', result.Message);
                        else if (result.Status == 1)
                            DojoWeb.ActionAlert.success('ownerPayout-alert', result.Message);
                        else
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', result.Message);
                    },
                    error: function (jqXHR, status, errorThrown) {
                        DojoWeb.Busy.hide();
                        if (status == 'error') {
                            var message = 'There was an intrna error in Owner Payout Excel future import testing.';
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', message);
                        }
                    }
                });
            });

            $('#ownerPayoutCsvImport').on('click', function (e) {
                DojoWeb.Busy.show();
                $.ajax({
                    type: 'POST',
                    url: '/OwnerPayout/DoTestCsvImport?isCompleted=1',
                    success: function (result) {
                        DojoWeb.Busy.hide();
                        if (result.Status == 0)
                            DojoWeb.ActionAlert.warn('ownerPayout-alert', result.Message);
                        else if (result.Status == 1)
                            DojoWeb.ActionAlert.success('ownerPayout-alert', result.Message);
                        else
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', result.Message);
                    },
                    error: function (jqXHR, status, errorThrown) {
                        DojoWeb.Busy.hide();
                        if (status == 'error') {
                            var message = 'There was an intrna error in Owner Payout csv import testing.';
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', message);
                        }
                    }
                });
            });

            $('#futureOwnerPayoutCsvImport').on('click', function (e) {
                DojoWeb.Busy.show();
                $.ajax({
                    type: 'POST',
                    url: '/OwnerPayout/DoTestCsvImport?isCompleted=0',
                    success: function (result) {
                        DojoWeb.Busy.hide();
                        if (result.Status == 0)
                            DojoWeb.ActionAlert.warn('ownerPayout-alert', result.Message);
                        else if (result.Status == 1)
                            DojoWeb.ActionAlert.success('ownerPayout-alert', result.Message);
                        else
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', result.Message);
                    },
                    error: function (jqXHR, status, errorThrown) {
                        DojoWeb.Busy.hide();
                        if (status == 'error') {
                            var message = 'There was an intrna error in Owner Payout csv future import testing.';
                            DojoWeb.ActionAlert.fail('ownerPayout-alert', message);
                        }
                    }
                });
            });
        }

    return {
        init: init
    }
}();

