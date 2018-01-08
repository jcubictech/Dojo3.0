"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.StackChart = function () {
    var _stackHeight = 180,
        _legendFont = '10px Segoe UI, Tahoma, Geneva, Verdana, sans-serif',
        _titleFont = '14px Segoe UI, Tahoma, Geneva, Verdana, sans-serif',
        _titleColor = '#333',
        _reportColors = {
            payout: '#7cc27e',
            unit: '#E97751',
            na: '#aaa'
        },

        marketChart = function (id, title, group, isPercent, chartData, categories, clickEvent) {
             _stackHeight = 180;
             var dataSeries = [
                 {
                     name: 'Payout',
                     stack: { group: group },
                     data: []
                 },
             ];

             if (id == 'marketPayout') {
                 _.each(chartData, function (data, key, list) {
                     var total = _.reduce(data, function (memo, item) {
                         return memo + item / 1000;
                     }, 0);

                     dataSeries[0].data.push(total);
                     dataSeries[0].stack.group = group;
                 });
             }
             else {
                 dataSeries[0].name = 'Rented';
                 _.each(chartData, function (data, key, list) {
                     var count = _.countBy(data, function (item) {
                         return 'total';
                     });
                     dataSeries[0].data.push(count.total);
                     dataSeries[0].stack.group = group;
                 });
             }

             columnbar(id, title, isPercent, dataSeries, categories, clickEvent);
         },

        columnbar = function (id, title, isPercent, data, categories, clickEvent) {
            var color = id == 'marketPayout' ? _reportColors.payout : _reportColors.unit;
            function createChart(id, title, data) {
                var chart = $('#' + id).height(_stackHeight).kendoChart({
                    title: {
                        text: title,
                        font: _titleFont,
                        color: _titleColor
                    },
                    legend: { visible: false },
                    seriesDefaults: { type: 'column' },
                    series: data,
                    valueAxis: {
                        min: 0,
                        labels: { step: 1, skip: 0, font: _legendFont },
                        line: { visible: false }
                    },
                    categoryAxis: {
                        categories: categories, // the x-axis
                        labels: { rotation: 0, template: '#= DojoWeb.StackChart.shortMarketLabel(value) #', font: _legendFont },
                        majorGridLines: { visible: false }
                    },
                    seriesColors: [ color ],
                    tooltip: {
                        visible: true,
                        color: '#ffffff',
                        template: '#= DojoWeb.StackChart.tooltip(category, series.name, value) #' //"#= category #: #= series.name # #= value #"
                    },
                    seriesClick: clickEvent
                });

                if (isPercent == true) {
                    $('#' + id).data("kendoChart").setOptions({ seriesDefaults: { stack: { type: '100%' } } });
                }
                else {
                    $('#' + id).data("kendoChart").setOptions({ valueAxis: { labels: { template: '#= DojoWeb.StackChart.integerLabel(value) #' } } });
                }
            }

            $(document).ready(function () {
                createChart(id, title, data);
            });

            $(document).bind('kendo:skinChange', function () {
                createChart(id, title, data);
            });
        },

        tooltip = function (category, name, value) {
            var index = ('' + value).indexOf('.');
            var trimmedValue = index >= 0 ? ('' + value).substring(0, index) : value;
            if (name.toLowerCase() == 'payout') trimmedValue = '$' + trimmedValue + 'k';
            return category + ' ' + name + ':' + trimmedValue;
        },

        shortMarketLabel = function (label) {
            switch (label.toLowerCase()) {
                case 'Llos angeles': return 'LA'; break;
                case 'san francisco': return 'SF'; break;
                case 'san diego': return 'SD'; break;
                case 'cabo san lucas': return 'Cabo'; break;
                case 'southampton': return 'Hampton'; break;
                case 'playa del carmen': return 'Playa'; break;
                case 'rio de janeiro': return 'Rio'; break;
                case 'florianópolis': return 'Florida'; break;
                default: return label;
            }
            return label;
        },

        integerLabel = function (label) {
            if (('' + label).indexOf('.') < 0)
                return label;
            else // hide number that has decimal
                return '';
        }

    return {
        marketChart: marketChart,
        tooltip: tooltip,
        shortMarketLabel: shortMarketLabel,
        integerLabel: integerLabel
    }
}();

