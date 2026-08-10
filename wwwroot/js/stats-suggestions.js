const monthOrder = ["Janvier", "Fevrier", "Mars", "Avril", "Mai", "Juin", "Juillet", "Aout", "Septembre", "Octobre", "Novembre", "Decembre"];
const monthShort = ["Jan", "Fév", "Mar", "Avr", "Mai", "Juin", "Juil", "Août", "Sep", "Oct", "Nov", "Déc"];
const TARGET_COLOR = "#2563eb";
const REAL_COLOR = "#f97316";
let scopeDeptTargets = [];
let scopeChartMode = "percent";

// Scopes à ne plus afficher individuellement dans le graphe "Contribution par Scope" :
// leurs suggestions/objectifs sont regroupés dans le scope OTHER_SCOPE_NAME.
const EXCLUDED_SCOPE_NAMES = ["Innovator", "Laboratory"];
const OTHER_SCOPE_NAME = "Other";

Chart.register(ChartDataLabels);
let charts = {};
function destroyChart(key) { if (charts[key]) charts[key].destroy(); }

function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
}

let suggestionsData = [];
let departements = [];
let scopes = [];
let statuts = [];
let moisList = [];
let departmentObjectives = {};
let scopeObjectives = {};
let statutObjectives = [];

async function loadAll() {
    const [filtersRes, suggestionsRes, objRes] = await Promise.all([
        fetch('/StatistiquesSuggestions/GetFilters'),
        fetch('/Initiatives/GetAll'),
        fetch('/StatistiquesSuggestions/GetObjectivesData')
    ]);
    const filters = await filtersRes.json();
    suggestionsData = await suggestionsRes.json();
    const objData = await objRes.json();

    departements = filters.departements;
    scopes = filters.scopes;
    statuts = filters.statuts;
    moisList = filters.moisList;

    departmentObjectives = {};
    objData.departments.forEach(d => {
        departmentObjectives[d.nom] = { id: d.id, headcount: d.headcount, annualTargets: d.annualTargets, monthlyTargets: d.monthlyTargets };
    });
    scopeObjectives = objData.scopeObjectives;
    scopeDeptTargets = objData.scopeDeptTargets;
    statutObjectives = objData.statutObjectives;

    initStatsFilter();
    initGlobalMoisAnneeFilter();
    renderAll();
    renderObjectifsChart();
}

function fillSelect(selectEl, items, includeAll, allLabel) {
    selectEl.innerHTML = "";
    if (includeAll) {
        const opt = document.createElement("option");
        opt.value = "all"; opt.textContent = allLabel;
        selectEl.appendChild(opt);
    }
    items.forEach(item => {
        const opt = document.createElement("option");
        opt.value = item.value ?? item;
        opt.textContent = item.label ?? item;
        selectEl.appendChild(opt);
    });
}

function getEffectivePeriod() {
    let year = document.getElementById("filterAnneeGlobal").value;
    let mois = document.getElementById("filterMoisGlobal").value;

    if (year === "all") year = new Date().getFullYear().toString();
    if (mois === "all") mois = monthOrder[new Date().getMonth()];

    return { year, mois };
}

// Retourne les noms de scope à considérer pour une "carte" de scope donnée :
// pour OTHER_SCOPE_NAME, on inclut aussi les scopes exclus (Innovator, Laboratory).
function getMergedScopeSourceNames(scopeName) {
    return scopeName === OTHER_SCOPE_NAME ? [scopeName, ...EXCLUDED_SCOPE_NAMES] : [scopeName];
}

// Liste des scopes à afficher comme barres distinctes dans le graphe par scope
// (Innovator et Laboratory sont retirés, leurs valeurs étant fusionnées dans "Other").
function getDisplayedScopeNames() {
    return scopes.map(s => s.nom).filter(name => !EXCLUDED_SCOPE_NAMES.includes(name));
}

