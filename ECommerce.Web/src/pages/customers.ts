import {
    customersApi,
    type CustomerResponse,
    type CreateCustomerRequest,
    type UpdateCustomerRequest,
    type CustomerQueryParams
} from '../api/customers';
import { showToast } from '../components/toast';
import { showConfirmModal } from '../components/modal';

let currentPage = 1;
let currentSearch = '';
let editingId: string | null = null;

export function initCustomerPage(container: HTMLElement): void {
    container.innerHTML = pageTemplate();
    bindEvents();
    loadCustomers();
}

async function loadCustomers(): Promise<void>{
    const tbody = document.getElementById('customers-tbody')!;
    const pagination = document.getElementById('pagination')!;
    tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">Loading…</td></tr>';

    try {
        const params: CustomerQueryParams = {
            page: currentPage,
            pageSize: 10,
            search: currentSearch || undefined,
        };

        const { data } = await customersApi.getAll(params);

        tbody.innerHTML = data.data.length
            ? data.data.map(renderRow).join('')
            : '<tr><td colspan="6" class="text-center text-muted py-4">No customers found.</td></tr>';

        pagination.innerHTML = renderPagination(data.page, data.totalPages);
        bindTableEvents();
        bindPaginationEvents(data.totalPages);

    } catch (err: any) {
        tbody.innerHTML = `<tr><td colspan="6" class="text-center text-danger py-4">${err.message}</td></tr>`;
    }
}

function pageTemplate(): string {
    return `
    <div class="d-flex justify-content-between align-items-center mb-4">
      <h1 class="h3 mb-0">Customers</h1>
      <button class="btn btn-primary btn-sm" id="btn-new-customer">+ New Customer</button>
    </div>

    <div class="input-group mb-3" style="max-width:360px">
      <input id="search-input" type="text" class="form-control form-control-sm"
             placeholder="Search by name or email…" value="${currentSearch}">
      <button class="btn btn-outline-secondary btn-sm" id="btn-search">Search</button>
    </div>

    <div class="table-responsive">
      <table class="table table-hover align-middle">
        <thead class="table-light">
          <tr>
            <th>Name</th><th>Email</th><th>Phone</th>
            <th>Status</th><th>Created</th><th style="width:140px">Actions</th>
          </tr>
        </thead>
        <tbody id="customers-tbody"></tbody>
      </table>
    </div>

    <nav id="pagination" class="mt-3"></nav>

    <!-- Drawer overlay -->
    <div id="drawer-overlay" class="d-none"
         style="position:fixed;inset:0;background:rgba(0,0,0,.35);z-index:1040"></div>

    <!-- Side drawer form -->
    <div id="customer-drawer"
         style="position:fixed;top:0;right:-420px;width:420px;height:100vh;
                background:#fff;box-shadow:-4px 0 20px rgba(0,0,0,.15);
                z-index:1050;transition:right .3s ease;overflow-y:auto;padding:2rem">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h5 id="drawer-title" class="mb-0">New Customer</h5>
        <button class="btn-close" id="btn-close-drawer"></button>
      </div>
      <form id="customer-form" novalidate></form>
    </div>`;
}

function renderRow(c: CustomerResponse): string {
    const badge = c.isActive
        ? '<span class="badge bg-success-subtle text-success-emphasis">Active</span>'
        : '<span class="badge bg-secondary-subtle text-secondary-emphasis">Inactive</span>'
    const date = new Date(c.createdAt).toLocaleDateString();
    return `
    <tr>
      <td>${escHtml(c.fullName)}</td>
      <td>${escHtml(c.email)}</td>
      <td>${escHtml(c.phone ?? '—')}</td>
      <td>${badge}</td>
      <td>${date}</td>
      <td>
        <button class="btn btn-outline-primary btn-sm me-1 btn-edit" data-id="${c.id}">Edit</button>
        <button class="btn btn-outline-danger  btn-sm btn-delete" data-id="${c.id}"
                ${!c.isActive ? 'disabled' : ''}>
          ${c.isActive ? 'Deactivate' : 'Inactive'}
        </button>
      </td>
    </tr>`;
}

function renderPagination(page: number, totalPages: number): string {
    if (totalPages <= 1) return '';
    const prev = `<li class="page-item ${page === 1 ? 'disabled' : ''}">
    <a class="page-link" href="#" data-page="${page - 1}">Previous</a></li>`;
    const next = `<li class="page-item ${page === totalPages ? 'disabled' : ''}">
    <a class="page-link" href="#" data-page="${page + 1}">Next</a></li>`
    const pages = Array.from({ length: totalPages }, (_, i) => i + 1)
        .map(p => `<li class="page-item ${p === page ? 'active' : ''}">
      <a class="page-link" href="#" data-page="${p}">${p}</a></li>`)
        .join('');
    return `<ul class="pagination pagination-sm">${prev}${pages}${next}</ul>`;
}

