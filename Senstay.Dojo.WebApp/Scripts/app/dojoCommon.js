"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.Busy = function () {
    var
        show = function () {
            $('#waitAnimation').show();
        },

        hide = function () {
            $('#waitAnimation').hide();
        },

        cursor = function (selector, isBusy) {
            isBusy == true ? $(selector).css('cursor', 'wait') : $(selector).css('cursor', 'pointer');
        }

    return {
        show: show,
        hide: hide,
        cursor: cursor
    }
}();

DojoWeb.Cookie = function () {
    var _cookieLifeSpan = 14,
        _cookiePath = '/',
        cookieParamName = 'name',
        lifespanName = 'life',
        dataName = 'data',

        exist = function (options) {
            var cookieName = (options && options[cookieParamName]) ? options[cookieParamName] : undefined;
            return cookieName == undefined ? null : $.cookie(cookieName);
        },

        set = function (options) {
            var cookieName = (options && options[cookieParamName]) ? options[cookieParamName] : undefined;
            var lifespan = (options && options[lifespanName]) ? options[lifespanName] : _cookieLifeSpan;
            var data = (options && options[dataName]) ? options[dataName] : null;
            if (cookieName != undefined && data != null) {
                $.cookie(cookieName, data, { expires: lifespan, path: _cookiePath });
                return true;
            }
            return false;
        },

        get = function (cookieName) {
            if (cookieName != undefined && $.cookie(cookieName) != undefined) {
                return $.cookie(cookieName);
            }
            return null;
        },

        remove = function (cookieName) {
            return cookieName == undefined ? false : $.removeCookie(cookieName, { path: _cookiePath });
        },

        reset = function (options) {
            var cookieName = (options && options[cookieParamName]) ? options[cookieParamName] : undefined;
            var lifespan = (options && options[lifespanName]) ? options[lifespanName] : _cookieLifeSpan;
            if (cookieName != undefined) {
                $.cookie(cookieName, '', { expires: lifespan, path: _cookiePath })
            }
        }

    return {
        exist: exist,
        set: set,
        get: get,
        remove: remove,
        reset: reset
    }
}();

DojoWeb.Date = function () {
    var
        parseJsonDate = function (jsonDateString) {
            if (jsonDateString)
                return new Date(parseInt(jsonDateString.replace('/Date(', '')));
            else
                return undefined;
        }

    return {
        parseJsonDate: parseJsonDate
    }
}();

DojoWeb.Helpers = function () {
    var _wheelSelector = 'body',
        _distance = 300,

        getUrlPart = function (part) {
            return $(location).attr(part); // part is the property name above
        },

        getQueryString = function (name, qstring) {
            var qs = qstring;
            if (qstring == undefined) qs = window.location.search.substring(1);
            var kvps = qs.split('&');
            for (var i = 0; i < kvps.length; i++) {
                var kv = kvps[i].split('=');
                if (kv[0] == name) {
                    return kv[1];
                }
            }
            return null;
        },

        formatMoney = function(n) {
            return n.toFixed(2).replace(/(\d)(?=(\d{3})+\.)/g, '$1,'); // use formatCurrency for $ format
        },

        formatCurrency = function (amount) {
            var formatter = new Intl.NumberFormat('en-US', {
                style: 'currency',
                currency: 'USD',
                minimumFractionDigits: 2, // default
            });
            return formatter.format(amount);

            // can also use this if locale is not a strict concerns
            //return amount.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
        },

        removeUsCurrencyMask = function (currency) {
            return currency.replace(/\$|,|\)/g, '').replace('(', '-');
        },

        preventBackspaceForDropdown = function (selector) {
            $(selector).keypress(function (e) { return cancelBackspace(e) });
            $(selector).keydown(function (e) { return cancelBackspace(e) });
        },

        cancelBackspace = function (e) {
            if (e.keyCode == 8) {
                return false;
            }
        },

        isChrome = function () {
            return navigator.vendor && navigator.vendor.indexOf('Google') > -1;
            //var ua = navigator.userAgent.toLowerCase();
            //return ua.indexOf('safari') != -1 && ua.indexOf('chrome') > -1;
        },

        isSafari = function () {
            return navigator.vendor && navigator.vendor.indexOf('Apple') > -1 &&
                   navigator.userAgent && !navigator.userAgent.match('CriOS');

            // return navigator.userAgent.toLowerCase().indexOf('safari') > -1;
            //var ua = navigator.userAgent.toLowerCase();
            //return ua.indexOf('safari') != -1 && ua.indexOf('chrome') == -1;
        },

        isMac = function () {
            var osName = '';
            if (navigator.appVersion.indexOf('Mac') != -1) {
                osName = 'MacOS';
                //alert('Detect Mac');
            }
            return osName != '';

            //var isOpera = !!window.opera || navigator.userAgent.indexOf('Opera') >= 0;
            //// Opera 8.0+ (UA detection to detect Blink/v8-powered Opera)
            //var isFirefox = typeof InstallTrigger !== 'undefined';   // Firefox 1.0+
            //var isSafari = Object.prototype.toString.call(window.HTMLElement).indexOf('Constructor') > 0;
            //// At least Safari 3+: "[object HTMLElementConstructor]"
            //var isChrome = !!window.chrome; // Chrome 1+
            //var isIE = /*@cc_on!@*/false; // At least IE6
        
            //if (osName == 'MacOS' && isChrome == true) {
            //    return 1;
            //}
            //else if (osName == 'MacOS' && isSafari == true) {
            //    return 2;
            //}
            //else
            //    return 0;
        },

        isEdge = function () {
            var ua = navigator.userAgent.toLowerCase();
            return ua.indexOf('Edge/') != -1 ||
                   typeof CSS !== 'undefined' && CSS.supports('(-ms-ime-align:auto)');
        },

        setupBackToTop = function () {
            if (($(window).height() + 100) < $(document).height()) {
                $('#top-link-block').removeClass('hidden').affix({
                    // how far to scroll down before link "slides" into view
                    offset: { top: 100 }
                });
            }
        },

        resizeGrid = function (gridName) {
            var gridElement = $(gridName),
                dataArea = gridElement.find(".k-grid-content"),
                gridHeight = gridElement.innerHeight(),
                otherElements = gridElement.children().not(".k-grid-content"),
                otherElementsHeight = 0;

            otherElements.each(function () {
                otherElementsHeight += $(this).outerHeight();
            });

            dataArea.height(gridHeight - (otherElementsHeight));
        },

        scrollToTarget = function (id, speed) {
            var $id = $('#' + id);
            if ($id.offset()) {
                var eTop = $id.offset().top; //get the offset top of the element
                var wTop = $(window).scrollTop(); //position of the element w.r.t window
                var height = $(window).height();
                if (eTop - wTop > height) {
                    $('html,body').animate({ scrollTop: eTop - height/2 + 50 }, speed);
                }
                else {
                    $('html,body').animate({ scrollTop: eTop - 240 }, speed);
                }
            }
        },

        // this only work for page scrolling
        addWheelListener = function (selector, distance) {
            if (distance !== undefined) {
                _wheelSelector = selector;
                _distance = distance;
            }
            if (window.addEventListener) {
                window.addEventListener('DOMMouseScroll', wheelSpeed, false);
                window.onmousewheel = document.onmousewheel = wheelSpeed;
            }
        },

        wheelSpeed = function(event) {
            var delta = 0;
            if (event.wheelDelta) delta = event.wheelDelta / 120;
            else if (event.detail) delta = -event.detail / 3;

            wheelHandle(delta, _distance);
            if (event.preventDefault) event.preventDefault();
            event.returnValue = false;
        },
        
        wheelHandle = function (delta, distance) {
            var time = 1000;
    
            $(_wheelSelector).stop().animate({
                scrollTop: $(window).scrollTop() - (distance * delta)
            }, time );
        },

        // kendo grid with verrtical scrollbar squeeze the grid width; this is a hack to add an artifical grid column
        // to compensate for it
        injectDummyHeaderColumn = function (gridId, width) {
            if (!$(gridId + ' table thead tr th:last').hasClass('k-scrollbar-header')) {
                width = width == undefined ? 16 : width;
                $(gridId + ' table thead tr').append('<th class="k-header k-scrollbar-header" role="columnheader" rowspan="1" style="padding:0;min-width:0px;width:' + width + 'px;"> </th>');
            }
        }

    return {
        getUrlPart: getUrlPart,
        getQueryString: getQueryString,
        formatMoney: formatMoney,
        formatCurrency: formatCurrency,
        removeUsCurrencyMask: removeUsCurrencyMask,
        isMac: isMac,
        isChrome: isChrome,
        isSafari: isSafari,
        isEdge: isEdge,
        setupBackToTop: setupBackToTop,
        resizeGrid: resizeGrid,
        scrollToTarget: scrollToTarget,
        injectDummyHeaderColumn: injectDummyHeaderColumn,
        preventBackspaceForDropdown: preventBackspaceForDropdown,
        //addWheelListener: addWheelListener,
        //wheelHandle: wheelHandle
    }

}();

