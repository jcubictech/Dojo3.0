"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.ImportForm = function () {
    var $ImportDate = $('#ImportDate'),

        init = function () {
            initEvents();
            $ImportDate.data('kendoDatePicker').value('');
        },

        initEvents = function () {
            // set up file uploads
            DojoWeb.ImportFile.initUpload({ clickId: 'attachImportFile' });

            // install uploaded file removal event
            $('.dojo-file-remove').unbind('click').on('click', function (e) {
                var fileFieldId = $('#' + e.target.id).attr('data-field');
                if (fileFieldId && fileFieldId != '') {
                    var $fileSelector = $('#' + fileFieldId);
                    $fileSelector.val('');
                    $fileSelector.blur(); // allow validation to kick in wihout user interaction
                }
            });

            $('#excelImport').unbind('click').on('click', function (e) {
                doImport();
            });

            $ImportDate.kendoDatePicker({
                format: 'MM/dd/yyyy',
                open: function (e) {
                    toggleDateTyping(true); // hack: no direct date typing
                },
                close: function (e) {
                },
                change: function (e) {
                }
            }).data('kendoDatePicker');
        },

        doImport = function () {
            var formData = new FormData();
            // add the form fields
            var filetype = $('input[name="FileType"]:checked').val();
            formData.append('ImportDate', $ImportDate.val());
            formData.append('FileType', filetype);
            formData.append('ImportFile', $('#ImportFile').val());
            //  add import file
            var importFileUpload = $("#attachedImportFile").get(0);
            formData.append('attachedImportFile', importFileUpload.files[0]);

            DojoWeb.Busy.show();

            $.ajax({
                type: 'POST',
                url: '/Import/ImportNonAirbnb',
                contentType: false, // Not to set any content header if FormData() is used
                processData: false, // Not to process data if FormData() is used
                data: formData,
                success: function (result) {
                    if (result) {
                        DojoWeb.Busy.hide();
                        var count = parseInt(result);
                        if (count < 0) {
                            var message = '';
                            if (count < -9999)
                                message = kendo.format('There are {0} errors while importing data from Excel file. See InputError table for details.', -ErrorCount);
                            else
                                message = kendo.format('There are database saving errors while importing data from Excel file.');

                            DojoWeb.ActionAlert.fail('import-alert', message);
                        }
                        else {
                            var good = (count / 10000) | 0; // convert to integer
                            var bad = count % 10000;
                            if (bad == 0)
                                DojoWeb.ActionAlert.success('import-alert', kendo.format('{0} records are imported successfully.', good));
                            else
                                DojoWeb.ActionAlert.success('import-alert', kendo.format('{0} records are imported successfully; {1} property codes are not found.', good, bad));
                        }
                    }
                    else {
                        DojoWeb.Notification.show('There was an error importing data from Excel file.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error importing importing data from Excel file.';
                        alert(message);
                    }
                }
            });
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
        refreshForm: refreshForm,
        serverError: serverError
    }
}();

DojoWeb.ImportFile = function () {
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