function customerFormTemplate(customer?: CustomerResponse): string {
    const v = (val?: string) => val ? `value="${escHtml(val)}"` : '';
    return `
    <div class="mb-3">
      <label class="form-label">First name <span class="text-danger">*</span></label>
      <input name="firstName" class="form-control" required ${v(customer?.firstName)}>
    </div>
    <div class="mb-3">
      <label class="form-label">Last name <span class="text-danger">*</span></label>
      <input name="lastName" class="form-control" required ${v(customer?.lastName)}>
    </div>
    <div class="mb-3">
      <label class="form-label">Email <span class="text-danger">*</span></label>
      <input name="email" type="email" class="form-control"
             ${customer ? 'disabled' : 'required'} ${v(customer?.email)}>
      ${customer ? '<div class="form-text">Email cannot be changed.</div>' : ''}
    </div>
    <div class="mb-3">
      <label class="form-label">Phone</label>
      <input name="phone" class="form-control" placeholder="+18095551234" ${v(customer?.phone)}>
    </div>
    <hr class="my-3">
    <p class="text-muted small mb-2">Address (optional)</p>
    <div class="mb-2">
      <input name="address.street" class="form-control" placeholder="Street"
             ${v(customer?.address?.street)}>
    </div>
    <div class="row g-2 mb-2">
      <div class="col">
        <input name="address.city" class="form-control" placeholder="City"
               ${v(customer?.address?.city)}>
      </div>
      <div class="col">
        <input name="address.state" class="form-control" placeholder="State"
               ${v(customer?.address?.state)}>
      </div>
    </div>
    <div class="row g-2 mb-3">
      <div class="col">
        <input name="address.zipCode" class="form-control" placeholder="ZIP"
               ${v(customer?.address?.zipCode)}>
      </div>
      <div class="col">
        <input name="address.country" class="form-control" placeholder="Country"
               ${v(customer?.address?.country)}>
      </div>
    </div>
    <div id="form-error" class="alert alert-danger d-none"></div>
    <div class="d-flex gap-2 justify-content-end">
      <button type="button" class="btn btn-secondary btn-sm" id="btn-cancel-form">Cancel</button>
      <button type="submit" class="btn btn-primary btn-sm" id="btn-submit-form">Save</button>
    </div>`;
}

function bindEvents(): void {
    document.getElementById('btn-new-customer')!
        .addEventListener('click', () => openDrawer());

    document.getElementById('btn-search')!
        .addEventListener('click', () => {
            currentSearch = (document.getElementById('search-input') as HTMLInputElement).value;
            currentPage = 1;
            loadCustomers();
        });

    document.getElementById('search-input')!
        .addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                currentSearch = (e.target as HTMLInputElement).value;
                currentPage = 1;
                loadCustomers();
            }
        });

    document.getElementById('btn-close-drawer')!
        .addEventListener('click', closeDrawer);

    document.getElementById('drawer-overlay')!
        .addEventListener('click', closeDrawer);
}

function bindTableEvents(): void {
    document.querySelectorAll('.btn-edit').forEach((btn) => 
        btn.addEventListener('click', async (e) => {
            const id = (e.currentTarget as HTMLElement).dataset.id!;
            try {
                const { data } = await customersApi.getById(id);
                openDrawer(data);
            } catch (err: any) {
                showToast(err.message, 'danger')
            }
        })
    );

    document.querySelectorAll('.btn-delete').forEach((btn) => 
        btn.addEventListener('click', async (e) => {
            const id = (e.currentTarget as HTMLElement).dataset.id!;
            const confirmed = await showConfirmModal(
                'Deactivate this customer? They will no longer be able to place orders.'
            );
            if (!confirmed) return;
            try {
                await customersApi.deactivate(id);
                showToast('Customer deactivated.', 'success');
                loadCustomers();
            } catch (err: any) {
                showToast(err.message, 'danger')
            }
        })
    );
}

function bindPaginationEvents(totalPages: number): void {
    document.querySelectorAll('#pagination .page-link').forEach((link) => 
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const page = parseInt((e.currentTarget as HTMLElement).dataset.page ?? '1');
            if (page < 1 || page > totalPages) return;
            currentPage = page;
            loadCustomers();
        })
    );
}

function openDrawer(customer?: CustomerResponse): void {
    editingId = customer?.id ?? null;
    document.getElementById('drawer-title')!.textContent =
        customer ? 'Edit Customer' : 'New Customer';
    document.getElementById('customer-form')!.innerHTML =
        customerFormTemplate(customer);

    document.getElementById('customer-drawer')!.style.right = '0';
    document.getElementById('drawer-overlay')!.classList.remove('d-none');

    document.getElementById('btn-cancel-form')!
        .addEventListener('click', closeDrawer);
    
    document.getElementById('customer-form')!
        .addEventListener('submit', handleFormSubmit);    
}

function closeDrawer(): void {
    document.getElementById('customer-drawer')!.style.right = '-420px';
    document.getElementById('drawer-overlay')!.classList.add('d-none');
    editingId = null;
}

async function handleFormSubmit(e: Event): Promise<void> {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const errorE1 = document.getElementById('form-error')!;
    const submitBtn = document.getElementById('btn-submit-form') as HTMLButtonElement;

    errorE1.classList.add('d-none');
    submitBtn.disabled = true;
    submitBtn.textContent = 'Saving...';

    try {
        const fd = new FormData(form);
        const address = {
            street: fd.get('address.street') as string,
            city: fd.get('address.city') as string,
            state: fd.get('address.state') as string,
            zipCode: fd.get('address.zipCode') as string,
            country: fd.get('address.country') as string,
        };
        const hasAddress = Object.values(address).some((v) => v?.trim()); 

        if (editingId) {
            const payload: UpdateCustomerRequest = {
                firstName: fd.get('firstName') as string,
                lastName: fd.get('lastName') as string,
                phone: (fd.get('phone') as string) || undefined,
                address: hasAddress ? address : undefined
            };
            await customersApi.update(editingId, payload);
            showToast('Customer updated.', 'success');
        } else {
            const payload: CreateCustomerRequest = {
                firstName: fd.get('firstName') as string,
                lastName: fd.get('lastName') as string,
                email: fd.get('email') as string,
                phone: (fd.get('phone') as string) || undefined,
                address: hasAddress ? address : undefined
            };
            await customersApi.create(payload);
            showToast('Customer created.', 'success')
        }

        closeDrawer();
        loadCustomers();
    } catch (err: any) {
        errorE1.textContent = err.message;
        errorE1.classList.remove('d-none')
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = 'Save';
    }
}

function escHtml(str: string): string {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}