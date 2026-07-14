import { customersApi, type CustomerResponse } from '../api/customers';
import { productsApi, type ProductResponse } from '../api/products';
import { ordersApi, type CreateOrderItemRequest } from '../api/orders';
import { showToast } from './toast';

const TAX_RATE = 0.18;

interface LineItem {
    product: ProductResponse;
    quantity: number;
}

let selectedCustomer: CustomerResponse | null = null;
let lineItems: LineItem[] = [];
let onSuccess: () => void = () => {};

export function openCreateOrderWizard(onOrderCreated: () => void): void {
    onSuccess = onOrderCreated;
    selectedCustomer = null;
    lineItems = [];

    const existing = document.getElementById('create-order-overlay');
    if (existing) existing.remove();

    document.body.insertAdjacentHTML('beforeend', wizardTemplate());
    bindWizardEvents();
}

function wizardTemplate(): string {
    return `
    <div id="create-order-overlay"
         style="position:fixed;inset:0;background:rgba(0,0,0,.5);
                z-index:2000;display:flex;align-items:flex-start;
                justify-content:center;padding-top:3rem;overflow-y:auto">
      <div style="background:#fff;border-radius:12px;width:680px;max-width:95vw;
                  padding:2rem;margin-bottom:3rem">

        <!-- Header -->
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h5 class="mb-0">New Order</h5>
          <button class="btn-close" id="wizard-close"></button>
        </div>

        <!-- Step 1: Customer -->
        <div class="mb-4">
          <div class="small text-muted fw-semibold mb-2">1 · CUSTOMER</div>
          <div class="input-group input-group-sm">
            <input id="customer-search" type="text"
                   class="form-control"
                   placeholder="Search by name or email…">
            <button class="btn btn-outline-secondary" id="btn-customer-search">
              Search
            </button>
          </div>
          <div id="customer-results" class="mt-2"></div>
          <div id="selected-customer" class="mt-2"></div>
        </div>

        <!-- Step 2: Products -->
        <div class="mb-4">
          <div class="small text-muted fw-semibold mb-2">2 · PRODUCTS</div>
          <div class="input-group input-group-sm">
            <input id="product-search" type="text"
                   class="form-control"
                   placeholder="Search by name or SKU…">
            <button class="btn btn-outline-secondary" id="btn-product-search">
              Search
            </button>
          </div>
          <div id="product-results" class="mt-2"></div>
          <div id="line-items" class="mt-3"></div>
        </div>

        <!-- Step 3: Shipping address -->
        <div class="mb-4">
          <div class="small text-muted fw-semibold mb-2">3 · SHIPPING ADDRESS</div>
          <div class="row g-2">
            <div class="col-12">
              <input id="addr-street" class="form-control form-control-sm"
                     placeholder="Street *">
            </div>
            <div class="col-6">
              <input id="addr-city" class="form-control form-control-sm"
                     placeholder="City *">
            </div>
            <div class="col-6">
              <input id="addr-state" class="form-control form-control-sm"
                     placeholder="State">
            </div>
            <div class="col-6">
              <input id="addr-zip" class="form-control form-control-sm"
                     placeholder="ZIP Code *">
            </div>
            <div class="col-6">
              <input id="addr-country" class="form-control form-control-sm"
                     placeholder="Country *">
            </div>
          </div>
        </div>

        <!-- Step 4: Notes -->
        <div class="mb-4">
          <div class="small text-muted fw-semibold mb-2">4 · NOTES (optional)</div>
          <textarea id="order-notes" class="form-control form-control-sm"
                    rows="2" placeholder="Special instructions…"></textarea>
        </div>

        <!-- Order summary -->
        <div id="order-summary" class="d-none mb-4 p-3 bg-light rounded-3">
          <div class="small text-muted fw-semibold mb-2">ORDER SUMMARY</div>
          <div id="summary-content"></div>
        </div>

        <!-- Error -->
        <div id="wizard-error" class="alert alert-danger d-none small py-2 mb-3">
        </div>

        <!-- Footer -->
        <div class="d-flex justify-content-end gap-2">
          <button class="btn btn-secondary btn-sm" id="wizard-cancel">
            Cancel
          </button>
          <button class="btn btn-primary btn-sm" id="wizard-submit" disabled>
            Place Order
          </button>
        </div>
      </div>
    </div>`;
}

function bindWizardEvents(): void {
    document.getElementById('wizard-close')!
        .addEventListener('click', closeWizard);
    document.getElementById('wizard-cancel')!
        .addEventListener('click', closeWizard);

    document.getElementById('btn-customer-search')!
        .addEventListener('click', searchCustomers);
    document.getElementById('customer-search')!
        .addEventListener('keydown', e => {
            if (e.key === 'Enter') searchCustomers();
        });

    document.getElementById('btn-product-search')!
        .addEventListener('click', searchProducts)
    document.getElementById('product-search')!
        .addEventListener('keydown', e => {
            if (e.key === 'Enter') searchProducts();
        });

    document.getElementById('wizard-submit')!
        .addEventListener('click', submitOrder)
}

