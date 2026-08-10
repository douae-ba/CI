const monthOrder = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
const monthShort = ["Jan", "Fév", "Mar", "Avr", "Mai", "Juin", "Juil", "Août", "Sep", "Oct", "Nov", "Déc"];
const TARGET_COLOR = "#2563eb";
const REAL_COLOR = "#f97316";

const ENGLISH_TO_FRENCH_MOIS = {
    "January": "Janvier", "February": "Fevrier", "March": "Mars", "April": "Avril",
    "May": "Mai", "June": "Juin", "July": "Juillet", "August": "Aout",
    "September": "Septembre", "October": "Octobre", "November": "Novembre", "December": "Decembre"
};

const TYPE_MAP = { "Quick Kaizen": "Kaizen", "A3 Kaizen": "A3", "TIE Kaizen": "VAVE" };

Chart.register(ChartDataLabels);
let charts = {};
function destroyChart(key) { if (charts[key]) charts[key].destroy(); }

function getToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value;
}

let targetRows = [];
let departements = [];
let scopes = [];
let statuts = [];
let moisList = [];
let departmentObjectives = {};
let scopeObjectives = {};
let scopeDeptTargets = [];
let statutObjectives = [];
let scopeChartMode = "percent";

async function loadAll() {
    const [dataRes, filtersRes, objRes] = await Promise.all([
        fetch('/StatistiquesGlobale/GetData'),
        fetch('/StatistiquesSuggestions/GetFilters'),
        fetch('/StatistiquesSuggestions/GetObjectivesData')
    ]);
    const dataResult = await dataRes.json();
    const filters = await filtersRes.json();
    const objData = await objRes.json();

    if (!dataResult.success) {
        document.querySelector('.page-body').insertAdjacentHTML('afterbegin',
            `<div class="empty-state-card" style="margin-bottom:20px;"><p class="empty-state-title">Impossible de charger les statistiques</p><p class="empty-state-text">${dataResult.message}</p></div>`);
        targetRows = [];
    } else {
        targetRows = dataResult.rows;
        if (dataResult.warnings && dataResult.warnings.length) {
            console.warn("Fichiers cibles ignorés (introuvables ou illisibles) :", dataResult.warnings);
        }
    }

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

    initFilters();
    renderAll();
}

function fillSelect(selectEl, options, includeAll, allLabel) {
    const current = selectEl.value;
    selectEl.innerHTML = "";
    if (includeAll) {
        const opt = document.createElement("option");
        opt.value = "all"; opt.textContent = allLabel;
        selectEl.appendChild(opt);
    }
    options.forEach(([val, label]) => {
        const opt = document.createElement("option");
        opt.value = val; opt.textContent = label;
        selectEl.appendChild(opt);
    });
    if ([...selectEl.options].some(o => o.value === current)) selectEl.value = current;
}

function initFilters() {
    fillSelect(document.getElementById("filterTypeGlobal"),
        [["Quick Kaizen", "Kaizen"], ["A3 Kaizen", "A3"], ["TIE Kaizen", "VAVE"]], true, "Tous les types");

    fillSelect(document.getElementById("filterDelay"),
        [["No delayed", "Pas de retard"], ["Delayed", "En retard"]], true, "Tous (retard)");

    fillSelect(document.getElementById("filterCompletion"),
        [["Completed", "Terminé"], ["Ongoing", "En cours"]], true, "Toutes (avancement)");

    const pdcaValues = [...new Set(targetRows.map(r => r.currentStatus).filter(Boolean))].sort();
    fillSelect(document.getElementById("filterPdca"), pdcaValues.map(v => [v, v]), true, "Toutes les étapes");

    fillSelect(document.getElementById("filterMoisGlobal"), monthOrder.map(m => [m, m]), true, "Tous les mois");

    const years = targetRows.filter(r => r.registerDate).map(r => new Date(r.registerDate).getFullYear());
    const nowYear = new Date().getFullYear();
    const minY = years.length ? Math.min(...years, nowYear) : nowYear;
    const maxY = years.length ? Math.max(...years, nowYear) : nowYear;
    const yearOptions = [];
    for (let y = minY; y <= maxY; y++) yearOptions.push([y.toString(), "Année " + y]);
    fillSelect(document.getElementById("filterAnneeGlobal"), yearOptions, true, "Toutes les années");
}

