"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.PricingForm = function () {
    var
        _processNote = 'Depending on how many prices to push to Airbnb, it may take a while to complete. Please wait for the completion...',

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

            $('#updateOption input[type=radio]').change(function () {
                if ($(this).val() == '1')
                    $('#doPricing').prop('value', 'Update Airbnb Pricing');
                else
                    $('#doPricing').prop('value', 'Update Airbnb Custom Stay');
            });

            $('#doPricing').unbind('click').on('click', function (e) {
                doListingUpdate();
            });

            $('#testPropertyList').unbind('click').on('click', function (e) {
                testFantasticApi('PropertyListing', 'GET');
            });

            $('#testCalendarList').unbind('click').on('click', function (e) {
                testFantasticApi('PriceListing', 'GET');
            });

            $('#testPricePush').unbind('click').on('click', function (e) {
                testFantasticApi('PricePush', 'POST');
            });
        },

        doListingUpdate = function () {
            var formData = new FormData();
            // add the form fields
            formData.append('FileType', 'Excel');
            formData.append('PricingFile', $('#PricingFile').val());

            // determine which action to take
            var updateType = $('input[name="UpdateType"]:checked').val();
            var method = 'UpdatePrices';
            var messageTemplate = '{0}: Airbnb price update result: total = {1}, successful = {2}, failed = {3}.';
            if (updateType == '2') { // custom stay
                method = 'UpdateCustomStays';
                messageTemplate = '{0}: Airbnb custom stay update result: total = {1}, successful = {2}, failed = {3}.';
            }

            //  add import file
            var importFileUpload = $("#attachedPricingFile").get(0);
            formData.append('attachedPricingFile', importFileUpload.files[0]);
            $('#longProcessNote').html(kendo.toString(new Date(), 'G') + ': ' + _processNote);
            $('#notificationMessage').addClass('hide');

            DojoWeb.Busy.show();

            $.ajax({
                type: 'POST',
                url: '/Pricing/' + method,
                contentType: false, // Not to set any content header if FormData() is used
                processData: false, // Not to process data if FormData() is used
                data: formData,
                success: function (result) {
                    DojoWeb.Busy.hide();
                    if (result.imported == 1) { // input file has been processed
                        var message = kendo.format(messageTemplate, kendo.toString(new Date(), 'G'), result.total, result.good, result.bad);
                        if (result.bad > 0)
                            DojoWeb.ActionAlert.fail('import-alert', message + ' Error message = ' + result.message);
                        else
                            DojoWeb.ActionAlert.success('import-alert', message);
                    }
                    else { // fail
                        DojoWeb.ActionAlert.fail('import-alert', result.message);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error updating Airbnb listings from Excel file.';
                        alert(message);
                    }
                }
            });
        },

        testFantasticApi = function (method, type) {
            DojoWeb.Busy.show();
            $.ajax({
                type: type,
                url: '/Pricing/' + method,
                success: function (result) {
                    DojoWeb.Busy.hide();
                    if (result != 0) { // success
                        if (method == 'PropertyListing') {
                            var response = JSON.parse(result);
                            var count = response.listings.length;
                            alert('there are total of ' + count + ' properties returned.');
                        }
                        else if (method == 'PriceListing') {
                            var response = JSON.parse(result);
                            var count = response.calendar.length;
                            alert('there are total of ' + count + ' price objects returned.');
                        }
                        else if (method == 'PricePush') {
                            var response = JSON.parse(result);
                            if (response.success == true) {
                                alert('Price push is successful.');
                            }
                            else {
                                alert('The Price push request has failed.');
                            }
                        }
                    }
                    else { // fail
                        DojoWeb.Notification.show('There was an error calling Fantastic API for ' + method);
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    DojoWeb.Busy.hide();
                    if (status == 'error') {
                        var message = 'There was an error calling Fantastic API for ' + method;
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

