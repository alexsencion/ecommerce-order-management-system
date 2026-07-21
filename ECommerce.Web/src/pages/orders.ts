import { openCreateOrderWizard } from '../components/createOrderWizard';
import {
    ALLOWED_TRANSITIONS,
    ORDER_STATUS_LABELS,
    ordersApi,
    OrderStatus,
    type OrderResponse,
    type OrderQueryParams,
} from '../api/orders';
import { showToast } from '../components/toast';
import { showConfirmModal } from '../components/modal';

let currentPage = 1;
let currentStatusFilter = '';

export function initOrdersPage(container: HTMLElement): void {
    container.innerHTML = pageTemplate();
    bindEvents();
    loadOrders();
}

async function loadOrders(): Promise<void> {
    const tbody = document.getElementById('orders-tbody')!;
    const pagination = document.getElementById('orders-pagination')!;
    tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">Loading…</td></tr>';

    try {
        const params: OrderQueryParams = {
            page: currentPage,
            pageSize: 10,
            status: currentStatusFilter ? parseInt(currentStatusFilter) : undefined,
        };

        const { data } = await ordersApi.getAll(params);

        tbody.innerHTML = data.data.length
            ? data.data.map(renderRow).join('')
            : '<tr><td colspan="7" class="text-center text-muted py-4">No orders found.</td></tr>';

        pagination.innerHTML = renderPagination(data.page, data.totalPages);
        bindTableEvents();
        bindPaginationEvents(data.totalPages);

    } catch (err: any) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-danger text-center py-4">${err.message}</td></tr>`;
    }
}

function pageTemplate(): string {
    const statusOptions = Object.entries(ORDER_STATUS_LABELS)
      .map(([value, label]) => 
        `<option value="${value}" ${currentStatusFilter === value ? 'selected' : ''}>
            ${label}
            </option>`)
      .join('');

    return `
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h1 class="h3 mb-0">Orders</h1>
      <button class="btn btn-primary btn-sm" id="btn-new-order">
        + New Order
      </button>
    </div>

    <div class="row g-2 mb-3">
      <div class="col-auto">
        <select id="status-filter" class="form-select form-select-sm">
          <option value="">All statuses</option>
          ${statusOptions}
        </select>
      </div>
      <div class="col-auto">
        <button class="btn btn-outline-secondary btn-sm" id="btn-filter">
          Filter
        </button>
      </div>
    </div>

    <div class="table-responsive">
      <table class="table table-hover align-middle">
        <thead class="table-light">
          <tr>
            <th>Order ID</th>
            <th>Customer</th>
            <th>Items</th>
            <th>Total</th>
            <th>Status</th>
            <th>Created</th>
            <th style="width:120px">Actions</th>
          </tr>
        </thead>
        <tbody id="orders-tbody"></tbody>
      </table>
    </div>

    <nav id="orders-pagination" class="mt-3"></nav>

    <!-- Detail panel overlay -->
    <div id="panel-overlay" class="d-none"
         style="position:fixed;inset:0;background:rgba(0,0,0,.35);z-index:1040"></div>

    <!-- Order detail side panel -->
    <div id="order-panel"
         style="position:fixed;top:0;right:-560px;width:560px;height:100vh;
                background:#fff;box-shadow:-4px 0 20px rgba(0,0,0,.15);
                z-index:1050;transition:right .3s ease;overflow-y:auto;padding:2rem">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h5 class="mb-0">Order Detail</h5>
        <button class="btn-close" id="btn-close-panel"></button>
      </div>
      <div id="order-panel-content">
        <p class="text-muted">Loading…</p>
      </div>
    </div>`;
}

function renderRow(o: OrderResponse): string {
    const badge = statusBadge(o.status);
    const created = new Date(o.createdAt).toLocaleDateString();
    const shortId = o.id.split('-')[0].toUpperCase();

    return `
    <tr>
      <td>
        <code class="small text-muted">#${shortId}</code>
      </td>
      <td>
        <div class="fw-semibold small">${escHtml(o.customerName)}</div>
        <div class="text-muted" style="font-size:.75rem">
          ${escHtml(o.customerEmail)}
        </div>
      </td>
      <td>
        <span class="badge bg-light text-dark border">
          ${o.items.length} item${o.items.length !== 1 ? 's' : ''}
        </span>
      </td>
      <td class="fw-semibold">$${o.total.toFixed(2)}</td>
      <td>${badge}</td>
      <td class="text-muted small">${created}</td>
      <td>
        <button class="btn btn-outline-primary btn-sm btn-view-order"
                data-id="${o.id}">
          View
        </button>
      </td>
    </tr>`;
}

