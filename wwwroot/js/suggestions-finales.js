let suggestionsData = [];

function getSelectedIds() {
    return JSON.parse(localStorage.getItem("selectedSuggestionIds") || "[]");
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
function saveSelectedIds(ids) {
    localStorage.setItem("selectedSuggestionIds", JSON.stringify(ids));
}

async function loadFinalList() {
    const res = await fetch('/Initiatives/GetAll');
    suggestionsData = await res.json();
    renderFinalList();
}

function renderFinalList() {
    const selectedIds = getSelectedIds();
    const selected = suggestionsData.filter(s => selectedIds.includes(s.id));

    const emptyState = document.getElementById("emptyState");
    const tableContainer = document.getElementById("finalTableContainer");
    const tbody = document.getElementById("finalTableBody");

    const total = selected.reduce((sum, s) => sum + Number(s.economies || 0), 0);
    document.getElementById("entryCount").textContent = suggestionsData.length + " entrées";
    document.getElementById("selectedCount").textContent = selected.length;
    document.getElementById("selectedTotal").textContent = "€ " + total.toLocaleString('fr-FR');

    if (selected.length === 0) {
        emptyState.style.display = "block";
        tableContainer.style.display = "none";
        return;
    }

    emptyState.style.display = "none";
    tableContainer.style.display = "block";

    tbody.innerHTML = "";
    selected.forEach(s => {
        const tr = document.createElement("tr");
        const kaizenClass = s.kaizen ? "kaizen-yes" : "kaizen-no";
        const statusClass = "status-" + (s.statut === "Applique" ? "applique" : "non-applique");

        tr.innerHTML = `
            <td>${s.plant || ""}</td>
            <td>${s.numeroSuggestion}</td>
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
                    <span onclick="removeFromSelection(${s.id})" title="Retirer">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <line x1="18" y1="6" x2="6" y2="18"></line>
                            <line x1="6" y1="6" x2="18" y2="18"></line>
                        </svg>
                    </span>
                </div>
            </td>
        `;
        tbody.appendChild(tr);
    });
    requestAnimationFrame(() => {
        document.querySelectorAll(".text-cell-content").forEach(el => {
            const toggle = document.getElementById("toggle-" + el.id);
            if (!toggle) return;
            const overflowing = el.scrollHeight > el.clientHeight + 1;
            toggle.style.display = overflowing ? "inline-block" : "none";
        });
    });
}

function formatDate(dateStr) {
    if (!dateStr) return "";
    const d = new Date(dateStr);
    return String(d.getDate()).padStart(2, "0") + "/" + String(d.getMonth() + 1).padStart(2, "0") + "/" + d.getFullYear();
}

function removeFromSelection(id) {
    const ids = getSelectedIds().filter(x => x !== id);
    saveSelectedIds(ids);
    renderFinalList();
}

loadFinalList();