function getFilteredRows() {
    const type = document.getElementById("filterTypeGlobal").value;
    const delay = document.getElementById("filterDelay").value;
    const completion = document.getElementById("filterCompletion").value;
    const pdca = document.getElementById("filterPdca").value;
    const mois = document.getElementById("filterMoisGlobal").value;
    const year = document.getElementById("filterAnneeGlobal").value;

    return targetRows.filter(r => {
        const matchType = type === "all" || r.improvementType === type;
        const matchDelay = delay === "all" || r.delayStatus === delay;
        const matchCompletion = completion === "all" || r.closureStatus === completion;
        const matchPdca = pdca === "all" || r.currentStatus === pdca;
        const matchMois = mois === "all" || r.registerMonth === mois;
        const matchYear = year === "all" || (r.registerDate && new Date(r.registerDate).getFullYear().toString() === year);
        return matchType && matchDelay && matchCompletion && matchPdca && matchMois && matchYear;
    });
}

function getEffectivePeriod() {
    let year = document.getElementById("filterAnneeGlobal").value;
    let moisEn = document.getElementById("filterMoisGlobal").value;

    if (year === "all") year = new Date().getFullYear().toString();
    if (moisEn === "all") moisEn = monthOrder[new Date().getMonth()];

    const mois = ENGLISH_TO_FRENCH_MOIS[moisEn] || moisEn;
    return { year, mois };
}

function isCurrentPeriod(year, mois) {
    const nowYear = new Date().getFullYear().toString();
    const nowMoisEn = monthOrder[new Date().getMonth()];
    const nowMoisFr = ENGLISH_TO_FRENCH_MOIS[nowMoisEn];
    return year === nowYear && mois === nowMoisFr;
}

function getStatutTargetCount(year, mois) {
    const entry = statutObjectives.find(o => o.statut === "Applique" && o.year.toString() === year && o.mois === mois);
    return entry ? entry.targetCount : 0;
}

function getScopeTargetPercent(scopeName, year, mois) {
    const scope = scopes.find(s => s.nom === scopeName);
    if (!scope) return 0;

    let scopeSum = 0;
    let grandTotal = 0;

    scopeDeptTargets.forEach(t => {
        if (t.year.toString() !== year || t.mois !== mois) return;
        grandTotal += t.targetCount;
        if (t.scopeId === scope.id) scopeSum += t.targetCount;
    });

    return grandTotal > 0 ? Math.round(scopeSum / grandTotal * 100) : 0;
}

function updateKPIs() {
    const data = getFilteredRows();
    const total = data.length;
    const kaizenCount = data.filter(r => r.improvementType === "Quick Kaizen").length;
    const a3Count = data.filter(r => r.improvementType === "A3 Kaizen").length;
    const vaveCount = data.filter(r => r.improvementType === "TIE Kaizen").length;

    document.getElementById("entryCount").textContent = targetRows.length + " entrées";
    document.getElementById("kpiKaizenCount").textContent = kaizenCount;
    document.getElementById("kpiKaizenPct").textContent = (total ? Math.round(kaizenCount / total * 100) : 0) + "% du total";
    document.getElementById("kpiA3Count").textContent = a3Count;
    document.getElementById("kpiA3Pct").textContent = (total ? Math.round(a3Count / total * 100) : 0) + "% du total";
    document.getElementById("kpiVaveCount").textContent = vaveCount;
    document.getElementById("kpiVavePct").textContent = (total ? Math.round(vaveCount / total * 100) : 0) + "% du total";
}