function getScopeTargetPercent(scopeName, year, mois) {
    const sourceNames = getMergedScopeSourceNames(scopeName);
    const scopeIds = scopes.filter(s => sourceNames.includes(s.nom)).map(s => s.id);
    if (scopeIds.length === 0) return 0;

    let scopeSum = 0;
    let grandTotal = 0;

    scopeDeptTargets.forEach(t => {
        if (t.year.toString() !== year || t.mois !== mois) return;
        grandTotal += t.targetCount;
        if (scopeIds.includes(t.scopeId)) scopeSum += t.targetCount;
    });

    return grandTotal > 0 ? Math.round(scopeSum / grandTotal * 100) : 0;
}
function fillYearSelect(selectEl, allLabel) {
    selectEl.innerHTML = "";
    const optAll = document.createElement("option");
    optAll.value = "all"; optAll.textContent = allLabel;
    selectEl.appendChild(optAll);

    const years = suggestionsData.filter(s => s.dateSuggestion).map(s => new Date(s.dateSuggestion).getFullYear());
    const nowYear = new Date().getFullYear();
    const minYear = years.length ? Math.min(...years, nowYear) : nowYear;
    const maxYear = years.length ? Math.max(...years, nowYear) : nowYear;
    for (let y = minYear; y <= maxYear; y++) {
        const opt = document.createElement("option");
        opt.value = y; opt.textContent = "Année " + y;
        selectEl.appendChild(opt);
    }
}

function initStatsFilter() {
    const select = document.getElementById("filterKaizenStats");
    select.innerHTML = "";
    [["all", "Toutes les suggestions"], ["true", "Kaizen Yes"], ["false", "Kaizen No"]].forEach(([val, label]) => {
        const opt = document.createElement("option");
        opt.value = val; opt.textContent = label;
        select.appendChild(opt);
    });
}

function initGlobalMoisAnneeFilter() {
    fillSelect(document.getElementById("filterMoisGlobal"), moisList, true, "Tous les mois");
    fillYearSelect(document.getElementById("filterAnneeGlobal"), "Toutes les années");
    fillSelect(document.getElementById("filterStatutStats"), statuts, true, "Statut");
}

function getFilteredData() {
    const kaizenFilter = document.getElementById("filterKaizenStats").value;
    const moisFilter = document.getElementById("filterMoisGlobal").value;
    const yearFilter = document.getElementById("filterAnneeGlobal").value;
    const statutFilter = document.getElementById("filterStatutStats").value;

    return suggestionsData.filter(s => {
        const matchKaizen = kaizenFilter === "all" || s.kaizen.toString() === kaizenFilter;
        const matchMois = moisFilter === "all" || s.mois === moisFilter;
        const matchYear = yearFilter === "all" || (s.dateSuggestion && new Date(s.dateSuggestion).getFullYear().toString() === yearFilter);
        const matchStatut = statutFilter === "all" || s.statut === statutFilter;
        return matchKaizen && matchMois && matchYear && matchStatut;
    });
}

function updateKPIs() {
    const data = getFilteredData();
    const total = data.length;
    const economies = data.reduce((sum, s) => sum + Number(s.economies || 0), 0);
    const kaizenYes = data.filter(s => s.kaizen).length;

    document.getElementById("entryCount").textContent = suggestionsData.length + " entrées";
    document.getElementById("kpiTotal").textContent = total;
    document.getElementById("kpiEconomies").textContent = "€ " + economies.toLocaleString('fr-FR');
    document.getElementById("kpiKaizen").textContent = kaizenYes;
    document.getElementById("kpiKaizenPct").textContent = (total ? Math.round(kaizenYes / total * 100) : 0) + "% taux kaizen";
}

