let suggestionsData = [];
let editingId = null;
let currentPage = 1;
let pageSize = 25;

function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
}

function cleanupExportedFromSelection() {
    const ids = getSelectedIds();
    const exportedIds = suggestionsData.filter(s => s.exporteVersGlobal).map(s => s.id);
    const cleaned = ids.filter(id => !exportedIds.includes(id));
    if (cleaned.length !== ids.length) {
        saveSelectedIds(cleaned);
    }
}

async function loadSuggestions() {
    await Promise.all([loadFilters(), loadSuggestionsData()]);
    cleanupExportedFromSelection();
    initYearFilter();
    currentPage = 1;
    renderTable();
}
async function loadFilters() {
    const res = await fetch('/Initiatives/GetFilters');
    const data = await res.json();

    fillSelectPreserving("filterDepartement", data.departements, "all", "Département");
    fillSelectPreserving("filterScope", data.scopes, "all", "Scope");
    fillSelectPreserving("formDepartement", data.departements, null, null);
    fillSelectPreserving("formScope", data.scopes, null, null);
}

function fillSelectPreserving(selectId, items, allValue, allLabel) {
    const select = document.getElementById(selectId);
    const currentValue = select.value;

    select.innerHTML = "";
    if (allValue !== null) {
        const opt = document.createElement("option");
        opt.value = allValue;
        opt.textContent = allLabel;
        select.appendChild(opt);
    }
    items.forEach(item => {
        const opt = document.createElement("option");
        opt.value = item.id;
        opt.textContent = item.nom;
        select.appendChild(opt);
    });

    if ([...select.options].some(o => o.value === currentValue)) {
        select.value = currentValue;
    }
}

async function loadSuggestionsData() {
    const res = await fetch('/Initiatives/GetAll');
    suggestionsData = await res.json();
}

function initYearFilter() {
    const yearSelect = document.getElementById("filterYear");
    const currentValue = yearSelect.value;
    yearSelect.innerHTML = '<option value="all">Toutes les années</option>';

    const years = suggestionsData
        .filter(s => s.dateSuggestion)
        .map(s => new Date(s.dateSuggestion).getFullYear());

    if (years.length > 0) {
        const minYear = Math.min(...years);
        const maxYear = Math.max(...years);
        for (let y = minYear; y <= maxYear; y++) {
            const opt = document.createElement("option");
            opt.value = y;
            opt.textContent = "Année " + y;
            yearSelect.appendChild(opt);
        }
    }
    if (currentValue) yearSelect.value = currentValue;
}

function getSelectedIds() {
    return JSON.parse(localStorage.getItem("selectedSuggestionIds") || "[]");
}
function saveSelectedIds(ids) {
    localStorage.setItem("selectedSuggestionIds", JSON.stringify(ids));
}

function getFilteredData() {
    const search = document.getElementById("filterSearch").value.toLowerCase();
    const dep = document.getElementById("filterDepartement").value;
    const scope = document.getElementById("filterScope").value;
    const moisVal = document.getElementById("filterMois").value;
    const statut = document.getElementById("filterStatut").value;
    const kaizenFilter = document.getElementById("filterKaizen").value;
    const year = document.getElementById("filterYear").value;

    return suggestionsData.filter(s => {
        const matchSearch = (s.description || "").toLowerCase().includes(search) ||
            (s.numeroSuggestion || "").toLowerCase().includes(search) ||
            (s.responsable || "").toLowerCase().includes(search) ||
            formatDate(s.dateSuggestion).toLowerCase().includes(search);
        const matchDep = dep === "all" || s.departementId.toString() === dep;
        const matchScope = scope === "all" || s.scopeId.toString() === scope;
        const matchMois = moisVal === "all" || s.mois === moisVal;
        const matchStatut = statut === "all" || s.statut === statut;
        const matchKaizen = kaizenFilter === "all" || s.kaizen.toString() === kaizenFilter;
        const matchYear = year === "all" || (s.dateSuggestion && new Date(s.dateSuggestion).getFullYear().toString() === year);
        return matchSearch && matchDep && matchScope && matchMois && matchStatut && matchKaizen && matchYear;
    });
}

