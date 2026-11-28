import { Chart, registerables } from "chart.js";
import { reportsApi, type DashboardResponse } from "../api/reports";
import { ORDER_STATUS_LABELS } from "../api/orders";
import { escHtml, money, formatDateOnly, isoDate } from "../utils/format";

Chart.register(...registerables);

let revenueChart: Chart | null = null;
let requestId = 0;

export function initDashboardPage(container: HTMLElement): void {
    requestId++;
    revenueChart?.destroy();
    revenueChart = null;

    container.innerHTML = pageTemplate();
    bindEvents();
    loadDashboard();
}

async function loadDashboard(from?: string, to?: string): Promise<void> {
    const body = document.getElementById('dashboard-body');
    if (!body) return;

    const myRequest = ++requestId;
    const isStale = () => myRequest !== requestId || !body.isConnected;

    body.innerHTML = '<p class="text-muted py-4 text-center">Loading…</p>';

    try {
        const { data } = await reportsApi.getDashboard({ from, to, top: 5 });
        if (isStale()) return;

        fromInput().value = data.from;
        toInput().value = data.to;

        body.innerHTML = renderBody(data);
        renderRevenueChart(body, data);
    } catch (err) {
        if (isStale()) return;

        revenueChart?.destroy();
        revenueChart = null;
        body.innerHTML = 
            `<div class="alert alert-danger">${escHtml((err as Error).message)}</div>`;
    }
}

function pageTemplate(): string {
    return `
        <div class="d-flex flex-wrap justify-content-between align-items-end gap-3 mb-4">
            <h1 class="h3 mb-0">Dashboard</h1>

            <div class="d-flex flex-wrap align-items-end gap-2">
                    <div class="btn-group btn-group-sm" role="group">
                        <button class="btn btn-outline-secondary" data-preset="7">7d</button>
                        <button class="btn btn-outline-secondary" data-preset="30">30d</button>
                        <button class="btn btn-outline-secondary" data-preset="90">90d</button>
                    </div>
                    <div>
                        <label class="form-label small mb-1" for="filter-from">From</label>
                        <input type="date" id="filter-from" class="form-control form-control-sm">
                    </div>
                    <div>
                        <label class="form-label small mb-1" for="filter-to">To</label>
                        <input type="date" id="filter-to" class="form-control form-control-sm">
                    </div>
                <button class="btn btn-primary btn-sm" id="btn-apply-range">Apply</button>
            </div>
        </div>

        <div id="range-error" class="alert alert-warning d-none small py-2"></div>
        <div id="dashboard-body"></div>`;
}

function renderBody(d: DashboardResponse): string {
    const s = d.summary;
    return `
    <!-- Summary cards -->
    <div class="row g-3 mb-4">
      ${card('Gross revenue', money(s.grossRevenue), 'Paid orders, incl. tax')}
      ${card('Net revenue', money(s.netRevenue), `${money(s.taxCollected)} tax collected`)}
      ${card('Paid orders', String(s.orderCount),
             `${formatDateOnly(d.from)} – ${formatDateOnly(d.to)}`)}
      ${card('Avg. order value', money(s.averageOrderValue), 'Gross ÷ paid orders')}
    </div>

    <!-- Daily revenue -->
    <div class="card border-0 shadow-sm mb-4">
      <div class="card-body">
        <div class="small text-muted fw-semibold mb-3">DAILY REVENUE</div>
            <div style="position:relative;height:280px">
            <canvas id="revenue-chart"></canvas>
            </div>
      </div>
    </div>

    <div class="row g-3">
      <!-- Top products -->
      <div class="col-lg-7">
        <div class="card border-0 shadow-sm h-100">
          <div class="card-body">
            <div class="small text-muted fw-semibold mb-3">TOP PRODUCTS</div>
            ${renderTopProducts(d)}
          </div>
        </div>
      </div>

      <!-- Status breakdown -->
      <div class="col-lg-5">
        <div class="card border-0 shadow-sm h-100">
          <div class="card-body">
            <div class="small text-muted fw-semibold mb-3">ORDERS BY STATUS</div>
            ${renderStatusBreakdown(d)}
          </div>
        </div>
      </div>
    </div>`; 
}

