import { categoriesApi, productsApi, type CategoryResponse, type ProductResponse, type ProductQueryParams } from "../api/products";
import { showToast } from "../components/toast";
import { showConfirmModal } from "../components/modal";

let currentPage = 1;
let currentSearch = '';
let currentCatId = '';
let categories: CategoryResponse[] = [];
let editingId: string | null = null;

export async function initProductsPage(container: HTMLElement): Promise<void> {
    container.innerHTML = '<p class="text-muted">Loading…</p>';
    try {
        const { data } = await categoriesApi.getAll();
        categories = data;
    } catch {
        categories = [];
    }
    container.innerHTML = pageTemplate();
    bindEvents();
    loadProducts();
}

async function loadProducts(): Promise<void> {
    const tbody = document.getElementById('products-tbody')!;
    const pagination = document.getElementById('prod-pagination')!;
    tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">Loading…</td></tr>';

    try {
        const params: ProductQueryParams = {
            page: currentPage,pageSize: 12,
            search: currentSearch || undefined,
            categoryId: currentCatId || undefined
        };
        const { data } = await productsApi.getAll(params);

        tbody.innerHTML = data.data.length
          ? data.data.map(renderRow).join('')
          : '<tr><td colspan="8" class="text-center text-muted py-4">No products found.</td></tr>';

        pagination.innerHTML = renderPagination(data.page, data.totalPages);
        bindTableEvents();
        bindPaginationEvents(data.totalPages);
    } catch (err: any) {
        tbody.innerHTML = `<tr><td colspan="8" class="text-center text-danger py-4">${err.message}</td></tr>`;
    }
}

function pageTemplate(): string {
    const catOptions = categories.map(c =>
    `<option value="${c.id}">${escHtml(c.name)}</option>`).join('');

  return `
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h1 class="h3 mb-0">Products</h1>
      <button class="btn btn-primary btn-sm" id="btn-new-product">+ New Product</button>
    </div>

    <div class="row g-2 mb-3">
      <div class="col-auto">
        <input id="prod-search" type="text" class="form-control form-control-sm"
               placeholder="Search name or SKU…" value="${currentSearch}">
      </div>
      <div class="col-auto">
        <select id="prod-cat-filter" class="form-select form-select-sm">
          <option value="">All categories</option>${catOptions}
        </select>
      </div>
      <div class="col-auto">
        <button class="btn btn-outline-secondary btn-sm" id="btn-prod-search">Filter</button>
      </div>
    </div>

    <div class="table-responsive">
      <table class="table table-hover align-middle">
        <thead class="table-light">
          <tr>
            <th>Name</th><th>SKU</th><th>Category</th><th>Price</th>
            <th>Stock</th><th>Available</th><th>Status</th>
            <th style="width:160px">Actions</th>
          </tr>
        </thead>
        <tbody id="products-tbody"></tbody>
      </table>
    </div>
    <nav id="prod-pagination" class="mt-3"></nav>

    <!-- Drawer -->
    <div id="drawer-overlay" class="d-none"
         style="position:fixed;inset:0;background:rgba(0,0,0,.35);z-index:1040"></div>
    <div id="product-drawer"
         style="position:fixed;top:0;right:-460px;width:460px;height:100vh;
                background:#fff;box-shadow:-4px 0 20px rgba(0,0,0,.15);
                z-index:1050;transition:right .3s ease;overflow-y:auto;padding:2rem">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h5 id="drawer-title" class="mb-0">New Product</h5>
        <button class="btn-close" id="btn-close-drawer"></button>
      </div>
      <form id="product-form" novalidate></form>
    </div>

    <!-- Stock adjust modal -->
    <div id="stock-modal" class="d-none"
         style="position:fixed;inset:0;background:rgba(0,0,0,.5);
                display:none;align-items:center;justify-content:center;z-index:10000">
      <div style="background:#fff;border-radius:8px;padding:1.5rem;width:340px">
        <h6 class="mb-3">Adjust Stock</h6>
        <div class="mb-2">
          <label class="form-label small">Delta (positive = add, negative = remove)</label>
          <input id="stock-delta" type="number" class="form-control form-control-sm">
        </div>
        <div class="mb-3">
          <label class="form-label small">Reason</label>
          <input id="stock-reason" type="text" class="form-control form-control-sm"
                 placeholder="e.g. Restock, Damage write-off">
        </div>
        <div id="stock-error" class="alert alert-danger d-none small py-2"></div>
        <div class="d-flex gap-2 justify-content-end">
          <button class="btn btn-secondary btn-sm" id="btn-stock-cancel">Cancel</button>
          <button class="btn btn-primary btn-sm"   id="btn-stock-confirm">Apply</button>
        </div>
      </div>
    </div>`;

}

