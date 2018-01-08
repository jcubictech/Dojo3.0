"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.ExcelExport = function () {
    var _exportIndicator = false,
        $gridData = undefined,

        download = function (gridId, columnsToShow, columnsToHide) {
            customDownload(gridId, columnsToShow, columnsToHide);
        },

        customDownload = function (gridId, columnsToShow, columnsToHide) {
            if (columnsToShow != undefined) {
                $.each(columnsToShow, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.showColumn(col);
                });
            }

            if (columnsToHide != undefined) {
                $.each(columnsToHide, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.hideColumn(col);
                });
            }

            $gridData = $(gridId).data('kendoGrid');
            $gridData.saveAsExcel();
            //var dataURI = "data:text/plain;base64," + kendo.util.encodeBase64($gridData);
            //kendo.saveAs({
            //    dataURI: dataURI,
            //    fileName: filename,
            //    proxyURL: "/proxy/save",
            //    forceProxy: true
            //});

            if (columnsToShow != undefined) {
                $.each(columnsToShow, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.hideColumn(col);
                });
            }

            if (columnsToHide != undefined) {
                $.each(columnsToHide, function (name) {
                    var col = _.findIndex($gridData.options.columns, function (item) { return item.field == name })
                    $gridData.showColumn(col);
                });
            }
        }

    return {
        download: download,
        customDownload: customDownload
    }
}();