function textCell(id, field, value) {
    const safe = value || "";
    const cellId = `${field}-${id}`;
    return `
        <td class="text-cell">
            <div class="text-cell-content" id="${cellId}">${safe}</div>
            <span class="text-cell-toggle" id="toggle-${cellId}" style="display:none;" onclick="toggleCell('${cellId}', this)">Lire plus</span>
        </td>
    `;
}

function toggleCell(cellId, toggleEl) {
    const el = document.getElementById(cellId);
    const expanded = el.classList.toggle("expanded");
    toggleEl.textContent = expanded ? "Réduire" : "Lire plus";
}

function renderTable() {
    const filtered = getFilteredData();
    const selectedIds = getSelectedIds();

    const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize));
    if (currentPage > totalPages) currentPage = totalPages;
    if (currentPage < 1) currentPage = 1;

    const start = (currentPage - 1) * pageSize;
    const pageItems = filtered.slice(start, start + pageSize);

    const tbody = document.getElementById("suggestionsTableBody");
    tbody.innerHTML = "";

    pageItems.forEach(s => {
        const tr = document.createElement("tr");
        if (s.modifieManuellement) tr.classList.add("row-protected");
        const statusClass = "status-" + (s.statut === "Applique" ? "applique" : "non-applique");
        const kaizenClass = s.kaizen ? "kaizen-yes" : "kaizen-no";

        const checkboxCell = s.exporteVersGlobal
            ? `<td><span class="exported-check" title="Déjà exportée vers le CI Plan — non re-sélectionnable">✓</span></td>`
            : `<td><input type="checkbox" class="rowCheckbox" data-id="${s.id}" ${selectedIds.includes(s.id) ? "checked" : ""}></td>`;

        tr.innerHTML = `
    ${checkboxCell}
            <td>${s.plant || ""}</td>
            <td>${s.numeroSuggestion}${s.modifieManuellement ? ' <span class="protected-badge" title="Modifié manuellement — protégé de la resynchronisation">✎</span>' : ''}</td>
            <td>${formatDate(s.dateSuggestion)}</td>
            <td>${s.scopeName}</td>
            <td>${s.zoneLigne || ""}</td>
            ${textCell(s.id, "sit", s.situation)}
            ${textCell(s.id, "desc", s.description)}
            <td>${s.departementName}</td>
            <td>${s.responsable || ""}</td>
            <td><span class="status-badge ${statusClass}">${s.statut === "Applique" ? "Appliqué" : "Non appliqué"}</span></td>
            <td>${s.dateMiseEnOeuvre ? formatDate(s.dateMiseEnOeuvre) : ""}</td>
            <td>€ ${Number(s.economies).toLocaleString('fr-FR')}</td>
            <td>${s.kaizenNumber || ""}</td>
            <td>${s.mois || ""}</td>
            <td class="${kaizenClass}">${s.kaizen ? "Yes" : "No"}</td>
            <td>
                <div class="action-icons">
                    <span onclick="editSuggestion(${s.id})" title="Modifier">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4z"></path></svg>
                    </span>
                    <span onclick="deleteSuggestion(${s.id})" title="Supprimer">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"></path><path d="M10 11v6"></path><path d="M14 11v6"></path><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"></path></svg>
                    </span>
                </div>
            </td>
        `;
        tbody.appendChild(tr);
    });

    // Affiche "Lire plus" seulement si le texte déborde réellement des 2 lignes visibles
    requestAnimationFrame(() => {
        document.querySelectorAll(".text-cell-content").forEach(el => {
            const toggle = document.getElementById("toggle-" + el.id);
            if (!toggle) return;
            const overflowing = el.scrollHeight > el.clientHeight + 1;
            toggle.style.display = overflowing ? "inline-block" : "none";
        });
    });

    document.getElementById("entryCount").textContent = suggestionsData.length + " entrées";
    document.getElementById("sectionSubtitle").textContent = "Global Continuous Improvement — " + filtered.length + " entrées";

    renderPagination(totalPages, filtered.length, start, pageItems.length);
    attachCheckboxEvents();
}