async function searchCustomers(): Promise<void> {
    const query = (document.getElementById('customer-search') as HTMLInputElement).value.trim();
    const results = document.getElementById('customer-results')!;

    if (!query) return;

    results.innerHTML = '<p class="text-muted small">Searching…</p>'

    try {
        const { data } = await customersApi.getAll({ search: query, isActive: true, pageSize: 5});

        if (!data.data.length) {
            results.innerHTML = '<p class="text-muted small">No active customers found.</p>';
            return;
        }

        results.innerHTML = `
        <div class="list-group list-group-flush border rounded">
            ${data.data.map(c => `
            <button type="button"
                    class="list-group-item list-group-item-action py-2 btn-select-customer"
                    data-id="${c.id}">
                <div class="fw-semibold small">${escHtml(c.fullName)}</div>
                <div class="text-muted" style="font-size:.75rem">
                ${escHtml(c.email)}
                </div>
            </button>`).join('')}
        </div>`;

        results.querySelectorAll('.btn-select-customer').forEach(btn => 
            btn.addEventListener('click', () => {
                const customer = data.data.find(
                    c => c.id === (btn as HTMLElement).dataset.id)!;
                selectCustomer(customer);
            }))
    } catch (err: any) {
        results.innerHTML = 
        `<p class="text-danger small">${err.message}</p>`;
    }
}

function selectCustomer(customer: CustomerResponse): void {
    selectedCustomer = customer;

    document.getElementById('customer-results')!.innerHTML = '';
    document.getElementById('selected-customer')!.innerHTML = `
    <div class="d-flex align-items-center justify-content-between
                p-2 bg-success-subtle rounded-3">
      <div>
        <div class="small fw-semibold">${escHtml(customer.fullName)}</div>
        <div class="text-muted" style="font-size:.75rem">
          ${escHtml(customer.email)}
        </div>
      </div>
      <button class="btn btn-sm btn-link text-danger p-0"
              id="btn-clear-customer">
        Change
      </button>
    </div>`;

    document.getElementById('btn-clear-customer')!
        .addEventListener('click', () => {
            selectedCustomer = null;
            document.getElementById('selected-customer')!.innerHTML = '';
            (document.getElementById('customer-search') as HTMLInputElement).value = '';
            refreshSummary();
        })
}

async function searchProducts(): Promise<void> {
  const query = (document.getElementById('product-search') as HTMLInputElement).value.trim();
  const results = document.getElementById('product-results')!;

  if (!query) return;

  results.innerHTML = '<p class="text-muted small">Searching...</p>';

  try {
    const { data } = await productsApi.getAll({
      search: query,
      isActive: true,
      inStockOnly: true,
      pageSize: 5
    });

    if (!data.data.length) {
      results.innerHTML = 
        '<p class="text-muted small">No in-stock products found.</p>';
      return;
    }

    results.innerHTML = `
      <div class="list-group list-group-flush border rounded">
        ${data.data.map(p => {
          const alreadyAdded = lineItems.some(li => li.product.id === p.id);
          return `
            <div class="list-group-item py-2 d-flex
                        justify-content-between align-items-center">
              <div>
                <div class="small fw-semibold">${escHtml(p.name)}</div>
                <div class="text-muted" style="font-size:.75rem">
                  ${escHtml(p.sku)} ·
                  $${p.price.toFixed(2)} ·
                  <span class="${p.availableStock < 10
                    ? 'text-warning' : 'text-success'}">
                    ${p.availableStock} available
                  </span>
                </div>
              </div>
              <button class="btn btn-sm btn-outline-primary btn-add-product"
                      data-id="${p.id}"
                      ${alreadyAdded ? 'disabled' : ''}>
                ${alreadyAdded ? 'Added' : 'Add'}
              </button>
            </div>`;
        }).join('')}
      </div>`;

    results.querySelectorAll('.btn-add-product').forEach(btn =>
      btn.addEventListener('click', () => {
        const product = data.data.find(
          p => p.id === (btn as HTMLElement).dataset.id)!;
        addLineItem(product);
        results.innerHTML = '';
        (document.getElementById('product-search') as HTMLInputElement).value = ''
      }));
  } catch (err: any) {
    results.innerHTML = 
      `<p class="text-danger small">${err.message}</p>`;
  }
}

function addLineItem(product: ProductResponse): void {
  if (lineItems.some(li => li.product.id === product.id)) {
    showToast('Product already in this order.', 'warning');
    return;
  }
  lineItems.push({ product, quantity: 1});
  renderLineItems();
  refreshSummary();
}

