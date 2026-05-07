import 'bootstrap/dist/css/bootstrap.min.css';
import { initCustomerPage } from './pages/customers';

const app = document.getElementById('app')!;

function router(): void {
    const hash = window.location.hash.replace('#', '') || 'customers';
    document.querySelectorAll('.nav-item').forEach((el) => {
        el.classList.toggle('active', (el as HTMLElement).dataset.page === hash);
    });

    app.innerHTML = '';

    if (hash === 'customers') {
        initCustomerPage(app);
    } else {
        app.innerHTML = `<p class="text-muted mt-4">
            ${hash.charAt(0).toUpperCase() + hash.slice(1)} module coming in a future phase.</p>`;
    }
}

window.addEventListener('hashchange', router)
router();