function renderKaizenTable() {
    const data = getFilteredRows();
    const depNames = departements.map(d => d.nom);
    const scopeNames = scopes.map(s => s.nom);

    const thead = document.getElementById("kaizenSuggThead");
    thead.innerHTML = `<tr>
        <th>Département</th>
        <th># Kaizens</th>
        <th># Suggestions</th>
        ${scopeNames.map(sc => `<th># Kaizen ${sc}</th>`).join("")}
        <th># A3</th>
    </tr>`;

    const tbody = document.getElementById("kaizenSuggTbody");
    tbody.innerHTML = "";

    const scopeTotals = {};
    scopeNames.forEach(sc => scopeTotals[sc] = 0);
    let totalKaizens = 0, totalSuggestions = 0, totalA3 = 0;

    depNames.forEach(depName => {
        const rows = data.filter(r => r.department === depName);
        const kaizenRows = rows.filter(r => r.improvementType === "Quick Kaizen");
        const a3Count = rows.filter(r => r.improvementType === "A3 Kaizen").length;

        const scopeCells = scopeNames.map(sc => {
            const count = kaizenRows.filter(r => r.attackedLosses === sc).length;
            scopeTotals[sc] += count;
            return `<td>${count}</td>`;
        }).join("");

        totalKaizens += kaizenRows.length;
        totalSuggestions += rows.length;
        totalA3 += a3Count;

        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td>${depName}</td>
            <td>${kaizenRows.length}</td>
            <td>${rows.length}</td>
            ${scopeCells}
            <td>${a3Count}</td>
        `;
        tbody.appendChild(tr);
    });

    const totalRow = document.createElement("tr");
    totalRow.className = "scope-total-row";
    totalRow.innerHTML = `
        <td style="font-weight:700;">Total</td>
        <td style="font-weight:700;">${totalKaizens}</td>
        <td style="font-weight:700;">${totalSuggestions}</td>
        ${scopeNames.map(sc => `<td style="font-weight:700;">${scopeTotals[sc]}</td>`).join("")}
        <td style="font-weight:700;">${totalA3}</td>
    `;
    tbody.appendChild(totalRow);

    const pctRow = document.createElement("tr");
    pctRow.className = "scope-pct-row";
    pctRow.innerHTML = `
        <td style="font-weight:700;">%</td>
        <td></td>
        <td></td>
        ${scopeNames.map(sc => {
        const pct = totalKaizens > 0 ? Math.round(scopeTotals[sc] / totalKaizens * 100) : 0;
        return `<td style="font-weight:700; color:#0d1b4c;">${pct}%</td>`;
    }).join("")}
        <td></td>
    `;
    tbody.appendChild(pctRow);
}

