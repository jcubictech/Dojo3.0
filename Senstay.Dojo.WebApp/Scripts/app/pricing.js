"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.PricingForm = function () {
    var
        init = function () {
            initEvents();
        },

        initEvents = function () {
            // set up file uploads
            DojoWeb.ImportFile.initUpload({ clickId: 'attachPricingFile' });

            // install uploaded file removal event
            $('.dojo-file-remove').unbind('click').on('click', function (e) {
                var fileFieldId = $('#' + e.target.id).attr('data-field');
                if (fileFieldId && fileFieldId != '') {
                    var $fileSelector = $('#' + fileFieldId);
                    $fileSelector.val('');
                    $fileSelector.blur(); // allow validation to kick in wihout user interaction
                }
            });

            $('#doPricing').unbind('click').on('click', function (e) {
                doPricing();
            });
        },

        doPricing = function () {
            var formData = new FormData();
            // add the form fields
            formData.append('FileType', 'Excel');
            formData.append('PricingFile', $('#PricingFile').val());
            //  add import file
            var importFileUpload = $("#attachedPricingFile").get(0);
            formData.append('attachedPricingFile', importFileUpload.files[0]);

            DojoWeb.Busy.show();

            $.ajax({
                type: 'POST',
                url: '/Pricing/UploadPrices',
                contentType: false, // Not to set any content header if FormData() is used
                processData: false, // Not to process data if FormData() is used
                data: formData,
                success: function (result) {
                        DojoWeb.Busy.hide();
                        if (result == 0) { // success

                        }
                        else { // fail
                            DojoWeb.Notification.show('There was an error uploading price to Airbnb from Excel file.');
                        }
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error uploading price to Airbnb from Excel file.';
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

