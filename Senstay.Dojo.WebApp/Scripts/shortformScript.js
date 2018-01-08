
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



//$(function () { // will trigger when the document is ready
//    $('.datepicker').datepicker(); //Initialise any date pickers
//});

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


        $('#selectfocus').focusin(function () { $('.select-autocomplete').select2("open") });

        /*
        var inqDate = Date.now();

        $('#Check_inDate').change(
            function () {
                inDate = $('#Check_inDate').val();
                
                $('#Check_InDay').val(getDayofWeek(inDate));
                outDate = $('#Check_outDate').val();
                //addandchange(inDate, inqDate, "daysOut", "daysOutin");
                $('#DaysOut').val(count_diff(inDate, inqDate));
                //addandchange(outDate, inDate, "lengthofStay", "lengthofStayin");
                los = count_diff(outDate, inDate);
                $('#LengthofStay').val(los);
                //(outDate, inDate, "daystillcheckin", "daystillcheckin_input");
                $('#Daystillcheckin').val(count_diff(Date.now(), inDate));
                tp = $('#TotalPayout').val();
                cl = $('#Cleaning_Fee').val();
                $('#NightlyRate').val(NightlyRate(tp, cl, los));
            });
        $('#Check_outDate').change(
            function () {
                outDate = $('#Check_outDate').val();
                $('#Check_OutDay').val(getDayofWeek(outDate));
                inDate = $('#Check_inDate').val();
                //addandchange(outDate, inDate, "lengthofStay", "lengthofStayin");
                los = count_diff(outDate, inDate);
                $('#LengthofStay').val(los);
                tp = $('#TotalPayout').val();
                cl = $('#Cleaning_Fee').val();
                $('#NightlyRate').val(NightlyRate(tp, cl, los));
            });

        $('#TotalPayout').change(
            function () {
                tp = $('#TotalPayout').val();
                cl = $('#Cleaning_Fee').val();
                los = $('#LengthofStay').text();
                $('#NightlyRate').val(NightlyRate(tp, cl, los));

            });

        $('#OwnerApprovalNeeded').change(
           function () {
               a = $('#OwnerApprovalNeeded').val();
               if (a[0] == 'Y') { $('#ApprovedbyOwner').val("TRUE") } else { $('#ApprovedbyOwner').val("N/A") }
           });

        $('#Cleaning_Fee').change(
        function () {
            tp = $('#TotalPayout').val();
            cl = $('#Cleaning_Fee').val();
            los = $('#LengthofStay').text();
            $('#NightlyRate').val(NightlyRate(tp, cl, los));
        });
        */

    })
        );
