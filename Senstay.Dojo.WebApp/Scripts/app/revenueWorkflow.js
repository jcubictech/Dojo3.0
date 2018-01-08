"use strict";
var DojoWeb = DojoWeb || {};

DojoWeb.RevenueWorkflow = function () {
    var _canReview = false,
        _canApprove = false,
        _canFinalize = false,
        _states = {
            notstarted: 0,
            reviewed: 1,
            approved: 2,
            finalized: 3,
            closed: 4
        },
        _moveTo = undefined,
        _isDisabled = false,

        // revenue workflow rules:
        // 1. if the workflow is not started, review button is enabled; approve and finalize buttons are disabled
        // 2. if the workflow has been reviewed, review button is replaced with green check icon; approve button is enabled; finalize button is disabled
        // 3. if the workflow has been approved, review and approve buttons are replaced with green check icon; finalize button is enabled
        // 4. if the workflow has been finalized, all buttons are replaced with green check icon
        // 5. workflow role is observed

        install = function (moveTo) {
            // these classes are added to grid element to indicate the access roles
            _canReview = $('.revenue-grid-reviewer').length > 0;
            _canApprove = $('.revenue-grid-approver').length > 0;
            _canFinalize = $('.revenue-grid-finalizer').length > 0;
            _moveTo = moveTo;
        },

        init = function (id, state, currentState) {
            // initially set workflow to start from current state
            var html = '';
            if (currentState == states().notstarted) {
                if (state == states().reviewed && canEdit(state))
                    html = seedHtml(id, stateName(state), actionHtml(id, state));
                else
                    html = seedHtml(id, stateName(state), inactiveHtml(id, state));
            }
            else if (currentState == states().reviewed) {
                if (state == states().reviewed) {
                    html = _canReview ? seedHtml(id, stateName(state), retractState(id, state)) :
                                        seedHtml(id, stateName(state), checkState(id, state));
                }
                else if (state == states().approved) {
                    html = _canApprove ? seedHtml(id, stateName(state), actionHtml(id, state)) :
                                         seedHtml(id, stateName(state), inactiveHtml(id, state));
                }
                else if (state == states().finalized) {
                    html = seedHtml(id, stateName(state), inactiveHtml(id, state));
                }
            }
            else if (currentState == states().approved) {
                if (state == states().reviewed)
                    html = seedHtml(id, stateName(state), checkState(id, state));
                else if (state == states().approved) {
                    html = _canApprove ? seedHtml(id, stateName(state), retractState(id, state)) :
                                         seedHtml(id, stateName(state), checkState(id, state));;
                }
                else if (state == states().finalized) {
                    html = _canFinalize ? seedHtml(id, stateName(state), actionHtml(id, state)) :
                                          seedHtml(id, stateName(state), inactiveHtml(id, state));
                }
            }
            else if (currentState == states().finalized) {
                if (state == states().reviewed) {
                    html = seedHtml(id, stateName(state), checkState(id, state));
                }
                else if (state == states().approved) {
                    html = seedHtml(id, stateName(state), checkState(id, state));
                }
                else if (state == states().finalized)
                    html = _canFinalize ? seedHtml(id, stateName(state), retractState(id, state)) :
                                          seedHtml(id, stateName(state), checkState(id, state));
            }
            else if (currentState == -1) {
                var x = 0;
            }

            return html;
        },

        disable = function (disabled) {
            _isDisabled = disabled;
        },

        moveToNext = function (id, state) {
            if (_isDisabled) return;

            // change current workflow state to check + retract or check if view-only
            if (canEdit) {
                if (id == 0)
                    inactiveState(id, state);
                else {
                    retractState(id, state);
                    if (_moveTo != undefined) _moveTo(id, state, 1);
                }
            }
            else
                checkState(id, state);

            // change next workflow state to current or inactive if view-only
            if (id > 0 && state != states().finalized) {
                var next = nextState(state);
                if (canEdit(next))
                    actionState(id, next);
                else
                    inactiveState(id, next);
            }

            // change previous workflow state to check
            if (id > 0 && state > states().reviewed) {
                checkState(id, prevState(state));
            }
        },

        moveToPrev = function (id, state) {
            if (_isDisabled) return;

            // change current workflow state to current or inactive if view-only
            if (canEdit(state)) {
                if (id == 0)
                    inactiveState(id, state);
                else {
                    actionState(id, state);
                    if (_moveTo != undefined) _moveTo(id, state, -1);
                }
            }
            else
                inactiveState(id, state);

            // change next workflow state to non-clickable
            if (id > 0 && state < states().finalized) {
                inactiveState(id, nextState(state));
            }

            // change previous workflow state to click + retract or click if view-only
            if (id > 0 && state > states().reviewed) {
                var prev = prevState(state);
                if (canEdit(prev))
                    retractState(id, prev);
                else
                    checkState(id, prev);
            }
        },

        checkState = function (id, state) {
            if (validState(state)) {
                var $workflowStep = stateStep(id, state);
                var workflowHtml = checkHtml();
                $workflowStep.html(workflowHtml);
                return workflowHtml;
            }
        },

        retractState = function (id, state) {
            if (validState(state)) {
                var $workflowStep = stateStep(id, state);
                var workflowHtml = retractHtml(id, state);
                $workflowStep.html(workflowHtml);
                return workflowHtml;
            }
            return '';
        },

        actionState = function (id, state) {
            if (validState(state)) {
                var $workflowStep = stateStep(id, state);
                var workflowName = stateName(state);
                var workflowHtml = actionHtml(id, state, workflowName);
                $workflowStep.html(workflowHtml);
                return workflowHtml;
            }
            return '';
        },

        inactiveState = function (id, state) {
            if (validState(state)) {
                var $workflowStep = stateStep(id, state);
                var workflowName = stateName(state);
                var workflowHtml = inactiveHtml(id, state);
                $workflowStep.html(workflowHtml);
            }
        },

        validState = function (state) {
            return state >= states().reviewed && state <= states().finalized;
        },

        stateStep = function (id, state) {
            // state step id is defined 
            var idPrefix = (state == states().finalized ? '#workflow-finalize-id-' :
                            (state == states().approved ? '#workflow-approve-id-' :
                            (state == states().reviewed ? '#workflow-review-id-' : '#workflow-notstarted-')));
            return $(idPrefix + id);
        },

        stateName = function (state) {
            return (state == states().reviewed ? 'Review' : 
                    (state == states().approved ? 'Approve' : 
                    (state == states().finalized ? 'Finalize' : 'Not Started')));
        },

        nextState = function (state) {
            return (state == states().notstarted ? states().reviewed :
                    (state == states().reviewed ? states().approved :
                    (state == states().approved ? states().finalized : states().closed())));
        },

        prevState = function (state) {
            return (state == states().reviewed ? states().notstarted :
                    (state == states().approved ? states().reviewed :
                    (state == states().finalized ? states().approved : state)));
        },

        actionHtml = function (id, state) {
            return kendo.format('<button class="btn round btn-primary" onclick="javascript:DojoWeb.RevenueWorkflow.moveToNext({0},{1});">{2}</button>',
                                id, state, stateName(state));
        },

        retractHtml = function (id, state) {
            if (!_isDisabled)
                return kendo.format('<div><i class="fa fa-check green icon-gap"></i><span class="dojo-pointer" onclick="javascript:DojoWeb.RevenueWorkflow.moveToPrev({0},{1});"><i class="fa fa-reply red"></i></span></div>',
                                    id, state);
            else
                return '<div><i class="fa fa-check green icon-gap">';
        },

        inactiveHtml = function (id, state) {
            return kendo.format('<button class="btn round btn-default">{0}</button>', stateName(state));
        },

        checkHtml = function () {
            return '<div><i class="fa fa-check green"></i></div>';
        },

        seedHtml = function (id, name, action) {
            return kendo.format('<div id="workflow-{2}-id-{0}" class="gridcell-btn" title="{1}" data-id="{0}">{3}</div>',
                                id, name, name.toLowerCase(), action);
        },

        canEdit = function (state) {
            return (_canReview && state == states().reviewed) ||
                    (_canApprove && state == states().approved) ||
                    (_canFinalize && state == states().finalized);
        },

        states = function () {
            return _states;
        }

    return {
        install: install,
        init: init,
        disable: disable,
        moveToNext: moveToNext,
        moveToPrev: moveToPrev,
        states: states
    }
}();