function renderPagination(totalPages, totalFiltered, start, shownCount) {
    document.getElementById("pageIndicator").textContent = `Page ${currentPage} / ${totalPages}`;
    document.getElementById("paginationInfo").textContent =
        totalFiltered === 0 ? "Aucun résultat" : `Affichage de ${start + 1} à ${start + shownCount} sur ${totalFiltered}`;

    document.getElementById("prevPageBtn").disabled = currentPage <= 1;
    document.getElementById("nextPageBtn").disabled = currentPage >= totalPages;
}

document.getElementById("prevPageBtn").addEventListener("click", () => {
    if (currentPage > 1) { currentPage--; renderTable(); }
});
document.getElementById("nextPageBtn").addEventListener("click", () => {
    currentPage++; renderTable();
});
document.getElementById("pageSizeSelect").addEventListener("change", function () {
    pageSize = parseInt(this.value);
    currentPage = 1;
    renderTable();
});

function formatDate(dateStr) {
    if (!dateStr) return "";
    const d = new Date(dateStr);
    return String(d.getDate()).padStart(2, "0") + "/" + String(d.getMonth() + 1).padStart(2, "0") + "/" + d.getFullYear();
}

function attachCheckboxEvents() {
    document.querySelectorAll(".rowCheckbox").forEach(cb => {
        cb.addEventListener("change", function () {
            const id = parseInt(this.dataset.id);
            let ids = getSelectedIds();
            if (this.checked) {
                if (!ids.includes(id)) ids.push(id);
            } else {
                ids = ids.filter(x => x !== id);
            }
            saveSelectedIds(ids);
            updateSelectAllState();
        });
    });
    updateSelectAllState();
}

function updateSelectAllState() {
    const allCheckboxes = document.querySelectorAll(".rowCheckbox");
    const selectAll = document.getElementById("selectAllCheckbox");
    if (allCheckboxes.length === 0) { selectAll.checked = false; return; }
    selectAll.checked = Array.from(allCheckboxes).every(cb => cb.checked);
}

document.getElementById("selectAllCheckbox").addEventListener("change", function () {
    const isChecked = this.checked;
    let ids = getSelectedIds();
    document.querySelectorAll(".rowCheckbox").forEach(cb => {
        cb.checked = isChecked;
        const id = parseInt(cb.dataset.id);
        if (isChecked) {
            if (!ids.includes(id)) ids.push(id);
        } else {
            ids = ids.filter(x => x !== id);
        }
    });
    saveSelectedIds(ids);
});

let pendingDeleteId = null;

function deleteSuggestion(id) {
    pendingDeleteId = id;
    document.getElementById("confirmDeleteModal").classList.add("active");
}

function closeConfirmDeleteModal() {
    pendingDeleteId = null;
    document.getElementById("confirmDeleteModal").classList.remove("active");
}

document.getElementById("closeConfirmDeleteBtn").addEventListener("click", closeConfirmDeleteModal);
document.getElementById("cancelConfirmDeleteBtn").addEventListener("click", closeConfirmDeleteModal);

document.getElementById("confirmDeleteBtn").addEventListener("click", async function () {
    if (pendingDeleteId == null) return;
    const id = pendingDeleteId;
    const btn = this;
    btn.disabled = true;

    try {
        const res = await fetch('/Initiatives/Delete', {
            method: "POST",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
            body: JSON.stringify(id)
        });
        const data = await res.json();

        if (data.success) {
            let ids = getSelectedIds().filter(x => x !== id);
            saveSelectedIds(ids);
            closeConfirmDeleteModal();
            await loadSuggestions();
        } else {
            alert(data.message || "Erreur lors de la suppression.");
        }
    } finally {
        btn.disabled = false;
    }
});