DojoWeb.DonutChart = function () {
    var _donutWidth = 100,
        _donutHeight = 100,
        _gridId,
        _chartNames = {
            market: 'Maket',
            team: 'Team',
            status: 'Status',
        },

        render = function (gridId, data) {
            _gridId = gridId;
            updateTotalCount(gridId);

            // status donut charts; chart ID follows naming convention of chart field name + 'Chart'
            rgbDonut(_chartNames.market + 'Chart', _chartNames.market, _.countBy(data, _chartNames.market), marketChartClick);
            rgbDonut(_chartNames.team + 'Chart', _chartNames.team, _.countBy(data, _chartNames.team), teamChartClick);
            rgbDonut(_chartNames.status + 'Chart', _chartNames.status, _.countBy(data, _chartNames.status), statusChartClick);
        },

        updateTotalCount = function (gridId) {
            var data = $(_gridId).data().kendoGrid.dataSource.view();
            $('#StatusCount').html(data.length.toString());
        },

        marketChartClick = function (e) {
            chartClick(_chartNames.overall, e, DojoWeb.Icons.statusColorIds()[e.category]);
        },

        teamChartClick = function (e) {
            chartClick(_chartNames.scope, e, DojoWeb.Icons.statusColorIds()[e.category]);
        },

        statusChartClick = function (e) {
            chartClick(_chartNames.schedule, e, DojoWeb.Icons.statusColorIds()[e.category]);
        },

        chartClick = function (field, e, value) {
            // create donut filter with kendo-understood form
            var ds = $(_gridId).data('kendoGrid').dataSource;
            var chartFilter = { filters: [{ field: field, operator: 'eq', value: value }], logic: 'or' };

            // combine filters between grid filters and donut filter
            var filter = ds.filter();
            if (filter) {
                if (filter.logic != 'and') { // if the filters are 'or' together, we 'and' it with donut filter group
                    var combinedFilters = { filters: [], logic: 'and' };
                    combinedFilters.filters.push(filter);
                    combinedFilters.filters.push(chartFilter);
                    ds.filter(combinedFilters);
                } else { // if the filter groups are already 'and' together, we just add donut filter to filter group
                    filter.filters.push(chartFilter);
                    ds.filter(filter);
                }
            }
            else { // donut is the first filter
                ds.filter(chartFilter);
            }

            render(_gridId, $(_gridId).data().kendoGrid.dataSource.view());
        },

        rgbDonut = function (id, name, data, clickEvent) {
            var chartData = [];
            _.each(data, function (value, key, list) {
                var category = DojoWeb.Icons.statusNames()[key.toLowerCase()];
                var color = DojoWeb.Icons.statusColors()[category];
                chartData.push({
                    category: category,
                    value: value,
                    color: color
                });
            });

            donut(id, name, chartData, clickEvent);
        },

        donut = function (id, name, data, clickEvent) {
            function createChart(id, name, data) {
                $('#' + id).width(_donutWidth).height(_donutHeight).kendoChart({
                    title: {},
                    legend: { visible: false },
                    chartArea: { background: '' },
                    seriesDefaults: { type: 'donut', startAngle: 0 },
                    series: [{
                        name: name,
                        data: data,
                        labels: {
                            visible: true,
                            background: 'transparent',
                            color: '#ffffff',
                            template: '#= value#'
                        }
                    }],
                    tooltip: {
                        visible: true,
                        color: '#ffffff',
                        template: '#= category # : #= value #'
                    },
                    seriesClick: clickEvent
                });
            }

            $(document).ready(function () {
                createChart(id, name, data);
            });

            $(document).bind('kendo:skinChange', function () {
                createChart(id, name, data);
            });
        }

    return {
        render: render,
        rgbDonut: rgbDonut
    }
}();

DojoWeb.Icons = function () {
    var _booleanIcons = {
        yes: true,
        no: false
    },
        _progressIcons = {
            notStarted: 0,
            inProgress: 1,
            completed: 2
        },
        _statusColorIds = {
            none: 0,
            green: 1,
            yellow: 2,
            red: 3
        },
        _statusColors = {
            none: '#888',
            green: '#2AB05B',
            yellow: '#DFC400',
            red: '#FD595B'
        },
        _statusNames = {
            '0': 'none',
            '1': 'green',
            '2': 'yellow',
            '3': 'red'
        },

        statusIcon = function (data) {
            if (data == _statusColorIds.red) {
                return '<div style="text-align:center"><i class="fa fa-circle red"></i></div>';
            } else if (data == _statusColorIds.yellow) {
                return '<div style="text-align:center"><i class="fa fa-circle yellow"></i></div>';
            } else if (data == _statusColorIds.green) {
                return '<div style="text-align:center"><i class="fa fa-circle green"></i></div>';
            } else {
                return '<div style="text-align:center"><i class="fa fa-circle-thin"></i></div>';
            }
        },

        dependencyIcon = function (data) {
            if (data == _statusColorIds.red) {
                return '<div style="text-align:center"><i class="fa fa-square red"></i></div>';
            } else if (data == _statusColorIds.yellow) {
                return '<div style="text-align:center"><i class="fa fa-square yellow"></i></div>';
            } else if (data == _statusColorIds.green) {
                return '<div style="text-align:center"><i class="fa fa-square green"></i></div>';
            } else {
                return '<div style="text-align:center"><i class="fa fa-square-o"></i></div>';
            }
        },

        progressIcon = function (data) {
            if (data == progressIcons().completed) {
                return '<div style="text-align:center"><i class="fa fa-battery-full green"></i></div>';
            } else if (data == progressIcons().inProgress) {
                return '<div style="text-align:center"><i class="fa fa-battery-half cyan"></i></div>';
            } else {
                return '<div style="text-align:center"><i class="fa fa-battery-empty red"></i></div>';
            }
        },

        booleanIcon = function (data) {
            if (data == booleanIcons().yes) {
                return '<div style="text-align:center"><i class="fa fa-check icon-active"></i></div>';
            } else {
                return '<div style="text-align:center"><i class="fa fa-close icon-not-active"></i></div>';
            }
        },

        statusColors = function () {
            return _statusColors;
        },

        statusColorIds = function () {
            return _statusColorIds;
        },

        statusNames = function () {
            return _statusNames;
        },

        progressIcons = function () {
            return _progressIcons;
        },

        booleanIcons = function () {
            return _booleanIcons;
        }

    return {
        booleanIcon: booleanIcon,
        booleanIcons: booleanIcons,
        statusIcon: statusIcon,
        statusColorIds: statusColorIds,
        statusColors: statusColors,
        statusNames: statusNames,
        progressIcon: progressIcon,
        dependencyIcon: dependencyIcon,
        progressIcons: progressIcons
    }
}();

