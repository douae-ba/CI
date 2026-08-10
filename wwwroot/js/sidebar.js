document.addEventListener('DOMContentLoaded', () => {
    const sidebar = document.getElementById('appSidebar');
    const toggleBtn = document.getElementById('sidebarToggle');

    if (!sidebar || !toggleBtn) return;

    // Se souvient de l'état choisi par l'utilisateur
    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (isCollapsed) sidebar.classList.add('collapsed');

    toggleBtn.addEventListener('click', () => {
        sidebar.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
    });
});