DojoWeb.Alert = function () {
    var _alertTypes = {
            success: 'alert-success',
            warn: 'alert-warning',
            error: 'alert-danger'
        },

        alertTypes = function () {
            return _alertTypes;
        }

    return {
        alertTypes: alertTypes
    }
}();

DojoWeb.MultiSelect = function () {
    var
        install = function (options) {

            if (typeof (options) != 'object') return;

            var id = options['id'] ? options['id'] : undefined;
            var urlAction = options['url'] ? options['url'] : undefined;
            var dataSource = options['dataSource'] ? options['dataSource'] : undefined;
            var countId = options['countId'] ? options['countId'] : undefined;

            if ((urlAction === undefined && dataSource === undefined) || id === undefined) return;

            if (urlAction == undefined) {
                initMultiSelect(id, dataSource, options);
            }
            else {
                $.ajax({
                    url: urlAction,
                    dataType: 'json',
                    success: function (result) {
                        initMultiSelect(id, result, options);
                        if (countId != undefined) {
                            $('#' + countId).html(' (' + result.length + ')');
                        }
                    }
                });
            }
        },

        destroy = function (id) {
            if ($('#' + id).multiselect) $('#' + id).multiselect('destroy');
        },

        initMultiSelect = function (id, result, options) {
            var callback = options['callback'] ? options['callback'] : undefined;
            var buttonClass = options['buttonClass'] ? options['buttonClass'] : undefined;
            var defaultValue = options['default'] ? options['default'] : undefined;
            var rightCaret = options['rightCaret'] ? options['rightCaret'] : undefined; // custom property for this app
            var includeAll = options['includeAll'] ? options['includeAll'] : false;
            var selectAllText = options['selectAllText'] ? options['selectAllText'] : undefined;
            var enableFilter = options['enableFilter'] ? options['enableFilter'] : undefined;
            var includeMarker = options['includeMarker'] ? options['includeMarker'] : undefined;
            var markerName = options['markerName'] ? options['markerName'] : undefined;
            var defaultMarker = options['defaultMarker'] ? options['defaultMarker'] : '';
            var markerHeader = options['markerHeader'] ? options['markerHeader'] : undefined;
            var markerCallback = options['markerCallback'] ? options['markerCallback'] : undefined;
            var viewOnly = options['viewOnly'] ? options['viewOnly'] : false;

            var containerId = '#' + id;

            if (selectAllText == undefined) selectAllText = "Select All";
            var data = [];
            $.each(result, function (index, source) {
                data.push({
                    'label': source.name ? source.name : source.Text,
                    'value': source.id ? source.id : source.Value
                });
            });

            var multiSelect = $(containerId).multiselect({
                buttonWidth: '100%',
                numberDisplayed: 1,
                selectedClass: null,
                buttonClass: buttonClass == undefined ? 'form-control product-dropdown input-sm' : buttonClass,
                dropRight: true,
                includeMarker: includeMarker != undefined ? includeMarker : false,
                markerName: markerName != undefined ? markerName : '',
                defaultMarker: defaultMarker,
                markerHeader: markerHeader,
                markerCallback: markerCallback,
                includeSelectAllOption: includeAll,
                selectAllText: selectAllText,
                enableFiltering: enableFilter,
            });

            $(containerId).multiselect('dataprovider', data);

            if (defaultValue !== undefined) {
                if (defaultValue instanceof Array) {
                    for (var i = 0; i < defaultValue.length; i++) {
                        $(containerId).multiselect('select', defaultValue[i], true);
                    }
                }
                else {
                    $(containerId).multiselect('select', defaultValue, true);
                }
            }

            $(containerId).change();

            if (rightCaret == true) {
                $('button.multiselect b').css('float', 'right').css('margin-top', '8px');
            }

            if (callback != undefined) callback(multiSelect);

            if (viewOnly) $('.multiselect-container input').attr("disabled", true);
        },

        setSelectedState = function (id, items, isSelected) {
            if (items != null) {
                try {
                    if (isSelected) {
                        $(id).multiselect('deselectAll', false);
                        $(id).multiselect('select', items);
                    }
                    else
                        $(id).multiselect('deselect', items);
                }
                catch (e) {
                    // ignore and continue
                }
            }
        }

    return {
        install: install,
        destroy: destroy,
        setSelectedState: setSelectedState
    }
}();

DojoWeb.ActionAlert = function () {
    var
        success = function (id, message, delay) {
            window.alerts.showAlert(
                {
                    message: message,
                    alertClass: DojoWeb.Alert.alertTypes().success // 'alert-success'
                },
                {
                    id: id,
                    delay: (delay == undefined ? null : delay)
                }
            );
        },

        warn = function (id, message, delay) {
            window.alerts.showAlert(
                {
                    message: message,
                    alertClass: DojoWeb.Alert.alertTypes().warn // alert-warning
                },
                {
                    id: id,
                    delay: (delay == undefined ? null : delay)
                }
            );
        },

        fail = function (id, message, delay) {
            window.alerts.showAlert(
                {
                    message: message,
                    alertClass: DojoWeb.Alert.alertTypes().error // 'alert-danger'
                },
                {
                    id: id,
                    delay: (delay == undefined ? null : delay)
                }
        );
    },

        remove = function (id) {
            $('#' + id).html('');
        }

    return {
        success: success,
        warn: warn,
        fail: fail,
        remove: remove
    }
}();

DojoWeb.SessionMonitor = function () {
    var _now = Date.now,
        _immediate = false,

        // sliding timer to reload page after inactive for preset time
        install = function (timeout, url) {
            $('html, body').on(
                'click mousemove keyup', 
                _.debounce(function () {
                    $(window).scrollTop(0); // so that we can see the timeout message

                    $('.container')
                        .css('opacity', 0.3)  // blur it
                        .css('pointer-events', 'none'); // make page not clickable

                    $(':checkbox, :radio, select').prop("disabled", true);

                    var alertId = 'postback-alert';
                    $('#' + alertId).html(''); // clear the existing timeout alert

                    // display the timeout messsage on top of the page
                    window.alerts.error(
                        'Your session has expired. Please dismiss this message to reload the page.',
                        {
                            id: alertId,
                            callback: function() {
                                window.location.href = url;
                            }
                        });
                }, timeout, _immediate)
            )
        }

    return {
        install: install
    }
}();

