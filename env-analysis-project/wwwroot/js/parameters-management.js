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
    const exportBtn = document.getElementById('exportParameterBtn');

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

    const paginationInfoEl = document.getElementById('parameterPaginationInfo');
    const pageIndicatorEl = document.getElementById('parameterPageIndicator');
    const pageSizeSelect = document.getElementById('parameterPageSizeSelect');
    const prevPageBtn = document.getElementById('parameterPrevPage');
    const nextPageBtn = document.getElementById('parameterNextPage');

    const state = {
        parameters: [],
        filtered: [],
        page: 1,
        pageSize: parseInt(pageSizeSelect?.value || '10', 10)
    };

    const sortRows = (rows) => {
        return [...rows].sort((a, b) => {
            const deletedDiff = Number(a.isDeleted) - Number(b.isDeleted);
            if (deletedDiff !== 0) return deletedDiff;
            return (a.parameterName || '').localeCompare(b.parameterName || '');
        });
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
        document.getElementById('addParameterType').value = 'water';
        document.getElementById('addParameterUnit').value = '';
        document.getElementById('addParameterStandard').value = '';
        document.getElementById('addParameterDescription').value = '';
    };

    const fillEditFields = (data) => {
        document.getElementById('editParameterCode').value = data.parameterCode;
        document.getElementById('editParameterCodeDisplay').value = data.parameterCode;
        document.getElementById('editParameterName').value = data.parameterName ?? '';
        document.getElementById('editParameterType').value = (data.type || 'water').toLowerCase();
        document.getElementById('editParameterUnit').value = data.unit ?? '';
        document.getElementById('editParameterStandard').value = data.standardValue ?? '';
        document.getElementById('editParameterDescription').value = data.description ?? '';
    };

    const renderRows = (rows) => {
        if (!tableBody) return;
        if (!rows.length) {
            tableBody.innerHTML = '<tr><td colspan="9" class="px-3 py-6 text-center text-gray-400 text-sm">No parameters found.</td></tr>';
            updatePaginationMeta(0, 0, 0);
            return;
        }

        const ordered = sortRows(rows);
        const totalItems = ordered.length;
        const totalPages = Math.max(1, Math.ceil(totalItems / state.pageSize));
        state.page = Math.min(state.page, totalPages);

        const startIndex = (state.page - 1) * state.pageSize;
        const pageItems = ordered.slice(startIndex, startIndex + state.pageSize);
        if (!pageItems.length) {
            tableBody.innerHTML = '<tr><td colspan="9" class="px-3 py-6 text-center text-gray-400 text-sm">No parameters found.</td></tr>';
            updatePaginationMeta(totalItems, totalPages, state.page);
            return;
        }

        tableBody.innerHTML = pageItems.map(row => {
            const isDeleted = !!row.isDeleted;
            const rowClasses = isDeleted ? 'bg-red-200/70 text-gray-700' : 'hover:bg-gray-50 transition';
            const statusMarkup = isDeleted
                ? '<span class="text-red-600 font-semibold uppercase text-[11px]">Deleted</span>'
                : '<span class="text-green-600 font-semibold text-[11px]">Active</span>';
            const actionButtons = isDeleted
                ? `<button type="button"
                           class="w-7 h-7 flex items-center justify-center border border-green-400 text-green-600 rounded-md hover:bg-green-50 transition parameter-restore-btn"
                           title="Restore Parameter"
                           data-code="${row.parameterCode}">
                        <i class="bi bi-arrow-counterclockwise text-[10px]"></i>
                   </button>`
                : `<button type="button"
                           class="w-7 h-7 flex items-center justify-center border border-blue-300 rounded-md text-blue-600 hover:bg-blue-100 transition parameter-edit-btn"
                           title="Edit Parameter"
                           data-code="${row.parameterCode}">
                        <i class="bi bi-eye text-[10px]"></i>
                   </button>
                   <button type="button"
                           class="w-7 h-7 flex items-center justify-center border border-red-400 text-red-500 rounded-md hover:bg-red-50 transition parameter-delete-btn"
                           title="Delete Parameter"
                           data-code="${row.parameterCode}"
                           data-name="${row.parameterName ?? row.parameterCode}">
                        <i class="bi bi-trash text-[10px]"></i>
                   </button>`;
            return `
            <tr class="${rowClasses}">
                <td class="px-3 py-2 font-medium text-gray-900">${row.parameterCode}</td>
                <td class="px-3 py-2">${row.parameterName ?? ''}</td>
                <td class="px-3 py-2 capitalize">${row.type ?? 'water'}</td>
                <td class="px-3 py-2">${row.unit ?? '-'}</td>
                <td class="px-3 py-2">${row.standardValue ?? '-'}</td>
                <td class="px-3 py-2 text-xs text-gray-600">${row.description ?? '-'}</td>
                <td class="px-3 py-2 text-xs text-gray-500">${formatDate(row.createdAt)}</td>
                <td class="px-3 py-2 text-xs text-gray-500">${formatDate(row.updatedAt)}</td>
                <td class="px-3 py-2 text-center">
                    <div class="flex items-center justify-center gap-2">${actionButtons}</div>
                </td>
            </tr>
        `;
        }).join('');

        updatePaginationMeta(totalItems, totalPages, state.page, startIndex, pageItems.length);
    };

    const updatePaginationMeta = (totalItems, totalPages, currentPage, startIndex = 0, pageCount = 0) => {
        if (paginationInfoEl) {
            if (!totalItems) {
                paginationInfoEl.textContent = 'No parameters to display';
            } else {
                const start = startIndex + 1;
                const end = startIndex + pageCount;
                paginationInfoEl.textContent = `Showing ${start}-${end} of ${totalItems} parameters`;
            }
        }

        if (pageIndicatorEl) {
            pageIndicatorEl.textContent = `Page ${Math.max(currentPage, 1)} of ${Math.max(totalPages, 1)}`;
        }

        if (prevPageBtn) {
            prevPageBtn.disabled = currentPage <= 1;
        }
        if (nextPageBtn) {
            nextPageBtn.disabled = currentPage >= totalPages || totalItems === 0;
        }
    };

    const filterRows = () => {
        const keyword = (searchInput?.value || '').trim().toLowerCase();
        if (!keyword) {
            state.filtered = [...state.parameters];
        } else {
            state.filtered = state.parameters.filter(item =>
                item.parameterCode.toLowerCase().includes(keyword) ||
                (item.parameterName ?? '').toLowerCase().includes(keyword) ||
                (item.unit ?? '').toLowerCase().includes(keyword) ||
                (item.type ?? '').toLowerCase().includes(keyword)
            );
        }
        state.page = 1;
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
            state.parameters = Array.isArray(data) ? data.map(item => ({
                ...item,
                isDeleted: !!item.isDeleted,
                type: (item.type || 'water').toLowerCase()
            })) : [];
            state.filtered = [...state.parameters];
            renderRows(state.filtered);
        } catch (error) {
            console.error(error);
            if (tableBody) {
                tableBody.innerHTML = `<tr><td colspan="9" class="px-3 py-6 text-center text-red-500">${error.message || 'Failed to load parameters.'}</td></tr>`;
            }
        }
    };

    const collectParameterPayload = (prefix) => {
        const code = document.getElementById(`${prefix}ParameterCode`)?.value.trim();
        const name = document.getElementById(`${prefix}ParameterName`)?.value.trim();
        const unit = document.getElementById(`${prefix}ParameterUnit`)?.value.trim();
        const standardRaw = document.getElementById(`${prefix}ParameterStandard`)?.value;
        const description = document.getElementById(`${prefix}ParameterDescription`)?.value.trim();
        const type = (document.getElementById(`${prefix}ParameterType`)?.value || 'water').toLowerCase();

        const standardValue = standardRaw === '' ? null : Number(standardRaw);
        if (standardValue !== null && Number.isNaN(standardValue)) {
            throw new Error('Standard value is invalid.');
        }

        return {
            parameterCode: code,
            parameterName: name,
            type,
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
            state.parameters.push({
                ...created,
                isDeleted: !!created.isDeleted,
                type: (created.type || 'water').toLowerCase()
            });
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
                state.parameters[index] = {
                    ...state.parameters[index],
                    ...updated,
                    isDeleted: !!updated.isDeleted,
                    type: (updated.type || state.parameters[index].type || 'water').toLowerCase()
                };
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
            const deleted = unwrapOrThrow(json, 'Failed to delete parameter.');
            alert(json?.message || 'Deleted.');
            const index = state.parameters.findIndex(p => p.parameterCode === code);
            if (index > -1) {
                state.parameters[index] = {
                    ...state.parameters[index],
                    ...deleted,
                    isDeleted: true,
                    type: (deleted.type || state.parameters[index].type || 'water').toLowerCase()
                };
            }
            filterRows();
            if (!editModal.classList.contains('hidden') && document.getElementById('editParameterCode').value === code) {
                toggleModal(editModal, false);
            }
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to delete parameter.');
        }
    };

    const restoreParameter = async (code) => {
        if (!code) return;
        try {
            const res = await fetch(resolveRoute('restore'), {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ parameterCode: code })
            });
            if (!res.ok) await handleFetchError(res);
            const json = await res.json();
            const restored = unwrapOrThrow(json, 'Failed to restore parameter.');
            alert(json?.message || 'Restored.');
            const index = state.parameters.findIndex(p => p.parameterCode === code);
            if (index > -1) {
                state.parameters[index] = {
                    ...state.parameters[index],
                    ...restored,
                    isDeleted: false,
                    type: (restored.type || state.parameters[index].type || 'water').toLowerCase()
                };
            }
            filterRows();
        } catch (error) {
            console.error(error);
            alert(error.message || 'Failed to restore parameter.');
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
            return;
        }

        const restoreBtn = event.target.closest('.parameter-restore-btn');
        if (restoreBtn) {
            restoreParameter(restoreBtn.dataset.code);
        }
    });

    searchInput?.addEventListener('input', filterRows);
    exportBtn?.addEventListener('click', () => {
        if (!routes.export) return;
        window.open(routes.export, '_blank');
    });

    pageSizeSelect?.addEventListener('change', () => {
        const value = parseInt(pageSizeSelect.value, 10);
        state.pageSize = Number.isNaN(value) ? 10 : value;
        state.page = 1;
        renderRows(state.filtered);
    });

    prevPageBtn?.addEventListener('click', () => {
        if (state.page > 1) {
            state.page -= 1;
            renderRows(state.filtered);
        }
    });

    nextPageBtn?.addEventListener('click', () => {
        const totalPages = Math.max(1, Math.ceil(state.filtered.length / state.pageSize));
        if (state.page < totalPages) {
            state.page += 1;
            renderRows(state.filtered);
        }
    });

    // initial load
    loadParameters();
})();
