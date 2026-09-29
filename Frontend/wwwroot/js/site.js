document.addEventListener("DOMContentLoaded", function () {
  var toggle = document.getElementById("sidebarToggle");
  var sidebar = document.getElementById("sidebar");
  if (toggle && sidebar) {
    toggle.addEventListener("click", function () {
      sidebar.classList.toggle("open");
    });
    document.addEventListener("click", function (e) {
      if (!sidebar.contains(e.target) && !toggle.contains(e.target)) {
        sidebar.classList.remove("open");
      }
    });
  }

  document.querySelectorAll(".sidebar-nav .nav-group").forEach(function (group) {
    group.addEventListener("toggle", function () {
      if (!group.open) return;
      document.querySelectorAll(".sidebar-nav .nav-group[open]").forEach(function (other) {
        if (other !== group) other.open = false;
      });
    });
  });
});