DojoWeb.Plugin = function () {
    var _$formDialog = undefined, // one and only one <div id="formDialog"> must be defined for this module to work

        initDatePicker = function (selector) {
            if (selector == undefined) {
                $('.datepicker').kendoDatePicker({ value: '' });
            }
            else {
                $(selector).kendoDatePicker({ value: '' });
            }
        },

        initSearchableList = function (selector, focusId) {
            if (selector == undefined) selector = '.kendo-autoselect';
            $(selector).kendoComboBox({
                filter: 'contains',
                suggest: true
            });
        },

        initNumericEditor = function (rowSelector, fieldSelector) {
            var dataRows = $(rowSelector);
            $.each(dataRows, function (index, item) {
                var amounts = $(this).find(fieldSelector);
                if (amounts.length > 0) {
                    if (amounts[0].id !== undefined) {
                        $('#' + amounts[0].id).inputmask('numeric', {
                            radixPoint: ".",
                            groupSeparator: ',',
                            digits: 2,
                            autoGroup: true, // will put , for every 3-digit
                            prefix: '$', // No Space, this will truncate the first character
                            rightAlign: false,
                            oncleared: function () { self != undefined ? self.Value('') : ''; }
                        });

                        // kendo seems to have limit on how many numeric textboxes it can create
                        //$('#payment-item-' + index).kendoNumericTextBox({
                        //    spinners: false,
                        //    format: 'c',
                        //    decimals: 2
                        //});
                    }
                }
            });
        },

        initFormDialog = function (options) {
            var selector = (options && options.selector) || undefined;
            var caption = (options && options.caption) || 'Form Dialog';
            var url = (options && options.url) || undefined;
            var formId = (options && options.formId) || undefined;
            var width = (options && options.width) || '600px';
            var initEvent = (options && options.initEvent) || undefined;
            var closeEvent = (options && options.closeEvent) || undefined;
            var readonly = (options && options.readonly) || '.app-field-readonly';
            var modal = (options && options.modal != undefined) ? options.modal : true;

            if (selector == undefined || url == undefined) return;

            $(selector).click(function (e) {
                if (url != undefined) {
                    DojoWeb.Busy.show();
                    var id = $(this).data('id') || 0;
                    var link = $(this).data('link') || '';
                    if (id == 0) caption = caption.replace('Edit', 'New');
                    var params = link == '' ? { 'Id': id } : { 'Id': id, 'data': link };
                    $.ajax({
                        url: url,
                        data: params,
                        success: function (data) {
                            DojoWeb.Busy.hide();
                            _$formDialog = $('#formDialog');
                            if (_$formDialog.length > 0) {
                                $('#formDialog .dialog-body').html(data);
                                if (readonly && $(readonly).length > 0) $(readonly).attr('readonly', 'readonly');
                                if (!_$formDialog.data('kendoWindow')) {
                                    _$formDialog.kendoWindow({
                                        width: width,
                                        title: caption,
                                        actions: ['Close'], //modal == false ? [] : ['Close'],
                                        visible: false,
                                        resizable: false,
                                        close: closeEvent ? closeEvent : null,
                                        modal: modal, // need to set to try to catch the event that clicking outside of the popup
                                    });
                                }
                                else { // set dialog options
                                    _$formDialog.data('kendoWindow').setOptions({
                                        width: width,
                                        title: caption,
                                        actions: modal == false ? [] : ['Close'],
                                        visible: false,
                                        resizable: false,
                                        close: closeEvent ? closeEvent : null,
                                        modal: modal, // need to set to try to catch the event that clicking outside of the popup
                                    });
                                }
                                _$formDialog.data('kendoWindow').title(caption);
                                _$formDialog.data('kendoWindow').setOptions({ width: width });
                                _$formDialog.data('kendoWindow').open().center(); // open() needs to come before center()

                                // this enable closing popup when clicking outside of it if modal = true 
                                $(document).unbind('click').on('click', '.k-overlay', function (e) {
                                    if (modal == true) _$formDialog.data('kendoWindow').close();
                                });

                                ///because the page is loaded with ajax, the validation rules are lost, we have to rebind them:
                                var $form = $('#' + formId);
                                $form.removeData('validator');
                                $form.removeData('unobtrusiveValidation');
                                $form.each(function () { $.data($(this)[0], 'validator', false); }); //enable to display the error messages
                                $.validator.unobtrusive.parse('#' + formId);

                                if (initEvent != undefined) initEvent(formId, id);
                            }
                        },
                        error: function (jqXHR, status, errorThrown) {
                            DojoWeb.Busy.hide();
                            if (status == 'error') {
                                //displayServerError();
                            }
                        }
                    })
                }
            });
        },

        closeFormDialog = function () {
            if (_$formDialog && _$formDialog.length > 0) _$formDialog.data('kendoWindow').close();
        },

        noHorizontalScroll = function () {
            if (_$formDialog) _$formDialog.css('overflow', 'hidden');
        }

    return {
        initDatePicker: initDatePicker,
        initSearchableList: initSearchableList,
        initNumericEditor: initNumericEditor,
        initFormDialog: initFormDialog,
        closeFormDialog: closeFormDialog,
        noHorizontalScroll: noHorizontalScroll
    }
}();

DojoWeb.MenuActions = function () {
    var 
        install = function (id, url) {
            // deprecated
            // set search box position
            //$(window).unbind('resize').on('resize', function () {
            //    var position = $(window).width() - $('.actionBar-inquiry-search').width() - 300;
            //    if (position > 550) {
            //        $('.actionBar-inquiry-search').css('left', position + 'px');
            //        $('.actionBar-inquiry-search').show();
            //    }
            //    else {
            //        $('.actionBar-inquiry-search').hide();
            //        //$('.app-page-container div.navbar-fixed-top').removeClass('navbar-fixed-top');
            //    }
            //});
            //$(window).trigger('resize');
            //$('.actionBar-inquiry-search span').on('click', function (e) {
            //    var id = $('#inquiryId').val();
            //    window.location.href = '/Inquiry/index/?id=' + id;
            //});

            var fixedTop = 110;
            if (!DojoWeb.Helpers.isChrome() && !DojoWeb.Helpers.isSafari()) fixedTop = 112;
            $('.actionBar-fixed-top').css('top', fixedTop + 'px');
            $('.app-grid .k-grid-header').css('top', fixedTop + 60 + 'px');

            $('li.dropdown a').on('click', function (e) {
                $('.app-page-container div.navbar-fixed-top').css('top', -fixedTop + 'px');
            });

            $(window).click(function () {
                //$('.app-page-container div.navbar-fixed-top').css('top', fixedTop + 'px');
            });

            $('li.dropdown .dropdown-menu').on('click', function (e) {
                event.stopPropagation();
            });
        }

    return {
        install: install
    }
}();