function renderEconomiesChart() {
    const data = getFilteredData();
    const economiesByMonth = monthOrder.map(m => data.filter(s => s.mois === m).reduce((sum, s) => sum + Number(s.economies || 0), 0));

    destroyChart("economiesMois");
    charts.economiesMois = new Chart(document.getElementById("chartEconomiesMois"), {
        type: "line",
        data: { labels: monthShort, datasets: [{ data: economiesByMonth, borderColor: "#a01c2b", backgroundColor: "rgba(160,28,43,0.1)", fill: true, tension: 0.4 }] },
        options: { plugins: { legend: { display: false } }, maintainAspectRatio: false }
    });
}

function renderScopeStatutCharts() {
    const data = getFilteredData();
    const total = data.length;

    const scopeNames = getDisplayedScopeNames();
    const period = getEffectivePeriod();

    let scopeReal, scopeTarget;
    if (scopeChartMode === "count") {
        scopeReal = scopeNames.map(name => {
            const sourceNames = getMergedScopeSourceNames(name);
            return data.filter(s => sourceNames.includes(s.scopeName)).length;
        });
        scopeTarget = scopeNames.map(name => {
            const sourceNames = getMergedScopeSourceNames(name);
            const scopeIds = scopes.filter(s => sourceNames.includes(s.nom)).map(s => s.id);
            let sum = 0;
            scopeDeptTargets.forEach(t => {
                if (t.year.toString() === period.year && t.mois === period.mois && scopeIds.includes(t.scopeId)) sum += t.targetCount;
            });
            return sum;
        });
    } else {
        scopeReal = scopeNames.map(name => {
            const sourceNames = getMergedScopeSourceNames(name);
            const count = data.filter(s => sourceNames.includes(s.scopeName)).length;
            return total ? Math.round(count / total * 100) : 0;
        });
        scopeTarget = scopeNames.map(name => getScopeTargetPercent(name, period.year, period.mois));
    }

    const scopeFormatter = scopeChartMode === "count" ? (v => v) : (v => v + "%");
    destroyChart("scope");
    charts.scope = new Chart(document.getElementById("chartParScope"), {
        type: "bar",
        data: {
            labels: scopeNames, datasets: [
                { label: "Target % Contribution", data: scopeTarget, backgroundColor: TARGET_COLOR, borderRadius: 4 },
                { label: "% Real Contribution", data: scopeReal, backgroundColor: REAL_COLOR, borderRadius: 4 }
            ]
        },
        options: {
            maintainAspectRatio: false,
            scales: { x: { ticks: { autoSkip: false, maxRotation: 45, minRotation: 45 } }, y: { beginAtZero: true } },
            plugins: { legend: { position: "bottom" }, datalabels: { anchor: "end", align: "top", color: "#333", font: { weight: "bold", size: 10 }, formatter: scopeFormatter } }
        }
    });

    const statutReal = statuts.map(st => total ? Math.round(data.filter(s => s.statut === st.value).length / total * 100) : 0);
    const statutLabels = statuts.map(st => st.value === "Applique" ? "Applied" : "Not Applied");

    const appliqueCount = getStatutTargetCount(period.year, period.mois);
    const appliqueTargetPct = total > 0 ? Math.round(appliqueCount / total * 100) : 0;
    const statutTarget = statuts.map(st => st.value === "Applique" ? appliqueTargetPct : 0);

    destroyChart("statut");
    charts.statut = new Chart(document.getElementById("chartParStatut"), {
        type: "bar",
        data: {
            labels: statutLabels, datasets: [
                { label: "Target % Contribution", data: statutTarget, backgroundColor: TARGET_COLOR, borderRadius: 4 },
                { label: "% Real Contribution", data: statutReal, backgroundColor: REAL_COLOR, borderRadius: 4 }
            ]
        },
        options: {
            maintainAspectRatio: false,
            scales: { y: { beginAtZero: true } },
            plugins: { legend: { position: "bottom" }, datalabels: { anchor: "end", align: "top", color: "#333", font: { weight: "bold", size: 10 }, formatter: v => v + "%" } }
        }
    });
}

