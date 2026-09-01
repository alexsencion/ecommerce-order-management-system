import {
  ordersApi,
  type OrderResponse,
  OrderStatus,
  ORDER_STATUS_LABELS,
  ALLOWED_TRANSITIONS,
  type OrderQueryParams,
} from '../api/orders';
import { showToast }        from '../components/toast';
import { showConfirmModal } from '../components/modal';
import { openCreateOrderWizard } from '../components/createOrderWizard';
import { PAYMENT_STATUS_LABELS, paymentsApi, PaymentStatus } from '../api/payments';
import { openPaymentModal } from '../components/paymentModal';

// ── State ──────────────────────────────────────────────────────────────────
let currentPage         = 1;
let currentStatusFilter = '';

// ── Entry point ────────────────────────────────────────────────────────────
export function initOrdersPage(container: HTMLElement): void {
  container.innerHTML = pageTemplate();
  bindEvents();
  loadOrders();
}

// ── Data ───────────────────────────────────────────────────────────────────
async function loadOrders(): Promise<void> {
  const tbody      = document.getElementById('orders-tbody')!;
  const pagination = document.getElementById('orders-pagination')!;
  tbody.innerHTML  =
    '<tr><td colspan="7" class="text-center py-4">Loading…</td></tr>';

  try {
    const params: OrderQueryParams = {
      page:     currentPage,
      pageSize: 10,
      status:   currentStatusFilter ? parseInt(currentStatusFilter) : undefined,
    };

    const { data } = await ordersApi.getAll(params);

    tbody.innerHTML = data.data.length
      ? data.data.map(renderRow).join('')
      : '<tr><td colspan="7" class="text-center text-muted py-4">No orders found.</td></tr>';

    pagination.innerHTML = renderPagination(data.page, data.totalPages);
    bindTableEvents();
    bindPaginationEvents(data.totalPages);
  } catch (err: any) {
    tbody.innerHTML =
      `<tr><td colspan="7" class="text-danger text-center py-4">${err.message}</td></tr>`;
  }
}

// ── Templates ──────────────────────────────────────────────────────────────
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
  const badge   = statusBadge(o.status);
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