DojoWeb.File = function () {
    var
        // options example: { browseId:'attachFile', removeId: 'removeFile' } or { browseId:'attachFile', removeId: 'removeFile', filenameId:'UploadFile', uploadId:'fileToUpload' }
        initUpload = function (options) { 
            var browseId = options && options.browseId ? options.browseId : undefined;
            if (browseId == undefined) return;

            var $browseSelector = $('#' + browseId);

            var filenameId = options && options.filenameId ? options.filenameId : $browseSelector.attr('data-field');
            var uploadId = options && options.uploadId ? options.uploadId : $browseSelector.attr('data-upload');

            if (!filenameId || !uploadId) return;

            var $removeSelector = options && options.removeId ? $('#' + options.removeId) : undefined;

            var $filenameSelector = $('#' + filenameId);
            var $uploadSelector = $('#' + uploadId);

            $browseSelector.on('click', function () {
                $uploadSelector.click(); // delegate to file select control
                $uploadSelector.change(); // force the change in case the file is the same
            });

            if ($removeSelector != undefined) {
                $removeSelector.on('click', function () {
                    $filenameSelector.val('');
                });
            }

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
            if (DojoWeb.Helpers.isChrome() || DojoWeb.Helpers.isSafari()) {
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
        }

    return {
        initUpload: initUpload,
        download: download
    }
}();

DojoWeb.Download = function () {
    var _cookieName = 'DojoDownload',
        _downloadToken = 'done',
        _downloadMonitor = undefined,
        _callback = undefined,

        // TODO: this logic does not work for removing busy icon
        monitor = function (id, callback) {
            if (_downloadMonitor != undefined) clearInterval(_downloadMonitor);
            _downloadMonitor = undefined;
            _callback = callback;
            //var token = new Date().getTime(); //use the current timestamp as the token value
            //$('#' + id).val(token);
            $.blockUI.defaults.message = null;
            $.blockUI();

            _downloadMonitor = window.setInterval(function () {
                var cookieValue = $.cookie(_cookieName);
                if (cookieValue == _downloadToken) finish();
            }, 1000);
        },

        finish = function () {
            if (_downloadMonitor != undefined) clearInterval(_downloadMonitor);
            _downloadMonitor = undefined;
            $.cookie(_cookieName, null); //clears this cookie value
            if (_callback != undefined) _callback();
            $.unblockUI();
        }

    return {
        monitor: monitor
    }
}();

DojoWeb.Confirmation = function () {
    var _dialogId = null,
        _okEvent = undefined,
        _cancelEvent = undefined,
        _closeEvent = undefined,
        _width = "480px",

        init = function (options) {
            var id = (options && options.id) || '';
            var caption = (options && options.caption) || 'Confirmation';
            var action = (options && options.action) || ['Close'];
            _width = (options && options.width) || '480px';
            _okEvent = (options && options.ok) || undefined;
            _cancelEvent = (options && options.cancel) || undefined;
            _closeEvent = (options && options.close) || undefined;

            _dialogId = id;
            var $dialog = $('#' + id);
            if ($dialog != undefined) {
                if (!$dialog.data('kendoWindow')) {
                    $dialog.kendoWindow({
                        modal: true,
                        width: _width,
                        title: caption,
                        actions: action,
                        visible: false,
                        resizable: false,
                    });

                    if ($('#' + _dialogId + ' .dialog-ok') != undefined) {
                        $('#' + _dialogId + ' .dialog-ok').unbind('click').on('click', function () {
                            if (_okEvent != undefined) {
                                _okEvent();
                            }
                            else {
                                window.location.href = '/';
                            }
                            if ($dialog.data('kendoWindow')) $dialog.data('kendoWindow').close();
                        });
                    }

                    if ($('#' + _dialogId + ' .dialog-cancel') != undefined) {
                        $('#' + _dialogId + ' .dialog-cancel').unbind('click').on('click', function () {
                            if (_cancelEvent != undefined) _cancelEvent();
                            $dialog.data('kendoWindow').close();
                        });
                    }

                    $('#' + _dialogId).data('kendoWindow').bind('close', function (e) {
                        if (_closeEvent != undefined) _closeEvent();
                    });
                }
                else {
                    $dialog.data('kendoWindow').title(caption);
                }
            }

            $dialog.data('kendoWindow').setOptions({ width: _width });
        },

        show = function (msg) {
            if (_dialogId != null) {
                var $dialog = $('#' + _dialogId);
                $('#' + _dialogId + ' .dialog-instruction').html(msg);
                $dialog.removeClass('hide');
                $dialog.data('kendoWindow').setOptions({ width: _width });
                $dialog.show();
                if ($dialog.data('kendoWindow')) {
                    $dialog.data('kendoWindow').open().center(); // open() needs to come before center()
                }
            }
        },

        confirmClose = function (options) {
            var message = (options && options.message) || '';
            init(options);
            show(message);
        },

        confirmDiscard = function (options) {
            var message = (options && options.message) || 'You have unsaved changes. Discard them?';
            init(options);
            show(message);
        }

    return {
        init: init,
        show: show,
        confirmClose: confirmClose,
        confirmDiscard: confirmDiscard
    }
}();

DojoWeb.ActionBar = function () {
    var _beginDate = undefined,
        _endDate = undefined,

        install = function (beginDate, endDate) {
            _beginDate = beginDate;
            _endDate = endDate;
            if (_beginDate == undefined) {
                _endDate = new Date();
                _beginDate = (3).months().ago();
            }

            $('#beginDatePicker').kendoDatePicker({
                value: _beginDate,
                change: function () {
                    _beginDate = this.value();
                }
            });

            $('#endDatePicker').kendoDatePicker({
                value: _endDate,
                change: function () {
                    _endDate = this.value();
                }
            });

            $('#actionBarDateRange').kendoValidator({
                validateOnBlur: false,
                rules: {
                    greaterdate: function (input) {
                        if (input.is('[data-greaterdate-msg]') && input.val() != "") {
                            var endDateVal = kendo.parseDate(input.val()),
                                beginDateVal = kendo.parseDate($("[name='" + input.data("greaterdateField") + "']").val());
                            return beginDateVal <= endDateVal;
                        }
                        return true;
                    }
                }
            });
        },

        attachEvent = function (goEvent, customEvent1, customEvent2, customEvent3) {
            if (customEvent1 != undefined) {
                $('.actionBar-custom-group').unbind('click').on('click', function (e) {
                    // unselect to remove the filter
                    var unselect = $('#' + e.target.id).hasClass('custom-filter-selected');
                    $('.actionBar-custom-group').removeClass('custom-filter-selected');
                    if (!unselect) $('#' + e.target.id).addClass('custom-filter-selected');
                    customEvent1(_beginDate, _endDate, e);
                });
            }

            if (customEvent2 != undefined) {
                if ($('.actionBar-status-group').length > 0) {
                    $('.actionBar-status-group').unbind('click').on('click', function (e) {
                        customEvent2(_beginDate, _endDate, e);
                    });
                }
                else if ($('.actionBar-channel-group').length > 0) {
                    $('.actionBar-channel-group').unbind('click').on('click', function (e) {
                        customEvent2(_beginDate, _endDate, e);
                    });
                }
            }

            if (customEvent3 != undefined) {
                if ($('.actionBar-vertical-group').length > 0) {
                    $('.actionBar-vertical-group').unbind('click').on('click', function (e) {
                        customEvent3(_beginDate, _endDate, e);
                    });
                } else if ($('.actionBar-approval-group').length > 0) {
                    $('.actionBar-approval-group').unbind('click').on('click', function (e) {
                        customEvent3(_beginDate, _endDate, e);
                    });
                }
            }

            if (goEvent != undefined) {
                $('#actionBarGo').unbind('click').on('click', function (e) {
                    // keep filters around for new query
                    //$('.actionBar-custom-group').removeClass('custom-filter-selected');
                    //$('.actionBar-status-group').prop('checked', false)
                    //$('.actionBar-vertical-group').prop('checked', false)
                    goEvent(_beginDate, _endDate);
                });
            }
        },

        getDateRange = function () {
            return { beginDate: _beginDate, endDate: _endDate }
        },

        setDateRange = function (beginDate, endDate) {
            _beginDate = beginDate;
            _endDate = endDate;
            $('#beginDatePicker').val(kendo.toString(_beginDate, 'MM/dd/yyyy'));
            $('#endDatePicker').val(kendo.toString(_endDate, 'MM/dd/yyyy'));
        },

        validateDateRange = function () {
            var rangeValidator = $("#actionBarDateRange").data("kendoValidator");
            return rangeValidator != undefined && rangeValidator.validate();
        }

    return {
        install: install,
        attachEvent: attachEvent,
        getDateRange: getDateRange,
        setDateRange: setDateRange,
        validateDateRange: validateDateRange
    }
}();

DojoWeb.GridFilters = function () {
    var
        loadFilters = function (gridId, cachedFilters) {
            try {
                if (gridId != undefined && cachedFilters != undefined)
                    $(gridId).data('kendoGrid').dataSource.filter(cachedFilters);
            }
            catch(e) {
            }
        },

        applyCustomFilter = function (gridId, customFilter, filterNames) {
            if (gridId && customFilter) {
                var ds = $(gridId).data('kendoGrid').dataSource;
                var gridFilters = ds.filter();
                if (gridFilters == undefined)
                    ds.filter(customFilter);
                else {
                    removeFilter(gridFilters, filterNames);
                    if (gridFilters.logic == 'or') gridFilters.logic = 'and';
                    gridFilters.filters.push(customFilter);
                    ds.filter(gridFilters.filters);
                }
            }
        },

        removeFilter = function(filter, fields){
            for (var i = 0; i < filter.filters.length; i++) {
                // For combination 'and' and 'or', kendo use nested filters so here is recursion
                if(filter.filters[i].hasOwnProperty('filters')){
                    filter.filters[i] = removeFilter(filter.filters[i], fields);
                    if($.isEmptyObject(filter.filters[i])){
                        filter.filters.splice(i, 1);
                        i--;
                        continue;
                    }
                }

                // Remove filters
                if(filter.filters[i].hasOwnProperty('field')){
                    if( fields.indexOf(filter.filters[i].field) > -1){
                        filter.filters.splice(i, 1);
                        i--;
                        continue;
                    }
                }
            }

            if(filter.filters.length === 0){
                filter = {};
            }

            return filter;
        }

    return {
        loadFilters: loadFilters,
        applyCustomFilter: applyCustomFilter,
    }
}(),

DojoWeb.Template = function () {
    var googleMapTemplate = '<div><a href="https://www.google.com/maps/place/{0}+{1}+{2}+{3}" target="_blank">{4}</a></div>',

        dateUS = function (data) {
            try { // assume it is a json date strong format /Date....../
                return kendo.toString(DojoWeb.Date.parseJsonDate(data), 'MM/dd/yyyy');
            }
            catch(e) {
                return kendo.toString(data, 'MM/dd/yyyy');
            }
        },

        nullable = function (data) {
            if (data)
                return data;
            else
                return '';
        },

        center = function (data) {
            return '<div style="text-align:center">' + data + '</div>';
        },

        right = function (data) {
            return '<div style="text-align:right">' + data + '</div>';
        },

        spacing = function (data) {
            return '<div class="app-cell-spacing">' + data + '</div>';
        },

        makelink = function (data, label, urlbase) {
            if (data && data != '') {
                var url = urlbase + data;
                if (data.indexOf('http') >= 0) {
                    if (data.indexOf('homeaway') >= 0) {
                        var i0 = data.indexOf('/p') + 2;
                        var i1 = data.indexOf('?');
                        if (i0 > 1 && i1 > i0) label = data.substring(i0, i1);
                    }
                    url = data;
                }
                else { // use data as link label
                    label = data;
                }
                return '<div style="text-align:center;"><a href="' + url + '" target="_blank">' + label + '</a></div>';
            }
            else
                return '';
        },

        link = function (data, label) {
            if (data)
                return '<div style="text-align:center;"><a href="' + data + '" target="_blank">' + label + '</a></div>';
            else
                return '';
        },

        linkOrText = function (data, label) {
            if (data) {
                if (data.indexOf('http') == 0)
                    return '<div style="text-align:center;"><a href="' + data + '" target="_blank">' + label + '</a></div>';
                else
                    return data;
            }
            else
                return '';
        },

        boolean = function (data) {
            return data == true ? '<div style="text-align:center"><i class="fa fa-check red"></i></div>' : '';
        },

        linkDate = function (source, date, ownerPayoutId) {
            if (source) {
                return kendo.format('<div><a href="/OwnerPayout/OwnerPayoutView?month={0}&source={1}&ownerPayoutId={2}" target="_blank">{0}</a></div>', dateUS(date), source, ownerPayoutId);
            }
            else
                return dateUS(date);
        },

        highlightDate = function (data, color) {
            if (color && color != '')
                return '<div style="color:' + color + '">' + dateUS(data) + '</div>';
            else
                return dateUS(data);
        },

        active = function (data) {
            if (!data) return '';
            data = data.toLowerCase();
            if (data.indexOf('active') == 0)
                return '<div style="text-align:center"><i class="fa fa-flag green"></i></div>';
            else if (data == 'inactive')
                return '<div style="text-align:center"><i class="fa fa-flag red"></i></div>';
            else if (data == 'pending-onboarding')
                return '<div style="text-align:center"><i class="fa fa-flag yellow"></i></div>';
            else if (data == 'pending-contract')
                return '<div style="text-align:center"><i class="fa fa-flag-o"></i></div>';
            else
                return '<div style="text-align:center"><i class="fa fa-flag black"></i></div>';
        },

        belt = function (data) {
            if (!data) return '';
            data = data.toLowerCase();
            if (data.indexOf('yellow') >= 0)
                return '<div style="text-align:center"><i class="fa fa-bookmark yellow"></i></div>';
            else if (data.indexOf('black') >= 0)
                return '<div style="text-align:center"><i class="fa fa-bookmark black"></i></div>';
            else
                return '<div style="text-align:center"><i class="fa fa-bookmark-o"></i></div>';
        },

        textSearch = function () {
            return {
                extra: true,
                operators: {
                    string: {
                        contains: "Contains", // this is the default
                        doesNotContain: "Does not contain",
                        eq: "Is equal to",
                        neq: "Is not equal to",
                        startswith: "Starts with",
                        endswith: "Ends with"
                    }
                }
            }
        },

        numberSearch = function () {
            return {
                extra: true,
                operators: {
                    number: {
                        gt: "Greater Than",
                        lt: "Less Than",
                        eq: "Equal to",
                        neq: "Not equal to",
                    }
                }
            }
        },

        dateSearch = function () {
            return {
                extra: true,
                operators: {
                    date: {
                        gt: "After",
                        lt: "Before",
                        eq: "Equal to",
                        neq: "Not equal to",
                    }
                }
            }
        },

        money = function (n, showZero) {
            if (n != undefined) {
                if (n != 0 || showZero)
                    return kendo.toString(n, 'c');
                else
                    return '';
            }
            else
                return '';
        },

        percentage = function (data) {
            if (data)
                return data.toFixed(2) * 100 + '%';
            else
                return '';
        },

        decimal = function (data, digits) {
            if (data)
                return data.toFixed(digits);
            else
                return '';
        },

        propertyColorLegend = function () {
            return '# if (data.Empty == 1) { #' +
                       '<span style="color:gray;text-decoration:line-through;">#: data.PropertyCodeAndAddress #</span>' +
                   '# } else if (data.Empty == 2) { #' +
                       '<span style="color:red;">#: data.PropertyCodeAndAddress #</span>' +
                   '# } else if (data.AllApproved == 1) { #' +
                       '<span style="color:blue;">#: data.PropertyCodeAndAddress #</span>' +
                   '# } else if (data.AllReviewed == 1) { #' +
                       '<span style="color:green;">#: data.PropertyCodeAndAddress #</span>' +
                   '# } else { #' +
                       '<span>#: data.PropertyCodeAndAddress #</span>' +
                   '# } #';
        },

        googleMap = function (address, city, state, zipcode) {
            if (address != null && address != undefined) {
                return kendo.format(googleMapTemplate,
                                    encodeURIComponent(address).replace(/ /g, '+') + ',',
                                    city ? city.replace(/ /g, '+') + ',' : ',',
                                    state ? state.replace(/ /g, '+') + ',' : ',',
                                    zipcode ? zipcode : '',
                                    address);
            }
            else
                return '';
        }

    return {
        nullable: nullable,
        dateUS: dateUS,
        center: center,
        right: right,
        spacing: spacing,
        makelink: makelink,
        link: link,
        linkOrText: linkOrText,
        linkDate: linkDate,
        boolean: boolean,
        highlightDate: highlightDate,
        active: active,
        belt: belt,
        textSearch: textSearch,
        numberSearch: numberSearch,
        dateSearch: dateSearch,
        money: money,
        percentage: percentage,
        decimal: decimal,
        propertyColorLegend: propertyColorLegend,
        googleMap: googleMap
    }
}();

DojoWeb.GridHelper = function () {
    var
	    // data.set will actually refresh the entire grid and send a databound event in some cases. This is very slow and unnecessary. 
        // It will also collapse any expanded detail templates which is not ideal.
        // I would recommend you to use this function that I wrote to update a single row in a kendo grid.

        // Updates a single row in a kendo grid without firing a databound event.
        // This is needed since otherwise the entire grid will be redrawn.

        selectRow = function ($gridId, rowId, needScroll) { // $gridId is jquery object of the grid. i.e. $('#your-grid-id'); rowId is the key of the row
            try {
                if ($gridId.data('kendoGrid').selectable()) {
                    var ds = $gridId.data('kendoGrid').dataSource;
                    var model = ds.get(rowId); // get the model
                    if (model != undefined) {
                        var index = ds.indexOf(model); // get the index of the item into the DataSource
                        //ds.page(index / ds.pageSize() + 1);  // page to the item  
                        var row = $gridId.find("tbody > tr[data-uid=" + model.uid + "]");
                        $gridId.data('kendoGrid').select(row);
                        DojoWeb.Helpers.scrollToTarget('edit-id-' + rowId, 1000);
                    }
                }
            }
            catch (e) {
                // let it fall through, does nothing
            }
        },

        udpateRowSample = function ($gridId, newData) {
            var dataGrid = $gridId.data('kendoGrid'); // Get a reference to the grid data
            if (dataGrid.selectable()) {
                var selectedRow = dataGrid.select(); // Access the row that is selected
                var rowData = dataGrid.dataItem(selectedRow); // and now the data
                // set data here
                //rowData.set(field-name, newData.field-name);
                redrawRow(dataGrid, selectedRow); // Redraw only the single row in question which needs updating
                //myDataBoundEvent.apply(grid); if you want to call your own databound event for post processing  
            }
        },

        redrawRow = function (grid, row) {
            var dataItem = grid.dataItem(row);
            var rowChildren = $(row).children('td[role="gridcell"]');
            for (var i = 0; i < grid.columns.length; i++) {
                var column = grid.columns[i];
                var template = column.template;
                var cell = rowChildren.eq(i);

                if (template !== undefined) {
                    var kendoTemplate = kendo.template(template);
                    cell.html(kendoTemplate(dataItem)); // Render using template
                } else {
                    var fieldValue = dataItem[column.field];

                    var format = column.format;
                    var values = column.values;

                    if (values !== undefined && values != null) {
                        // use the text value mappings (for enums)
                        for (var j = 0; j < values.length; j++) {
                            var value = values[j];
                            if (value.value == fieldValue) {
                                cell.html(value.text);
                                break;
                            }
                        }
                    } else if (format !== undefined) {
                        cell.html(kendo.format(format, fieldValue)); // use the format
                    } else {
                        cell.html(fieldValue); // Just dump the plain old value
                    }
                }
            }
        },
        
        controlScroll = function (element) {
            var activeElement;

            $(document).bind('mousewheel DOMMouseScroll', function(e) {
                var scrollTo = null;

                if (!$(activeElement).closest(".k-popup").length) {
                    return;
                }

                if (e.type == 'mousewheel') {
                    scrollTo = (e.originalEvent.wheelDelta * -1);
                }
                else if (e.type == 'DOMMouseScroll') {
                    scrollTo = 40 * e.originalEvent.detail;
                }

                if (scrollTo) {
                    e.preventDefault();
                    element.scrollTop(scrollTo + element.scrollTop());
                }
            });

            $(document).on('mouseover', function(e) {
                activeElement = e.target;
            });
        }

    return {
        selectRow: selectRow,
        redrawRow: redrawRow,
        controlScroll: controlScroll
    }
}();

DojoWeb.Validation = function () {
    var validateDropdown = function (id, msg) {
            if (showDropdownError(id, msg) == true) return 1;
            return 0;
        },

        validateSearchableDropdown = function (id, msg) {
            var value = $(id).val();
            var hasError = (value == '') ? true : false;
            if (showStandardParentError(hasError, id, msg) == true) return 1;
            return 0;
        },

        validateDropdownMultiSelect = function (id, msg) {
            if (showDropdownMultiSelectError(id, msg) == true) return 1;
            return 0;
        },

        validateDate = function (id, msg) {
            if (showDateError(id, msg) == true) return 1;
            return 0;
        },

        validateDateRange = function (startDateId, endDateId, msg) {
            var startDate = $(startDateId).data('kendoDatePicker').value();
            var endDate = $(endDateId).data('kendoDatePicker').value();
            var hasError = endDate < startDate;
            showDateRangeError(hasError, startDateId, msg);
            return hasError ? 1 : 0;
        },

        validateInputGroup = function (id, msg) {
            var value = $(id).val();
            var hasError = (value == '') ? true : false;
            if (showStandardParentError(hasError, id, msg) == true) return 1;
            return 0;
        },

        validateTextBox = function (id, msg) {
            if (showTexBoxError(id, msg) == true) return 1;
            return 0;
        },

        validateTextEditor = function (containerId, editorId, msg) {
            return DojoWeb.CKEditor.validate(containerId, editorId, msg) == true ? 0 : 1;
        },

        validateListView = function (id, msg) {
            if (showListViewError(id, msg) == true) return 1;
            return 0;
        },

        validateUrl = function (url) {
            var urlregex = new RegExp("^(http|https|ftp)\://([a-zA-Z0-9\.\-]+(\:[a-zA-Z0-9\.&amp;%\$\-]+)*@)*((25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])|([a-zA-Z0-9\-]+\.)*[a-zA-Z0-9\-]+\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{2}))(\:[0-9]+)*(/($|[a-zA-Z0-9\.\,\?\'\\\+&amp;%\$#\=~_\-]+))*$");
            return urlregex.test(url);
        },

        validateNaturalNumber = function (id, message) {
            if ($(id).val() == '') return 0;
            var input = $(id).val().trim();
            var n = parseInt(input, 10);
            var result = (n >= 0 && n.toString() === input) ? 0 : 1;
            if (result != 0) DojoWeb.Validation.showStandardError(true, id, message);
            return result;
        },

        validatePositiveNumber = function (id, message) {
            if ($(id).val() == '') return 0;
            var input = $(id).val().trim();
            var n = parseInt(input, 10);
            var result = (n > 0 && n.toString() === input) ? 0 : 1;
            if (result != 0) DojoWeb.Validation.showStandardError(true, id, message);
            return result;
        },

        validateDecimal = function (id, message) {
            if ($(id).val() == '') return 0;
            var input = $(id).val().trim();
            if (input == '') { // all spaces
                DojoWeb.Validation.showStandardParentError(true, id, message);
                return 1;
            }
            else {
                var invalid = isNaN(input);
                if (invalid) DojoWeb.Validation.showStandardParentError(true, id, message);
                return invalid ? 1 : 0;
            }
        },

        validatePositiveDecimal = function (id, message) {
            if (validateDecimal(id, message) == 0) {
                if (parseFloat($(id).val().trim()) > 0) {
                    return 0;
                }
                else {
                    DojoWeb.Validation.showStandardParentError(true, id, message);
                    return 1;
                }
            }
            else
                return 1;
        },

        clearMessage = function (id) {
            $('#' + id).removeClass('input-validation-error').addClass('valid');
            var $span = $('#' + id).siblings('span');
            if ($span != undefined) {
                $span.addClass('field-validation-valid').removeClass('field-validation-error');
                $span.html('');
            }
        },

        clearParentMessage = function (id) {
            var $span = $('#' + id).parent().siblings('span');
            if ($span != undefined) {
                $span.addClass('field-validation-valid').removeClass('field-validation-error');
                $span.html('');
            }
        },

        clearDateMessage = function (id) {
            var $span = $('#' + id).parent().parent().siblings('span');
            if ($span != undefined) {
                $span.addClass('field-validation-valid').removeClass('field-validation-error');
                $span.html('');
            }
        },

        clearTextEditorMessage = function (containerId, editorId) {
            DojoWeb.CKEditor.clearMessage(containerId, editorId);
        },

        showDateError = function (id, msg) {
            // arrange error to sync up with MVC error style
            var datepicker = $(id).data("kendoDatePicker");
            var dateEntered = datepicker.value();
            if (dateEntered != '') {
                if (dateEntered == null) {
                    dateEntered = $(id).val();
                    if (!kendo.parseDate(dateEntered)) {
                        if (msg == undefined) msg = "Valid DATE is required";
                        var $span = $(id).parent().parent().siblings('span');
                        if ($span != undefined) {
                            $span.addClass('field-validation-error').removeClass('field-validation-valid');
                            $span.html('<span id="' + id.substring(1) + '-error">' + msg + '</span>');
                            return true;
                        }
                    }
                }
                var $span = $(id).parent().parent().siblings('span');
                if ($span != undefined) {
                    $span.addClass('field-validation-valid').removeClass('field-validation-error');
                    $span.html('');
                    return false;
                }
            }
            else {
                if (msg == undefined) msg = "DATE is required.";
                var $span = $(id).parent().parent().siblings('span');
                if ($span != undefined) {
                    $span.addClass('field-validation-error').removeClass('field-validation-valid');
                    $span.html('<span id="' + id.substring(1) + '-error">' + msg + '</span>');
                    return true;
                }
            }
            return false;
        },

        showDateRangeError = function (hasError, id, msg) {
            if (hasError) {
                if (msg == undefined) msg = "Date Range is invalid.";
                var $span = $(id).parent().parent().siblings('span');
                if ($span != undefined) {
                    $span.addClass('field-validation-error').removeClass('field-validation-valid');
                    $span.html('<span id="' + id.substring(1) + '-error">' + msg + '</span>');
                    return true;
                }
            }
            else {
                var $span = $(id).parent().parent().siblings('span');
                if ($span != undefined) {
                    $span.addClass('field-validation-valid').removeClass('field-validation-error');
                    $span.html('');
                    return false;
                }
            }
            return false;
        },

        showDropdownError = function (id, msg) {
            var value = $(id + ' option:selected').val();
            var hasError = (value == '') ? true : false;
            return showStandardError(hasError, id, msg);
        },

        showDropdownMultiSelectError = function (id, msg) {
            var count = $("select[id='" + id.substring(1) + "'] option:selected").length;
            var hasError = count <= 0 ? true : false;
            return showStandardError(hasError, id, msg);
        },

        showTexBoxError = function (id, msg) {
            var hasError = $(id).val() != '' ? false : true;
            return showStandardError(hasError, id, msg);
        },

        showComboboxError = function (id, msg) {
            var comboBoxText = $(id).data('kendoComboBox').value();
            var hasError = comboBoxText != '' ? false : true;
            return showStandardParentError(hasError, id, msg);
        },

        showListViewError = function (id, msg) {
            // arrange error to sync up with MVC error style
            // Kendo list view uses <ul> tag; so we need to use Kendo API call to get data
            var listView = $(id).kendoListView().data("kendoListView");
            var hasError = listView.dataItems() != '' ? false : true;
            return showStandardError(hasError, id, msg);
        },

        showStandardParentError = function (hasError, id, msg) {
            if (!hasError) {
                var $span = $(id).parent().siblings('span');
                if ($span != undefined) {
                    $span.addClass('field-validation-valid').removeClass('field-validation-error');
                    $span.html('');
                    return false;
            }
            }
            else {
                if (msg == undefined) msg = "Field data is required.";
                var $span = $(id).parent().siblings('span'); // hack: navigate to kenod editor tag hierarchy
                if ($span != undefined) {
                    $span.addClass('field-validation-error').removeClass('field-validation-valid');
                    $span.html('<span id="' + id.substring(1) + '-error">' +msg + '</span>');
                    return true;
                }
            }
            return false;
        },

        showStandardError = function (hasError, id, msg) {
            // arrange error to sync up with MVC error style
            if (!hasError) {
                var $span = $(id).siblings('span');
                if ($span != undefined) {
                    $span.addClass('field-validation-valid').removeClass('field-validation-error');
                    $span.html('');
                    return false;
                }
            }
            else {
                if (msg == undefined) msg = "Field data is required.";
                var $span = $(id).siblings('span'); // hack: navigate to kenod editor tag hierarchy
                if ($span != undefined) {
                    $span.addClass('field-validation-error').removeClass('field-validation-valid');
                    $span.html('<span id="' + id.substring(1) + '-error">' + msg + '</span>');
                    return true;
                }
            }
            return false;
        }

    return {
        validateDropdown: validateDropdown,
        validateSearchableDropdown: validateSearchableDropdown,
        validateDropdownMultiSelect: validateDropdownMultiSelect,
        validateDate: validateDate,
        validateDateRange: validateDateRange,
        validateInputGroup: validateInputGroup,
        validateTextBox: validateTextBox,
        validateTextEditor: validateTextEditor,
        validateListView: validateListView,
        showComboboxError: showComboboxError,
        validateUrl: validateUrl,
        validateNaturalNumber: validateNaturalNumber,
        validatePositiveNumber: validatePositiveNumber,
        validateDecimal: validateDecimal,
        validatePositiveDecimal: validatePositiveDecimal,
        clearMessage: clearMessage,
        clearParentMessage: clearParentMessage,
        clearDateMessage: clearDateMessage,
        clearTextEditorMessage: clearTextEditorMessage,
        showStandardError: showStandardError,
        showStandardParentError: showStandardParentError
    }
}();

DojoWeb.UUID = function () {

    var EMPTY = '00000000-0000-0000-0000-000000000000',

    _padLeft = function (paddingString, width, replacementChar) {
        return paddingString.length >= width ? paddingString : _padLeft(replacementChar + paddingString, width, replacementChar || ' ');
    },

    _s4 = function (number) {
        var hexadecimalResult = number.toString(16);
        return _padLeft(hexadecimalResult, 4, '0');
    },

    _cryptoGuid = function () {
        var buffer = new window.Uint16Array(8);
        window.crypto.getRandomValues(buffer);
        return [_s4(buffer[0]) + _s4(buffer[1]), _s4(buffer[2]), _s4(buffer[3]), _s4(buffer[4]), _s4(buffer[5]) + _s4(buffer[6]) + _s4(buffer[7])].join('-');
    },

    _guid = function () {
        var currentDateMilliseconds = new Date().getTime();
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (currentChar) {
            var randomChar = (currentDateMilliseconds + Math.random() * 16) % 16 | 0;
            currentDateMilliseconds = Math.floor(currentDateMilliseconds / 16);
            return (currentChar === 'x' ? randomChar : (randomChar & 0x7 | 0x8)).toString(16);
        });
    },

    create = function () {
        var hasCrypto = false; //typeof (window.crypto) != 'undefined',
        var hasRandomValues = false; //typeof (window.crypto.getRandomValues) != 'undefined';
        return (hasCrypto && hasRandomValues) ? _cryptoGuid() : _guid();
    }

    return {
        newGuid: create,
        empty: EMPTY
    }
}();

