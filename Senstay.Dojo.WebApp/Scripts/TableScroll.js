
fnAdjustTable = function () {

    //function to support scrolling of title and first column
    fnScroll = function () {
        $(thead).scrollLeft($('.grid-table').scrollLeft());
        //$('#firstcol').scrollTop($('#table_div').scrollTop());
    };
}