function card(title: string, value: string, hint: string): string {
    return `
        <div class="col-sm-6 col-xl-3">
            <div class="card border-0 shadow-sm h-100">
                <div class="card-body">
                    <div class="text-muted small">${title}</div>
                    <div class="fs-4 fw-bold">${value}</div>
                    <div class="text-muted" style="font-size:.75rem">${escHtml(hint)}</div>
                </div>
            </div>
        </div>`;
}

function renderTopProducts(d: DashboardResponse): string {
    if (d.topProducts.length === 0) {
        return '<p class="text-muted small mb-0">No paid sales in this period.</p>';
    }

    return `
        <table class="table table-sm align-middle mb-0">
            <thead class="table-light">
                <tr>
                <th>Product</th>
                <th class="text-end">Units</th>
                <th class="text-end">Revenue</th>
                </tr>
            </thead>
            <tbody>
                ${d.topProducts.map(p => `
                    <tr>
                        <td>
                            <div class="small fw-semibold">${escHtml(p.name)}</div>
                            <div class="text-muted font-monospace" style="font-size:.72rem">
                                ${escHtml(p.sku)}
                            </div>
                        </td>
                        <td class="text-end">${p.unitsSold}</td>
                        <td class="text-end fw-semibold">${money(p.revenue)}</td>
                    </tr>`).join('')}
            </tbody>
        </table>`;
}

function renderStatusBreakdown(d: DashboardResponse): string {
    const total = d.statusBreakdown.reduce((sum, s) => sum + s.count, 0);
    if (total === 0) {
        return '<p class="text-muted small mb-0">No orders in this period.</p>';
    }

    return d.statusBreakdown.map(s => {
        const pct = Math.round((s.count / total) * 100);
        return `
            <div class="mb-2">
                <div class="d-flex justify-content-between small">
                    <span>${ORDER_STATUS_LABELS[s.status]}</span>
                    <span class="text-muted">${s.count} · ${pct}%</span>
                </div>
                <div class="progress" style="height:6px">
                    <div class="progress-bar" style="width:${pct}%"></div>
                </div>
            </div>`;
    }).join('');
}

function renderRevenueChart(root: HTMLElement, d: DashboardResponse): void {
    revenueChart?.destroy();
    revenueChart = null;

    const canvas = root.querySelector<HTMLCanvasElement>('#revenue-chart');
    if (!canvas) return;

    revenueChart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels: d.dailySales.map(p => formatDateOnly(p.date)),
            datasets: [{
                label: 'Revenue',
                data: d.dailySales.map(p => p.revenue),
                backgroundColor: 'rgba(13, 110, 253, 0.6)',
                borderRadius: 4,
            }],
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: ctx => money(ctx.parsed.y ?? 0),
                        afterLabel: ctx => `${d.dailySales[ctx.dataIndex].orderCount} paid orders`,
                    },
                },
            },
            scales: {
                y: {
                  beginAtZero: true,
                  ticks: { callback: value => money(Number(value)) },
                },
            },
        },
    });
}

function fromInput(): HTMLInputElement {
    return document.getElementById('filter-from') as HTMLInputElement;
}

function toInput(): HTMLInputElement {
    return document.getElementById('filter-to') as HTMLInputElement;
}

function bindEvents(): void {
    const rangeError = document.getElementById('range-error')!;

    document.getElementById('btn-apply-range')!.addEventListener('click', () => {
        const from = fromInput().value;
        const to = toInput().value;

        if (from && to && from > to) {
            rangeError.textContent = '"From" must be on or before "To".';
            rangeError.classList.remove('d-none');
            return;
        }
        rangeError.classList.add('d-none');
        loadDashboard(from || undefined, to || undefined);
    });

    document.querySelectorAll<HTMLButtonElement>('[data-preset]').forEach(btn =>
        btn.addEventListener('click', () => {
            const days = Number(btn.dataset.preset);
            rangeError.classList.add('d-none');
            loadDashboard(isoDate(-(days - 1)), isoDate());
        }));
}