function renderRow(p: ProductResponse): string {
  const badge = p.isActive
    ? '<span class="badge bg-success-subtle text-success-emphasis">Active</span>'
    : '<span class="badge bg-secondary-subtle text-secondary-emphasis">Inactive</span>';
  const stockBadge = p.availableStock === 0
    ? `<span class="text-danger fw-semibold">0</span>`
    : p.availableStock < 10
      ? `<span class="text-warning fw-semibold">${p.availableStock}</span>`
      : `${p.availableStock}`;

  return `
    <tr>
      <td>${escHtml(p.name)}</td>
      <td><code class="small">${escHtml(p.sku)}</code></td>
      <td>${escHtml(p.categoryName)}</td>
      <td>$${p.price.toFixed(2)}</td>
      <td>${p.stockQuantity}</td>
      <td>${stockBadge}</td>
      <td>${badge}</td>
      <td>
        <button class="btn btn-outline-primary btn-sm me-1 btn-edit-prod"
                data-id="${p.id}">Edit</button>
        <button class="btn btn-outline-secondary btn-sm me-1 btn-adjust-stock"
                data-id="${p.id}">Stock</button>
        <button class="btn btn-outline-danger btn-sm btn-deactivate-prod"
                data-id="${p.id}" ${!p.isActive ? 'disabled' : ''}>
          ${p.isActive ? 'Off' : '—'}
        </button>
      </td>
    </tr>`;
}

function productFormTemplate(p?: ProductResponse): string {
  const v = (val?: string | number) => val != null ? `value="${escHtml(String(val))}"` : '';
  const catOptions = categories.map(c =>
    `<option value="${c.id}" ${p?.categoryId === c.id ? 'selected' : ''}>${escHtml(c.name)}</option>`
  ).join('');

  return `
    <div class="mb-3">
      <label class="form-label">Name <span class="text-danger">*</span></label>
      <input name="name" class="form-control" required ${v(p?.name)}>
    </div>
    <div class="mb-3">
      <label class="form-label">SKU <span class="text-danger">*</span></label>
      <input name="sku" class="form-control" ${p ? 'disabled' : 'required'}
             placeholder="UPPERCASE-ONLY" ${v(p?.sku)}>
      ${p ? '<div class="form-text">SKU cannot be changed.</div>' : ''}
    </div>
    <div class="mb-3">
      <label class="form-label">Category <span class="text-danger">*</span></label>
      <select name="categoryId" class="form-select" required>
        <option value="">Select…</option>${catOptions}
      </select>
    </div>
    <div class="row g-2 mb-3">
      <div class="col">
        <label class="form-label">Price <span class="text-danger">*</span></label>
        <input name="price" type="number" step="0.01" min="0.01"
               class="form-control" required ${v(p?.price)}>
      </div>
      ${!p ? `
      <div class="col">
        <label class="form-label">Initial Stock</label>
        <input name="stockQuantity" type="number" min="0"
               class="form-control" value="0">
      </div>` : ''}
    </div>
    <div class="mb-3">
      <label class="form-label">Description</label>
      <textarea name="description" class="form-control" rows="2">${escHtml(p?.description ?? '')}</textarea>
    </div>
    <div class="mb-3">
      <label class="form-label">Image URL</label>
      <input name="imageUrl" type="url" class="form-control" ${v(p?.imageUrl)}>
    </div>
    <div id="form-error" class="alert alert-danger d-none"></div>
    <div class="d-flex gap-2 justify-content-end">
      <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-form">Cancel</button>
      <button type="submit" class="btn btn-primary btn-sm" id="btn-submit-form">Save</button>
    </div>`;
}


function renderPagination(page: number, totalPages: number): string {
  if (totalPages <= 1) return '';
  const prev = `<li class="page-item ${page===1?'disabled':''}">
    <a class="page-link" href="#" data-page="${page-1}">Previous</a></li>`;
  const next = `<li class="page-item ${page===totalPages?'disabled':''}">
    <a class="page-link" href="#" data-page="${page+1}">Next</a></li>`;
  const pages = Array.from({length: totalPages},(_,i)=>i+1).map(p=>
    `<li class="page-item ${p===page?'active':''}">
      <a class="page-link" href="#" data-page="${p}">${p}</a></li>`).join('');
  return `<ul class="pagination pagination-sm">${prev}${pages}${next}</ul>`;
}

function bindEvents(): void {
    document.getElementById('btn-new-product')!
      .addEventListener('click', () => openDrawer());
    document.getElementById('btn-prod-search')!
      .addEventListener('click', () => {
        currentSearch = (document.getElementById('prod-search') as HTMLInputElement).value;
        currentCatId = (document.getElementById('prod-cat-filter') as HTMLSelectElement).value;
        currentPage = 1;
        loadProducts();
      });
    document.getElementById('btn-close-drawer')!
      .addEventListener('click', closeDrawer);
    document.getElementById('drawer-overlay')!
      .addEventListener('click', closeDrawer);
}

