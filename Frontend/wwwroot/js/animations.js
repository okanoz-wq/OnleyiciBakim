// Sayfa geçişlerinde ve veri gösteriminde küçük, anlamlı animasyonlar.
// GSAP CDN üzerinden yükleniyor (_Layout.cshtml). GSAP bir sebeple
// yüklenemezse (ör. internet yoksa) sayfa yine de normal çalışmaya devam eder.
document.addEventListener("DOMContentLoaded", function () {
  function parseLocalizedNumber(value) {
    var normalized = String(value || "0").trim().replace(",", ".");
    var number = Number.parseFloat(normalized);
    return Number.isFinite(number) ? number : 0;
  }

  // Risk / güven skoru barları harici animasyon kütüphanesine bağlı değildir.
  // Türkçe kültürde Razor'ın ürettiği "72,5" gibi değerler de geçerli yüzdeye çevrilir.
  var widthEls = document.querySelectorAll("[data-width]");
  widthEls.forEach(function (el) {
    var width = Math.min(100, Math.max(0, parseLocalizedNumber(el.dataset.width)));
    el.setAttribute("role", "progressbar");
    el.setAttribute("aria-valuemin", "0");
    el.setAttribute("aria-valuemax", "100");
    el.setAttribute("aria-valuenow", width.toString());
    el.dataset.normalizedWidth = width.toString();
  });

  if (typeof gsap === "undefined") {
    widthEls.forEach(function (el) {
      el.style.width = el.dataset.normalizedWidth + "%";
    });
    document.querySelectorAll(".count-up").forEach(function (el) {
      var target = parseLocalizedNumber(el.dataset.target);
      var decimals = el.dataset.decimals ? parseInt(el.dataset.decimals, 10) : 0;
      el.textContent = target.toFixed(decimals);
    });
    return;
  }

  // ---- Sidebar + topbar giriş animasyonu ----
  gsap.from(".sidebar-brand, .sidebar-nav .nav-link, .sidebar-footer", {
    opacity: 0,
    x: -16,
    duration: 0.5,
    ease: "power2.out",
    stagger: 0.05,
  });

  gsap.from(".topbar-title, .topbar-subtitle", {
    opacity: 0,
    y: -8,
    duration: 0.45,
    ease: "power2.out",
    stagger: 0.08,
  });

  // ---- Genel içerik giriş animasyonu ----
  // Herhangi bir view'da ".reveal" class'ı taşıyan elemanlar sırayla
  // yukarıdan belirerek gelir (kartlar, tablo satırları, form alanları vs).
  var revealEls = document.querySelectorAll(".reveal");
  if (revealEls.length) {
    gsap.from(revealEls, {
      opacity: 0,
      y: 18,
      duration: 0.55,
      ease: "power2.out",
      stagger: 0.06,
    });
  }

  // ---- Sayaç animasyonu ----
  // <span class="count-up" data-target="42" data-decimals="0">0</span>
  document.querySelectorAll(".count-up").forEach(function (el) {
    var target = parseLocalizedNumber(el.dataset.target);
    var decimals = el.dataset.decimals ? parseInt(el.dataset.decimals, 10) : 0;
    var counter = { val: 0 };
    gsap.to(counter, {
      val: target,
      duration: 1.1,
      ease: "power1.out",
      onUpdate: function () {
        el.textContent = counter.val.toFixed(decimals);
      },
    });
  });

  // ---- Risk / güven skoru barlarının dolma animasyonu ----
  // <div class="risk-bar"><span data-width="72" style="background:#ef4444"></span></div>
  widthEls.forEach(function (el) {
    gsap.to(el, {
      width: el.dataset.normalizedWidth + "%",
      duration: 1,
      ease: "power2.out",
      delay: 0.15,
    });
  });

  // ---- 404 / hata sayfası özel animasyonu ----
  if (document.querySelector(".error-page")) {
    var tl = gsap.timeline();
    tl.from(".error-code", { opacity: 0, y: -30, duration: 0.6, ease: "back.out(1.6)" })
      .from(".error-icon", { opacity: 0, scale: 0.5, duration: 0.5, ease: "back.out(2)" }, "-=0.3")
      .from(".error-title, .error-message, .error-actions", {
        opacity: 0,
        y: 16,
        duration: 0.45,
        ease: "power2.out",
        stagger: 0.08,
      }, "-=0.2");

    gsap.to(".error-icon", {
      y: -8,
      duration: 1.6,
      repeat: -1,
      yoyo: true,
      ease: "sine.inOut",
    });
  }
});