function renderScopeStatutCharts() {
    const data = getFilteredRows();
    const total = data.length;
    const period = getEffectivePeriod();

    const scopeNames = scopes.map(s => s.nom);
    let scopeReal, scopeTarget;

    if (scopeChartMode === "count") {
        scopeReal = scopeNames.map(name => data.filter(r => r.attackedLosses === name).length);
        scopeTarget = scopeNames.map(name => {
            const scopeObj = scopes.find(s => s.nom === name);
            if (!scopeObj) return 0;
            let sum = 0;
            scopeDeptTargets.forEach(t => {
                if (t.year.toString() === period.year && t.mois === period.mois && t.scopeId === scopeObj.id) sum += t.targetCount;
            });
            return sum;
        });
    } else {
        scopeReal = scopeNames.map(name => total ? Math.round(data.filter(r => r.attackedLosses === name).length / total * 100) : 0);
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

    const closureLabels = ["Completed", "Ongoing"];
    const statutDisplay = ["Applied", "Not Applied"];
    const closureReal = closureLabels.map(v => total ? Math.round(data.filter(r => r.closureStatus === v).length / total * 100) : 0);

    const appliqueCount = getStatutTargetCount(period.year, period.mois);
    const appliqueTargetPct = total > 0 ? Math.round(appliqueCount / total * 100) : 0;
    const closureTarget = [appliqueTargetPct, 0];

    destroyChart("statut");
    charts.statut = new Chart(document.getElementById("chartParStatut"), {
        type: "bar",
        data: {
            labels: statutDisplay, datasets: [
                { label: "Target % Contribution", data: closureTarget, backgroundColor: TARGET_COLOR, borderRadius: 4 },
                { label: "% Real Contribution", data: closureReal, backgroundColor: REAL_COLOR, borderRadius: 4 }
            ]
        },
        options: {
            maintainAspectRatio: false,
            scales: { y: { beginAtZero: true } },
            plugins: { legend: { position: "bottom" }, datalabels: { anchor: "end", align: "top", color: "#333", font: { weight: "bold", size: 10 }, formatter: v => v + "%" } }
        }
    });
}

function getObjectiveTarget(depName, yearVal, moisValEn) {
    const obj = departmentObjectives[depName];
    if (!obj || yearVal === "all") return 0;

    const headcount = obj.headcount || 0;
    if (headcount === 0) return 0;

    const monthLimitIdx = moisValEn === "all" ? 11 : monthOrder.indexOf(moisValEn);
    let sum = 0;
    for (let i = 0; i <= monthLimitIdx; i++) {
        const mFr = ENGLISH_TO_FRENCH_MOIS[monthOrder[i]];
        const key = yearVal + "-" + mFr;
        sum += (obj.monthlyTargets && obj.monthlyTargets[key] !== undefined) ? obj.monthlyTargets[key] : 0;
    }
    return Math.round(sum / headcount * 100);
}

function getCumulativeParticipationPct(data, depName, yearVal, moisValEn) {
    const obj = departmentObjectives[depName];
    const headcount = obj ? (obj.headcount || 0) : 0;

    let pool = data;
    if (yearVal !== "all") {
        pool = pool.filter(r => r.registerDate && new Date(r.registerDate).getFullYear().toString() === yearVal);
    }
    if (moisValEn !== "all") {
        const idx = monthOrder.indexOf(moisValEn);
        pool = pool.filter(r => r.registerMonth && monthOrder.indexOf(r.registerMonth) <= idx);
    }
    const count = pool.filter(r => r.department === depName).length;

    if (headcount === 0) {
        const total = pool.length;
        return total > 0 ? Math.round(count / total * 100) : 0;
    }
    return Math.round(count / headcount * 100);
}

function renderObjectifsChart() {
    const moisVal = document.getElementById("filterMoisGlobal").value;
    const yearVal = document.getElementById("filterAnneeGlobal").value;
    const data = getFilteredRows();

    const depNames = departements.map(d => d.nom);
    const targetData = depNames.map(name => getObjectiveTarget(name, yearVal, moisVal));
    const realData = depNames.map(name => getCumulativeParticipationPct(data, name, yearVal, moisVal));

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

function renderEconomiesCharts() {
    const data = getFilteredRows();

    const byMonth = monthOrder.map(m => data.filter(r => r.registerMonth === m).reduce((sum, r) => sum + Number(r.totalSaving || 0), 0));
    destroyChart("economiesMois");
    charts.economiesMois = new Chart(document.getElementById("chartEconomiesMois"), {
        type: "line",
        data: { labels: monthShort, datasets: [{ data: byMonth, borderColor: "#a01c2b", backgroundColor: "rgba(160,28,43,0.1)", fill: true, tension: 0.4 }] },
        options: { plugins: { legend: { display: false } }, maintainAspectRatio: false }
    });

    const depNames = departements.map(d => d.nom);
    const byDept = depNames.map(name => data.filter(r => r.department === name).reduce((sum, r) => sum + Number(r.totalSaving || 0), 0));
    destroyChart("economiesDepartement");
    charts.economiesDepartement = new Chart(document.getElementById("chartEconomiesDepartement"), {
        type: "bar",
        data: { labels: depNames, datasets: [{ data: byDept, backgroundColor: "#12703c", borderRadius: 4 }] },
        options: { plugins: { legend: { display: false } }, maintainAspectRatio: false, scales: { x: { ticks: { autoSkip: false, maxRotation: 45, minRotation: 45 } } } }
    });
}

function renderAll() {
    updateKPIs();
    renderKaizenTable();
    renderScopeStatutCharts();
    renderObjectifsChart();
    renderEconomiesCharts();
}

function getYearsList() {
    const years = targetRows.filter(r => r.registerDate).map(r => new Date(r.registerDate).getFullYear());
    if (!years.length) return [new Date().getFullYear()];
    const minY = Math.min(...years), maxY = Math.max(...years);
    const list = [];
    for (let y = minY; y <= maxY; y++) list.push(y);
    return list;
}

function populateObjectivesTable() {
    const year = document.getElementById("objModalYear").value;
    const tbody = document.getElementById("objectivesTableBody");
    tbody.innerHTML = "";
    const nowYear = new Date().getFullYear().toString();
    const nowMoisFr = ENGLISH_TO_FRENCH_MOIS[monthOrder[new Date().getMonth()]];

    departements.forEach(d => {
        const obj = departmentObjectives[d.nom] || { headcount: d.headcount, annualTargets: {}, monthlyTargets: {} };
        const annualVal = (obj.annualTargets && obj.annualTargets[year] !== undefined) ? obj.annualTargets[year] : 0;
        let monthCells = "";
        monthOrder.forEach(mEn => {
            const mFr = ENGLISH_TO_FRENCH_MOIS[mEn];
            const key = year + "-" + mFr;
            const val = (obj.monthlyTargets && obj.monthlyTargets[key] !== undefined) ? obj.monthlyTargets[key] : "";
            const isEditable = (year === nowYear && mFr === nowMoisFr);
            monthCells += `<td><input type="number" min="0" max="100" class="objMonthly" data-dep="${d.id}" data-month="${mFr}" value="${val}" style="width:55px;" ${isEditable ? "" : "disabled"}></td>`;
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
    const currentYear = document.getElementById("filterAnneeGlobal").value;
    if (currentYear !== "all") yearSelect.value = currentYear;
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
        if (input.disabled) return;
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
    const monthLabel = Object.keys(ENGLISH_TO_FRENCH_MOIS).find(k => ENGLISH_TO_FRENCH_MOIS[k] === mois) || mois;
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
        const moisEn = Object.keys(ENGLISH_TO_FRENCH_MOIS).find(k => ENGLISH_TO_FRENCH_MOIS[k] === mois);
        document.getElementById("filterAnneeGlobal").value = year;
        document.getElementById("filterMoisGlobal").value = moisEn;
        renderAll();
        closeScopeObjModal();
    });
    modal.classList.add("active");
}
function closeScopeObjModal() { document.getElementById("scopeObjModal").classList.remove("active"); }

document.getElementById("openScopeObjBtn").addEventListener("click", openScopeObjModal);
document.getElementById("closeScopeObjBtn").addEventListener("click", closeScopeObjModal);
document.getElementById("cancelScopeObjBtn").addEventListener("click", closeScopeObjModal);

document.getElementById("saveScopeObjBtn").addEventListener("click", async function () {
    const modal = document.getElementById("scopeObjModal");
    if (modal.dataset.readonly === "true") return;

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

document.getElementById("openHeadcountBtn").addEventListener("click", function () {
    const tbody = document.querySelector("#headcountTable tbody");
    tbody.innerHTML = "";
    departements.forEach(d => {
        const tr = document.createElement("tr");
        tr.innerHTML = `<td>${d.nom}</td><td><input type="number" min="0" class="headcountInput" data-dep="${d.id}" value="${d.headcount || 0}" style="width:90px;"></td>`;
        tbody.appendChild(tr);
    });
    document.getElementById("headcountModal").classList.add("active");
});
document.getElementById("closeHeadcountBtn").addEventListener("click", () => document.getElementById("headcountModal").classList.remove("active"));
document.getElementById("cancelHeadcountBtn").addEventListener("click", () => document.getElementById("headcountModal").classList.remove("active"));
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
    document.getElementById("headcountModal").classList.remove("active");
});

document.getElementById("openStatutObjBtn")?.addEventListener("click", function () {
    const period = getEffectivePeriod();
    const readOnly = !isCurrentPeriod(period.year, period.mois);

    const modal = document.getElementById("statutObjModal");
    modal.dataset.year = period.year;
    modal.dataset.mois = period.mois;
    modal.dataset.readonly = readOnly ? "true" : "false";
    document.getElementById("saveStatutObjBtn")?.addEventListener("click", async function () {
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
        const moisEn = Object.keys(ENGLISH_TO_FRENCH_MOIS).find(k => ENGLISH_TO_FRENCH_MOIS[k] === mois);
        document.getElementById("filterAnneeGlobal").value = year;
        document.getElementById("filterMoisGlobal").value = moisEn;
        renderAll();
        document.getElementById("statutObjModal").classList.remove("active");
    });

    const tbody = document.querySelector("#statutObjTable tbody");
    tbody.innerHTML = "";
    const applique = statuts.find(st => st.value === "Applique");
    if (applique) {
        const currentCount = getStatutTargetCount(period.year, period.mois);
        const total = getFilteredRows().length;
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
});
document.getElementById("closeStatutObjBtn")?.addEventListener("click", () => document.getElementById("statutObjModal").classList.remove("active"));
document.getElementById("cancelStatutObjBtn")?.addEventListener("click", () => document.getElementById("statutObjModal").classList.remove("active"));
document.getElementById("saveStatutObjBtn")?.addEventListener("click", async function () {
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
    document.getElementById("statutObjModal").classList.remove("active");
});

document.getElementById("scopeModePct")?.addEventListener("click", function () {
    scopeChartMode = "percent";
    this.classList.add("active");
    document.getElementById("scopeModeCount").classList.remove("active");
    renderScopeStatutCharts();
});
document.getElementById("scopeModeCount")?.addEventListener("click", function () {
    scopeChartMode = "count";
    this.classList.add("active");
    document.getElementById("scopeModePct").classList.remove("active");
    renderScopeStatutCharts();
});

["filterTypeGlobal", "filterDelay", "filterCompletion", "filterPdca", "filterMoisGlobal", "filterAnneeGlobal"].forEach(id => {
    document.getElementById(id).addEventListener("change", renderAll);
});

loadAll();