function renderDepartementCharts() {
    const data = getFilteredData();
    const depNames = departements.map(d => d.nom);
    const depEconomies = depNames.map(name => data.filter(s => s.departementName === name).reduce((sum, s) => sum + Number(s.economies || 0), 0));

    destroyChart("economiesDepartement");
    charts.economiesDepartement = new Chart(document.getElementById("chartEconomiesDepartement"), {
        type: "bar",
        data: { labels: depNames, datasets: [{ data: depEconomies, backgroundColor: "#12703c", borderRadius: 4 }] },
        options: { plugins: { legend: { display: false } }, maintainAspectRatio: false, scales: { x: { ticks: { autoSkip: false, maxRotation: 45, minRotation: 45 } } } }
    });
}

function getObjectiveTarget(depName, yearVal, moisVal) {
    const obj = departmentObjectives[depName];
    if (!obj || yearVal === "all") return 0;

    const headcount = obj.headcount || 0;
    if (headcount === 0) return 0;

    const monthLimit = moisVal === "all" ? 11 : monthOrder.indexOf(moisVal);
    let sum = 0;
    for (let i = 0; i <= monthLimit; i++) {
        const m = monthOrder[i];
        const key = yearVal + "-" + m;
        sum += (obj.monthlyTargets && obj.monthlyTargets[key] !== undefined) ? obj.monthlyTargets[key] : 0;
    }
    return Math.round(sum / headcount * 100);
}

function getCumulativeParticipationPct(data, depName, yearVal, moisVal) {
    const obj = departmentObjectives[depName];
    const headcount = obj ? (obj.headcount || 0) : 0;

    let pool = data;
    if (yearVal !== "all") {
        pool = pool.filter(s => s.dateSuggestion && new Date(s.dateSuggestion).getFullYear().toString() === yearVal);
    }
    if (moisVal !== "all") {
        const idx = monthOrder.indexOf(moisVal);
        pool = pool.filter(s => s.mois && monthOrder.indexOf(s.mois) <= idx);
    }
    const count = pool.filter(s => s.departementName === depName).length;

    // Si l'effectif du département n'est pas encore renseigné, on affiche quand même
    // la contribution réelle en % du total des suggestions de la période, au lieu de 0.
    if (headcount === 0) {
        const total = pool.length;
        return total > 0 ? Math.round(count / total * 100) : 0;
    }
    return Math.round(count / headcount * 100);
}

function renderObjectifsChart() {
    const moisVal = document.getElementById("filterMoisGlobal").value;
    const yearVal = document.getElementById("filterAnneeGlobal").value;

    const depNames = departements.map(d => d.nom);
    const targetData = depNames.map(name => getObjectiveTarget(name, yearVal, moisVal));
    const realData = depNames.map(name => getCumulativeParticipationPct(suggestionsData, name, yearVal, moisVal));

    destroyChart("objectifs");
    charts.objectifs = new Chart(document.getElementById("chartObjectifs"), {
        type: "bar",
        data: {
            labels: depNames, datasets: [
                { label: "Target % Contribution", data: targetData, backgroundColor: TARGET_COLOR, borderRadius: 4 },
                { label: "% Real Contribution", data: realData, backgroundColor: REAL_COLOR, borderRadius: 4 }
            ]
        },
        options: {
            maintainAspectRatio: false,
            scales: { x: { ticks: { autoSkip: false, maxRotation: 45, minRotation: 45 } }, y: { beginAtZero: true } },
            plugins: { legend: { position: "bottom" }, datalabels: { anchor: "end", align: "top", color: "#333", font: { weight: "bold", size: 10 }, formatter: v => v + "%" } }
        }
    });
}


function renderAll() {
    updateKPIs();
    renderEconomiesChart();
    renderScopeStatutCharts();
    renderDepartementCharts();
}

function getYearsList() {
    const years = suggestionsData.filter(s => s.dateSuggestion).map(s => new Date(s.dateSuggestion).getFullYear());
    if (!years.length) return [new Date().getFullYear()];
    const minY = Math.min(...years), maxY = Math.max(...years);
    const list = [];
    for (let y = minY; y <= maxY; y++) list.push(y);
    return list;
}

