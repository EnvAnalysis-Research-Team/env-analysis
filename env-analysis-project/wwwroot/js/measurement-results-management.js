'use strict';

(function () {
    const routes = window.measurementResultRoutes || {};
    const lookups = window.measurementResultLookups || { emissionSources: [], parameters: [] };

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
        }
    };

    const addForm = {
        source: document.getElementById('addResultSource'),
        parameter: document.getElementById('addResultParameter'),
        value: document.getElementById('addResultValue'),
        unit: document.getElementById('addResultUnit'),
        date: document.getElementById('addResultDate'),
        status: document.getElementById('addResultStatus'),
        approvedAt: document.getElementById('addResultApprovedAt'),
        remark: document.getElementById('addResultRemark')
    };

    const editForm = {
        id: document.getElementById('editResultId'),
        source: document.getElementById('editResultSource'),
        parameter: document.getElementById('editResultParameter'),
        value: document.getElementById('editResultValue'),
        unit: document.getElementById('editResultUnit'),
        date: document.getElementById('editResultDate'),
        status: document.getElementById('editResultStatus'),
        approvedAt: document.getElementById('editResultApprovedAt'),
        remark: document.getElementById('editResultRemark')
    };

    const state = {
        results: [],
        activeTab: 'all'
    };

    const MODAL_ANIMATION_MS = 200;
    const toggleModal = (modal, show) => {
        if (!modal) return;
        if (show) {
            modal.classList.remove('hidden');
            modal.classList.add('flex');
            requestAnimationFrame(() => {
                modal.classList.remove('-translate-y-5', 'opacity-0');
                modal.classList.add('translate-y-0', 'opacity-100');
            });
        } else {
            modal.classList.remove('translate-y-0', 'opacity-100');
            modal.classList.add('-translate-y-5', 'opacity-0');
            setTimeout(() => {
                modal.classList.add('hidden');
                modal.classList.remove('flex');
            }, MODAL_ANIMATION_MS);
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

    const renderOptions = (select, items, valueKey, labelKey) => {
        if (!select) return;
        select.innerHTML = items.map(item => `<option value="${item[valueKey]}">${item[labelKey]}</option>`).join('');
    };

    const renderTables = () => {
        const renderForType = (type, body, badge) => {
            const rows = type === 'all' ? state.results : state.results.filter(r => r.type === type);
            if (badge) badge.textContent = `${rows.length} ${rows.length === 1 ? 'result' : 'results'}`;

            if (!body) return;
            if (!rows.length) {
                const cols = body.closest('table')?.querySelectorAll('thead th').length ?? 7;
                body.innerHTML = `<tr><td colspan="${cols}" class="px-3 py-6 text-center text-gray-400">No ${type} measurements found.</td></tr>`;
                return;
            }

            body.innerHTML = rows.map(result => {
                const statusBadge = result.isApproved
                    ? '<span class="px-2 py-0.5 rounded-full text-[11px] font-medium bg-green-50 text-green-600">Approved</span>'
                    : '<span class="px-2 py-0.5 rounded-full text-[11px] font-medium bg-yellow-50 text-yellow-600">Pending</span>';

                const typeColumn = type === 'all'
                    ? `<td class="px-3 py-2 capitalize">${result.type}</td>`
                    : '';

                return `
                    <tr class="hover:bg-gray-50 transition">
                        <td class="px-3 py-2 font-medium text-gray-900">#${result.resultID}</td>
                        ${typeColumn}
                        <td class="px-3 py-2 truncate" title="${result.emissionSourceName ?? ''}">${result.emissionSourceName ?? '-'}</td>
                        <td class="px-3 py-2 truncate" title="${result.parameterName ?? ''}">${result.parameterName ?? result.parameterCode}</td>
                        <td class="px-3 py-2">${result.value ?? '-'} ${result.unit ?? ''}</td>
                        <td class="px-3 py-2 text-xs text-gray-500">${formatDate(result.measurementDate)}</td>
                        <td class="px-3 py-2 text-center">${statusBadge}</td>
                        <td class="px-3 py-2 text-center">
                            <div class="flex items-center justify-center gap-2">
                                <button type="button"
                                        class="w-7 h-7 flex items-center justify-center border border-blue-300 rounded-md text-blue-600 hover:bg-blue-100 transition result-edit-btn"
                                        title="Edit result" data-id="${result.resultID}">
                                    <i class="bi bi-pencil text-[10px]"></i>
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
        };

        renderForType('all', elements.allBody);
        renderForType('water', elements.waterBody, elements.waterBadge);
        renderForType('air', elements.airBody, elements.airBadge);
    };

    const showTab = (tabName) => {
        state.activeTab = tabName;
        elements.tabButtons.forEach(btn => {
            const isActive = btn.dataset.tab === tabName;
            btn.classList.toggle('text-blue-600', isActive);
            btn.classList.toggle('border-blue-600', isActive);
            btn.classList.toggle('border-transparent', !isActive);
            btn.classList.toggle('text-gray-500', !isActive);
        });
        Object.entries(elements.tabPanels).forEach(([key, panel]) => {
            panel?.classList.toggle('hidden', key !== tabName);
        });
    };

    const handleErrorResponse = async (response) => {
        let message = `Request failed (${response.status})`;
        try {
            const payload = await response.json();
            if (payload?.error) message = payload.error;
        } catch {}
        throw new Error(message);
    };

    const loadResults = async () => {
        try {
            const res = await fetch(routes.list, { credentials: 'same-origin' });
            if (!res.ok) await handleErrorResponse(res);
            const data = await res.json();
            state.results = Array.isArray(data) ? data.map(d => ({ ...d, type: d.type || 'water' })) : [];
            renderTables();
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to load measurement results.');
        }
    };

    const collectPayload = (mode = 'add') => {
        const form = mode === 'add' ? addForm : editForm;
        const typeName = mode === 'add' ? 'addResultType' : 'editResultType';
        const checkedType = document.querySelector(`input[name="${typeName}"]:checked`)?.value || 'water';

        return {
            type: checkedType,
            emissionSourceId: Number(form.source.value),
            parameterCode: form.parameter.value,
            value: form.value.value === '' ? null : Number(form.value.value),
            unit: form.unit.value || null,
            measurementDate: form.date.value ? new Date(form.date.value).toISOString() : null,
            isApproved: (form.status.value || 'Approved').toLowerCase() === 'approved',
            approvedAt: form.approvedAt.value ? new Date(form.approvedAt.value).toISOString() : null,
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
            if (!json?.success) throw new Error(json?.error || 'Failed to create measurement result.');
            state.results.unshift(json.data);
            renderTables();
            toggleModal(elements.addModal, false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to create measurement result.');
        }
    };

    const loadDetail = async (id) => {
        const res = await fetch(`${routes.detail}/${encodeURIComponent(id)}`, { credentials: 'same-origin' });
        if (!res.ok) await handleErrorResponse(res);
        return res.json();
    };

    const openEditModal = async (id) => {
        try {
            const data = await loadDetail(id);
            editForm.id.value = data.resultID;
            editForm.source.value = data.emissionSourceID;
            editForm.parameter.value = data.parameterCode;
            editForm.value.value = data.value ?? '';
            editForm.unit.value = data.unit ?? '';
            editForm.date.value = formatInputDate(data.measurementDate);
            editForm.status.value = data.isApproved ? 'Approved' : 'Pending';
            editForm.approvedAt.value = formatInputDate(data.approvedAt);
            editForm.remark.value = data.remark ?? '';
            document.querySelectorAll('input[name="editResultType"]').forEach(radio => {
                radio.checked = radio.value === (data.type || 'water');
            });
            toggleModal(elements.editModal, true);
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
            if (!json?.success) throw new Error(json?.error || 'Failed to update measurement result.');
            const index = state.results.findIndex(r => r.resultID === Number(id));
            if (index !== -1) {
                state.results[index] = json.data;
            } else {
                state.results.unshift(json.data);
            }
            renderTables();
            toggleModal(elements.editModal, false);
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
            if (!json?.success) throw new Error(json?.error || 'Failed to delete measurement result.');
            state.results = state.results.filter(r => r.resultID !== Number(id));
            renderTables();
            toggleModal(elements.editModal, false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to delete measurement result.');
        }
    };

    const resetAddForm = () => {
        document.querySelector('input[name="addResultType"][value="water"]').checked = true;
        addForm.source.selectedIndex = 0;
        addForm.parameter.selectedIndex = 0;
        addForm.value.value = '';
        addForm.unit.value = '';
        addForm.date.value = '';
        addForm.status.value = 'Approved';
        addForm.approvedAt.value = '';
        addForm.remark.value = '';
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
    };

    const tableClickHandler = (event) => {
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
        toggleModal(elements.addModal, true);
    });
    elements.closeAddBtn?.addEventListener('click', () => toggleModal(elements.addModal, false));
    elements.cancelAddBtn?.addEventListener('click', () => toggleModal(elements.addModal, false));
    elements.saveAddBtn?.addEventListener('click', createResult);

    elements.closeEditBtn?.addEventListener('click', () => toggleModal(elements.editModal, false));
    elements.cancelEditBtn?.addEventListener('click', () => toggleModal(elements.editModal, false));
    elements.updateEditBtn?.addEventListener('click', updateResult);
    elements.deleteEditBtn?.addEventListener('click', () => deleteResult(editForm.id.value));

    elements.refreshBtn?.addEventListener('click', loadResults);
    elements.exportBtn?.addEventListener('click', () => {
        const url = `${routes.list}?type=${encodeURIComponent(state.activeTab)}`;
        window.open(url, '_blank');
    });

    elements.allBody?.addEventListener('click', tableClickHandler);
    elements.waterBody?.addEventListener('click', tableClickHandler);
    elements.airBody?.addEventListener('click', tableClickHandler);

    initTabs();
    initSelects();
    loadResults();
})();