function bindTableEvents(): void {
  document.querySelectorAll('.btn-edit-prod').forEach(btn => 
    btn.addEventListener('click', async e => {
      const id = (e.currentTarget as HTMLElement).dataset.id!;
      try {
        const { data } = await productsApi.getById(id)
        openDrawer(data);
      } catch (err: any) { showToast(err.message, 'danger'); }
    }));

  document.querySelectorAll('.btn-deactivate-prod').forEach(btn =>
    btn.addEventListener('click', async e => {
      const id = (e.currentTarget as HTMLElement).dataset.id!;
      const ok = await showConfirmModal('Deactivate this product?');
      if (!ok) return;
      try {
        await productsApi.deactivate(id);
        showToast('Product deactivated.', 'success');
        loadProducts();
      } catch (err: any) { showToast(err.message, 'danger'); }
    }));

  document.querySelectorAll('.btn-adjust-stock').forEach(btn => 
    btn.addEventListener('click', e => {
      const id = (e.currentTarget as HTMLElement).dataset.id!;
      openStockModal(id);
    }));
}

function bindPaginationEvents(totalPages: number): void {
  document.querySelectorAll('#prod-pagination .page-link').forEach(link => 
    link.addEventListener('click', e => {
      e.preventDefault();
      const page = parseInt((e.currentTarget as HTMLElement).dataset.page ?? '1');
      if (page < 1 || page > totalPages) return;
      currentPage = page;
      loadProducts();
    }));
}

function openDrawer(product?: ProductResponse): void {
  editingId = product?.id ?? null;
  document.getElementById('drawer-title')!.textContent =
    product ? 'Edit Product' : 'New Product';
  document.getElementById('product-form')!.innerHTML = productFormTemplate(product);
  document.getElementById('btn-cancel-form')!.addEventListener('click', closeDrawer);
  document.getElementById('product-form')!.addEventListener('submit', handleFormSubmit);
  document.getElementById('product-drawer')!.style.right = '0';
  document.getElementById('drawer-overlay')!.classList.remove('d-none');
}

function closeDrawer(): void {
  document.getElementById('product-drawer')!.style.right = '-460px';
  document.getElementById('drawer-overlay')!.classList.add('d-none');
  editingId = null;
}

async function handleFormSubmit(e: Event): Promise<void> {
  e.preventDefault();
  const form = e.target as HTMLFormElement;
  const errorEl = document.getElementById('form-error')!;
  const submitBtn = document.getElementById('btn-submit-form') as HTMLButtonElement;
  errorEl.classList.add('d-none');
  submitBtn.disabled = true;
  submitBtn.textContent = 'Saving...';

  try {
    const fd = new FormData(form);
    if (editingId) {
      await productsApi.update(editingId, {
        categoryId: fd.get('categoryId') as string,
        name: fd.get('name') as string,
        description: (fd.get('description') as string) || undefined,
        price: parseFloat(fd.get('price') as string),
        imageUrl: (fd.get('imageUrl') as string) || undefined,
      });
      showToast('Product updated.', 'success');
    } else {
      await productsApi.create({
        categoryId: fd.get('categoryId') as string,
        name: fd.get('name') as string,
        sku: (fd.get('sku') as string).toUpperCase(),
        description: (fd.get('description') as string) || undefined,
        price: parseFloat(fd.get('price') as string),
        stockQuantity: parseInt(fd.get('stockQuantity') as string) || 0,
        imageUrl: (fd.get('imageUrl') as string) || undefined,
      });
      showToast('Product created.', 'success');
    }
    closeDrawer();
    loadProducts();
  } catch (err: any) {
    errorEl.textContent = err.message;
    errorEl.classList.remove('d-none');
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = 'Save';
  }
}

function openStockModal(productId: string): void {
  const modal = document.getElementById('stock-modal')!;
  const errorEl = document.getElementById('stock-error')!;
  (document.getElementById('stock-delta') as HTMLInputElement).value = '';
  (document.getElementById('stock-reason') as HTMLInputElement).value = '';
  errorEl.classList.add('d-none');
  modal.style.display = 'flex';

  document.getElementById('btn-stock-cancel')!
    onclick = () => { modal.style.display = 'none'; };

  document.getElementById('btn-stock-confirm')!.onclick = async () => {
    const delta = parseInt((document.getElementById('stock-delta') as HTMLInputElement).value);
    const reason = (document.getElementById('stock-reason') as HTMLInputElement).value.trim();
    if (isNaN(delta) || delta === 0) {
      errorEl.textContent = 'Enter a non-zero quantity.';
      errorEl.classList.remove('d-none');
      return;
    }
    if (!reason) {
      errorEl.textContent = 'Reason is required.';
      errorEl.classList.remove('d-none');
      return;
    }
    try {
      await productsApi.adjustStock(productId, delta, reason);
      showToast('Stock adjusted.', 'success')
    } catch (err: any) {
      errorEl.textContent = err.message;
      errorEl.classList.remove('d-none');
    }
  };
}

function escHtml(str: string): string {
    return str.replace(/&/g, '&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
}