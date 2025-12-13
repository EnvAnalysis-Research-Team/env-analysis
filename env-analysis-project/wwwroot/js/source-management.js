'use strict';

(function () {
    const MODAL_ANIMATION_MS = 200;
    const DEFAULT_CENTER = [15.9031, 105.8067];

    let mapInstance = null;
    let mapMarker = null;

    const dom = {
        addTypeModal: document.getElementById('addTypeModal'),
        editTypeModal: document.getElementById('editTypeModal'),
        addSourceModal: document.getElementById('addSourceModal'),
        editSourceModal: document.getElementById('editSourceModal'),
        emissionSearchInput: document.getElementById('emissionSearchInput'),
        emissionSearchReset: document.getElementById('emissionSearchReset'),
        emissionNoMatchRow: document.getElementById('emissionNoMatchRow')
    };

    const escapeHtml = (value) => {
        if (value == null) return '';
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    };

    const unwrapApiResponse = (payload) => {
        if (!payload || typeof payload !== 'object') return payload;
        return Object.prototype.hasOwnProperty.call(payload, 'data') ? payload.data : payload;
    };

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

    const parseCoordinate = (value) => {
        const parsed = parseFloat(value);
        return Number.isFinite(parsed) ? parsed : null;
    };

    const updateMarker = (lat, lng, label) => {
        if (!mapInstance || lat == null || lng == null) return;
        if (!mapMarker) {
            mapMarker = L.marker([lat, lng]).addTo(mapInstance);
        } else {
            mapMarker.setLatLng([lat, lng]);
        }
        if (label) {
            mapMarker.bindPopup(label);
        }
        mapMarker.openPopup();
        mapInstance.setView([lat, lng], Math.max(mapInstance.getZoom(), 12));
    };

    const focusRowOnMap = (row) => {
        if (!row || !mapInstance) return;
        const lat = parseCoordinate(row.dataset.lat);
        const lng = parseCoordinate(row.dataset.lng);
        if (lat == null || lng == null) return;
        const label = row.dataset.name || row.dataset.code || 'Emission source';
        updateMarker(lat, lng, label);
    };

    const bindEmissionRowEvents = () => {
        document.querySelectorAll('.emission-source-row').forEach(row => {
            row.addEventListener('click', (event) => {
                if (event.target.closest('button')) return;
                focusRowOnMap(row);
            });
        });
    };

    const buildEmissionSearchIndex = (row) => {
        const tokens = [
            row.dataset.name,
            row.dataset.code,
            row.dataset.location,
            row.dataset.type,
            row.dataset.status
        ].filter(Boolean).map(value => value.toLowerCase());
        row.dataset.searchIndex = tokens.join(' ');
    };

    const applyEmissionSearch = (query) => {
        const normalized = (query || '').trim().toLowerCase();
        let visibleCount = 0;
        document.querySelectorAll('.emission-source-row').forEach(row => {
            if (!row.dataset.searchIndex) {
                buildEmissionSearchIndex(row);
            }
            const matches = !normalized || row.dataset.searchIndex.includes(normalized);
            row.classList.toggle('hidden', !matches);
            if (matches) visibleCount += 1;
        });
        if (dom.emissionNoMatchRow) {
            dom.emissionNoMatchRow.classList.toggle('hidden', visibleCount !== 0);
        }
    };

    const bindEmissionSearchControls = () => {
        if (!dom.emissionSearchInput) return;
        document.querySelectorAll('.emission-source-row').forEach(buildEmissionSearchIndex);
        dom.emissionSearchInput.addEventListener('input', () => {
            applyEmissionSearch(dom.emissionSearchInput.value);
        });
        dom.emissionSearchReset?.addEventListener('click', () => {
            dom.emissionSearchInput.value = '';
            applyEmissionSearch('');
        });
        applyEmissionSearch(dom.emissionSearchInput.value);
    };

    const initMap = () => {
        const container = document.getElementById('sourceMap');
        if (!container || typeof L === 'undefined') {
            console.warn('Leaflet scripts are missing; map disabled.');
            if (container) {
                container.innerHTML = '<div class="absolute inset-0 flex items-center justify-center text-gray-500 text-sm">Map assets missing.</div>';
            }
            return;
        }

        const firstRow = document.querySelector('.emission-source-row');
        const lat = firstRow ? parseCoordinate(firstRow.dataset.lat) : null;
        const lng = firstRow ? parseCoordinate(firstRow.dataset.lng) : null;
        const startCoords = (lat != null && lng != null) ? [lat, lng] : DEFAULT_CENTER;
        const startZoom = lat != null ? 12 : 5;

        mapInstance = L.map(container).setView(startCoords, startZoom);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(mapInstance);

        if (lat != null && lng != null) {
            updateMarker(lat, lng, firstRow?.dataset.name || 'Emission source');
        }
    };

    const loadSourceTypes = async () => {
        try {
            const res = await fetch('/SourceTypes/GetList', { credentials: 'same-origin' });
            if (!res.ok) throw new Error('Failed to fetch source types.');
            const envelope = await res.json();
            if (envelope?.success === false) throw new Error(envelope.message || 'Failed to fetch source types.');

            const list = unwrapApiResponse(envelope);
            const tbody = document.getElementById('sourceTypeTbody');
            if (!tbody) return;

            tbody.innerHTML = '';
            const items = Array.isArray(list) ? list : [];
            if (!items.length) {
                tbody.innerHTML = '<tr><td colspan="2" class="px-2 py-3 text-center text-xs text-gray-400">No source types found.</td></tr>';
                return;
            }

            items.forEach(item => {
                const tr = document.createElement('tr');
                tr.className = 'type-row hover:bg-white cursor-pointer';
                tr.dataset.id = item.sourceTypeID ?? item.SourceTypeID ?? item.SourceTypeId;
                tr.dataset.name = item.sourceTypeName ?? item.SourceTypeName ?? '';
                tr.dataset.desc = item.description ?? item.Description ?? '';
                tr.dataset.isactive = item.isActive ?? item.IsActive ?? '';
                tr.dataset.count = item.emissionSourceCount ?? item.EmissionSourceCount ?? item.count ?? item.Count ?? 0;
                tr.innerHTML = `
                    <td class="px-2 py-2 truncate" title="${escapeHtml(tr.dataset.name)}">${escapeHtml(tr.dataset.name)}</td>
                    <td class="px-2 py-2 text-center font-medium">${escapeHtml(tr.dataset.count)}</td>
                `;
                tr.addEventListener('click', onSourceTypeRowClick);
                tbody.appendChild(tr);
            });
        } catch (error) {
            console.error(error);
        }
    };

    const onSourceTypeRowClick = async (event) => {
        const tr = event.currentTarget;
        const id = tr.dataset.id;
        if (!id) return;
        try {
            const res = await fetch(`/SourceTypes/Get/${encodeURIComponent(id)}`, { credentials: 'same-origin' });
            if (!res.ok) throw new Error('Failed to load details.');
            const envelope = await res.json();
            if (envelope?.success === false) throw new Error(envelope.message || 'Failed to load details.');
            const dto = unwrapApiResponse(envelope) || {};

            document.getElementById('edit_SourceTypeID').value = dto.sourceTypeID ?? dto.SourceTypeID ?? '';
            document.getElementById('edit_SourceTypeName').value = dto.sourceTypeName ?? dto.SourceTypeName ?? '';
            document.getElementById('edit_Description').value = dto.description ?? dto.Description ?? '';
            document.getElementById('edit_IsActive').checked = !!(dto.isActive ?? dto.IsActive);
            toggleModal(dom.editTypeModal, true);
        } catch (error) {
            console.error(error);
            alert(error.message || 'Error loading source type details.');
        }
    };

    const submitSourceTypeForm = async (form, url) => {
        const formData = new FormData(form);
        const res = await fetch(url, {
            method: 'POST',
            body: formData,
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        const envelope = await res.json().catch(() => ({}));
        if (!res.ok || envelope?.success === false) {
            const errors = Array.isArray(envelope?.errors) ? envelope.errors : null;
            throw new Error(errors?.join('\n') || envelope?.message || envelope?.error || 'Request failed.');
        }
        return envelope;
    };

    const attachSourceTypeForms = () => {
        const addForm = document.getElementById('addTypeForm');
        if (addForm) {
            addForm.addEventListener('submit', async (event) => {
                event.preventDefault();
                try {
                    const response = await submitSourceTypeForm(addForm, addForm.action);
                    alert(response?.message || 'Source type created successfully.');
                    setTimeout(() => location.reload(), 600);
                } catch (error) {
                    alert(error.message);
                }
            });
        }

        const editForm = document.getElementById('editTypeForm');
        if (editForm) {
            editForm.addEventListener('submit', async (event) => {
                event.preventDefault();
                const id = document.getElementById('edit_SourceTypeID').value;
                if (!id) return alert('Missing type id.');
                const fd = new FormData(editForm);
                fd.append('UpdatedAt', new Date().toISOString());
                try {
                    const res = await fetch(`/SourceTypes/Edit/${encodeURIComponent(id)}`, {
                        method: 'POST',
                        body: fd,
                        credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' }
                    });
                    const envelope = await res.json().catch(() => ({}));
                    if (!res.ok || envelope?.success === false) {
                        const errors = Array.isArray(envelope?.errors) ? envelope.errors : null;
                        alert(errors?.join('\n') || envelope?.message || envelope?.error || 'Failed to update source type.');
                        return;
                    }
                    alert(envelope?.message || 'Source type updated successfully.');
                    setTimeout(() => location.reload(), 500);
                } catch (error) {
                    alert(error.message || 'Failed to update source type.');
                }
            });
        }

        const deleteBtn = document.getElementById('deleteTypeBtn');
        if (deleteBtn) {
            deleteBtn.addEventListener('click', async () => {
                if (!confirm('Are you sure you want to delete this source type?')) return;
                const id = document.getElementById('edit_SourceTypeID').value;
                if (!id) return alert('Missing id.');
                const token = editForm?.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
                const fd = new FormData();
                fd.append('id', id);
                if (token) fd.append('__RequestVerificationToken', token);
                try {
                    const res = await fetch(`/SourceTypes/Delete/${encodeURIComponent(id)}`, {
                        method: 'POST',
                        body: fd,
                        credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' }
                    });
                    const envelope = await res.json().catch(() => ({}));
                    if (!res.ok || envelope?.success === false) {
                        alert(envelope?.message || envelope?.error || 'Delete failed.');
                        return;
                    }
                    alert(envelope?.message || 'Source type deleted successfully.');
                    setTimeout(() => location.reload(), 400);
                } catch (error) {
                    alert(error.message || 'Delete failed.');
                }
            });
        }
    };

    const setupSourceTypeModals = () => {
        document.getElementById('openAddTypeModalBtn')?.addEventListener('click', () => toggleModal(dom.addTypeModal, true));
        document.getElementById('closeAddTypeModal')?.addEventListener('click', () => toggleModal(dom.addTypeModal, false));
        document.getElementById('cancelAddTypeBtn')?.addEventListener('click', () => toggleModal(dom.addTypeModal, false));
        dom.addTypeModal?.addEventListener('click', (event) => {
            if (event.target === dom.addTypeModal) toggleModal(dom.addTypeModal, false);
        });

        document.getElementById('closeEditModal')?.addEventListener('click', () => toggleModal(dom.editTypeModal, false));
        document.getElementById('cancelEditBtn')?.addEventListener('click', () => toggleModal(dom.editTypeModal, false));
        dom.editTypeModal?.addEventListener('click', (event) => {
            if (event.target === dom.editTypeModal) toggleModal(dom.editTypeModal, false);
        });
    };

    const setupEmissionSourceModals = () => {
        document.getElementById('openAddSourceModalBtn')?.addEventListener('click', () => toggleModal(dom.addSourceModal, true));
        document.getElementById('closeAddModal')?.addEventListener('click', () => toggleModal(dom.addSourceModal, false));
        document.getElementById('cancelAddBtn')?.addEventListener('click', () => toggleModal(dom.addSourceModal, false));
        dom.addSourceModal?.addEventListener('click', (event) => {
            if (event.target === dom.addSourceModal) toggleModal(dom.addSourceModal, false);
        });
    };

    const attachEmissionAddForm = () => {
        const addSourceForm = document.getElementById('addSourceForm');
        if (!addSourceForm) return;
        addSourceForm.addEventListener('submit', async (event) => {
            event.preventDefault();
            const formData = new FormData(addSourceForm);
            try {
                const res = await fetch(addSourceForm.action, {
                    method: 'POST',
                    body: formData,
                    credentials: 'same-origin',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                const envelope = await res.json().catch(() => ({}));
                if (!res.ok || envelope?.success === false) {
                    const errors = Array.isArray(envelope?.errors) ? envelope.errors : null;
                    alert(errors?.join('\n') || envelope?.message || envelope?.error || 'Failed to create emission source.');
                    return;
                }

                alert(envelope?.message || 'Emission source created successfully.');
                addSourceForm.reset();
                toggleModal(dom.addSourceModal, false);
                setTimeout(() => window.location.reload(), 500);
            } catch (error) {
                console.error(error);
                alert('Could not create emission source. Please try again.');
            }
        });
    };

    const getAntiForgeryToken = () =>
        document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    const requestEmissionDelete = async (id) => {
        const headers = {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest'
        };
        const token = getAntiForgeryToken();
        if (token) {
            headers['RequestVerificationToken'] = token;
        }
        const res = await fetch(window.sourceManagementConfig?.deleteUrl || '/EmissionSources/Delete', {
            method: 'POST',
            credentials: 'same-origin',
            headers,
            body: JSON.stringify({ id: Number(id) })
        });
        const envelope = await res.json().catch(() => ({}));
        if (!res.ok || envelope?.success === false) {
            throw new Error(envelope?.message || envelope?.error || 'Failed to delete emission source.');
        }
        return envelope;
    };

    const attachEmissionSourceEvents = () => {
        document.querySelectorAll('.delete-source-btn').forEach(btn => {
            btn.addEventListener('click', async () => {
                const id = btn.dataset.id;
                const name = btn.dataset.name ? `"${btn.dataset.name}"` : 'this emission source';
                if (!id || !confirm(`Are you sure you want to delete ${name}?`)) return;
                btn.disabled = true;
                try {
                    await requestEmissionDelete(id);
                    btn.closest('tr')?.remove();
                    alert('Emission source deleted successfully.');
                } catch (error) {
                    alert(error.message);
                } finally {
                    btn.disabled = false;
                }
            });
        });
    };

    const attachEmissionDetailEvents = () => {
        const editModal = dom.editSourceModal;
        const closeBtn = document.getElementById('closeEditSourceModal');
        const cancelBtn = document.getElementById('cancelEditSourceBtn');
        closeBtn?.addEventListener('click', () => toggleModal(editModal, false));
        cancelBtn?.addEventListener('click', () => toggleModal(editModal, false));
        editModal?.addEventListener('click', (event) => {
            if (event.target === editModal) toggleModal(editModal, false);
        });

        document.querySelectorAll('button[title="View Details"]').forEach(btn => {
            btn.addEventListener('click', async (event) => {
                event.preventDefault();
                const row = btn.closest('tr');
                const id = btn.getAttribute('data-id');
                if (!id) return;
                try {
                    const res = await fetch(`/EmissionSources/Detail/${encodeURIComponent(id)}`, { credentials: 'same-origin' });
                    if (!res.ok) throw new Error('Failed to load emission source.');
                    const envelope = await res.json();
                    if (envelope?.success === false) throw new Error(envelope.message || 'Failed to load emission source.');
                    const dto = unwrapApiResponse(envelope) || {};

                    document.getElementById('edit_SourceID').value = dto.emissionSourceID ?? dto.EmissionSourceID ?? '';
                    document.getElementById('edit_SourceCode').value = dto.sourceCode ?? dto.SourceCode ?? '';
                    document.getElementById('edit_SourceName').value = dto.sourceName ?? dto.SourceName ?? '';
                    document.getElementById('edit_Latitude').value = dto.latitude ?? dto.Latitude ?? '';
                    document.getElementById('edit_Longitude').value = dto.longitude ?? dto.Longitude ?? '';
                    document.getElementById('edit_Location').value = dto.location ?? dto.Location ?? '';
                    document.getElementById('edit_SourceTypeID').value = dto.sourceTypeID ?? dto.SourceTypeID ?? '';
                    document.getElementById('edit_IsActiveSource').checked = !!(dto.isActive ?? dto.IsActive);
                    document.getElementById('emission_sources_des').value = dto.description ?? dto.Description ?? '';

                    const select = document.getElementById('emission_sources_stid');
                    const typeId = dto.sourceTypeID ?? dto.SourceTypeID;
                    if (select && typeId != null) select.value = typeId;

                    if (row) {
                        if (dto.latitude ?? dto.Latitude) row.dataset.lat = dto.latitude ?? dto.Latitude;
                        if (dto.longitude ?? dto.Longitude) row.dataset.lng = dto.longitude ?? dto.Longitude;
                        if (dto.sourceName ?? dto.SourceName) row.dataset.name = dto.sourceName ?? dto.SourceName;
                        focusRowOnMap(row);
                    }

                    toggleModal(dom.editSourceModal, true);
                } catch (error) {
                    console.error(error);
                    alert(error.message || 'Error loading emission source.');
                }
            });
        });

        const editForm = document.getElementById('editSourceForm');
        const deleteSourceBtn = document.getElementById('deleteSourceBtn');
        if (editForm) {
            editForm.addEventListener('submit', async (event) => {
                event.preventDefault();
                const id = document.getElementById('edit_SourceID').value;
                if (!id) return alert('Missing SourceID.');
                const fd = new FormData(editForm);
                try {
                    const res = await fetch(`/EmissionSources/Edit/${encodeURIComponent(id)}`, {
                        method: 'POST',
                        body: fd,
                        credentials: 'same-origin',
                        headers: { 'X-Requested-With': 'XMLHttpRequest' }
                    });
                    const envelope = await res.json().catch(() => ({}));
                    if (!res.ok || envelope?.success === false) {
                        const errors = Array.isArray(envelope?.errors) ? envelope.errors : null;
                        alert(errors?.join('\n') || envelope?.message || envelope?.error || 'Failed to update.');
                        return;
                    }
                    alert(envelope?.message || 'Emission source updated.');
                    setTimeout(() => location.reload(), 500);
                } catch (error) {
                    console.error(error);
                    alert(error.message || 'Error updating emission source.');
                }
            });
        }

        if (deleteSourceBtn) {
            deleteSourceBtn.addEventListener('click', async () => {
                const id = document.getElementById('edit_SourceID').value;
                if (!id || !confirm('Delete this emission source?')) return;
                try {
                    await requestEmissionDelete(id);
                    alert('Emission source deleted successfully.');
                    setTimeout(() => location.reload(), 500);
                } catch (error) {
                    alert(error.message || 'Failed to delete emission source.');
                }
            });
        }
    };

    document.addEventListener('DOMContentLoaded', () => {
        initMap();
        bindEmissionRowEvents();
        const firstRow = document.querySelector('.emission-source-row');
        if (firstRow) {
            focusRowOnMap(firstRow);
        }
        loadSourceTypes();
        setupSourceTypeModals();
        setupEmissionSourceModals();
        attachSourceTypeForms();
        attachEmissionAddForm();
        attachEmissionSourceEvents();
        attachEmissionDetailEvents();
        bindEmissionSearchControls();
    });
})();