// --- Modal objectifs par département ---
function populateObjectivesTable() {
    const year = document.getElementById("objModalYear").value;
    const tbody = document.getElementById("objectivesTableBody");
    tbody.innerHTML = "";
    departements.forEach(d => {
        const obj = departmentObjectives[d.nom] || { headcount: d.headcount, annualTargets: {}, monthlyTargets: {} };
        const annualVal = (obj.annualTargets && obj.annualTargets[year] !== undefined) ? obj.annualTargets[year] : 0;
        let monthCells = "";
        const nowYear = new Date().getFullYear().toString();
        const nowMois = monthOrder[new Date().getMonth()];

        moisList.forEach(m => {
            const key = year + "-" + m.value;
            const val = (obj.monthlyTargets && obj.monthlyTargets[key] !== undefined) ? obj.monthlyTargets[key] : "";
            const isEditable = (year === nowYear && m.value === nowMois);
            monthCells += `<td><input type="number" min="0" max="100" class="objMonthly" data-dep="${d.id}" data-month="${m.value}" value="${val}" style="width:55px;" ${isEditable ? "" : "disabled"}></td>`;
        });
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${d.nom}</td>
            <td><input type="number" min="0" class="objHeadcount" data-dep="${d.id}" value="${obj.headcount ?? d.headcount}" style="width:70px;"></td>
            <td><input type="number" min="0" max="100" class="objAnnual" data-dep="${d.id}" value="${annualVal}" style="width:70px;"></td>
            ${monthCells}
        `;
        tbody.appendChild(tr);
    });
}

function openObjectivesModal() {
    const yearSelect = document.getElementById("objModalYear");
    yearSelect.innerHTML = "";
    getYearsList().forEach(y => {
        const opt = document.createElement("option");
        opt.value = y; opt.textContent = "Année " + y;
        yearSelect.appendChild(opt);
    });
    const currentObjYear = document.getElementById("filterAnneeGlobal").value;
    if (currentObjYear !== "all") yearSelect.value = currentObjYear;
    populateObjectivesTable();
    document.getElementById("objectivesModal").classList.add("active");
}
function closeObjectivesModal() { document.getElementById("objectivesModal").classList.remove("active"); }

document.getElementById("openObjectivesBtn").addEventListener("click", openObjectivesModal);
document.getElementById("closeObjectivesBtn").addEventListener("click", closeObjectivesModal);
document.getElementById("cancelObjectivesBtn").addEventListener("click", closeObjectivesModal);
document.getElementById("objModalYear").addEventListener("change", populateObjectivesTable);

document.getElementById("saveObjectivesBtn").addEventListener("click", async function () {
    const year = parseInt(document.getElementById("objModalYear").value);
    const rows = {};

    document.querySelectorAll(".objHeadcount").forEach(input => {
        const depId = parseInt(input.dataset.dep);
        rows[depId] = rows[depId] || { departementId: depId, headcount: 0, annualTarget: 0, monthlyTargets: {} };
        rows[depId].headcount = parseInt(input.value) || 0;
    });
    document.querySelectorAll(".objAnnual").forEach(input => {
        const depId = parseInt(input.dataset.dep);
        rows[depId] = rows[depId] || { departementId: depId, headcount: 0, annualTarget: 0, monthlyTargets: {} };
        rows[depId].annualTarget = parseInt(input.value) || 0;
    });
    document.querySelectorAll(".objMonthly").forEach(input => {
        const depId = parseInt(input.dataset.dep);
        rows[depId] = rows[depId] || { departementId: depId, headcount: 0, annualTarget: 0, monthlyTargets: {} };
        if (input.value !== "") rows[depId].monthlyTargets[input.dataset.month] = parseInt(input.value) || 0;
    });

    await fetch('/StatistiquesSuggestions/SaveDepartmentObjectives', {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
        body: JSON.stringify({ year, departments: Object.values(rows) })
    });

    await loadAll();
    closeObjectivesModal();
});

function populateScopeObjTable(year, mois, readOnly) {
    const monthLabel = moisList.find(m => m.value === mois)?.label || mois;
    document.getElementById("scopeObjPeriodLabel").textContent =
        `Objectifs pour ${monthLabel} ${year}` + (readOnly ? " (lecture seule — mois non modifiable)" : "");

    const thead = document.getElementById("scopeObjThead");
    thead.innerHTML = `<tr><th>Département</th>${scopes.map(s => `<th>${s.nom}</th>`).join("")}</tr>`;

    const tbody = document.getElementById("scopeObjBody");
    tbody.innerHTML = "";

    departements.forEach(d => {
        const cells = scopes.map(s => {
            const existing = scopeDeptTargets.find(t =>
                t.departementId === d.id && t.scopeId === s.id && t.year.toString() === year && t.mois === mois);
            const val = existing ? existing.targetCount : 0;
            return `<td><input type="number" min="0" class="scopeDeptInput" data-dep="${d.id}" data-scope="${s.id}" value="${val}" style="width:55px;" ${readOnly ? "disabled" : ""}></td>`;
        }).join("");
        const tr = document.createElement("tr");
        tr.innerHTML = `<td>${d.nom}</td>${cells}`;
        tbody.appendChild(tr);
    });

    const totalRow = document.createElement("tr");
    totalRow.className = "scope-total-row";
    totalRow.innerHTML = `<td style="font-weight:700;">Total</td>${scopes.map(s => `<td id="scopeTotalCount-${s.id}" style="font-weight:700;">0</td>`).join("")}`;
    tbody.appendChild(totalRow);

    const pctRow = document.createElement("tr");
    pctRow.className = "scope-pct-row";
    pctRow.innerHTML = `<td style="font-weight:700;">%</td>${scopes.map(s => `<td id="scopeTotalPct-${s.id}" style="font-weight:700; color:#0d1b4c;">0%</td>`).join("")}`;
    tbody.appendChild(pctRow);

    recalcScopeTotals();

    document.querySelectorAll(".scopeDeptInput").forEach(input => {
        input.addEventListener("input", recalcScopeTotals);
    });
}

function recalcScopeTotals() {
    let grandTotal = 0;
    const perScope = {};

    scopes.forEach(s => {
        let sum = 0;
        document.querySelectorAll(`.scopeDeptInput[data-scope="${s.id}"]`).forEach(input => {
            sum += parseInt(input.value) || 0;
        });
        perScope[s.id] = sum;
        grandTotal += sum;
    });

    scopes.forEach(s => {
        const countEl = document.getElementById(`scopeTotalCount-${s.id}`);
        const pctEl = document.getElementById(`scopeTotalPct-${s.id}`);
        if (countEl) countEl.textContent = perScope[s.id];
        if (pctEl) pctEl.textContent = (grandTotal > 0 ? Math.round(perScope[s.id] / grandTotal * 100) : 0) + "%";
    });
}

function openScopeObjModal() {
    const period = getEffectivePeriod();
    const readOnly = !isCurrentPeriod(period.year, period.mois);
    populateScopeObjTable(period.year, period.mois, readOnly);
    const modal = document.getElementById("scopeObjModal");
    modal.dataset.year = period.year;
    modal.dataset.mois = period.mois;
    modal.dataset.readonly = readOnly ? "true" : "false";
    document.getElementById("saveScopeObjBtn").addEventListener("click", async function () {
        const modal = document.getElementById("scopeObjModal");
        if (modal.dataset.readonly === "true") return;

        const year = modal.dataset.year;
        const mois = modal.dataset.mois;

        const targets = [];
        document.querySelectorAll(".scopeDeptInput").forEach(input => {
            targets.push({
                departementId: parseInt(input.dataset.dep),
                scopeId: parseInt(input.dataset.scope),
                targetCount: parseInt(input.value) || 0
            });
        });

        await fetch('/StatistiquesSuggestions/SaveDepartementScopeObjectives', {
            method: "POST",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
            body: JSON.stringify({ year: parseInt(year), mois, targets })
        });

        await loadAll();
        document.getElementById("filterAnneeGlobal").value = year;
        document.getElementById("filterMoisGlobal").value = mois;
        renderAll();
        renderObjectifsChart();
        closeScopeObjModal();
    });
    modal.classList.add("active");
}
function closeScopeObjModal() { document.getElementById("scopeObjModal").classList.remove("active"); }

document.getElementById("openScopeStatutObjBtn").addEventListener("click", openScopeObjModal);
document.getElementById("closeScopeObjBtn").addEventListener("click", closeScopeObjModal);
document.getElementById("cancelScopeObjBtn").addEventListener("click", closeScopeObjModal);

document.getElementById("saveScopeObjBtn").addEventListener("click", async function () {
    const modal = document.getElementById("scopeObjModal");
    const year = parseInt(modal.dataset.year);
    const mois = modal.dataset.mois;

    const targets = [];
    document.querySelectorAll(".scopeDeptInput").forEach(input => {
        targets.push({
            departementId: parseInt(input.dataset.dep),
            scopeId: parseInt(input.dataset.scope),
            targetCount: parseInt(input.value) || 0
        });
    });

    await fetch('/StatistiquesSuggestions/SaveDepartementScopeObjectives', {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
        body: JSON.stringify({ year, mois, targets })
    });

    await loadAll();
    closeScopeObjModal();
});

function closeScopeObjModal() { document.getElementById("scopeObjModal").classList.remove("active"); }

document.getElementById("openScopeStatutObjBtn").addEventListener("click", openScopeObjModal);
document.getElementById("closeScopeObjBtn").addEventListener("click", closeScopeObjModal);
document.getElementById("cancelScopeObjBtn").addEventListener("click", closeScopeObjModal);

// --- Modal objectifs Statut ---
function openStatutObjModal() {
    const period = getEffectivePeriod();
    const readOnly = !isCurrentPeriod(period.year, period.mois);

    const modal = document.getElementById("statutObjModal");
    modal.dataset.year = period.year;
    modal.dataset.mois = period.mois;
    modal.dataset.readonly = readOnly ? "true" : "false";
    document.getElementById("saveStatutObjBtn").addEventListener("click", async function () {
        const modal = document.getElementById("statutObjModal");
        if (modal.dataset.readonly === "true") return;

        const year = modal.dataset.year;
        const mois = modal.dataset.mois;
        const input = document.getElementById("statutObjInput");

        await fetch('/StatistiquesSuggestions/SaveStatutObjectives', {
            method: "POST",
            headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
            body: JSON.stringify({ year: parseInt(year), mois, targets: [{ statut: "Applique", targetCount: parseInt(input.value) || 0 }] })
        });

        await loadAll();
        document.getElementById("filterAnneeGlobal").value = year;
        document.getElementById("filterMoisGlobal").value = mois;
        renderAll();
        closeStatutObjModal();
    });

    const tbody = document.querySelector("#statutObjTable tbody");
    tbody.innerHTML = "";
    const applique = statuts.find(st => st.value === "Applique");
    if (applique) {
        const currentCount = getStatutTargetCount(period.year, period.mois);
        const total = getFilteredData().length;
        const pct = total > 0 ? Math.round(currentCount / total * 100) : 0;

        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${applique.label}</td>
            <td><input type="number" min="0" id="statutObjInput" value="${currentCount}" style="width:80px;" ${readOnly ? "disabled" : ""}></td>
            <td id="statutObjPctPreview" style="font-weight:700; color:#0d1b4c;">${pct}%</td>
        `;
        tbody.appendChild(tr);

        document.getElementById("statutObjInput").addEventListener("input", function () {
            const val = parseInt(this.value) || 0;
            const p = total > 0 ? Math.round(val / total * 100) : 0;
            document.getElementById("statutObjPctPreview").textContent = p + "%";
        });
    }
    document.getElementById("statutObjModal").classList.add("active");
}
function closeStatutObjModal() { document.getElementById("statutObjModal").classList.remove("active"); }

document.getElementById("openStatutObjBtn").addEventListener("click", openStatutObjModal);
document.getElementById("closeStatutObjBtn").addEventListener("click", closeStatutObjModal);
document.getElementById("cancelStatutObjBtn").addEventListener("click", closeStatutObjModal);

document.getElementById("saveStatutObjBtn").addEventListener("click", async function () {
    const modal = document.getElementById("statutObjModal");
    if (modal.dataset.readonly === "true") return;

    const input = document.getElementById("statutObjInput");
    const payload = {
        year: parseInt(modal.dataset.year),
        mois: modal.dataset.mois,
        targets: [{ statut: "Applique", targetCount: parseInt(input.value) || 0 }]
    };

    await fetch('/StatistiquesSuggestions/SaveStatutObjectives', {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
        body: JSON.stringify(payload)
    });
    await loadAll();
    closeStatutObjModal();
});

// --- Modal effectif par département ---
function openHeadcountModal() {
    const tbody = document.querySelector("#headcountTable tbody");
    tbody.innerHTML = "";
    departements.forEach(d => {
        const tr = document.createElement("tr");
        tr.innerHTML = `<td>${d.nom}</td><td><input type="number" min="0" class="headcountInput" data-dep="${d.id}" value="${d.headcount || 0}" style="width:90px;"></td>`;
        tbody.appendChild(tr);
    });
    document.getElementById("headcountModal").classList.add("active");
}
function closeHeadcountModal() { document.getElementById("headcountModal").classList.remove("active"); }

document.getElementById("openHeadcountBtn").addEventListener("click", openHeadcountModal);
document.getElementById("closeHeadcountBtn").addEventListener("click", closeHeadcountModal);
document.getElementById("cancelHeadcountBtn").addEventListener("click", closeHeadcountModal);

document.getElementById("saveHeadcountBtn").addEventListener("click", async function () {
    const payload = [];
    document.querySelectorAll(".headcountInput").forEach(input => {
        payload.push({ departementId: parseInt(input.dataset.dep), headcount: parseInt(input.value) || 0 });
    });
    await fetch('/StatistiquesSuggestions/SaveHeadcounts', {
        method: "POST",
        headers: { "Content-Type": "application/json", "RequestVerificationToken": getToken() },
        body: JSON.stringify(payload)
    });
    await loadAll();
    closeHeadcountModal();
});

["filterKaizenStats", "filterMoisGlobal", "filterAnneeGlobal", "filterStatutStats"].forEach(id => {
    document.getElementById(id).addEventListener("change", () => { renderAll(); renderObjectifsChart(); });
});

loadAll();

document.getElementById("scopeModePct").addEventListener("click", function () {
    scopeChartMode = "percent";
    this.classList.add("active");
    document.getElementById("scopeModeCount").classList.remove("active");
    renderScopeStatutCharts();
});
document.getElementById("scopeModeCount").addEventListener("click", function () {
    scopeChartMode = "count";
    this.classList.add("active");
    document.getElementById("scopeModePct").classList.remove("active");
    renderScopeStatutCharts();
});

function isCurrentPeriod(year, mois) {
    const nowYear = new Date().getFullYear().toString();
    const nowMois = monthOrder[new Date().getMonth()];
    return year === nowYear && mois === nowMois;
}

function getStatutTargetCount(year, mois) {
    const entry = statutObjectives.find(o => o.statut === "Applique" && o.year.toString() === year && o.mois === mois);
    return entry ? entry.targetCount : 0;
}