DojoWeb.NewFeature = function () {
    var _cookieName = 'DojoNewFeatureViewed',
        _viewed = '',
        _restoreOverflow = false,

        init = function () {
            $('#newFeature-dialog').hide();
            announcement(function (result) {
                if (result && result != '' && result.NewFeatureId > 0) {
                    var deployDate = DojoWeb.Date.parseJsonDate(result.DeployDate);
                    var expiredDate = DojoWeb.Date.parseJsonDate(result.ExpiredDate);
                    _viewed = kendo.toString(deployDate, 'yyyy-MM-dd');
                    var today = Date.today();
                    if (today > expiredDate) {
                        $('#NewFeatureAnnouncement').hide(); // show new feature announcement until its expiration date
                        var cookie = DojoWeb.Cookie.exist({ name: _cookieName });
                        if (cookie) DojoWeb.Cookie.remove(_cookieName);
                    }
                    else if (result != '') { // check cookie to see if it has been viewed
                        var cookie = DojoWeb.Cookie.get(_cookieName);
                        if (!cookie || cookie != _viewed) {
                            currentFeature(true);
                        }
                    }
                }
            });
        },

        currentFeature = function (noClose) {
            announcement(function (result) {
                if (!result || result == '') return;
                // enable scrolling if disabled in case the popup window is taller than the screen height
                if ($('body').css('overflow') == 'hidden') {
                    $('body').css('overflow', 'visible');
                    _restoreOverflow = true;
                }
                DojoWeb.Confirmation.init({
                    id: 'newFeature-dialog',
                    caption: 'New Feature Announcement',
                    action: (noClose == true ? [] : ['close']),
                    width: '600px',
                    ok: function () {
                        // add a cookie to acknowledge the announcement
                        var cookie = DojoWeb.Cookie.exist({ name: _cookieName });
                        if (cookie) DojoWeb.Cookie.remove(_cookieName);
                        DojoWeb.Cookie.set({
                            name: _cookieName,
                            data: _viewed,
                        });
                    },
                    close: function () {
                        // restore scrolling setting if temporarily altered above
                        if (_restoreOverflow) {
                            $(window).scrollTop(0);
                            $('body').css('overflow', 'hidden');
                            _restoreOverflow = false;
                        }
                    }
                });
                var content = result.Description;
                if (content && content != '') DojoWeb.Confirmation.show(content);
            });
        },

        recentFeature = function (id, releaseDate) {
            var url = '/NewFeature/Announcement?deployDate=' + releaseDate;
            var options = {
                type: 'GET',
                url: url
            };

            $.ajax(options).done(function (result) {
                var content = result.Description;
                $('#newFeature-dialog .dialog-instruction').html(content);
                $('div.recent-features ul li').removeClass('selected-feature');
                $('#' + id).addClass('selected-feature');
            });
        },

        announcement = function (action) {
            var url = '/NewFeature/Announcement';
            var options = {
                type: 'GET',
                url: url
            };

            $.ajax(options).done(function (result) {
                if (action) action(result);
            });
        }

    return {
        init: init,
        currentFeature: currentFeature,
        recentFeature: recentFeature
    }
}();

