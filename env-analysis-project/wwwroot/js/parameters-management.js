'use strict';

(function () {
    const routes = window.parameterRoutes || {};
    const resolveRoute = (key, id) => {
        if (!routes[key]) {
            throw new Error(`Route '${key}' is not configured.`);
        }
        return id ? `${routes[key]}/${encodeURIComponent(id)}` : routes[key];
    };

    const tableBody = document.getElementById('parameterTableBody');
    const searchInput = document.getElementById('parameterSearchInput');

    const addModal = document.getElementById('addParameterModal');
    const openAddBtn = document.getElementById('openAddParameterBtn');
    const closeAddBtn = document.getElementById('closeAddParameterBtn');
    const cancelAddBtn = document.getElementById('cancelAddParameterBtn');
    const saveAddBtn = document.getElementById('saveParameterBtn');

    const editModal = document.getElementById('editParameterModal');
    const closeEditBtn = document.getElementById('closeEditParameterBtn');
    const cancelEditBtn = document.getElementById('cancelEditParameterBtn');
    const updateBtn = document.getElementById('updateParameterBtn');
    const deleteBtn = document.getElementById('deleteParameterBtn');

    const state = {
        parameters: [],
        filtered: []
    };

    const formatDate = (value) => {
        if (!value) return '-';
        try {
            return new Date(value).toLocaleString();
        } catch {
            return value;
        }
    };

    const unwrapApiResponse = (payload) => {
        if (!payload || typeof payload !== 'object') return payload;
        return Object.prototype.hasOwnProperty.call(payload, 'data') ? payload.data : payload;
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

    const resetAddFields = () => {
        document.getElementById('addParameterCode').value = '';
        document.getElementById('addParameterName').value = '';
        document.getElementById('addParameterUnit').value = '';
        document.getElementById('addParameterStandard').value = '';
        document.getElementById('addParameterDescription').value = '';
    };

    const fillEditFields = (data) => {
        document.getElementById('editParameterCode').value = data.parameterCode;
        document.getElementById('editParameterCodeDisplay').value = data.parameterCode;
        document.getElementById('editParameterName').value = data.parameterName ?? '';
        document.getElementById('editParameterUnit').value = data.unit ?? '';
        document.getElementById('editParameterStandard').value = data.standardValue ?? '';
        document.getElementById('editParameterDescription').value = data.description ?? '';
    };

    const renderRows = (rows) => {
        if (!tableBody) return;
        if (!rows.length) {
            tableBody.innerHTML = '<tr><td colspan="8" class="px-3 py-6 text-center text-gray-400 text-sm">No parameters found.</td></tr>';
            return;
        }

        tableBody.innerHTML = rows.map(row => `
            <tr class="hover:bg-gray-50 transition">
                <td class="px-3 py-2 font-medium text-gray-900">${row.parameterCode}</td>
                <td class="px-3 py-2">${row.parameterName ?? ''}</td>
                <td class="px-3 py-2">${row.unit ?? '-'}</td>
                <td class="px-3 py-2">${row.standardValue ?? '-'}</td>
                <td class="px-3 py-2 text-sm text-gray-600">${row.description ?? '-'}</td>
                <td class="px-3 py-2 text-xs text-gray-500">${formatDate(row.createdAt)}</td>
                <td class="px-3 py-2 text-xs text-gray-500">${formatDate(row.updatedAt)}</td>
                <td class="px-3 py-2 text-center">
                    <div class="flex items-center justify-center gap-2">
                        <button type="button"
                                class="w-7 h-7 flex items-center justify-center border border-blue-300 rounded-md text-blue-600 hover:bg-blue-100 transition parameter-edit-btn"
                                title="Edit Parameter"
                                data-code="${row.parameterCode}">
                            <i class="bi bi-pencil text-[10px]"></i>
                        </button>
                        <button type="button"
                                class="w-7 h-7 flex items-center justify-center border border-red-400 text-red-500 rounded-md hover:bg-red-50 transition parameter-delete-btn"
                                title="Delete Parameter"
                                data-code="${row.parameterCode}"
                                data-name="${row.parameterName ?? row.parameterCode}">
                            <i class="bi bi-trash text-[10px]"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
    };

    const filterRows = () => {
        const keyword = (searchInput?.value || '').trim().toLowerCase();
        if (!keyword) {
            state.filtered = [...state.parameters];
        } else {
            state.filtered = state.parameters.filter(item =>
                item.parameterCode.toLowerCase().includes(keyword) ||
                (item.parameterName ?? '').toLowerCase().includes(keyword) ||
                (item.unit ?? '').toLowerCase().includes(keyword)
            );
        }
        renderRows(state.filtered);
    };

    const handleFetchError = async (response) => {
        let message = `Request failed (${response.status})`;
        try {
            const payload = await response.json();
            const errors = Array.isArray(payload?.errors) ? payload.errors.filter(Boolean) : [];
            message = payload?.message || payload?.error || message;
            if (errors.length) {
                message = `${message}\n${errors.join('\n')}`;
            }
        } catch {
            /* ignore */
        }
        throw new Error(message);
    };

    const unwrapOrThrow = (json, fallbackMessage) => {
        if (json?.success === false) {
            const errors = Array.isArray(json?.errors) ? json.errors.filter(Boolean) : [];
            const details = errors.length ? `\n${errors.join('\n')}` : '';
            throw new Error(`${json?.message || fallbackMessage || 'Request failed.'}${details}`);
        }
        return unwrapApiResponse(json);
    };

    const loadParameters = async () => {
        try {
            const res = await fetch(resolveRoute('list'), { credentials: 'same-origin' });
            if (!res.ok) return handleFetchError(res);
            const json = await res.json();
            const data = unwrapOrThrow(json, 'Failed to load parameters.');
            state.parameters = Array.isArray(data) ? data : [];
            state.filtered = [...state.parameters];
            renderRows(state.filtered);
        } catch (error) {
            console.error(error);
            if (tableBody) {
                tableBody.innerHTML = `<tr><td colspan="8" class="px-3 py-6 text-center text-red-500">${error.message || 'Failed to load parameters.'}</td></tr>`;
            }
        }
    };

    const collectParameterPayload = (prefix) => {
        const code = document.getElementById(`${prefix}ParameterCode`)?.value.trim();
        const name = document.getElementById(`${prefix}ParameterName`)?.value.trim();
        const unit = document.getElementById(`${prefix}ParameterUnit`)?.value.trim();
        const standardRaw = document.getElementById(`${prefix}ParameterStandard`)?.value;
        const description = document.getElementById(`${prefix}ParameterDescription`)?.value.trim();

        const standardValue = standardRaw === '' ? null : Number(standardRaw);
        if (standardValue !== null && Number.isNaN(standardValue)) {
            throw new Error('Standard value is invalid.');
        }

        return {
            parameterCode: code,
            parameterName: name,
            unit: unit || null,
            standardValue,
            description: description || null
        };
    };

    const createParameter = async () => {
        try {
            const payload = collectParameterPayload('add');
            if (!payload.parameterCode || !payload.parameterName) {
                alert('Parameter code and name are required.');
                return;
            }

            const res = await fetch(resolveRoute('create'), {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            });

            if (!res.ok) await handleFetchError(res);
            const json = await res.json();
            const created = unwrapOrThrow(json, 'Failed to create parameter.');
            alert(json?.message || 'Parameter created.');
            state.parameters.push(created);
            filterRows();
            resetAddFields();
            toggleModal(addModal, false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to create parameter.');
        }
    };

    const openEditModal = async (code) => {
        if (!code) return;
        try {
            const res = await fetch(resolveRoute('detail', code), { credentials: 'same-origin' });
            if (!res.ok) await handleFetchError(res);
            const json = await res.json();
            const data = unwrapOrThrow(json, 'Failed to load parameter detail.');
            fillEditFields(data);
            toggleModal(editModal, true);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to load parameter.');
        }
    };

    const updateParameter = async () => {
        const code = document.getElementById('editParameterCode').value;
        if (!code) {
            alert('Missing parameter code.');
            return;
        }

        try {
            const payload = collectParameterPayload('edit');
            payload.parameterCode = code;

            const res = await fetch(resolveRoute('update', code), {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin',
                body: JSON.stringify(payload)
            });

            if (!res.ok) await handleFetchError(res);
            const json = await res.json();
            const updated = unwrapOrThrow(json, 'Failed to update parameter.');
            alert(json?.message || 'Updated successfully.');
            const index = state.parameters.findIndex(p => p.parameterCode === code);
            if (index > -1) {
                state.parameters[index] = updated;
            }
            filterRows();
            toggleModal(editModal, false);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to update parameter.');
        }
    };

    const deleteParameter = async (code, displayName) => {
        if (!code) return;
        const confirmed = confirm(`Delete parameter ${displayName || code}?`);
        if (!confirmed) return;

        try {
            const res = await fetch(resolveRoute('delete', code), {
                method: 'DELETE',
                credentials: 'same-origin'
            });
            if (!res.ok) await handleFetchError(res);
            const json = await res.json();
            unwrapOrThrow(json, 'Failed to delete parameter.');
            alert(json?.message || 'Deleted.');
            state.parameters = state.parameters.filter(p => p.parameterCode !== code);
            filterRows();
            if (!editModal.classList.contains('hidden') && document.getElementById('editParameterCode').value === code) {
                toggleModal(editModal, false);
            }
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to delete parameter.');
        }
    };

    // Event bindings
    openAddBtn?.addEventListener('click', () => {
        resetAddFields();
        toggleModal(addModal, true);
    });
    closeAddBtn?.addEventListener('click', () => toggleModal(addModal, false));
    cancelAddBtn?.addEventListener('click', () => toggleModal(addModal, false));
    saveAddBtn?.addEventListener('click', createParameter);

    closeEditBtn?.addEventListener('click', () => toggleModal(editModal, false));
    cancelEditBtn?.addEventListener('click', () => toggleModal(editModal, false));
    updateBtn?.addEventListener('click', updateParameter);
    deleteBtn?.addEventListener('click', () => {
        const code = document.getElementById('editParameterCode').value;
        const name = document.getElementById('editParameterName').value;
        deleteParameter(code, name);
    });

    tableBody?.addEventListener('click', (event) => {
        const editBtn = event.target.closest('.parameter-edit-btn');
        if (editBtn) {
            openEditModal(editBtn.dataset.code);
            return;
        }

        const deleteRowBtn = event.target.closest('.parameter-delete-btn');
        if (deleteRowBtn) {
            deleteParameter(deleteRowBtn.dataset.code, deleteRowBtn.dataset.name);
        }
    });

    searchInput?.addEventListener('input', filterRows);

    // initial load
    loadParameters();
})();