function renderLineItems(): void {
  const container = document.getElementById('line-items')!;

  if (!lineItems.length) {
    container.innerHTML = '';
    return;
  }

  container.innerHTML = `
    <table class="table table-sm mb-0 border rounded">
      <thead class="table-light">
        <tr>
          <th>Product</th>
          <th class="text-center" style="width:120px">Quantity</th>
          <th class="text-end">Line Total</th>
          <th style="width:40px"></th>
        </tr>
      </thead>
      <tbody>
        ${lineItems.map((li, index) => `
          <tr>
            <td>
              <div class="small fw-semibold">${escHtml(li.product.name)}</div>
              <div class="text-muted" style="font-size:.72rem">
                $${li.product.price.toFixed(2)} each ·
                max ${li.product.availableStock}
              </div>
            </td>
            <td class="text-center">
              <input type="number"
                     class="form-control form-control-sm text-center qty-input"
                     data-index="${index}"
                     value="${li.quantity}"
                     min="1"
                     max="${li.product.availableStock}"
                     style="width:80px;margin:0 auto">
            </td>
            <td class="text-end small fw-semibold">
              $${(li.product.price * li.quantity).toFixed(2)}
            </td>
            <td>
              <button class="btn btn-sm btn-link text-danger p-0 btn-remove-item"
                      data-index="${index}">
                ✕
              </button>
            </td>
          </tr>`).join('')}
      </tbody>
    </table>`;

  container.querySelectorAll('.qty-input').forEach(input => 
    input.addEventListener('input', e => {
      const el = e.target as HTMLInputElement;
      const index = parseInt(el.value);
      const val = parseInt(el.value);
      const max = lineItems[index].product.availableStock;

      if (isNaN(val) || val < 1) {
        el.value = '1';
        lineItems[index].quantity = 1;
      } else if (val > max) {
        el.value = String(max);
        lineItems[index].quantity = max;
        showToast(`Maximun available stock is ${max}.`, 'warning');
      } else {
        lineItems[index].quantity = val;
      }

      renderLineItems();
      refreshSummary();
    }));

    container.querySelectorAll('.btn-remove-item').forEach(btn => 
      btn.addEventListener('click', e => {
        const index = parseInt((e.currentTarget as HTMLElement).dataset.index!);
        lineItems.splice(index, 1);
        renderLineItems();
        refreshSummary();
      }));
}

function refreshSummary(): void {
    const summaryEl = document.getElementById('order-summary')!;
    const submitBtn = document.getElementById('wizard-submit') as HTMLButtonElement;
    const contentEl = document.getElementById('summary-content')!;

    const hasCustomer = selectedCustomer !== null;
    const hasItems = lineItems.length > 0;
    const isReady = hasCustomer && hasItems;

    if (!isReady) {
        summaryEl.classList.add('d-none');
        submitBtn.disabled = true;
        return;
    }

    const subtotal = lineItems.reduce(
        (sum, li) => sum + li.product.price * li.quantity, 0);
    const tax = subtotal * TAX_RATE;
    const total = subtotal + tax;

    contentEl.innerHTML = `
    <div class="d-flex justify-content-between small mb-1">
      <span class="text-muted">
        ${lineItems.length} item${lineItems.length !== 1 ? 's' : ''}
      </span>
      <span>$${subtotal.toFixed(2)}</span>
    </div>
    <div class="d-flex justify-content-between small mb-1">
      <span class="text-muted">Tax (18%)</span>
      <span>$${tax.toFixed(2)}</span>
    </div>
    <div class="d-flex justify-content-between fw-bold border-top pt-1 mt-1">
      <span>Total</span>
      <span>$${total.toFixed(2)}</span>
    </div>`;

    summaryEl.classList.remove('d-none');
    submitBtn.disabled = false;
}

async function submitOrder(): Promise<void> {
  const errorEl = document.getElementById('wizard-error')!;
  const submitBtn = document.getElementById('wizard-submit') as HTMLButtonElement

  errorEl.classList.add('d-none');

  const street = (document.getElementById('addr-street') as HTMLInputElement).value.trim();
  const city = (document.getElementById('addr-city') as HTMLInputElement).value.trim();
  const state = (document.getElementById('addr-state') as HTMLInputElement).value.trim();
  const zipCode = (document.getElementById('addr-zip') as HTMLInputElement).value.trim();
  const country = (document.getElementById('addr-country') as HTMLInputElement).value.trim();
  const notes = (document.getElementById('order-notes') as HTMLTextAreaElement).value.trim();

  if (!street || !city || !zipCode || !country) {
    errorEl.textContent = 'Please complete all required shipping address fields.';
    errorEl.classList.remove('d-none');
    return;
  }

  if (!selectedCustomer) {
    errorEl.textContent = "Please select a customer.";
    errorEl.classList.remove('d-none');
    return;
  }

  if (!lineItems.length) {
    errorEl.textContent = "Please add at least one product.";
    errorEl.classList.remove('d-none');
    return;
  }

  submitBtn.disabled = true;
  submitBtn.textContent = 'Placing Order...'

  try {
    const items: CreateOrderItemRequest[] = 
      lineItems.map(li => ({
        productId: li.product.id,
        quantity: li.quantity,
      }));

    await ordersApi.create({
      customerId: selectedCustomer.id,
      items,
      shippingAddress: { street, city, state, zipCode, country },
      notes: notes || undefined
    });

    showToast('Order placed successfully.', 'success');
    closeWizard();
    onSuccess();


  } catch (err: any) {
    errorEl.textContent = err.message;
    errorEl.classList.remove('d-none');
    submitBtn.disabled = false;
    submitBtn.textContent = 'Place Order';
  }
}

function closeWizard(): void {
  document.getElementById('create-order-overlay')?.remove();
}

function escHtml(str: string): string {
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
}