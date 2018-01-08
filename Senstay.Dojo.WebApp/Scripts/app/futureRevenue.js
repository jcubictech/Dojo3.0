"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.FutureRevenue = function () {
    var 
        init = function (gridId) {
            DojoWeb.FutureRevenueView.init(gridId);
            DojoWeb.FutureRevenueView.render();
        },

        show = function () {
            DojoWeb.FutureRevenueView.render();
        }

    return {
        init: init,
        show: show
    }
}();

DojoWeb.FutureRevenueView = function () {
    var _gridId = undefined,
        _dataGrid = undefined,
        _tierItem = {
            t1: 'Tier 1',
            t2: 'Tier 2',
            t3: 'Tier 3'
        },
        _trainStatus = {
            ontime: 0,
            late: 1,
            na: 2
        },
        _pillarMap = undefined,
        _dateDelimiter = '-',

        init = function(id) {
            _gridId = '#' + id;
        },

        render = function () {
            getData();
        },

        getData = function () {
            DojoWeb.Busy.show();

            $.get('/Report/RetrieveFutureRevenue',
                function (data) {
                    clear();
                    if (data == '') {
                        DojoWeb.Busy.hide();
                        alert('There is no Future Revenue data to display.', DojoWeb.Alert.alertTypes().warn);
                        return;
                    }

                    _pillarMap = _.uniq(_.map(data, function (row) {
                            return { pillar: row.Pillar, pillarId: row.ConversationId };
                    }), false, function (e) { return e.pillarId; });

                    _dataGrid = $(_gridId).kendoGrid(configureGrid()).data("kendoGrid"); // init the grid
                    _dataGrid.bind('dataBound', function (e) {
                        showDashboard(true);
                        DojoWeb.Busy.hide();
                    });
                    _dataGrid.setDataSource(configureDataSource(data));
                    renderDashboard(data);
                })
                .error(function (errData) {
                    clear();
                    DojoWeb.Busy.hide();
                    alert('There was an error retrieving Future Revenue data. Please try refreshing this page. If the issue persists please contact Dojo App administrator.', DojoWeb.Alert.alertTypes().error);
                });
        },

        clear = function () {
            showDashboard(false);
            $(_gridId).empty();
        },

        configureGrid = function () {
            return { // set up grid configuration property
                editable: false,
                resizable: false,
                filterable: true,
                scrollable: false,
                sortable: true,
                groupable: false,
                columns: [
                    { field: 'Account', title: 'Account', width: '100px', },
                    { field: 'StartDate', title: 'Start Date', width: '100px', template: '#= DojoWeb.Template.dateUS(data.StartDate) #', },
                    { field: 'MonthCount', title: 'Month Count', width: '150px', template: '#= DojoWeb.Template.center(data.MonthCount) #', },
                    { field: 'QuarterCount', title: 'Quarter Count', width: '150px', template: '#= DojoWeb.Template.center(data.QuarterCount) #', },
                    { field: 'SemiAnnualCount', title: 'Semi Annual Count', width: '150px', template: '#= DojoWeb.Template.center(data.SemiAnnualCount) #', },
                    { field: 'MonthRevenue', title: 'Month Revenue', width: '150px', template: '#= DojoWeb.Template.money(data.MonthRevenue) #', },
                    { field: 'QuarterRevenue', title: 'Quarter Revenue', width: '150px', template: '#= DojoWeb.Template.money(data.QuarterRevenue) #', },
                    { field: 'SemiAnnualRevenue', title: 'Semi Annual Revenue', width: '150px', template: '#= DojoWeb.Template.money(data.SemiAnnualRevenue) #', },
                ],
            };
        },

        configureDataSource = function (data, pillarMap) {
            colorCodeData(data);
            return new kendo.data.DataSource({ // define grid data source grouped by workload
                data: data,
                change: function (e) { // will catch chart filter event
                    // render the dashboard with the data displayed in current grid
                    renderDashboard($(_gridId).data().kendoGrid.dataSource.view());
                },
                schema: {
                    model: {
                        fields: {
                            Account: { type: 'string' },
                            StartDate: { type: 'datetime' },
                            MonthCount: { type: 'number' },
                            QuarterCount: { type: 'number' },
                            SemiAnnualCount: { type: 'number' },
                            MonthRevenue: { type: 'number' },
                            QuarterRevenue: { type: 'number' },
                            SemiAnnualRevenue: { type: 'number' },
                        }
                    }
                }
            });
        },

        renderAccounts = function (howMany) {
            if (howMany == undefined) {
                $('#top3Acounts').addClass('hide');
                $('#allAccounts').removeClass('hide');
            }
            else if (howMany > 0) {
                $('#top3Acounts').removeClass('hide');
                $('#allAccounts').addClass('hide');
            }
            else { // use whatever owner rendering div is active
                howMany = $('#allAccounts').hasClass('hide') ? 3 : undefined;
            }

            var chartData = $(_gridId).data().kendoGrid.dataSource.view();
            renderByAccountDashboard(chartData, howMany);
        },

        renderDashboard = function (data) {
            showDashboard(true);
            renderFutureRevenue(data);
        },

        renderFutureRevenue = function (data) {
            var chartDate = {
                monthCount: 0,
                quarterCount: 0,
                semiAnnualCount: 0,
                monthRevenue: 0,
                quarterRevenue: 0,
                semiAnnualRevenue: 0,
            };
            chartDate.monthCount = _.pluck(data, 'MonthCount');
            chartDate.quarterCount = _.pluck(data, 'QuarterCount');
            chartDate.semiAnnualCount = _.pluck(data, 'SemiAnnualCount');
            chartDate.monthRevenue = _.pluck(data, 'MonthRevenue');
            chartDate.quarterRevenue = _.pluck(data, 'QuarterRevenue');
            chartDate.semiAnnualRevenue = _.pluck(data, 'SemiAnnualRevenue');

            var accounts = _.pluck(data, 'Account');

            DojoWeb.StackChart.futureRevenueBar('futureRevenueChart', 'Future Revenue', chartDate, accounts, futureRevenueChartClick);
        },

        colorCodeData = function (data) {
            $.each(data, function (i, row) {
                if (row.FAQ == _trainStatus.ontime) // this comes from original data source
                    row.FAQ = DojoWeb.StackChart.trainColorCodes().ontime;
                else
                    row.FAQ = DojoWeb.StackChart.trainColorCodes().late;

                if (row.News == _trainStatus.ontime) // this comes from original data source
                    row.News = DojoWeb.StackChart.trainColorCodes().ontime;
                else if (row.News == _trainStatus.late) // this comes from original data source
                    row.News = DojoWeb.StackChart.trainColorCodes().late;
                else
                    row.News = DojoWeb.StackChart.trainColorCodes().na;

                if (row.Blog == _trainStatus.ontime) // this comes from original data source
                    row.Blog = DojoWeb.StackChart.trainColorCodes().ontime;
                else if (row.Blog == _trainStatus.late) // this comes from original data source
                    row.Blog = DojoWeb.StackChart.trainColorCodes().late;
                else
                    row.Blog = DojoWeb.StackChart.trainColorCodes().na;
            });
        },

        showDashboard = function (show) {
            if (show)
                $('#futureRevenueChart').removeClass('hide');
            else
                $('#futureRevenueChart').addClass('hide');
        },

        futureRevenueChartClick = function (e) {
            chartClick(e, 'Account');
        },

        chartClick = function (e, field, opr) {
            var category = e.category;
            if (opr == undefined) opr = 'eq';

            var ds = $(_gridId).data('kendoGrid').dataSource; // kendo data source
            var filter = ds.filter(); // current grid filters

            // create chart filter that kendo understands
            var chartFilter = { filters: [{ field: field, operator: opr, value: category }], logic: 'or' };

            // combine grid filters and chart filters
            if (filter) {
                if (filter.logic != 'and') { // if the filters are 'or' together, we 'and' it with chart filter
                    var combinedFilters = { filters: [], logic: 'and' };
                    combinedFilters.filters.push(filter);
                    combinedFilters.filters.push(chartFilter);
                    ds.filter(combinedFilters);
                } else { // if grid filters are already 'and' together, we just add chart filter to grid filter group
                    filter.filters.push(chartFilter);
                    ds.filter(filter);
                }
            }
            else { // chart fiter is the only filter
                ds.filter(chartFilter);
            }

            renderDashboard($(_gridId).data().kendoGrid.dataSource.view()); // update the dashboard
        }

    return {
        init: init,
        getData: getData,
        render: render,
        renderFutureRevenue: renderFutureRevenue,
    }
}();

