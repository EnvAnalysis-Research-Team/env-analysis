'use strict';

(function () {
    const routes = window.measurementResultRoutes || {};
    const lookups = window.measurementResultLookups || { emissionSources: [], parameters: [] };
    const permissions = window.measurementResultPermissions || { canApprove: false };

    const getAntiForgeryToken = () =>
        document.querySelector('#measurementResultsAntiForgery input[name="__RequestVerificationToken"]')?.value || '';

    const withAntiForgery = (headers = {}) => {
        const token = getAntiForgeryToken();
        if (token) headers['RequestVerificationToken'] = token;
        return headers;
    };

    const elements = {
        addModal: document.getElementById('addResultModal'),
        editModal: document.getElementById('editResultModal'),
        openAddBtn: document.getElementById('openAddResultBtn'),
        closeAddBtn: document.getElementById('closeAddResultBtn'),
        cancelAddBtn: document.getElementById('cancelAddResultBtn'),
        saveAddBtn: document.getElementById('saveResultBtn'),
        closeEditBtn: document.getElementById('closeEditResultBtn'),
        cancelEditBtn: document.getElementById('cancelEditResultBtn'),
        updateEditBtn: document.getElementById('updateResultBtn'),
        deleteEditBtn: document.getElementById('deleteResultBtn'),
        allBody: document.getElementById('allResultsBody'),
        waterBody: document.getElementById('waterResultsBody'),
        airBody: document.getElementById('airResultsBody'),
        waterBadge: document.getElementById('waterCountBadge'),
        airBadge: document.getElementById('airCountBadge'),
        exportBtn: document.getElementById('exportResultsBtn'),
        tabButtons: document.querySelectorAll('.tab-button'),
        tabPanels: {
            all: document.getElementById('tabPanel-all'),
            water: document.getElementById('tabPanel-water'),
            air: document.getElementById('tabPanel-air')
        },
        trendSelect: document.getElementById('parameterTrendSelect'),
        trendTabButtons: document.querySelectorAll('[data-trend-tab]'),
        trendFilterForm: document.getElementById('trendFilterForm'),
        trendSelectHint: document.getElementById('trendSelectHint'),
        trendFilterStart: document.getElementById('trendFilterStart'),
        trendFilterEnd: document.getElementById('trendFilterEnd'),
        trendFilterReset: document.getElementById('trendFilterReset'),
        trendFilterSource: document.getElementById('trendFilterSource'),
        trendTableBody: document.getElementById('trendTableBody'),
        trendTableSummary: document.getElementById('trendTableSummary'),
        trendTablePageLabel: document.getElementById('trendTablePageLabel'),
        trendTablePrev: document.getElementById('trendTablePrev'),
        trendTableNext: document.getElementById('trendTableNext'),
        trendTablePageSize: document.getElementById('trendTablePageSize'),
        trendChartCanvas: document.getElementById('parameterTrendChart'),
        trendChartPlaceholder: document.getElementById('trendChartPlaceholder'),
        paginationBar: document.getElementById('resultsPaginationBar'),
        paginationSummary: document.getElementById('resultsPaginationSummary'),
        paginationPageLabel: document.getElementById('resultsPaginationPageLabel'),
        paginationPrev: document.getElementById('resultsPrevPage'),
        paginationNext: document.getElementById('resultsNextPage'),
        pageSizeSelect: document.getElementById('resultsPageSize'),
        resultsSearchInput: document.getElementById('resultsSearchInput'),
        resultsSearchButton: document.getElementById('resultsSearchButton'),
        resultsSearchReset: document.getElementById('resultsSearchReset'),
        filterModal: document.getElementById('resultsFilterModal'),
        openFilterBtn: document.getElementById('openResultsFilterBtn'),
        closeFilterBtn: document.getElementById('closeResultsFilterBtn'),
        cancelFilterBtn: document.getElementById('cancelResultsFilterBtn'),
        applyFilterBtn: document.getElementById('applyResultsFilterBtn'),
        resetFilterBtn: document.getElementById('resetResultsFilterBtn'),
        filterSourceSelect: document.getElementById('filterSourceSelect'),
        filterParameterSelect: document.getElementById('filterParameterSelect'),
        filterStatusSelect: document.getElementById('filterStatusSelect'),
        filterStartDate: document.getElementById('filterStartDate'),
        filterEndDate: document.getElementById('filterEndDate'),
        activeFiltersBadge: document.getElementById('activeFiltersBadge'),
        importModal: document.getElementById('importResultsModal'),
        importPanel: document.getElementById('importResultsModalPanel'),
        openImportBtn: document.getElementById('openImportResultsBtn'),
        closeImportBtn: document.getElementById('closeImportResultsBtn'),
        cancelImportBtn: document.getElementById('cancelImportResultsBtn'),
        previewImportBtn: document.getElementById('previewImportResultsBtn'),
        confirmImportBtn: document.getElementById('confirmImportResultsBtn'),
        backImportBtn: document.getElementById('backImportResultsBtn'),
        importFileInput: document.getElementById('importFileInput'),
        importFileLabel: document.getElementById('importFileLabel'),
        importStepUpload: document.getElementById('importStepUpload'),
        importStepPreview: document.getElementById('importStepPreview'),
        importStepUploadLabel: document.getElementById('importStepUploadLabel'),
        importStepPreviewLabel: document.getElementById('importStepPreviewLabel'),
        importPreviewSummary: document.getElementById('importPreviewSummary'),
        importPreviewHint: document.getElementById('importPreviewHint'),
        importPreviewTableBody: document.getElementById('importPreviewTableBody'),
        importSourceSelect: document.getElementById('importSourceSelect')
    };

    const addForm = {
        source: document.getElementById('addResultSource'),
        parameter: document.getElementById('addResultParameter'),
        value: document.getElementById('addResultValue'),
        date: document.getElementById('addResultDate'),
        approvedAt: document.getElementById('addResultApprovedAt'),
        remark: document.getElementById('addResultRemark'),
        approvedCheckbox: document.getElementById('addResultApprovedCheckbox')
    };

    const editForm = {
        id: document.getElementById('editResultId'),
        source: document.getElementById('editResultSource'),
        parameter: document.getElementById('editResultParameter'),
        value: document.getElementById('editResultValue'),
        date: document.getElementById('editResultDate'),
        approvedAt: document.getElementById('editResultApprovedAt'),
        remark: document.getElementById('editResultRemark'),
        approvedCheckbox: document.getElementById('editResultApprovedCheckbox')
    };


    const filterForm = {
        source: elements.filterSourceSelect,
        parameter: elements.filterParameterSelect,
        status: elements.filterStatusSelect,
        startDate: elements.filterStartDate,
        endDate: elements.filterEndDate
    };

    const TAB_KEYS = ['all', 'water', 'air'];
    const DEFAULT_PAGE_SIZE = 10;
    const loadingMessages = {
        all: 'Loading measurements...',
        water: 'Loading water measurements...',
        air: 'Loading air measurements...'
    };

    const createEmptyPagination = () => ({
        page: 1,
        pageSize: DEFAULT_PAGE_SIZE,
        totalItems: 0,
        totalPages: 1
    });

    const createDefaultFilters = () => ({
        sourceId: null,
        parameterCode: null,
        status: null,
        startDate: null,
        endDate: null
    });

    const state = {
        datasets: {
            all: [],
            water: [],
            air: []
        },
        pagination: {
            all: createEmptyPagination(),
            water: createEmptyPagination(),
            air: createEmptyPagination()
        },
        summary: {
            all: 0,
            water: 0,
            air: 0
        },
        activeTab: 'all',
        pageSize: DEFAULT_PAGE_SIZE,
        loadedTabs: new Set(),
        searchQuery: '',
        filters: createDefaultFilters()
    };

    const importState = {
        file: null,
        rows: [],
        totalRows: 0,
        validRows: 0,
        invalidRows: 0,
        step: 'upload',
        sourceId: null
    };

    if (elements.pageSizeSelect) {
        elements.pageSizeSelect.value = DEFAULT_PAGE_SIZE;
    }

    const findResultInState = (resultId) => {
        const numericId = Number(resultId);
        if (!Number.isFinite(numericId)) return null;
        for (const tab of TAB_KEYS) {
            const dataset = state.datasets[tab] ?? [];
            const match = dataset.find(item => Number(item.resultID) === numericId);
            if (match) return match;
        }
        return null;
    };

    const trend = {
        selectedCode: null,
        selectedCodes: [],
        activeType: 'air',
        chart: null,
        filter: {
            startMonth: null,
            endMonth: null,
            sourceId: null
        },
        table: {
            page: 1,
            pageSize: 12,
            pagination: {
                page: 1,
                pageSize: 12,
                totalItems: 0,
                totalPages: 1
            }
        }
    };

    const trendColorPalette = ['#2563eb', '#f97316', '#10b981', '#ef4444', '#8b5cf6', '#14b8a6'];

    const unwrapApiResponse = (json) => {
        if (!json || typeof json !== 'object') return json;
        if (Object.prototype.hasOwnProperty.call(json, 'data')) return json.data;
        return json;
    };

    const toggleAppModal = (modal, open) => {
        if (!modal) return;
        modal.classList[open ? 'add' : 'remove']('app-modal--open');
        modal.setAttribute('aria-hidden', open ? 'false' : 'true');
    };

    const registerModalDismiss = (modal, closeHandler) => {
        if (!modal) return;
        modal.addEventListener('click', (event) => {
            if (event.target === modal) {
                closeHandler();
            }
        });
    };

    const escapeHtml = (value) => {
        if (value === null || value === undefined) return '';
        return value
            .toString()
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;');
    };

    const setButtonBusy = (button, busy, busyLabel = null) => {
        if (!button) return;
        const defaultLabel = button.dataset.originalLabel || button.textContent;
        if (!button.dataset.originalLabel) {
            button.dataset.originalLabel = defaultLabel;
        }
        button.disabled = busy;
        button.classList.toggle('opacity-60', busy);
        button.classList.toggle('pointer-events-none', busy);
        if (busy && busyLabel) {
            button.textContent = busyLabel;
        } else {
            button.textContent = button.dataset.originalLabel;
        }
    };

    const formatDate = (value) => {
        if (!value) return '-';
        try {
            return new Date(value).toLocaleString();
        } catch {
            return value;
        }
    };

    const formatInputDate = (value) => {
        if (!value) return '';
        try {
            return new Date(value).toISOString().slice(0, 16);
        } catch {
            return '';
        }
    };

    const nowAsInputValue = () => {
        const now = new Date();
        now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
        return now.toISOString().slice(0, 16);
    };

    const formatNumericValue = (value) => {
        if (value === null || value === undefined) return '-';
        const number = Number(value);
        return Number.isFinite(number)
            ? number.toLocaleString(undefined, { maximumFractionDigits: 3 })
            : '-';
    };

    const toIsoStringOrNull = (value) => {
        if (!value) return null;
        const date = value instanceof Date ? value : new Date(value);
        return Number.isNaN(date.getTime()) ? null : date.toISOString();
    };

    const toNumericOrNull = (value) => {
        if (value === null || value === undefined || value === '') return null;
        const number = Number(value);
        return Number.isFinite(number) ? number : null;
    };

    const renderOptions = (select, items, valueKey, labelKey) => {
        if (!select) return;
        select.innerHTML = items.map(item => `<option value="${item[valueKey]}">${item[labelKey]}</option>`).join('');
    };

    const renderImportSourceOptions = () => {
        if (!elements.importSourceSelect) return;
        const items = lookups.emissionSources ?? [];
        const options = [
            '<option value="">Select an emission source</option>',
            ...items.map(item => `<option value="${item.id}">${item.label}</option>`)
        ];
        elements.importSourceSelect.innerHTML = options.join('');
        elements.importSourceSelect.value = '';
        importState.sourceId = null;
    };

    const handleImportSourceChange = (event) => {
        const value = event?.target?.value ?? '';
        const parsed = Number(value);
        importState.sourceId = Number.isFinite(parsed) ? parsed : null;
    };

    const syncApprovalCheckbox = (checkbox, approvedInput) => {
        if (!checkbox || !approvedInput) return;
        checkbox.checked = Boolean(approvedInput.value);
    };

    const bindApprovalCheckbox = (checkbox, approvedInput) => {
        if (!checkbox || !approvedInput) return;
        checkbox.addEventListener('change', () => {
            if (checkbox.checked) {
                if (!approvedInput.value) {
                    approvedInput.value = nowAsInputValue();
                }
            } else {
                approvedInput.value = '';
            }
        });
    };

    const findParameterMeta = (code) => {
        if (!code) return null;
        return (lookups.parameters || []).find(item => item.code === code) || null;
    };

    bindApprovalCheckbox(addForm.approvedCheckbox, addForm.approvedAt);
    bindApprovalCheckbox(editForm.approvedCheckbox, editForm.approvedAt);

    const setFilterFormValues = (values = state.filters) => {
        const normalized = { ...createDefaultFilters(), ...values };
        if (filterForm.source) {
            filterForm.source.value = normalized.sourceId != null ? normalized.sourceId.toString() : '';
        }
        if (filterForm.parameter) {
            filterForm.parameter.value = normalized.parameterCode ?? '';
        }
        if (filterForm.status) {
            filterForm.status.value = normalized.status ?? '';
        }
        if (filterForm.startDate) {
            filterForm.startDate.value = normalized.startDate ?? '';
        }
        if (filterForm.endDate) {
            filterForm.endDate.value = normalized.endDate ?? '';
        }
    };

    const renderFilterSelects = () => {
        if (filterForm.source) {
            const items = lookups.emissionSources ?? [];
            const options = [
                '<option value="">All sources</option>',
                ...items.map(item => `<option value="${item.id}">${item.label}</option>`)
            ];
            filterForm.source.innerHTML = options.join('');
        }
        if (filterForm.parameter) {
            const items = lookups.parameters ?? [];
            const options = [
                '<option value="">All parameters</option>',
                ...items.map(item => `<option value="${item.code}">${item.label}</option>`)
            ];
            filterForm.parameter.innerHTML = options.join('');
        }
        setFilterFormValues();
    };

    const sanitizeStatusValue = (value) => {
        const normalized = (value || '').trim().toLowerCase();
        return normalized === 'approved' || normalized === 'pending' ? normalized : null;
    };

    const readFilterFormValues = () => {
        const sourceValue = filterForm.source?.value ?? '';
        const parameterValue = (filterForm.parameter?.value ?? '').trim();
        const statusValue = filterForm.status?.value ?? '';
        const startValue = filterForm.startDate?.value ?? '';
        const endValue = filterForm.endDate?.value ?? '';

        const parsedSource = sourceValue ? Number(sourceValue) : null;
        return {
            sourceId: Number.isFinite(parsedSource) ? parsedSource : null,
            parameterCode: parameterValue || null,
            status: sanitizeStatusValue(statusValue),
            startDate: startValue || null,
            endDate: endValue || null
        };
    };

    const countActiveFilters = (filters = state.filters) => {
        if (!filters) return 0;
        let count = 0;
        if (filters.sourceId != null) count += 1;
        if (filters.parameterCode) count += 1;
        if (filters.status) count += 1;
        if (filters.startDate) count += 1;
        if (filters.endDate) count += 1;
        return count;
    };

    const updateFilterBadge = () => {
        if (!elements.activeFiltersBadge) return;
        const count = countActiveFilters();
        if (count > 0) {
            elements.activeFiltersBadge.textContent = count.toString();
            elements.activeFiltersBadge.classList.remove('hidden');
        } else {
            elements.activeFiltersBadge.classList.add('hidden');
        }
    };

    const applyAdvancedFilters = (payload) => {
        state.filters = { ...createDefaultFilters(), ...payload };
        TAB_KEYS.forEach(tab => {
            const pagination = ensurePaginationState(tab);
            pagination.page = 1;
        });
        state.loadedTabs = new Set();
        updateFilterBadge();
        setLoadingState(state.activeTab);
        loadResults(state.activeTab);
    };

    const normalizeParameterType = (value) => {
        if (typeof value !== 'string' || !value.trim()) return 'water';
        const normalized = value.trim().toLowerCase();
        return normalized === 'air' ? 'air' : 'water';
    };

    const getParametersForActiveType = () => {
        const desired = normalizeParameterType(trend.activeType);
        return (lookups.parameters ?? []).filter(item => normalizeParameterType(item.type) === desired);
    };

    const updateTrendTabButtons = () => {
        if (!elements.trendTabButtons) return;
        elements.trendTabButtons.forEach(button => {
            const target = normalizeParameterType(button?.dataset?.trendTab);
            const isActive = target === normalizeParameterType(trend.activeType);
            button.classList.toggle('bg-blue-600', isActive);
            button.classList.toggle('text-white', isActive);
            button.classList.toggle('text-gray-600', !isActive);
            button.classList.toggle('bg-white', !isActive);
            button.classList.toggle('hover:bg-gray-100', !isActive);
        });
    };

    const configureTrendSelectDisplay = (optionCount) => {
        if (!elements.trendSelect) return;
        const isWater = normalizeParameterType(trend.activeType) === 'water';
        elements.trendSelect.multiple = isWater;
        if (isWater) {
            elements.trendSelect.size = Math.min(Math.max(optionCount, 4), 8);
        } else {
            elements.trendSelect.size = 1;
        }
        if (elements.trendSelectHint) {
            elements.trendSelectHint.classList.toggle('hidden', !isWater);
        }
    };

    const renderTrendOptions = () => {
        if (!elements.trendSelect) return;
        const items = getParametersForActiveType();
        configureTrendSelectDisplay(items.length);

        if (!items.length) {
            elements.trendSelect.innerHTML = '<option value="">No parameters available</option>';
            elements.trendSelect.disabled = true;
            trend.selectedCode = null;
            trend.selectedCodes = [];
            return;
        }

        elements.trendSelect.disabled = false;
        elements.trendSelect.innerHTML = items
            .map(item => `<option value="${item.code}">${item.label} (${item.code})</option>`)
            .join('');

        if (normalizeParameterType(trend.activeType) === 'water') {
            const availableValues = new Set(items.map(item => item.code));
            let selectedValues = trend.selectedCodes.filter(code => availableValues.has(code));
            if (!selectedValues.length) {
                selectedValues = [items[0].code];
            }
            trend.selectedCodes = selectedValues;
            trend.selectedCode = null;
            Array.from(elements.trendSelect.options).forEach(option => {
                option.selected = selectedValues.includes(option.value);
            });
        } else {
            trend.selectedCodes = [];
            if (trend.selectedCode && items.some(item => item.code === trend.selectedCode)) {
                elements.trendSelect.value = trend.selectedCode;
            } else {
                trend.selectedCode = items[0].code;
                elements.trendSelect.value = trend.selectedCode;
            }
        }
    };

    const renderTrendSourceOptions = () => {
        if (!elements.trendFilterSource) return;
        const items = lookups.emissionSources ?? [];
        const options = [
            '<option value="">All sources</option>',
            ...items.map(item => `<option value="${item.id}">${item.label}</option>`)
        ];
        elements.trendFilterSource.innerHTML = options.join('');
    };

    const hexToRgba = (hex, alpha = 1) => {
        const sanitized = hex?.replace('#', '');
        if (!sanitized || sanitized.length !== 6) return `rgba(37, 99, 235, ${alpha})`;
        const numeric = parseInt(sanitized, 16);
        const r = (numeric >> 16) & 255;
        const g = (numeric >> 8) & 255;
        const b = numeric & 255;
        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    };

    const toggleTrendPlaceholder = (hasData) => {
        if (!elements.trendChartPlaceholder || !elements.trendChartCanvas) return;
        elements.trendChartPlaceholder.classList.toggle('hidden', hasData);
        elements.trendChartCanvas.classList.toggle('invisible', !hasData);
    };

    const clearTrendChart = () => {
        if (trend.chart) {
            trend.chart.destroy();
            trend.chart = null;
        }
        toggleTrendPlaceholder(false);
    };

    const renderTrendChart = (payload) => {
        if (!elements.trendChartCanvas) return;
        if (trend.chart) {
            trend.chart.destroy();
            trend.chart = null;
        }

        const labels = payload?.labels ?? [];
        const series = payload?.series ?? [];
        if (!labels.length || !series.length) {
            toggleTrendPlaceholder(false);
            return;
        }

        toggleTrendPlaceholder(true);
        const ctx = elements.trendChartCanvas.getContext('2d');
        const datasets = [];

        series.forEach((item, index) => {
            const baseColor = trendColorPalette[index % trendColorPalette.length];
            const values = item.points.map(point => point.value);
            const datasetLabelBase = item.parameterName || item.parameterCode || `Series ${index + 1}`;
            const datasetLabel = item.unit ? `${datasetLabelBase} (${item.unit})` : datasetLabelBase;

            datasets.push({
                label: datasetLabel,
                data: values,
                borderColor: baseColor,
                backgroundColor: hexToRgba(baseColor, 0.2),
                tension: 0.3,
                radius: 3,
                pointRadius: 3,
                fill: false
            });
        });

        trend.chart = new Chart(ctx, {
            type: 'line',
            data: { labels, datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        labels: { usePointStyle: true }
                    },
                    tooltip: {
                        callbacks: {
                            label: (context) => `${context.dataset.label}: ${formatNumericValue(context.parsed.y)}`
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: false,
                        ticks: {
                            callback: (value) => formatNumericValue(value)
                        }
                    }
                }
            }
        });
    };

    const updateTrendTableControls = (tablePayload, statusMessage) => {
        if (!elements.trendTableSummary) return;
        const pagination = tablePayload?.pagination ?? {
            page: trend.table.page,
            pageSize: trend.table.pageSize,
            totalItems: 0,
            totalPages: 1
        };

        trend.table.pagination = pagination;
        trend.table.page = pagination.page ?? trend.table.page;
        trend.table.pageSize = pagination.pageSize ?? trend.table.pageSize;

        const totalItems = pagination.totalItems ?? 0;
        const currentPage = pagination.page ?? 1;
        const pageSize = pagination.pageSize ?? trend.table.pageSize;
        const totalPages = Math.max(pagination.totalPages ?? 1, 1);

        const start = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
        const end = totalItems === 0 ? 0 : Math.min(currentPage * pageSize, totalItems);

        elements.trendTableSummary.textContent = statusMessage
            ? statusMessage
            : (totalItems === 0 ? 'No data' : `Showing ${start}-${end} of ${totalItems} measurements`);

        if (elements.trendTablePageLabel) {
            elements.trendTablePageLabel.textContent = `Page ${currentPage} of ${totalPages}`;
        }
        if (elements.trendTablePrev) {
            elements.trendTablePrev.disabled = currentPage <= 1;
        }
        if (elements.trendTableNext) {
            elements.trendTableNext.disabled = currentPage >= totalPages;
        }
        if (elements.trendTablePageSize) {
            const size = Number(elements.trendTablePageSize.value);
            if (size !== trend.table.pageSize) {
                elements.trendTablePageSize.value = trend.table.pageSize.toString();
            }
        }
    };

    const renderTrendTable = (tablePayload) => {
        if (!elements.trendTableBody) return;
        const emptyRow = `
            <tr>
                <td colspan="5" class="px-3 py-5 text-center text-gray-400">
                    Select parameter(s) to see available measurements.
                </td>
            </tr>`;

        if (!tablePayload || !Array.isArray(tablePayload.items) || tablePayload.items.length === 0) {
            elements.trendTableBody.innerHTML = emptyRow;
            updateTrendTableControls(null);
            return;
        }

        const unit = tablePayload.unit ?? '-';
        const rows = tablePayload.items.map(point => `
            <tr class="hover:bg-gray-50 transition">
                <td class="px-3 py-2 whitespace-nowrap">${point.label}</td>
                <td class="px-3 py-2">${point.parameterName ?? '-'}</td>
                <td class="px-3 py-2 truncate" title="${point.sourceName ?? ''}">${point.sourceName ?? '-'}</td>
                <td class="px-3 py-2">${formatNumericValue(point.value)}</td>
                <td class="px-3 py-2">${point.unit ?? unit}</td>
            </tr>
        `);

        elements.trendTableBody.innerHTML = rows.join('') || emptyRow;
        updateTrendTableControls(tablePayload);
    };

    const buildTrendUrl = () => {
        const isWater = normalizeParameterType(trend.activeType) === 'water';
        const params = new URLSearchParams();
        if (isWater) {
            const codes = trend.selectedCodes.filter(code => typeof code === 'string' && code.trim() !== '');
            if (!codes.length) return null;
            codes.forEach(code => params.append('codes', code));
        } else {
            if (!trend.selectedCode) return null;
            params.set('code', trend.selectedCode);
        }
        if (trend.filter.startMonth) params.set('startMonth', trend.filter.startMonth);
        if (trend.filter.endMonth) params.set('endMonth', trend.filter.endMonth);
        if (trend.filter.sourceId != null) params.set('sourceId', trend.filter.sourceId.toString());
        params.set('page', trend.table.page.toString());
        params.set('pageSize', trend.table.pageSize.toString());
        return `${routes.trend}?${params.toString()}`;
    };

    const loadParameterTrends = async () => {
        if (!elements.trendTableBody || !routes.trend) return;
        const url = buildTrendUrl();
        if (!url) {
            renderTrendTable(null);
            clearTrendChart();
            return;
        }

        elements.trendTableBody.innerHTML = `
            <tr>
                <td colspan="5" class="px-3 py-5 text-center text-gray-400">Loading monthly data...</td>
            </tr>`;
        updateTrendTableControls(null, 'Loading data...');

        try {
            const res = await fetch(url, { credentials: 'same-origin' });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || 'Failed to load trend data.');
            const payload = unwrapApiResponse(json);
            renderTrendChart(payload);
            renderTrendTable(payload?.table);
        } catch (error) {
            console.error(error);
            clearTrendChart();
            elements.trendTableBody.innerHTML = `
                <tr>
                    <td colspan="5" class="px-3 py-5 text-center text-red-500">${error.message || 'Failed to load trend data.'}</td>
                </tr>`;
            updateTrendTableControls(null, 'Error loading data');
        }
    };

    const setTrendTab = (type) => {
        const normalized = normalizeParameterType(type);
        if (trend.activeType === normalized) {
            return;
        }

        trend.activeType = normalized;
        trend.selectedCode = null;
        trend.selectedCodes = [];
        trend.table.page = 1;
        updateTrendTabButtons();
        renderTrendOptions();
        handleTrendSelectChange();
    };

    const handleTrendSelectChange = () => {
        if (!elements.trendSelect) return;
        trend.table.page = 1;
        if (normalizeParameterType(trend.activeType) === 'water') {
            const selected = Array.from(elements.trendSelect.selectedOptions)
                .map(option => option.value)
                .filter(value => value);
            trend.selectedCodes = selected;
        } else {
            trend.selectedCode = elements.trendSelect.value || null;
        }
        loadParameterTrends();
    };

    const initTrendSection = () => {
        if (elements.trendSelect) {
            renderTrendOptions();
            elements.trendSelect.addEventListener('change', () => {
                handleTrendSelectChange();
            });
            handleTrendSelectChange();
        }
        renderTrendSourceOptions();
        updateTrendTabButtons();
        elements.trendTabButtons?.forEach(button => {
            button.addEventListener('click', () => {
                setTrendTab(button.dataset?.trendTab || 'water');
            });
        });

        const submitTrendFilter = (event) => {
            event.preventDefault();
            const startValue = elements.trendFilterStart?.value || null;
            const endValue = elements.trendFilterEnd?.value || null;
            const sourceValue = elements.trendFilterSource?.value || '';
            if (startValue && endValue && startValue > endValue) {
                alert('End month must be greater than or equal to start month.');
                return;
            }
            const parsedSource = sourceValue ? Number(sourceValue) : null;
            trend.filter.startMonth = startValue;
            trend.filter.endMonth = endValue;
            trend.filter.sourceId = Number.isFinite(parsedSource) ? parsedSource : null;
            trend.table.page = 1;
            loadParameterTrends();
        };

        elements.trendFilterForm?.addEventListener('submit', submitTrendFilter);
        elements.trendFilterReset?.addEventListener('click', () => {
            if (elements.trendFilterStart) elements.trendFilterStart.value = '';
            if (elements.trendFilterEnd) elements.trendFilterEnd.value = '';
            if (elements.trendFilterSource) elements.trendFilterSource.value = '';
            trend.filter.startMonth = null;
            trend.filter.endMonth = null;
            trend.filter.sourceId = null;
            trend.table.page = 1;
            loadParameterTrends();
        });

        elements.trendTablePrev?.addEventListener('click', () => {
            if (trend.table.page <= 1) return;
            trend.table.page -= 1;
            loadParameterTrends();
        });

        elements.trendTableNext?.addEventListener('click', () => {
            if (trend.table.page >= (trend.table.pagination.totalPages || 1)) return;
            trend.table.page += 1;
            loadParameterTrends();
        });

        elements.trendTablePageSize?.addEventListener('change', (event) => {
            const selected = Number(event.target.value);
            if (![6, 12].includes(selected)) {
                event.target.value = trend.table.pageSize;
                return;
            }
            if (selected === trend.table.pageSize) return;
            trend.table.pageSize = selected;
            trend.table.page = 1;
            loadParameterTrends();
        });

        loadParameterTrends();
    };

    const sanitizeTab = (tab) => (TAB_KEYS.includes(tab) ? tab : 'all');

    const getBodyForTab = (tab) => {
        if (tab === 'water') return elements.waterBody;
        if (tab === 'air') return elements.airBody;
        return elements.allBody;
    };

    const getColumnCount = (tab) => {
        const body = getBodyForTab(tab);
        return body?.closest('table')?.querySelectorAll('thead th').length ?? 7;
    };

    const setTableMessage = (tab, message, classes = 'text-gray-400') => {
        const body = getBodyForTab(tab);
        if (!body) return;
        const cols = getColumnCount(tab);
        body.innerHTML = `
            <tr>
                <td colspan="${cols}" class="px-3 py-6 text-center ${classes}">${message}</td>
            </tr>`;
    };

    const setLoadingState = (tab) => {
        const message = loadingMessages[tab] ?? 'Loading measurements...';
        setTableMessage(tab, message, 'text-gray-400');
    };

    const setErrorState = (tab, message) => {
        setTableMessage(tab, message, 'text-red-500');
    };

    const ensurePaginationState = (tab) => {
        if (!state.pagination[tab]) {
            state.pagination[tab] = { ...createEmptyPagination(), pageSize: state.pageSize };
        }
        return state.pagination[tab];
    };

    const buildSummaryFromItems = (items) => {
        const summary = { all: items.length, water: 0, air: 0 };
        items.forEach(item => {
            const normalizedType = (item.type || 'water').toLowerCase();
            if (normalizedType === 'air') summary.air += 1;
            else summary.water += 1;
        });
        return summary;
    };

    const normalizeResponsePayload = (payload, tab, requestedPagination, isLegacy) => {
        const paginationSeed = {
            page: requestedPagination?.page ?? 1,
            pageSize: requestedPagination?.pageSize ?? state.pageSize
        };

        if (isLegacy) {
            const normalizedItems = (payload || []).map(item => ({ ...item, type: item.type || 'water' }));
            const totalItems = normalizedItems.length;
            const pageSize = paginationSeed.pageSize || DEFAULT_PAGE_SIZE;
            const totalPages = Math.max(1, Math.ceil(totalItems / Math.max(pageSize, 1)));
            const safePage = Math.min(Math.max(paginationSeed.page || 1, 1), totalPages);
            const startIndex = (safePage - 1) * pageSize;
            const pagedItems = normalizedItems.slice(startIndex, startIndex + pageSize);
            const summary = tab === 'all' ? buildSummaryFromItems(normalizedItems) : null;

            return {
                items: pagedItems,
                pagination: {
                    page: safePage,
                    pageSize,
                    totalItems,
                    totalPages
                },
                summary
            };
        }

        const normalizedItems = Array.isArray(payload?.items)
            ? payload.items.map(item => ({ ...item, type: item.type || 'water' }))
            : [];

        const paginationData = payload?.pagination ?? {};
        const pageSizeValue = paginationData.pageSize ?? paginationSeed.pageSize ?? DEFAULT_PAGE_SIZE;
        const totalItems = paginationData.totalItems ?? normalizedItems.length;
        const fallbackTotalPages = Math.ceil(totalItems / Math.max(pageSizeValue, 1)) || 1;
        const computedTotalPages = paginationData.totalPages ?? fallbackTotalPages;
        const totalPages = Math.max(1, computedTotalPages);
        const requestedPage = paginationData.page ?? paginationSeed.page ?? 1;
        const safePage = Math.min(Math.max(requestedPage, 1), totalPages);

        return {
            items: normalizedItems,
            pagination: {
                page: safePage,
                pageSize: pageSizeValue,
                totalItems,
                totalPages
            },
            summary: payload?.summary ?? null
        };
    };

    const updateCountBadges = () => {
        if (elements.waterBadge) {
            const count = state.summary.water ?? 0;
            elements.waterBadge.textContent = `${count} ${count === 1 ? 'result' : 'results'}`;
        }
        if (elements.airBadge) {
            const count = state.summary.air ?? 0;
            elements.airBadge.textContent = `${count} ${count === 1 ? 'result' : 'results'}`;
        }
    };

    const renderTable = (tab) => {
        const body = getBodyForTab(tab);
        if (!body) return;
        const rows = state.datasets[tab] ?? [];
        if (!rows.length) {
            const label = tab === 'all' ? 'measurement data' : `${tab} measurements`;
            setTableMessage(tab, `No ${label} found.`);
            return;
        }

        body.innerHTML = rows.map(result => {
            const statusBadge = result.isApproved
                ? '<span class="px-2 py-0.5 rounded-full text-[11px] font-medium bg-green-50 text-green-600">Approved</span>'
                : '<span class="px-2 py-0.5 rounded-full text-[11px] font-medium bg-yellow-50 text-yellow-600">Pending</span>';

            const typeColumn = tab === 'all'
                ? `<td class=\"px-3 py-2 capitalize\">${result.type}</td>`
                : '';

            const approvalAction = permissions.canApprove
                ? `<button type="button"
                           class="w-7 h-7 flex items-center justify-center border rounded-md ${result.isApproved ? 'border-green-400 text-green-600 hover:bg-green-50' : 'border-gray-300 text-gray-500 hover:bg-gray-100'} transition result-approve-btn"
                           title="${result.isApproved ? 'Unapprove result' : 'Approve result'}"
                           data-id="${result.resultID}">
                        <i class="bi bi-check text-[10px]"></i>
                    </button>`
                : '';

            return `
                <tr class="hover:bg-gray-50 transition">
                    ${typeColumn}
                    <td class="px-3 py-2 truncate" title="${result.emissionSourceName ?? ''}">${result.emissionSourceName ?? '-'}</td>
                    <td class="px-3 py-2 truncate" title="${result.parameterName ?? ''}">${result.parameterName ?? result.parameterCode}</td>
                    <td class="px-3 py-2">${formatNumericValue(result.value)} ${result.unit ?? ''}</td>
                    <td class="px-3 py-2 text-xs text-gray-500">${formatDate(result.measurementDate)}</td>
                    <td class="px-3 py-2 text-center">${statusBadge}</td>
                    <td class="px-3 py-2 text-center">
                        <div class="flex items-center justify-center gap-2">
                            ${approvalAction}
                            <button type="button"
                                    class="w-7 h-7 flex items-center justify-center border border-blue-300 rounded-md text-blue-600 hover:bg-blue-100 transition result-edit-btn"
                                    title="Edit result" data-id="${result.resultID}">
                                <i class="bi bi-eye text-[10px]"></i>
                            </button>
                            <button type="button"
                                    class="w-7 h-7 flex items-center justify-center border border-red-400 text-red-500 rounded-md hover:bg-red-50 transition result-delete-btn"
                                    title="Delete result" data-id="${result.resultID}">
                                <i class="bi bi-trash text-[10px]"></i>
                            </button>
                        </div>
                    </td>
                </tr>`;
        }).join('');

        if (tab === state.activeTab) {
            updatePaginationBar();
        }
    };

    const updatePaginationBar = () => {
        if (!elements.paginationBar) return;
        const pagination = state.pagination[state.activeTab] ?? createEmptyPagination();
        const totalItems = pagination.totalItems ?? 0;
        const totalPages = pagination.totalPages ?? 1;
        const currentPage = pagination.page ?? 1;
        const pageSize = pagination.pageSize ?? state.pageSize;
        const start = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
        const end = totalItems === 0 ? 0 : Math.min(currentPage * pageSize, totalItems);
        if (elements.paginationSummary) {
            elements.paginationSummary.textContent = totalItems === 0
                ? `No ${state.activeTab === 'all' ? '' : `${state.activeTab} `}results`
                : `Showing ${start}–${end} of ${totalItems} ${state.activeTab === 'all' ? '' : `${state.activeTab} `}results`;
        }
        if (elements.paginationPageLabel) {
            elements.paginationPageLabel.textContent = `Page ${currentPage} of ${totalPages || 1}`;
        }
        if (elements.paginationPrev) {
            elements.paginationPrev.disabled = currentPage <= 1;
        }
        if (elements.paginationNext) {
            elements.paginationNext.disabled = currentPage >= (totalPages || 1);
        }
        if (elements.pageSizeSelect && Number(elements.pageSizeSelect.value) !== state.pageSize) {
            elements.pageSizeSelect.value = state.pageSize;
        }
    };

    const showTab = (tabName) => {
        const tab = sanitizeTab(tabName);
        state.activeTab = tab;
        elements.tabButtons.forEach(btn => {
            const isActive = btn.dataset.tab === tab;
            btn.classList.toggle('text-blue-600', isActive);
            btn.classList.toggle('border-blue-600', isActive);
            btn.classList.toggle('border-transparent', !isActive);
            btn.classList.toggle('text-gray-500', !isActive);
        });
        Object.entries(elements.tabPanels).forEach(([key, panel]) => {
            panel?.classList.toggle('hidden', key !== tab);
        });

        if (state.loadedTabs.has(tab)) {
            renderTable(tab);
            updatePaginationBar();
        } else {
            setLoadingState(tab);
            loadResults(tab);
        }
    };

    const handleErrorResponse = async (response) => {
        let message = `Request failed (${response.status})`;
        try {
            const payload = await response.json();
            if (payload?.message) message = payload.message;
            else if (payload?.error) message = payload.error;
        } catch {}
        throw new Error(message);
    };

    const appendSearchAndFilters = (params) => {
        if (!params) return;
        if (state.searchQuery) {
            params.set('search', state.searchQuery);
        }
        const filters = state.filters ?? createDefaultFilters();
        if (filters.sourceId != null) {
            params.set('sourceId', filters.sourceId.toString());
        }
        if (filters.parameterCode) {
            params.set('parameterCode', filters.parameterCode);
        }
        if (filters.status) {
            params.set('status', filters.status);
        }
        if (filters.startDate) {
            params.set('startDate', filters.startDate);
        }
        if (filters.endDate) {
            params.set('endDate', filters.endDate);
        }
    };

    const buildListUrl = (tab) => {
        const target = sanitizeTab(tab);
        const params = new URLSearchParams();
        params.set('paged', 'true');
        const pagination = ensurePaginationState(target);
        params.set('page', (pagination.page ?? 1).toString());
        params.set('pageSize', state.pageSize.toString());
        if (target !== 'all') {
            params.set('type', target);
        }
        appendSearchAndFilters(params);
        return `${routes.list}${routes.list.includes('?') ? '&' : '?'}${params.toString()}`;
    };

    const loadResults = async (tab = state.activeTab) => {
        if (!routes.list) return;
        const targetTab = sanitizeTab(tab);
        const paginationSeed = ensurePaginationState(targetTab);
        try {
            const res = await fetch(buildListUrl(targetTab), { credentials: 'same-origin' });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || 'Failed to load measurement data.');
            const payload = unwrapApiResponse(json);
            const isLegacyResponse = Array.isArray(payload);
            const normalized = normalizeResponsePayload(payload, targetTab, paginationSeed, isLegacyResponse);
            state.datasets[targetTab] = normalized.items;
            state.pagination[targetTab] = normalized.pagination;

            if (normalized.summary) {
                state.summary = {
                    all: normalized.summary.all ?? state.summary.all,
                    water: normalized.summary.water ?? state.summary.water,
                    air: normalized.summary.air ?? state.summary.air
                };
            } else if (isLegacyResponse) {
                const totalItems = normalized.pagination.totalItems ?? normalized.items.length;
                if (targetTab === 'water') {
                    state.summary.water = totalItems;
                } else if (targetTab === 'air') {
                    state.summary.air = totalItems;
                } else if (targetTab === 'all') {
                    state.summary.all = totalItems;
                }
            }

            state.loadedTabs.add(targetTab);
            renderTable(targetTab);
            if (targetTab === state.activeTab) {
                updatePaginationBar();
            }
            updateCountBadges();
        } catch (error) {
            console.error(error);
            setErrorState(targetTab, error.message || 'Failed to load measurement data.');
        }
    };

    const refreshActiveTab = async (resetPage = false) => {
        const active = sanitizeTab(state.activeTab);
        const pagination = ensurePaginationState(active);
        if (resetPage) {
            pagination.page = 1;
        }
        await loadResults(active);
        state.loadedTabs = new Set([active]);
    };

    const applyResultsSearch = (query) => {
        const trimmed = (query || '').trim();
        state.searchQuery = trimmed;
        TAB_KEYS.forEach(tab => {
            const pagination = ensurePaginationState(tab);
            pagination.page = 1;
        });
        state.loadedTabs = new Set();
        setLoadingState(state.activeTab);
        loadResults(state.activeTab);
    };

    const collectPayload = (mode = 'add') => {
        const form = mode === 'add' ? addForm : editForm;

        const approvedAtIso = form.approvedAt?.value
            ? new Date(form.approvedAt.value).toISOString()
            : null;
        const parameterMeta = findParameterMeta(form.parameter.value);
        const measurementDateIso = mode === 'add'
            ? new Date().toISOString()
            : (form.date?.value ? new Date(form.date.value).toISOString() : null);

        return {
            emissionSourceId: Number(form.source.value),
            parameterCode: form.parameter.value,
            value: form.value.value === '' ? null : Number(form.value.value),
            unit: parameterMeta?.unit ?? null,
            measurementDate: measurementDateIso,
            isApproved: Boolean(approvedAtIso),
            approvedAt: approvedAtIso,
            remark: form.remark.value || null
        };
    };

    const createResult = async () => {
        try {
            const payload = collectPayload('add');
            const res = await fetch(routes.create, {
                method: 'POST',
                credentials: 'same-origin',
                headers: withAntiForgery({
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }),
                body: JSON.stringify(payload)
            });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || json?.error || 'Failed to create measurement result.');
            await refreshActiveTab(true);
            toggleAppModal(elements.addModal, false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to create measurement result.');
        }
    };

    const loadDetail = async (id) => {
        const res = await fetch(`${routes.detail}/${encodeURIComponent(id)}`, { credentials: 'same-origin' });
        if (!res.ok) await handleErrorResponse(res);
        const json = await res.json();
        if (json?.success === false) throw new Error(json?.message || 'Failed to load measurement result detail.');
        return unwrapApiResponse(json);
    };

    const openEditModal = async (id, focusApproval = false) => {
        try {
            const data = await loadDetail(id);
            editForm.id.value = data.resultID;
            editForm.source.value = data.emissionSourceID;
            editForm.parameter.value = data.parameterCode;
            editForm.value.value = data.value ?? '';
            editForm.date.value = formatInputDate(data.measurementDate);
            editForm.approvedAt.value = formatInputDate(data.approvedAt);
            editForm.remark.value = data.remark ?? '';
            syncApprovalCheckbox(editForm.approvedCheckbox, editForm.approvedAt);
            if (focusApproval && editForm.approvedCheckbox) {
                editForm.approvedCheckbox.focus();
            }
            toggleAppModal(elements.editModal, true);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to load measurement result detail.');
        }
    };

    const updateResult = async () => {
        const id = editForm.id.value;
        if (!id) return;
        try {
            const payload = collectPayload('edit');
            const res = await fetch(`${routes.update}/${encodeURIComponent(id)}`, {
                method: 'PUT',
                credentials: 'same-origin',
                headers: withAntiForgery({
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }),
                body: JSON.stringify(payload)
            });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || json?.error || 'Failed to update measurement result.');
            await refreshActiveTab(false);
            toggleAppModal(elements.editModal, false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to update measurement result.');
        }
    };

    const deleteResult = async (id) => {
        if (!id || !confirm('Delete this measurement result?')) return;
        try {
            const res = await fetch(`${routes.delete}/${encodeURIComponent(id)}`, {
                method: 'DELETE',
                credentials: 'same-origin',
                headers: withAntiForgery({ 'X-Requested-With': 'XMLHttpRequest' })
            });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || json?.error || 'Failed to delete measurement result.');
            await refreshActiveTab(false);
            toggleAppModal(elements.editModal, false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to delete measurement result.');
        }
    };

    const setImportStep = (step) => {
        importState.step = step;
        const isUpload = step === 'upload';
        elements.importStepUpload?.classList.toggle('hidden', !isUpload);
        elements.importStepPreview?.classList.toggle('hidden', isUpload);
        elements.previewImportBtn?.classList.toggle('hidden', !isUpload);
        elements.confirmImportBtn?.classList.toggle('hidden', isUpload);
        elements.backImportBtn?.classList.toggle('hidden', isUpload);
        elements.importStepUploadLabel?.classList.toggle('hidden', !isUpload);
        elements.importStepPreviewLabel?.classList.toggle('hidden', isUpload);
        if (elements.confirmImportBtn) {
            elements.confirmImportBtn.disabled = importState.validRows === 0;
        }
    };

    const renderImportSummary = () => {
        if (!elements.importPreviewSummary || !elements.importPreviewHint) return;
        if (!importState.totalRows) {
            elements.importPreviewSummary.textContent = 'No rows detected yet.';
            elements.importPreviewHint.textContent = 'Upload a file and run the preview before importing.';
            if (elements.confirmImportBtn) {
                elements.confirmImportBtn.disabled = true;
            }
            return;
        }

        elements.importPreviewSummary.textContent =
            `${importState.validRows} valid · ${importState.invalidRows} invalid · ${importState.totalRows} total`;
        elements.importPreviewHint.textContent = importState.invalidRows > 0
            ? 'Invalid rows will be skipped. Update the spreadsheet if you need to import them.'
            : 'All rows look valid. Click “Import rows” to insert them.';
        if (elements.confirmImportBtn) {
            elements.confirmImportBtn.disabled = importState.validRows === 0;
        }
    };

    const renderImportPreviewTable = () => {
        if (!elements.importPreviewTableBody) return;
        if (!importState.rows.length) {
            elements.importPreviewTableBody.innerHTML = `
                <tr>
                    <td colspan="6" class="px-3 py-6 text-center text-gray-400">
                        Upload a file and click “Preview data” to inspect the rows before importing.
                    </td>
                </tr>
            `;
            return;
        }

        const rowsHtml = importState.rows.map(row => {
            const isValid = row?.isValid !== false;
            const sourceText = row?.emissionSourceName
                ? escapeHtml(row.emissionSourceName)
                : (row?.emissionSourceId ? `Source #${row.emissionSourceId}` : '-');
            const parameterCode = row?.parameterCode || '';
            const parameterText = row?.parameterName
                ? `${escapeHtml(row.parameterName)} (${escapeHtml(parameterCode)})`
                : escapeHtml(parameterCode);
            const status = !isValid && Array.isArray(row?.errors) && row.errors.length
                ? `<ul class="list-disc ml-4 text-red-600 space-y-0.5">${row.errors.map(err => `<li>${escapeHtml(err)}</li>`).join('')}</ul>`
                : '<span class="text-green-600 font-semibold">Ready</span>';

            return `
                <tr class="${isValid ? '' : 'bg-red-50'}">
                    <td class="px-3 py-2 text-gray-500 font-mono">${row?.rowNumber ?? '-'}</td>
                    <td class="px-3 py-2">${sourceText}</td>
                    <td class="px-3 py-2">${parameterText}</td>
                    <td class="px-3 py-2">${formatNumericValue(row?.value)}</td>
                    <td class="px-3 py-2">${formatDate(row?.measurementDate)}</td>
                    <td class="px-3 py-2">${status}</td>
                </tr>
            `;
        }).join('');

        elements.importPreviewTableBody.innerHTML = rowsHtml;
    };

    const resetImportState = () => {
        importState.file = null;
        importState.rows = [];
        importState.totalRows = 0;
        importState.validRows = 0;
        importState.invalidRows = 0;
        importState.step = 'upload';
        importState.sourceId = null;
        if (elements.importFileInput) {
            elements.importFileInput.value = '';
        }
        if (elements.importFileLabel) {
            elements.importFileLabel.textContent = 'Choose file';
        }
        if (elements.importSourceSelect) {
            elements.importSourceSelect.value = '';
        }
        renderImportPreviewTable();
        renderImportSummary();
        setImportStep('upload');
    };

    const handleImportFileChange = (event) => {
        const file = event?.target?.files?.[0] ?? null;
        importState.file = file;
        if (elements.importFileLabel) {
            elements.importFileLabel.textContent = file ? file.name : 'Choose file';
        }
    };

    const requestImportPreview = async () => {
        if (!routes.importPreview) {
            alert('Import preview endpoint is not configured.');
            return;
        }
        if (!importState.file) {
            alert('Please choose an Excel file before running the preview.');
            return;
        }
        if (!Number.isFinite(importState.sourceId)) {
            alert('Select an emission source for this import.');
            return;
        }

        try {
            setButtonBusy(elements.previewImportBtn, true, 'Processing...');
            const formData = new FormData();
            formData.append('file', importState.file);
            formData.append('emissionSourceId', importState.sourceId.toString());
            const res = await fetch(routes.importPreview, {
                method: 'POST',
                credentials: 'same-origin',
                headers: withAntiForgery({ 'X-Requested-With': 'XMLHttpRequest' }),
                body: formData
            });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || 'Failed to preview the import file.');
            const data = unwrapApiResponse(json) || {};
            importState.rows = Array.isArray(data.rows) ? data.rows : [];
            importState.totalRows = Number(data.totalRows) || importState.rows.length;
            importState.validRows = Number(data.validRows) || importState.rows.filter(row => row?.isValid !== false).length;
            importState.invalidRows = Number(data.invalidRows);
            if (!Number.isFinite(importState.invalidRows)) {
                importState.invalidRows = Math.max(importState.totalRows - importState.validRows, 0);
            }
            renderImportPreviewTable();
            renderImportSummary();
            setImportStep('preview');
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to preview the import file.');
        } finally {
            setButtonBusy(elements.previewImportBtn, false);
        }
    };

    const confirmImportRows = async () => {
        if (!routes.importConfirm) {
            alert('Import confirm endpoint is not configured.');
            return;
        }
        const validRows = (importState.rows || []).filter(row => row?.isValid !== false);
        if (validRows.length === 0) {
            alert('No valid rows to import.');
            return;
        }

        try {
            setButtonBusy(elements.confirmImportBtn, true, 'Importing...');
            const payload = {
                rows: validRows.map(row => ({
                    rowNumber: row.rowNumber,
                    emissionSourceId: row.emissionSourceId,
                    parameterCode: row.parameterCode,
                    measurementDate: row.measurementDate,
                    entryDate: row.entryDate,
                    value: row.value,
                    unit: row.unit,
                    remark: row.remark,
                    isApproved: row.isApproved,
                    approvedAt: row.approvedAt
                }))
            };
            const res = await fetch(routes.importConfirm, {
                method: 'POST',
                credentials: 'same-origin',
                headers: withAntiForgery({
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }),
                body: JSON.stringify(payload)
            });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || 'Failed to import measurement results.');
            alert(json?.message || 'Import completed successfully.');
            closeImportModal();
            await refreshActiveTab(true);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to import measurement results.');
        } finally {
            setButtonBusy(elements.confirmImportBtn, false);
        }
    };

    const openImportModal = () => {
        resetImportState();
        toggleAppModal(elements.importModal, true);
    };

    const closeImportModal = () => {
        toggleAppModal(elements.importModal, false);
        resetImportState();
    };

    const buildApprovalTogglePayload = (result, targetState) => {
        if (!result) return null;
        const emissionSourceId = Number(result.emissionSourceID);
        if (!Number.isFinite(emissionSourceId)) return null;
        const measurementDateIso = toIsoStringOrNull(result.measurementDate) ?? new Date().toISOString();
        const approvedAtIso = targetState
            ? (toIsoStringOrNull(result.approvedAt) ?? new Date().toISOString())
            : null;

        const remarkValue = result.remark;
        const normalizedRemark = remarkValue === undefined || remarkValue === null || remarkValue === ''
            ? null
            : remarkValue;

        return {
            emissionSourceId,
            parameterCode: result.parameterCode,
            value: toNumericOrNull(result.value),
            unit: typeof result.unit === 'string' && result.unit !== '' ? result.unit : null,
            measurementDate: measurementDateIso,
            isApproved: targetState,
            approvedAt: approvedAtIso,
            remark: normalizedRemark
        };
    };

    const toggleResultApproval = async (buttonElement) => {
        const resultId = buttonElement?.dataset?.id;
        if (!resultId) return;
        const result = findResultInState(resultId);
        if (!result) {
            alert('Unable to locate measurement data for this action.');
            return;
        }
        const targetState = !result.isApproved;
        const payload = buildApprovalTogglePayload(result, targetState);
        if (!payload) {
            alert('Unable to prepare the approval request.');
            return;
        }

        if (buttonElement) {
            buttonElement.disabled = true;
            buttonElement.classList.add('opacity-50', 'pointer-events-none');
        }

        try {
            const res = await fetch(`${routes.update}/${encodeURIComponent(resultId)}`, {
                method: 'PUT',
                credentials: 'same-origin',
                headers: withAntiForgery({
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }),
                body: JSON.stringify(payload)
            });
            if (!res.ok) await handleErrorResponse(res);
            const json = await res.json();
            if (json?.success === false) throw new Error(json?.message || json?.error || 'Failed to update approval status.');
            await refreshActiveTab(false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to update approval status.');
        } finally {
            if (buttonElement) {
                buttonElement.disabled = false;
                buttonElement.classList.remove('opacity-50', 'pointer-events-none');
            }
        }
    };

    const resetAddForm = () => {
        addForm.source.selectedIndex = 0;
        addForm.parameter.selectedIndex = 0;
        addForm.value.value = '';
        if (addForm.date) {
            addForm.date.value = '';
        }
        addForm.approvedAt.value = '';
        addForm.remark.value = '';
        if (addForm.approvedCheckbox) {
            addForm.approvedCheckbox.checked = false;
        }
    };

    const initTabs = () => {
        elements.tabButtons.forEach(btn => {
            btn.addEventListener('click', () => showTab(btn.dataset.tab));
        });
        showTab('all');
    };

    const initSelects = () => {
        renderOptions(addForm.source, lookups.emissionSources ?? [], 'id', 'label');
        renderOptions(editForm.source, lookups.emissionSources ?? [], 'id', 'label');
        renderOptions(addForm.parameter, lookups.parameters ?? [], 'code', 'label');
        renderOptions(editForm.parameter, lookups.parameters ?? [], 'code', 'label');
        renderFilterSelects();
        renderImportSourceOptions();
    };

    const tableClickHandler = (event) => {
        const approveBtn = event.target.closest('.result-approve-btn');
        if (approveBtn) {
            toggleResultApproval(approveBtn);
            return;
        }
        const editBtn = event.target.closest('.result-edit-btn');
        if (editBtn) {
            openEditModal(editBtn.dataset.id);
            return;
        }

        const deleteBtn = event.target.closest('.result-delete-btn');
        if (deleteBtn) {
            deleteResult(deleteBtn.dataset.id);
        }
    };

    elements.openAddBtn?.addEventListener('click', () => {
        resetAddForm();
        toggleAppModal(elements.addModal, true);
    });
    elements.closeAddBtn?.addEventListener('click', () => toggleAppModal(elements.addModal, false));
    elements.cancelAddBtn?.addEventListener('click', () => toggleAppModal(elements.addModal, false));
    elements.saveAddBtn?.addEventListener('click', createResult);

    elements.closeEditBtn?.addEventListener('click', () => toggleAppModal(elements.editModal, false));
    elements.cancelEditBtn?.addEventListener('click', () => toggleAppModal(elements.editModal, false));
    elements.updateEditBtn?.addEventListener('click', updateResult);
    elements.deleteEditBtn?.addEventListener('click', () => deleteResult(editForm.id.value));

    elements.openImportBtn?.addEventListener('click', openImportModal);
    elements.closeImportBtn?.addEventListener('click', closeImportModal);
    elements.cancelImportBtn?.addEventListener('click', closeImportModal);
    elements.backImportBtn?.addEventListener('click', () => setImportStep('upload'));
    elements.previewImportBtn?.addEventListener('click', requestImportPreview);
    elements.confirmImportBtn?.addEventListener('click', confirmImportRows);
    elements.importFileInput?.addEventListener('change', handleImportFileChange);
    elements.importSourceSelect?.addEventListener('change', handleImportSourceChange);

    elements.openFilterBtn?.addEventListener('click', () => {
        setFilterFormValues();
        toggleAppModal(elements.filterModal, true);
    });
    elements.closeFilterBtn?.addEventListener('click', () => toggleAppModal(elements.filterModal, false));
    elements.cancelFilterBtn?.addEventListener('click', () => toggleAppModal(elements.filterModal, false));
    elements.applyFilterBtn?.addEventListener('click', () => {
        const values = readFilterFormValues();
        if (values.startDate && values.endDate && values.startDate > values.endDate) {
            alert('End date must be greater than or equal to start date.');
            return;
        }
        applyAdvancedFilters(values);
        toggleAppModal(elements.filterModal, false);
    });
    elements.resetFilterBtn?.addEventListener('click', () => {
        const defaults = createDefaultFilters();
        const hadFilters = countActiveFilters();
        setFilterFormValues(defaults);
        if (hadFilters > 0) {
            applyAdvancedFilters(defaults);
        } else {
            state.filters = defaults;
            updateFilterBadge();
        }
        toggleAppModal(elements.filterModal, false);
    });

    registerModalDismiss(elements.addModal, () => toggleAppModal(elements.addModal, false));
    registerModalDismiss(elements.editModal, () => toggleAppModal(elements.editModal, false));
    registerModalDismiss(elements.filterModal, () => toggleAppModal(elements.filterModal, false));
    registerModalDismiss(elements.importModal, closeImportModal);

    if (elements.importModal) {
        resetImportState();
    }

    elements.refreshBtn?.addEventListener('click', () => {
        setLoadingState(state.activeTab);
        loadResults(state.activeTab);
    });
    elements.exportBtn?.addEventListener('click', () => {
        const targetRoute = routes.export || routes.list;
        if (!targetRoute) return;
        const params = new URLSearchParams();
        if (state.activeTab !== 'all') {
            params.set('type', state.activeTab);
        }
        appendSearchAndFilters(params);
        if (!routes.export) {
            params.set('paged', 'false');
        }
        const url = `${targetRoute}${targetRoute.includes('?') ? '&' : '?'}${params.toString()}`;
        window.open(url, '_blank');
    });

    elements.paginationPrev?.addEventListener('click', () => {
        const pagination = ensurePaginationState(state.activeTab);
        if (pagination.page <= 1) return;
        pagination.page -= 1;
        setLoadingState(state.activeTab);
        loadResults(state.activeTab);
    });
    elements.paginationNext?.addEventListener('click', () => {
        const pagination = ensurePaginationState(state.activeTab);
        if (pagination.page >= pagination.totalPages) return;
        pagination.page += 1;
        setLoadingState(state.activeTab);
        loadResults(state.activeTab);
    });
    elements.pageSizeSelect?.addEventListener('change', (event) => {
        const newSize = Number(event.target.value);
        if (!Number.isFinite(newSize) || newSize <= 0) return;
        state.pageSize = newSize;
        TAB_KEYS.forEach(tab => {
            const pagination = ensurePaginationState(tab);
            pagination.page = 1;
            pagination.pageSize = newSize;
            if (tab !== state.activeTab) {
                state.loadedTabs.delete(tab);
            }
        });
        setLoadingState(state.activeTab);
        loadResults(state.activeTab);
    });

    const bindSearchControls = () => {
        const readValue = () => elements.resultsSearchInput?.value || '';
        let searchDebounce = null;
        const triggerSearch = () => applyResultsSearch(readValue());
        elements.resultsSearchInput?.addEventListener('input', () => {
            if (searchDebounce) clearTimeout(searchDebounce);
            searchDebounce = setTimeout(triggerSearch, 300);
        });
        elements.resultsSearchReset?.addEventListener('click', () => {
            if (elements.resultsSearchInput) {
                elements.resultsSearchInput.value = '';
            }
            if (searchDebounce) {
                clearTimeout(searchDebounce);
                searchDebounce = null;
            }

            const defaults = createDefaultFilters();
            const hadFilters = countActiveFilters() > 0;
            const hadSearch = !!state.searchQuery;

            setFilterFormValues(defaults);
            state.filters = defaults;
            updateFilterBadge();

            if (hadFilters) {
                state.searchQuery = '';
                applyAdvancedFilters(defaults);
            } else if (hadSearch) {
                applyResultsSearch('');
            } else {
                state.searchQuery = '';
            }
        });
    };

    bindSearchControls();

    elements.allBody?.addEventListener('click', tableClickHandler);
    elements.waterBody?.addEventListener('click', tableClickHandler);
    elements.airBody?.addEventListener('click', tableClickHandler);
    initTabs();
    initSelects();
    updateFilterBadge();
    initTrendSection();
})();
