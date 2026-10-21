// js/dashboard.js

document.addEventListener('DOMContentLoaded', function () {
  const user = getUserInfo();

  if (!user) {
    window.location.href = 'login.html';
    return;
  }

  // 1. Render User Info ke Header & Widget
  const elUserDisplay = document.getElementById('user-display-name');
  const elWelcomeUser = document.getElementById('welcome-username');
  const elWidgetRole = document.getElementById('widget-role');

  if (elUserDisplay) elUserDisplay.innerText = user.username;
  if (elWelcomeUser) elWelcomeUser.innerText = user.username;
  if (elWidgetRole) elWidgetRole.innerText = user.role;

  // 2. Render Konten Dinamis Berdasarkan Role (Admin vs User)
  renderDashboardByRole(user.role);
});

function renderDashboardByRole(role) {
  const mainContent = document.getElementById('main-content');
  if (!mainContent) return;

  if (role.toLowerCase() === 'admin') {
    mainContent.innerHTML = `
      <div class="col-12">
        <div class="alert alert-warning">
          <h5><i class="bi bi-shield-lock"></i> Panel Kontrol Admin</h5>
          <p class="mb-0">Kamu memiliki akses penuh untuk mengelola pengguna dan sistem.</p>
        </div>
      </div>`;
  } else {
    mainContent.innerHTML = `
      <div class="col-12">
        <div class="alert alert-success">
          <h5><i class="bi bi-person"></i> Panel Pengguna</h5>
          <p class="mb-0">Selamat datang di dashboard standar pengguna.</p>
        </div>
      </div>`;
  }
}