DojoWeb.StackChart = function () {
    var _stackWidth = 110,
        _stackHeight = 180,
        _earningDurations = {
            month: 'Next 30 Days',
            quarter: 'Next 3 months',
            semiAnnual: 'Next 6 Months'
        },
        _legendFont = '10px Segoe UI, Tahoma, Geneva, Verdana, sans-serif',
        _titleFont = '14px Segoe UI, Tahoma, Geneva, Verdana, sans-serif',
        _titleColor = '#333',

        futureRevenueBar = function (id, title, chartData, accounts, clickEvent) {
            stackbar(id, title, chartData, accounts, clickEvent);
        },

        stackbar = function (id, title, data, accounts, clickEvent) {
            var stackHeight = barChartHeight(accounts);
            var maxValue = getMaxValue(data);
            function createChart(id, title, data) {
                var chart = $('#' + id).height(stackHeight).kendoChart({
                    title: { text: title },
                    legend: { visible: false },
                    seriesDefaults: { type: 'bar' },
                    series: [
                        { name: 'Month Revenue', data: data.monthRevenue },
                        { name: 'Three-Month Revenue', data: data.quarterRevenue },
                        { name: 'Six-Month Revenue', data: data.semiAnnualRevenue }
                    ],
                    valueAxis: { // this is x-axis
                        max: maxValue,
                        line: { visible: false },
                        minorGridLines: { visible: true },
                        labels: { rotation: 'auto' }
                    },
                    categoryAxis: { // this is the y-axis
                        categories: accounts,
                        majorGridLines: { visible: false }
                    },
                    tooltip: { visible: true, template: "#= series.name #: #= value #" },
                    seriesClick: clickEvent
                });
            }

            $(document).ready(function () {
                createChart(id, title, data);
            });

            $(document).bind('kendo:skinChange', function () {
                createChart(id, title, data);
            });
        },

        earningDurations = function () {
            return _earningDurations;
        },

        shortLabel = function (label) {
            if (label == undefined) return '';
            var index = label.indexOf(' ');
            if (index >= 0) {
                return label.substring(0, index + 2) + '.';
            }
            else {
                return label;
            }
        },

        barChartHeight = function (categories) {
            return categories.length * 16 + 70;
        },

        integerLabel = function (label) {
            if (('' + label).indexOf('.') < 0) 
                return label;
            else // hide number that has decimal
                return '';
        }

    return {
        earningDurations: earningDurations,
        futureRevenueBar: futureRevenueBar,
        shortLabel: shortLabel,
        integerLabel: integerLabel
    }
}();