function renderOrderDetail(o: OrderResponse): string {
    const allowed = ALLOWED_TRANSITIONS[o.status];
    const canCancel = allowed.includes(OrderStatus.Cancelled);

    const transitionButtons = allowed
       .filter(s => s !== OrderStatus.Cancelled)
       .map(s => `
         <button class="btn btn-primary btn-sm btn-transition"
              data-id="${o.id}" data-status="${s}">
          Mark as ${ORDER_STATUS_LABELS[s]}
         </button>`)
        .join('');

    const cancelButton = canCancel
      ? `<button class="btn btn-outline-danger btn-sm btn-cancel-order"
               data-id="${o.id}">
        Cancel Order
       </button>`
      : '';

    const addr = o.shippingAddress;

     return `
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-start mb-3">
      <div>
        <div class="text-muted small mb-1">
          Order #${o.id.split('-')[0].toUpperCase()}
        </div>
        <div>${statusBadge(o.status)}</div>
      </div>
      <div class="text-end">
        <div class="fw-bold fs-5">$${o.total.toFixed(2)}</div>
        <div class="text-muted small">
          ${new Date(o.createdAt).toLocaleString()}
        </div>
      </div>
    </div>

    <!-- Customer -->
    <div class="card mb-3 border-0 bg-light rounded-3 p-3">
      <div class="small text-muted fw-semibold mb-1">CUSTOMER</div>
      <div class="fw-semibold">${escHtml(o.customerName)}</div>
      <div class="text-muted small">${escHtml(o.customerEmail)}</div>
    </div>

    <!-- Shipping address -->
    <div class="card mb-3 border-0 bg-light rounded-3 p-3">
      <div class="small text-muted fw-semibold mb-1">SHIP TO</div>
      <div class="small">
        ${escHtml(addr.street)}<br>
        ${escHtml(addr.city)}, ${escHtml(addr.state)} ${escHtml(addr.zipCode)}<br>
        ${escHtml(addr.country)}
      </div>
    </div>

    <!-- Items -->
    <div class="mb-3">
      <div class="small text-muted fw-semibold mb-2">ITEMS</div>
      <table class="table table-sm mb-0">
        <thead class="table-light">
          <tr>
            <th>Product</th>
            <th class="text-center">Qty</th>
            <th class="text-end">Unit</th>
            <th class="text-end">Total</th>
          </tr>
        </thead>
        <tbody>
          ${o.items.map(item => `
            <tr>
              <td>
                <div class="small fw-semibold">${escHtml(item.productName)}</div>
                <div class="text-muted" style="font-size:.72rem">
                  ${escHtml(item.productSku)}
                </div>
              </td>
              <td class="text-center">${item.quantity}</td>
              <td class="text-end">$${item.unitPrice.toFixed(2)}</td>
              <td class="text-end fw-semibold">$${item.lineTotal.toFixed(2)}</td>
            </tr>`).join('')}
        </tbody>
        <tfoot class="table-light">
          <tr>
            <td colspan="3" class="text-end text-muted small">Subtotal</td>
            <td class="text-end">$${o.subtotal.toFixed(2)}</td>
          </tr>
          <tr>
            <td colspan="3" class="text-end text-muted small">Tax (18%)</td>
            <td class="text-end">$${o.tax.toFixed(2)}</td>
          </tr>
          <tr>
            <td colspan="3" class="text-end fw-bold">Total</td>
            <td class="text-end fw-bold">$${o.total.toFixed(2)}</td>
          </tr>
        </tfoot>
      </table>
    </div>

    <!-- Status history -->
    ${o.statusHistory.length > 0 ? `
    <div class="mb-3">
      <div class="small text-muted fw-semibold mb-2">STATUS HISTORY</div>
      <div class="timeline">
        ${o.statusHistory.map(h => `
          <div class="d-flex gap-2 mb-2 align-items-start">
            <div class="mt-1" style="width:8px;height:8px;border-radius:50%;
                        background:var(--bs-primary);flex-shrink:0;margin-top:5px">
            </div>
            <div>
              <span class="small fw-semibold">
                ${ORDER_STATUS_LABELS[h.fromStatus]}
                → ${ORDER_STATUS_LABELS[h.toStatus]}
              </span>
              ${h.notes
                ? `<span class="text-muted small"> · ${escHtml(h.notes)}</span>`
                : ''}
              <div class="text-muted" style="font-size:.72rem">
                ${new Date(h.changedAt).toLocaleString()}
              </div>
            </div>
          </div>`).join('')}
      </div>
    </div>` : ''}

    <!-- Notes -->
    ${o.notes ? `
    <div class="mb-3">
      <div class="small text-muted fw-semibold mb-1">NOTES</div>
      <div class="small">${escHtml(o.notes)}</div>
    </div>` : ''}

    <!-- Actions -->
    ${transitionButtons || cancelButton ? `
    <div id="panel-action-error" class="alert alert-danger d-none small py-2 mb-2">
    </div>
    <div class="d-flex gap-2 flex-wrap pt-2 border-top">
      ${transitionButtons}
      ${cancelButton}
    </div>` : `
    <div class="text-muted small pt-2 border-top">
      No further actions available for this order.
    </div>`}`; 
}