function renderOrderDetail(
  o: OrderResponse,
  paymentStatus: PaymentStatus | null
): string {
  console.log('Order status:', o.status, 'Payment status:', paymentStatus);
  const allowed   = ALLOWED_TRANSITIONS[o.status];
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

  // Payment status badge — show only if order is Confirmed
  const paymentBadge = o.status === OrderStatus.Confirmed && paymentStatus !== null
    ? `<span class="badge ${paymentBadgeClass(paymentStatus)} ms-2">
        ${PAYMENT_STATUS_LABELS[paymentStatus]}
       </span>`
    : '';

  // "Pay Now" button — show only for confirmed, unpaid orders
  const payButton = o.status === OrderStatus.Confirmed &&
                    paymentStatus !== PaymentStatus.Succeeded
    ? `<button class="btn btn-success btn-sm btn-pay-order" data-id="${o.id}"
               data-amount="${o.total}">
        Pay Now
       </button>`
    : '';

  // Refund button — show only for paid orders that haven't shipped
  const refundButton = paymentStatus === PaymentStatus.Succeeded &&
                        [OrderStatus.Processing, OrderStatus.Packed].includes(o.status)
    ? `<button class="btn btn-outline-warning btn-sm btn-refund-order"
             data-id="${o.id}">
      Refund & Cancel
     </button>`
    : '';

  const addr = o.shippingAddress;

  return `
    <!-- Header -->
    <div class="d-flex justify-content-between align-items-start mb-4">
      <div>
        <div class="text-muted small mb-2">
          Order #${o.id.split('-')[0].toUpperCase()}
        </div>
        <div>${statusBadge(o.status)}${paymentBadge}</div>
      </div>
      <div class="text-end">
        <div class="fw-bold fs-5">$${o.total.toFixed(2)}</div>
        <div class="text-muted small">
          ${new Date(o.createdAt).toLocaleString()}
        </div>
      </div>
    </div>

    <!-- Customer Card -->
    <div class="card mb-3 border-0 bg-light rounded-3 p-3">
      <div class="small text-muted fw-semibold mb-1">CUSTOMER</div>
      <div class="fw-semibold">${escHtml(o.customerName)}</div>
      <div class="text-muted small">${escHtml(o.customerEmail)}</div>
    </div>

    <!-- Shipping Address Card -->
    <div class="card mb-3 border-0 bg-light rounded-3 p-3">
      <div class="small text-muted fw-semibold mb-1">SHIP TO</div>
      <div class="small">
        ${escHtml(addr.street)}<br>
        ${escHtml(addr.city)}, ${escHtml(addr.state)} ${escHtml(addr.zipCode)}<br>
        ${escHtml(addr.country)}
      </div>
    </div>

    <!-- Line Items -->
    <div class="mb-3">
      <div class="small text-muted fw-semibold mb-2">ITEMS</div>
      <table class="table table-sm mb-0">
        <thead class="table-light">
          <tr>
            <th>Product</th><th class="text-center">Qty</th>
            <th class="text-end">Unit</th><th class="text-end">Total</th>
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

    <!-- Status History -->
    ${o.statusHistory.length > 0 ? `
    <div class="mb-3">
      <div class="small text-muted fw-semibold mb-2">STATUS HISTORY</div>
      <div class="timeline">
        ${o.statusHistory.map(h => `
          <div class="d-flex gap-2 mb-2 align-items-start">
            <div style="width:8px;height:8px;border-radius:50%;
                        background:var(--bs-primary);flex-shrink:0;margin-top:5px">
            </div>
            <div>
              <span class="small fw-semibold">
                ${ORDER_STATUS_LABELS[h.fromStatus]}
                → ${ORDER_STATUS_LABELS[h.toStatus]}
              </span>
              ${h.notes ? `<span class="text-muted small"> · ${escHtml(h.notes)}</span>` : ''}
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

    <!-- Action Errors -->
    <div id="panel-action-error" class="alert alert-danger d-none small py-2 mb-3"></div>

    <!-- Actions -->
    <div class="d-flex gap-2 flex-wrap pt-3 border-top">
      ${payButton}
      ${transitionButtons}
      ${refundButton}
      ${cancelButton}
      ${!payButton && !transitionButtons && !refundButton && !cancelButton ? `
        <span class="text-muted small">No further actions available.</span>` : ''}
    </div>`;
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

// ── Status badge ───────────────────────────────────────────────────────────
function statusBadge(status: OrderStatus): string {
  const map: Record<OrderStatus, string> = {
    [OrderStatus.Pending]:    'bg-warning-subtle  text-warning-emphasis',
    [OrderStatus.Confirmed]:  'bg-info-subtle     text-info-emphasis',
    [OrderStatus.Processing]: 'bg-primary-subtle  text-primary-emphasis',
    [OrderStatus.Packed]:     'bg-primary-subtle  text-primary-emphasis',
    [OrderStatus.Shipped]:    'bg-info-subtle     text-info-emphasis',
    [OrderStatus.Delivered]:  'bg-success-subtle  text-success-emphasis',
    [OrderStatus.Cancelled]:  'bg-secondary-subtle text-secondary-emphasis',
  };
  return `<span class="badge ${map[status]}">
    ${ORDER_STATUS_LABELS[status]}
  </span>`;
}

// ── Events ─────────────────────────────────────────────────────────────────
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

function bindTableEvents(): void {
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
  const errorEl = document.getElementById('panel-action-error')!;

  // Pay Now button — opens payment modal
  document.querySelectorAll('.btn-pay-order').forEach(btn =>
    btn.addEventListener('click', async e => {
      const el = e.currentTarget as HTMLElement;
      const id = el.dataset.id!;
      const amount = parseFloat(el.dataset.amount!);

      await openPaymentModal({
        orderId: id,
        amount,
        onSuccess: () => {
          // Payment succeeded — reload this panel to show updated status
          openPanel(id);
          loadOrders(); // refresh table
        },
        onDismiss: () => {
          // User closed modal without paying — nothing to do
        },
      });
    }));

  // Status transitions
  document.querySelectorAll('.btn-transition').forEach(btn =>
    btn.addEventListener('click', async e => {
      const el = e.currentTarget as HTMLElement;
      const id = el.dataset.id!;
      const status = parseInt(el.dataset.status!) as OrderStatus;
      const label = ORDER_STATUS_LABELS[status];

      const ok = await showConfirmModal(`Mark this order as "${label}"?`);
      if (!ok) return;

      try {
        await ordersApi.updateStatus(id, status);
        showToast(`Order marked as ${label}.`, 'success');
        await openPanel(id);
        loadOrders();
      } catch (err: any) {
        errorEl.textContent = err.message;
        errorEl.classList.remove('d-none');
      }
    }));

  // Refund button
  document.querySelectorAll('.btn-refund-order').forEach(btn =>
    btn.addEventListener('click', async e => {
      const id = (e.currentTarget as HTMLElement).dataset.id!;
      const ok = await showConfirmModal(
        'Refund this payment and cancel the order? This cannot be undone.');
      if (!ok) return;

      try {
        await paymentsApi.refund(id);
        showToast('Payment refunded and order cancelled.', 'success');
        await openPanel(id);
        loadOrders();
      } catch (err: any) {
        errorEl.textContent = err.message;
        errorEl.classList.remove('d-none');
      }
    }));

  // Cancel button
  document.querySelectorAll('.btn-cancel-order').forEach(btn =>
    btn.addEventListener('click', async e => {
      const id = (e.currentTarget as HTMLElement).dataset.id!;
      const ok = await showConfirmModal(
        'Cancel this order? Stock reservations will be released.');
      if (!ok) return;

      try {
        await ordersApi.cancel(id, 'Cancelled by admin');
        showToast('Order cancelled.', 'success');
        await openPanel(id);
        loadOrders();
      } catch (err: any) {
        errorEl.textContent = err.message;
        errorEl.classList.remove('d-none');
      }
    }));
}

// ── Panel ──────────────────────────────────────────────────────────────────
async function openPanel(orderId: string): Promise<void> {
  const content = document.getElementById('order-panel-content')!;
  content.innerHTML = '<p class="text-muted">Loading…</p>';

  document.getElementById('order-panel')!.style.right = '0';
  document.getElementById('panel-overlay')!.classList.remove('d-none');

  try {
    // console.log('Opening panel for order:', orderId);
    const { data: order } = await ordersApi.getById(orderId);
    // console.log('Order fetched:', order);
    
    const paymentStatus = await fetchPaymentStatusSafely(orderId);
    // console.log('Final paymentStatus:', paymentStatus);

    content.innerHTML = renderOrderDetail(order, paymentStatus);
    bindPanelEvents();
  } catch (err: any) {
    content.innerHTML =
      `<div class="alert alert-danger small">${err.message}</div>`;
  }
}

async function fetchPaymentStatusSafely(orderId: string): Promise<PaymentStatus | null> {
  try {
    // console.log('Fetching payment for order:', orderId);
    const { data: payment } = await paymentsApi.getByOrder(orderId);
    // console.log('Payment fetched successfully:', payment);
    return payment.status;
  } catch (err: any) {
    // Payment may not exist yet — that's fine
    return null;
  }
}

function paymentBadgeClass(status: PaymentStatus): string {
  const map: Record<PaymentStatus, string> = {
    [PaymentStatus.Pending]:   'bg-warning-subtle text-warning-emphasis',
    [PaymentStatus.Succeeded]: 'bg-success-subtle text-success-emphasis',
    [PaymentStatus.Failed]:    'bg-danger-subtle  text-danger-emphasis',
    [PaymentStatus.Refunded]:  'bg-secondary-subtle text-secondary-emphasis',
  };
  return map[status];
}

function closePanel(): void {
  document.getElementById('order-panel')!.style.right = '-560px';
  document.getElementById('panel-overlay')!.classList.add('d-none');
}

// ── Helpers ────────────────────────────────────────────────────────────────
function escHtml(str: string): string {
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;');
}