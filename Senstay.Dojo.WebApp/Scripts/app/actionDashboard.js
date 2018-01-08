"use strict";
var DojoWeb = DojoWeb || {};

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

DojoWeb.ActionDashboardForm = function () {
    var 
        init = function () {
            initEvents();
        },

        initEvents = function () {
            // set up file uploads
            DojoWeb.File.initUpload({ clickId: 'attachedExcelFile' });

            // install uploaded file removal event
            $('.dojo-file-remove').unbind('click').on('click', function (e) {
                var fileFieldId = $('#' + e.target.id).attr('data-field');
                if (fileFieldId && fileFieldId != '') {
                    var $fileSelector = $('#' + fileFieldId);
                    $fileSelector.val('');
                    $fileSelector.blur(); // allow validation to kick in wihout user interaction
                }
            });

            $('#ContactUpdate').unbind('click').on('click', function (e) {
                doContactUpdate();
            });
        },

        doContactUpdate = function () {
            var formData = new FormData();
            var excelFileUpload = $("#attachedExcelFile").get(0);
            formData.append('excelFile', excelFileUpload.files[0]);

            DojoWeb.Busy.show();

            $.ajax({
                type: 'POST',
                url: '/Import/UpdatePropertyContacts',
                contentType: false, // Not to set any content header if FormData() is used
                processData: false, // Not to process data if FormData() is used
                data: formData,
                success: function (result) {
                    if (result != '') {
                        DojoWeb.Busy.hide();
                        if (result.Future.Bad > 0)
                            DojoWeb.ActionAlert.fail('import-alert', message);
                        else
                            DojoWeb.ActionAlert.success('import-alert', message);
                    }
                    else {
                        DojoWeb.Notification.show('There was an error importing Excel file.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error importing Excel file.';
                        alert(message);
                    }
                }
            });
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