function openModal() {
    editingId = null;
    document.getElementById("modalTitle").textContent = "Nouvelle Initiative";
    document.getElementById("addSuggestionForm").reset();
    document.getElementById("formPlant").value = "Morocco";
    document.getElementById("addModal").classList.add("active");
}
function closeModal() {
    document.getElementById("addModal").classList.remove("active");
}

function editSuggestion(id) {
    const s = suggestionsData.find(item => item.id === id);
    if (!s) return;

    editingId = id;
    document.getElementById("modalTitle").textContent = "Modifier l'Initiative";

    document.getElementById("formPlant").value = s.plant || "Morocco";
    document.getElementById("formDateSuggestion").value = s.dateSuggestion ? s.dateSuggestion.substring(0, 10) : "";
    document.getElementById("formDateMiseEnOeuvre").value = s.dateMiseEnOeuvre ? s.dateMiseEnOeuvre.substring(0, 10) : "";
    document.getElementById("formScope").value = s.scopeId;
    document.getElementById("formDepartement").value = s.departementId;
    document.getElementById("formResponsable").value = s.responsable || "";
    document.getElementById("formMois").value = s.mois || "";
    document.getElementById("formStatut").value = s.statut;
    document.getElementById("formZoneLigne").value = s.zoneLigne || "";
    document.getElementById("formEconomies").value = s.economies;
    document.getElementById("formKaizen").value = s.kaizen ? "true" : "false";
    document.getElementById("formKaizenNumber").value = s.kaizenNumber || "";
    document.getElementById("formSituation").value = s.situation || "";
    document.getElementById("formDescription").value = s.description;

    document.getElementById("addModal").classList.add("active");
}

document.getElementById("openAddModalBtn").addEventListener("click", openModal);
document.getElementById("closeModalBtn").addEventListener("click", closeModal);
document.getElementById("cancelModalBtn").addEventListener("click", closeModal);

document.getElementById("addSuggestionForm").addEventListener("submit", async function (event) {
    event.preventDefault();

    const payload = {
        id: editingId,
        plant: document.getElementById("formPlant").value || "Morocco",
        dateSuggestion: document.getElementById("formDateSuggestion").value || null,
        dateMiseEnOeuvre: document.getElementById("formDateMiseEnOeuvre").value || null,
        scopeId: parseInt(document.getElementById("formScope").value),
        departementId: parseInt(document.getElementById("formDepartement").value),
        responsable: document.getElementById("formResponsable").value,
        mois: document.getElementById("formMois").value || null,
        statut: document.getElementById("formStatut").value,
        zoneLigne: document.getElementById("formZoneLigne").value,
        economies: parseFloat(document.getElementById("formEconomies").value) || 0,
        kaizen: document.getElementById("formKaizen").value === "true",
        kaizenNumber: document.getElementById("formKaizenNumber").value,
        situation: document.getElementById("formSituation").value,
        description: document.getElementById("formDescription").value
    };

    const res = await fetch('/Initiatives/Save', {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
        body: JSON.stringify(payload)
    });
    const data = await res.json();

    if (data.success) {
        closeModal();
        await loadSuggestions();
    } else {
        alert(data.message || "Erreur lors de l'enregistrement.");
    }
});

["filterSearch", "filterDepartement", "filterScope", "filterMois", "filterStatut", "filterKaizen", "filterYear"].forEach(id => {
    document.getElementById(id).addEventListener("input", () => { currentPage = 1; renderTable(); });
    document.getElementById(id).addEventListener("change", () => { currentPage = 1; renderTable(); });
});

loadSuggestions();