DojoWeb.DateRange = function () {
    var _beginDateId = undefined,
        _endDateId = undefined,

        init = function (beginDateId, endDateId, beginDate, endDate) {
            _beginDateId = '#' + beginDateId;
            _endDateId = '#' + endDateId;
            var handle = {
                beginDateId: _beginDateId,
                endDateId: _endDateId
            }
            initEvents(handle, beginDate, endDate);

            return handle; // return the date range handle so this module can handle multiple instances
        },

        // this method requires html is tagged properly for kendo to validate the range of datepickers.
        // see example in _InquiryActionBarPartial.cshtml
        initValidator = function (validatorId) {
            $('#' + validatorId).kendoValidator({
                validateOnBlur: false,
                rules: {
                    greaterdate: function (input) {
                        if (input.is('[data-greaterdate-msg]') && input.val() != "") {
                            var endDateVal = kendo.parseDate(input.val()),
                                beginDateVal = kendo.parseDate($("[name='" + input.data("greaterdateField") + "']").val());
                            return beginDateVal <= endDateVal;
                        }
                        return true;
                    }
                }
            });
        },

        setRange = function (handle, beginDate, endDate) {
            if (beginDate) {
                $(handle.beginDateId).data('kendoDatePicker').value(beginDate);
                $(handle.beginDateId).val(kendo.toString(beginDate, 'MM/dd/yyyy'));
                var localBeginDate = beginDate ? new Date(beginDate.valueOf()) : null;
                setMinDate(handle.endDateId, localBeginDate);
            }
            if (endDate) {
                $(handle.endDateId).data('kendoDatePicker').value(endDate);
                $(handle.endDateId).val(kendo.toString(endDate, 'MM/dd/yyyy'));
                var localEndDate = endDate ? new Date(endDate.valueOf()) : null;
                setMaxDate(handle.beginDateId, localEndDate);
            }
        },

        getRange = function (handle) {
            return {
                beginDate: Date.parse($(handle.beginDateId).val()), //$(handlebeginDateId).data('kendoDatePicker').value(),
                endDate: Date.parse($(handle.endDateId).val()), //$(handle.endDateId).data('kendoDatePicker').value(),
            }
        },

        initEvents = function (handle, beginDate, endDate) {
            var localBeginDate = beginDate ? new Date(beginDate.valueOf()) : undefined;
            var localEndDate = endDate ? new Date(endDate.valueOf()) : undefined;
            $(handle.beginDateId).kendoDatePicker({
                value: beginDate,
                max: localEndDate ? localEndDate.addDays(-1) : undefined,
                change: function () {
                    var beginDate = this.value();
                    var localBeginDate = beginDate ? new Date(this.value().valueOf()) : null;
                    setMinDate(handle.endDateId, localBeginDate);
                }
            });

            $(handle.endDateId).kendoDatePicker({
                value: endDate,
                min: localBeginDate ? localBeginDate.addDays(1) : undefined,
                change: function () {
                    var endDate = this.value();
                    var localEndDate = endDate ? new Date(this.value().valueOf()) : null;
                    setMaxDate(handle.beginDateId, localEndDate);
                }
            });

            // disable direct date text input
            $(handle.beginDateId).prop('readonly', true);
            $(handle.endDateId).prop('readonly', true);

            setRange(handle, beginDate, endDate);
        },

        setMinDate = function (endDateId, beginDate) {
            if (beginDate != null) {
                var endPicker = $(endDateId).data('kendoDatePicker');
                endPicker.setOptions({ min: beginDate.addDays(1) });
            }
        },

        setMaxDate = function (beginDateId, endDate) {
            if (endDate != null) {
                var beginPicker = $(beginDateId).data('kendoDatePicker');
                beginPicker.setOptions({ max: endDate.addDays(-1) });
            }
        }
    
    return {
        init: init,
        initValidator: initValidator,
        setRange: setRange,
        getRange: getRange
    }
}();

