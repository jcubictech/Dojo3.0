
var days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

function count_diff(d1, d2) {
    if (d2 != "" & d1 != "") {
        var date_1 = new Date(d1);
        var date_2 = new Date(d2);
        var daterez = Math.floor((date_1 - date_2) / 86400000);
        return daterez;
    } else { return "" }
};

function getDayofWeek(d1) {
    if (d1 != "") {
        var date_1 = new Date(d1);
        var dayrez = days[date_1.getDay()];
        return dayrez;
    } else {
        return ""
    }
};

function NightlyRate(TP, CF, LoS) {
    var tp = parseFloat(TP);
    var cf = parseFloat(CF);
    var los = parseFloat(LoS);
    if (isFinite(tp) && isFinite(cf) && isFinite(los)) {
        return ((Math.round((((tp / 0.97) - cf) / los) * 100)) / 100);
    }
    else { return "" };

}

function addandchange(d1, d2, changeid, changeinputid) {
    $('#' + changeid + '').replaceWith('<p id="#' + changeid + '" class="form-control-static">' + count_diff(Date.now(), inDate) + '</p>')
    $('#' + changeinputid + '').val(count_diff(d1, d2));
}



$(function () { // will trigger when the document is ready
    $('.datepicker').datepicker(); //Initialise any date pickers
});

$(document).ready($(
    function () {
        $(".ablList").autocomplete({
            source: ActionPar,
            minLength: 2,
            select: function (event, ui) {

            $('#Account').val(ui.item.Account);
            $('#Bedrooms').val(ui.item.Bedrooms);
            $('#AirBnBURL').val(ui.item.AirBnBURL);
            $('#PropertyCode').val(ui.item.PropertyCode);
            $('#NeedsOwnerApproval').val(ui.item.NeedsOwnerApproval);
            $('#AirBnBHomeName').val(ui.item.AirBnBHomeName);
            $('#Cleaning_Fee').val(ui.item.CleaningFee);
            $('#BookingGuidelines').val(ui.item.BookingGuidelines);
        }
    });

$('#dataset').change(
    function () {
        inDate = $('#checkin').val();
        inqDate = $('#dataset').val();
        //
        $('#daysOut').replaceWith('<p id="daysOut" class="form-control-static">' + count_diff(inDate, inqDate) + '</p>')
        $('#daysOutin').val(count_diff(inDate, inqDate));
        //
        $('#inquiryAge').replaceWith('<p id="inquiryAge" class="form-control-static">' + count_diff(Date.now(), inqDate) + '</p>')
        $('#inquiryAgein').val(count_diff(Date.now(), inqDate));
    });

$('#checkin').change(
    function () {
        inDate = $('#checkin').val();
        $('#checkinday').replaceWith('<p id="checkinday" class="form-control-static">' + getDayofWeek(inDate) + '</p>')
        $('#checkindayin').val(getDayofWeek(inDate));
        inqDate = $('#dataset').val();
        outDate = $('#checkout').val();
        //addandchange(inDate, inqDate, "daysOut", "daysOutin");
        $('#daysOut').replaceWith('<p id="daysOut" class="form-control-static">' + count_diff(inDate, inqDate) + '</p>')
        $('#daysOutin').val(count_diff(inDate, inqDate));
        //addandchange(outDate, inDate, "lengthofStay", "lengthofStayin");
        los = count_diff(outDate, inDate);
        $('#lengthofStay').replaceWith('<p id="lengthofStay" class="form-control-static">' + los + '</p>')
        $('#lengthofStayin').val(los);
        //(outDate, inDate, "daystillcheckin", "daystillcheckin_input");
        $('#daystillcheckin').replaceWith('<p id="daystillcheckin" class="form-control-static">' + count_diff(Date.now(), inDate) + '</p>')
        $('#daystillcheckin_input').val(count_diff(Date.now(), inDate));
        tp = $('#totalPayout').val();
        cl = $('#cleaningFee').val();
        $('#nightlyRate').replaceWith('<p id="nightlyRate" class="form-control-static">' + NightlyRate(tp, cl, los) + '</p>')
        $('#nightlyRatein').val(NightlyRate(tp, cl, los));
    });
$('#checkout').change(
    function () {
        outDate = $('#checkout').val();
        $('#checkoutday').replaceWith('<p id="checkoutday" class="form-control-static">' + getDayofWeek(outDate) + '</p>')
        $('#checkoutdayin').val(getDayofWeek(outDate));
        inDate = $('#checkin').val();
        //addandchange(outDate, inDate, "lengthofStay", "lengthofStayin");
        los = count_diff(outDate, inDate);
        $('#lengthofStay').replaceWith('<p id="lengthofStay" class="form-control-static">' + count_diff(outDate, inDate) + '</p>')
        $('#lengthofStayin').val(los);
        tp = $('#totalPayout').val();
        cl = $('#cleaningFee').val();
        $('#nightlyRate').replaceWith('<p id="nightlyRate" class="form-control-static">' + NightlyRate(tp, cl, los) + '</p>')
        $('#nightlyRatein').val(NightlyRate(tp, cl, los));
    });

$('#totalPayout').change(
    function () {
        tp = $('#totalPayout').val();
        cl = $('#cleaningFee').val();
        los = $('#lengthofStay').text();
        $('#nightlyRate').replaceWith('<p id="nightlyRate" class="form-control-static">' + NightlyRate(tp, cl, los) + '</p>')
        $('#nightlyRatein').val(NightlyRate(tp, cl, los));

    });

$('#OwnerApprovalNeeded').change(
   function () {
       a = $('#OwnerApprovalNeeded').val();
       if (a[0] == 'Y') { $('#ApprovedbyOwner').val("TRUE") } else { $('#ApprovedbyOwner').val("N/A") }
   });

$('#cleaningFee').change(
function () {
    tp = $('#totalPayout').val();
    cl = $('#cleaningFee').val();
    los = $('#lengthofStay').text();
    $('#nightlyRate').replaceWith('<p id="nightlyRate" class="form-control-static">' + NightlyRate(tp, cl, los) + '</p>')
    $('#nightlyRatein').val(NightlyRate(tp, cl, los));
});


})
        );