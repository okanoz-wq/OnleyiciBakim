(() => {
    "use strict";

    const container = () => document.getElementById("globalModalContainer");

    function toast(message, type = "success") {
        const host = document.getElementById("toastContainer");
        if (!host) return;
        const element = document.createElement("div");
        element.className = `toast align-items-center border-0 text-bg-${type}`;
        element.setAttribute("role", "alert");
        element.innerHTML = `<div class="d-flex"><div class="toast-body"></div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Kapat"></button></div>`;
        element.querySelector(".toast-body").textContent = message;
        host.appendChild(element);
        const instance = new bootstrap.Toast(element, { delay: 4500 });
        element.addEventListener("hidden.bs.toast", () => element.remove());
        instance.show();
    }

    async function readError(response) {
        const type = response.headers.get("content-type") || "";
        if (type.includes("application/json")) {
            const json = await response.json();
            return json.message || json.detail || json.title || "İşlem tamamlanamadı.";
        }
        const text = await response.text();
        return text && !text.trimStart().startsWith("<") ? text : "İşlem tamamlanamadı.";
    }

    function currentModal() {
        return container()?.querySelector(".modal");
    }

    function resetModalUi() {
        document.querySelectorAll(".modal-backdrop").forEach(x => x.remove());
        document.body.classList.remove("modal-open");
        document.body.style.removeProperty("padding-right");
        document.body.style.removeProperty("overflow");
    }

    function closeCurrentModal() {
        const element = currentModal();
        if (!element) return;
        bootstrap.Modal.getInstance(element)?.hide();
    }

    function initializeValidation(root) {
        root.querySelectorAll("form").forEach(form => {
            // Dinamik modallarda tarayıcının yerel doğrulaması submit olayını daha
            // document seviyesindeki kayıt işleyicisine ulaşmadan kesebiliyor.
            // Doğrulamayı sunucuya bırak; hatalı form yine aynı modalda gösterilir.
            form.noValidate = true;
            if (!window.jQuery?.validator?.unobtrusive) return;
            window.jQuery(form).removeData("validator").removeData("unobtrusiveValidation");
            window.jQuery.validator.unobtrusive.parse(form);
        });
    }

    function initializeCascades(root) {
        const company = root.querySelector('[data-cascade="company"]');
        const branch = root.querySelector('[data-cascade="branch"]');
        const department = root.querySelector('[data-cascade="department"]');
        const line = root.querySelector('[data-cascade="line"]');
        if (!company || !branch || !department || !line) return;

        const catalogue = new Map();
        [branch, department, line].forEach(select => {
            catalogue.set(select, [...select.options].slice(1).map(option => ({
                value: option.value, text: option.text, parent: option.dataset.parent || ""
            })));
        });
        const refill = (select, parentValue, keepValue) => {
            select.replaceChildren(new Option("Seçiniz", ""));
            catalogue.get(select).filter(x => x.parent === parentValue).forEach(x => select.add(new Option(x.text, x.value)));
            if ([...select.options].some(x => x.value === keepValue)) select.value = keepValue;
        };
        const selected = { branch: branch.value, department: department.value, line: line.value };
        refill(branch, company.value, selected.branch);
        refill(department, branch.value, selected.department);
        refill(line, department.value, selected.line);
        company.addEventListener("change", () => { refill(branch, company.value, ""); refill(department, "", ""); refill(line, "", ""); });
        branch.addEventListener("change", () => { refill(department, branch.value, ""); refill(line, "", ""); });
        department.addEventListener("change", () => refill(line, department.value, ""));
    }

    function initializeMachineComponents(root) {
        const machine = root.querySelector("[data-machine-select]");
        const component = root.querySelector("[data-component-select]");
        if (!machine || !component) return;
        const items = [...component.options].slice(1).map(option => ({
            value: option.value,
            text: option.text,
            parent: option.dataset.parent || ""
        }));
        const refill = keepValue => {
            component.replaceChildren(new Option("Seçiniz", ""));
            items.filter(x => x.parent === machine.value)
                .forEach(x => component.add(new Option(x.text, x.value)));
            if ([...component.options].some(x => x.value === keepValue)) component.value = keepValue;
        };
        const selected = component.value;
        refill(selected);
        machine.addEventListener("change", () => refill(""));
    }

    function initializeChecklist(root) {
        const select = root.querySelector("[data-checklist-select]");
        if (!select) return;
        const groups = [...root.querySelectorAll("[data-checklist-items]")];
        const update = () => groups.forEach(group => {
            const active = group.dataset.checklistItems === select.value;
            group.classList.toggle("d-none", !active);
            group.querySelectorAll("[data-checklist-control]")
                .forEach(control => {
                    control.disabled = !active;
                    control.required = active && control.dataset.required === "true";
                });
        });
        update();
        select.addEventListener("change", update);
    }

    async function showModal(url) {
        const host = container();
        if (!host) return;
        const previous = currentModal();
        if (previous) bootstrap.Modal.getInstance(previous)?.dispose();
        resetModalUi();
        host.innerHTML = '<div class="modal-loading"><span class="spinner-border text-primary" role="status"></span></div>';
        try {
            const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
            if (!response.ok) throw new Error(await readError(response));
            host.innerHTML = await response.text();
            const modal = currentModal();
            if (!modal) throw new Error("Modal içeriği yüklenemedi.");
            initializeValidation(host);
            initializeCascades(host);
            initializeMachineComponents(host);
            initializeChecklist(host);
            modal.addEventListener("hidden.bs.modal", () => { if (host.contains(modal)) host.innerHTML = ""; }, { once: true });
            bootstrap.Modal.getOrCreateInstance(modal).show();
            modal.addEventListener("shown.bs.modal", () => modal.querySelector("input:not([type=hidden]), select, textarea")?.focus(), { once: true });
        } catch (error) {
            host.innerHTML = "";
            toast(error.message || "Modal yüklenemedi.", "danger");
        }
    }

    async function refreshMachineTable() {
        const region = document.getElementById("machineTableRegion");
        if (!region) return;
        const url = new URL("/Machines/Table", window.location.origin);
        new URLSearchParams(window.location.search).forEach((value, key) => url.searchParams.set(key, value));
        const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!response.ok) throw new Error("Makine listesi yenilenemedi.");
        region.innerHTML = await response.text();
    }

    async function refreshRegion(targetId, refreshUrl) {
        if (!targetId || !refreshUrl) return refreshMachineTable();
        const region = document.getElementById(targetId);
        if (!region) return;
        const url = new URL(refreshUrl, window.location.origin);
        if (targetId === "definitionTableRegion") {
            const inactive = new URLSearchParams(window.location.search).get("includeInactive");
            if (inactive) url.searchParams.set("includeInactive", inactive);
        }
        const response = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!response.ok) throw new Error("Liste yenilenemedi.");
        region.innerHTML = await response.text();
    }

    async function submitForm(form) {
        if (form.dataset.submitting === "true") return;
        form.dataset.submitting = "true";
        const refreshTarget = form.dataset.refreshTarget;
        const refreshUrl = form.dataset.refreshUrl;
        const button = form.querySelector("[data-submit-button], button[type=submit]");
        const spinner = button?.querySelector(".spinner-border");
        if (button) button.disabled = true;
        spinner?.classList.remove("d-none");
        try {
            const response = await fetch(form.action, {
                method: (form.method || "post").toUpperCase(), body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            const type = response.headers.get("content-type") || "";
            if (!response.ok && type.includes("text/html") && currentModal()) {
                const host = container();
                const old = currentModal();
                bootstrap.Modal.getInstance(old)?.dispose();
                resetModalUi();
                host.innerHTML = await response.text();
                const replacement = currentModal();
                initializeValidation(host); initializeCascades(host); initializeMachineComponents(host); initializeChecklist(host);
                bootstrap.Modal.getOrCreateInstance(replacement).show();
                replacement.querySelector(".input-validation-error, .field-validation-error")?.focus();
                return;
            }
            if (!response.ok) throw new Error(await readError(response));
            const result = await response.json();
            closeCurrentModal();
            toast(result.message || form.dataset.confirmMessage || "İşlem başarıyla tamamlandı.");
            if (form.dataset.reloadPage === "true") {
                window.location.reload();
                return;
            }
            await refreshRegion(refreshTarget, refreshUrl);
        } catch (error) {
            toast(error.message || "İşlem tamamlanamadı.", "danger");
        } finally {
            form.dataset.submitting = "false";
            if (button) button.disabled = false;
            spinner?.classList.add("d-none");
        }
    }

    document.addEventListener("click", event => {
        const addItem = event.target.closest("[data-add-checklist-item]");
        if (addItem) {
            const host = document.getElementById("checklistItems");
            if (!host) return;
            const index = host.children.length;
            const row = document.createElement("div");
            row.className = "input-group mb-2";
            row.innerHTML = `<input name="Items[${index}].Text" class="form-control" placeholder="Kontrol maddesi" required><span class="input-group-text"><input name="Items[${index}].Required" value="true" type="checkbox" class="form-check-input mt-0">&nbsp; Zorunlu</span><button class="btn btn-outline-danger" type="button" data-remove-checklist-item><i class="bi bi-x"></i></button>`;
            host.appendChild(row);
            row.querySelector("input")?.focus();
            return;
        }
        const removeItem = event.target.closest("[data-remove-checklist-item]");
        if (removeItem) { removeItem.closest(".input-group")?.remove(); return; }
        const trigger = event.target.closest("[data-modal-url]");
        if (!trigger) return;
        event.preventDefault();
        showModal(trigger.dataset.modalUrl);
    });
    document.addEventListener("submit", event => {
        const form = event.target.closest("form[data-modal-form]");
        if (!form) return;
        event.preventDefault();
        submitForm(form);
    });

    window.modalCrud = { open: showModal, toast, refreshMachineTable };
})();