DojoWeb.Notification = function () {
    var _notification = undefined,

        init = function (id, timeToShow) {
            if (timeToShow == undefined) timeToShow = 8000;
            _notification = $('#' + id).kendoNotification({
                                position: { top: null, left: null, bottom: 2, right: 2 },
                                autoHideAfter: timeToShow,
                                height: 40,
                                hideOnClick: true,
                                stacking: 'up',
            }).data('kendoNotification');
            $('.k-notification-wrap').css('font-size', '14px');
        },

        show = function (msg, status) { // status: 'error', 'info', 'warn', 'success'
            if (_notification) {
                _notification.show(msg, status);
            }
        },

        hide = function () {
            if (_notification) _notification.hide();
        }
    
    return {
        init: init,
        show: show,
        hide: hide
    }
}();

DojoWeb.Favorite = function () {
    var
        setStartPage = function () {
            // get the relative (local) url
            var url = DojoWeb.Helpers.getUrlPart('href'); // current full page url
            var host = DojoWeb.Helpers.getUrlPart('host');
            var localUrl = url.substring(url.indexOf(host) + host.length);
            $.ajax({
                type: 'POST',
                url: '/Account/SetStartPage?page=' + encodeURIComponent(localUrl),
                success: function (result) {
                    if (result == 'success') {
                        DojoWeb.Notification.show('This page is now your favorite start page.');
                        $('#UserStartPage i').removeClass('fa-heart-o');
                        $('#UserStartPage i').addClass('fa-heart');
                    }
                    else {
                        DojoWeb.Notification.show('There was an error saving your start page.');
                    }
                },
                error: function (jqXHR, status, errorThrown) {
                    if (status == 'error') {
                        alert('There was an error saving your start page.');
                    }
                }
            });
        }

    return {
        setStartPage: setStartPage
    }
}();