function renderPagination(page: number, totalPages: number): string {
    if (totalPages <= 1) return '';
    const prev = `<li class="page-item ${page === 1 ? 'disabled' : ''}">
        <a class="page-link" href="#" data-page="${page - 1}">Previous</a></li>`;
    const next = `<li class="page-item ${page === totalPages ? 'disabled' : ''}">
        <a class="page-link" href="#" data-page="${page + 1}">Next</a></li>`;
    const pages = Array.from({ length: totalPages }, (_, i) => i + 1)
        .map(p => `<li class="page-item ${p === page ? 'active' : ''}">
            <a class="page-link" href="#" data-page="${p}">${p}</a></li>`)
        .join('');
    return `<ul class="pagination pagination-sm">${prev}${pages}${next}</ul>`; 
}

function statusBadge(status: OrderStatus): string {
    const map: Record<OrderStatus, string> = {
      [OrderStatus.Pending]: 'bg-warning-subtle text=warning-emphasis',
      [OrderStatus.Confirmed]: 'bg-info-subtle text-info-emphasis',
      [OrderStatus.Processing]: 'bg-primary-subtle text-primary-emphasis',
      [OrderStatus.Packed]: 'bg-warning-subtle text=warning-emphasis',
      [OrderStatus.Shipped]: 'bg-info-subtle text-info-emphasis',
      [OrderStatus.Delivered]: 'bg-primary-subtle text-primary-emphasis',
      [OrderStatus.Cancelled]: 'bg-secondary-subtle text-secondary-emphasis',
    };
    return `<span class="badge ${map[status]}">
        ${ORDER_STATUS_LABELS[status]}
    </span>`;
}

function bindEvents(): void {
    document.getElementById('btn-new-order')!
      .addEventListener('click', () => {
        openCreateOrderWizard(() => loadOrders());      
      });

    document.getElementById('btn-filter')!.addEventListener('click', () => {
        currentStatusFilter = 
            (document.getElementById('status-filter') as HTMLSelectElement).value;
        currentPage = 1;
        loadOrders();
    });

    document.getElementById('btn-close-panel')!
        .addEventListener('click', closePanel);
    
    document.getElementById('panel-overlay')!
        .addEventListener('click', closePanel);
}

function bindTableEvents() {
    document.querySelectorAll('.btn-view-order').forEach(btn => 
        btn.addEventListener('click', async e => {
            const id = (e.currentTarget as HTMLElement).dataset.id!;
            await openPanel(id);
        }));
}

function bindPaginationEvents(totalPages: number): void {
    document.querySelectorAll('#orders-pagination .page-link').forEach(link => 
      link.addEventListener('click', e => {
        e.preventDefault();
        const page = parseInt((e.currentTarget as HTMLElement).dataset.page ?? '1');
        if (page < 1 || page > totalPages) return;
        currentPage = page;
        loadOrders();
      }));
}

function bindPanelEvents(): void {
  document.querySelectorAll('.btn-transition').forEach(btn => 
    btn.addEventListener('click', async e => {
      const el = e.currentTarget as HTMLElement;
      const id = el.dataset.id!;
      const status = parseInt(el.dataset.status!) as OrderStatus;
      const label = ORDER_STATUS_LABELS[status]

      const ok = await showConfirmModal(`Mark this order as "${label}"?`)
      if (!ok) return;

      const errorEl = document.getElementById('panel-action-error')!;
      try {
        await ordersApi.updateStatus(id, status);
        showToast(`Order marked as ${label}.`, 'success');
        await openPanel(id);
        loadOrders();
      } catch (err: any) {
        errorEl.textContent = err.message;
        errorEl.classList.remove('d-none');
      }
    })
  )
}

async function openPanel(orderId:string): Promise<void> {
  const content = document.getElementById('order-panel-content')!;
  content.innerHTML = '<p class="text-muted small">Loading…</p>';

  document.getElementById('order-panel')!.style.right = '0';
  document.getElementById('panel-overlay')!.classList.remove('d-none');

  try {
    const { data } = await ordersApi.getById(orderId);
    content.innerHTML = renderOrderDetail(data);
    bindPanelEvents();
  } catch (err: any) {
    content.innerHTML = 
      `<div class="alert alert-danger small">${err.message}</div>`;
  }
}


function closePanel(): void {
    document.getElementById('order-panel')!.style.right = '-560px';
    document.getElementById('panel-overlay')!.classList.add('d-none');
}

function escHtml(str: string): string {
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
}