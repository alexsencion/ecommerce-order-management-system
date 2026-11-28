import 'bootstrap/dist/css/bootstrap.min.css';
import { initCustomersPage } from './pages/customers';
import { initProductsPage } from './pages/products';
import { initOrdersPage } from "./pages/orders";
import { initDashboardPage } from './pages/dashboard';

const app = document.getElementById('app')!;

function router(): void {
    const hash = window.location.hash.replace('#', '') || 'dashboard';

    document.querySelectorAll('.nav-item').forEach((el) => {
        el.classList.toggle('active', (el as HTMLElement).dataset.page === hash);
    });
    app.innerHTML = '';
    if (hash === 'customers') initCustomersPage(app);
    else if (hash === 'products') initProductsPage(app);
    else if (hash === 'orders') initOrdersPage(app);
    else app.innerHTML = `<p class="text-muted mt-4">
      ${hash.charAt(0).toUpperCase() + hash.slice(1)} module coming soon.
    </p>`;

    if (hash === 'dashboard') initDashboardPage(app);
    else if (hash === 'customers') initCustomersPage(app);
    else if (hash === 'products') initProductsPage(app);
    else if (hash === 'orders') initOrdersPage(app);
}

window.addEventListener('hashchange', router);
router();