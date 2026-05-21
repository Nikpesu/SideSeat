(() => {
  if (window.jQuery && jQuery.validator) {
    jQuery.validator.setDefaults({
      onkeyup: false,
      onfocusout: (element) => {
        jQuery(element).valid();
      }
    });
  }
  const storageKey = "ss-theme";
  const root = document.documentElement;
  const toggleButton = document.getElementById("ss-theme-toggle");

  const getPreferredTheme = () => {
    const stored = localStorage.getItem(storageKey);
    if (stored === "light" || stored === "dark") {
      return stored;
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  };

  const setTheme = (theme) => {
    root.setAttribute("data-theme", theme);
    localStorage.setItem(storageKey, theme);

    if (toggleButton) {
      toggleButton.textContent = theme === "dark" ? "Tema: Dark" : "Tema: Light";
      toggleButton.setAttribute("aria-pressed", theme === "dark" ? "true" : "false");
    }
  };

  const initialTheme = getPreferredTheme();
  setTheme(initialTheme);

  if (toggleButton) {
    toggleButton.addEventListener("click", () => {
      const nextTheme = root.getAttribute("data-theme") === "dark" ? "light" : "dark";
      setTheme(nextTheme);
    });
  }

  const authModal = document.getElementById("authModal");
  if (authModal) {
    const tabs = Array.from(authModal.querySelectorAll("[data-auth-tab]"));
    const panels = Array.from(authModal.querySelectorAll("[data-auth-panel]"));

    const setActivePanel = (name) => {
      const target = name === "register" ? "register" : "login";
      tabs.forEach((tab) => tab.classList.toggle("is-active", tab.dataset.authTab === target));
      panels.forEach((panel) => panel.classList.toggle("is-active", panel.dataset.authPanel === target));
    };

    tabs.forEach((tab) => {
      tab.addEventListener("click", () => {
        setActivePanel(tab.dataset.authTab || "login");
      });
    });

    authModal.addEventListener("show.bs.modal", (event) => {
      const trigger = event.relatedTarget;
      if (trigger && trigger.dataset && trigger.dataset.authOpen) {
        setActivePanel(trigger.dataset.authOpen);
      } else {
        setActivePanel("login");
      }
    });
    
      const queryAuth = new URLSearchParams(window.location.search).get("auth");
      if (queryAuth === "login" || queryAuth === "register") {
        setActivePanel(queryAuth);
        const modalInstance = bootstrap.Modal.getOrCreateInstance(authModal);
        modalInstance.show();
      
        const cleanUrl = window.location.pathname;
        window.history.replaceState({}, "", cleanUrl);
      }

  }

  const escapeHtml = (value) => String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");

  const debounce = (callback, delay) => {
    let timerId = null;

    return (...args) => {
      if (timerId) {
        window.clearTimeout(timerId);
      }

      timerId = window.setTimeout(() => callback(...args), delay);
    };
  };

  const formatDateValue = (date, dateOnly) => {
    const day = String(date.getDate()).padStart(2, "0");
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const year = date.getFullYear();
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");
    const locale = (navigator.language || "hr").toLowerCase();

    if (locale.startsWith("hr")) {
      return dateOnly ? `${day}.${month}.${year}.` : `${day}.${month}.${year}. ${hours}:${minutes}`;
    }

    return dateOnly ? `${month}/${day}/${year}` : `${month}/${day}/${year} ${hours}:${minutes}`;
  };

  const parseDateValue = (value, dateOnly) => {
    const normalized = value.trim().replace(/\.$/, "");
    const buildDate = (year, month, day, hours = 0, minutes = 0) => new Date(year, month - 1, day, hours, minutes, 0, 0);

    if (dateOnly) {
      const hrDateOnlyPattern = /^(\d{1,2})\.(\d{1,2})\.(\d{4})(?:\.)?$/;
      const enDateOnlyPattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/;
      const isoDateOnlyPattern = /^(\d{4})-(\d{2})-(\d{2})$/;

      let matches = normalized.match(hrDateOnlyPattern);
      if (matches) {
        return buildDate(Number(matches[3]), Number(matches[2]), Number(matches[1]));
      }

      matches = normalized.match(enDateOnlyPattern);
      if (matches) {
        return buildDate(Number(matches[3]), Number(matches[1]), Number(matches[2]));
      }

      matches = normalized.match(isoDateOnlyPattern);
      if (matches) {
        return buildDate(Number(matches[1]), Number(matches[2]), Number(matches[3]));
      }

      return null;
    }

    const hrDatePattern = /^(\d{1,2})\.(\d{1,2})\.(\d{4})(?:\.)?\s*(\d{1,2}):(\d{2})$/;
    const enDatePattern = /^(\d{1,2})\/(\d{1,2})\/(\d{4})\s*(\d{1,2}):(\d{2})$/;
    const isoDatePattern = /^(\d{4})-(\d{2})-(\d{2})(?:[T\s](\d{2}):(\d{2}))?$/;

    let matches = normalized.match(hrDatePattern);
    if (matches) {
      return buildDate(Number(matches[3]), Number(matches[2]), Number(matches[1]), Number(matches[4]), Number(matches[5]));
    }

    matches = normalized.match(enDatePattern);
    if (matches) {
      return buildDate(Number(matches[3]), Number(matches[1]), Number(matches[2]), Number(matches[4]), Number(matches[5]));
    }

    matches = normalized.match(isoDatePattern);
    if (matches) {
      return buildDate(Number(matches[1]), Number(matches[2]), Number(matches[3]), Number(matches[4] ?? "0"), Number(matches[5] ?? "0"));
    }

    return null;
  };

  const initializeDateFields = () => {
    document.querySelectorAll("[data-ss-datetime]").forEach((field) => {
      if (field.dataset.ssBound === "true") {
        return;
      }

      const hidden = field.querySelector("[data-ss-datetime-hidden]");
      const input = field.querySelector("[data-ss-datetime-input]");
      const toggle = field.querySelector("[data-ss-datetime-toggle]");
      const picker = field.querySelector("[data-ss-datetime-picker]");
      if (!hidden || !input || !picker) {
        return;
      }

      field.dataset.ssBound = "true";
      const dateOnly = field.dataset.dateOnly === "true";
      const monthNames = ["Sijecanj", "Veljaca", "Ozujak", "Travanj", "Svibanj", "Lipanj", "Srpanj", "Kolovoz", "Rujan", "Listopad", "Studeni", "Prosinac"];
      const weekdays = ["Po", "Ut", "Sr", "Ce", "Pe", "Su", "Ne"];
      const now = new Date();

      let selectedDate = parseDateValue(hidden.value, dateOnly) ?? parseDateValue(input.value, dateOnly) ?? new Date(now.getFullYear(), now.getMonth(), now.getDate(), dateOnly ? 0 : now.getHours(), dateOnly ? 0 : now.getMinutes());
      let viewYear = selectedDate.getFullYear();
      let viewMonth = selectedDate.getMonth();

      const syncVisible = () => {
        if (!hidden.value) {
          input.value = "";
          return;
        }

        const parsed = dateOnly ? parseDateValue(hidden.value, true) : new Date(hidden.value);
        if (!Number.isNaN(parsed.getTime())) {
          selectedDate = new Date(parsed.getTime());
          viewYear = selectedDate.getFullYear();
          viewMonth = selectedDate.getMonth();
          input.value = formatDateValue(parsed, dateOnly);
        }
      };

      const syncHidden = (notify = true) => {
        const parsed = parseDateValue(input.value, dateOnly);
        if (!parsed || Number.isNaN(parsed.getTime())) {
          const previous = hidden.value;
          hidden.value = "";
          input.classList.add("is-invalid");
          if (notify && previous !== hidden.value) {
            hidden.dispatchEvent(new Event("change", { bubbles: true }));
          }
          return;
        }

        selectedDate = new Date(parsed.getTime());
        viewYear = selectedDate.getFullYear();
        viewMonth = selectedDate.getMonth();
        input.classList.remove("is-invalid");
        const nextValue = dateOnly
          ? `${parsed.getFullYear()}-${String(parsed.getMonth() + 1).padStart(2, "0")}-${String(parsed.getDate()).padStart(2, "0")}`
          : `${parsed.getFullYear()}-${String(parsed.getMonth() + 1).padStart(2, "0")}-${String(parsed.getDate()).padStart(2, "0")}T${String(parsed.getHours()).padStart(2, "0")}:${String(parsed.getMinutes()).padStart(2, "0")}`;
        const previous = hidden.value;
        hidden.value = nextValue;
        if (notify && previous !== hidden.value) {
          hidden.dispatchEvent(new Event("change", { bubbles: true }));
        }
      };

      const closePicker = () => {
        picker.hidden = true;
      };

      const positionPicker = () => {
        picker.classList.remove("is-drop-up", "is-align-right");
        const rect = picker.getBoundingClientRect();
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;
        if (rect.right > viewportWidth - 8) {
          picker.classList.add("is-align-right");
        }
        if (rect.bottom > viewportHeight - 8) {
          picker.classList.add("is-drop-up");
        }
      };

      const openPicker = () => {
        picker.hidden = false;
        window.requestAnimationFrame(positionPicker);
      };

      const renderPicker = () => {
        const timeMarkup = dateOnly
          ? ""
          : `<div class="ss-datetime-time">
               <label>Vrijeme</label>
               <input type="time" class="form-control" data-ss-datetime-time value="${String(selectedDate.getHours()).padStart(2, "0")}:${String(selectedDate.getMinutes()).padStart(2, "0")}">
             </div>`;

        const yearStart = now.getFullYear() - 100;
        const yearEnd = now.getFullYear() + 20;
        const yearOptions = [];
        for (let y = yearStart; y <= yearEnd; y += 1) {
          yearOptions.push(`<option value="${y}"${y === viewYear ? " selected" : ""}>${y}</option>`);
        }

        const monthOptions = monthNames.map((name, index) => `<option value="${index}"${index === viewMonth ? " selected" : ""}>${name}</option>`).join("");

        const firstDay = new Date(viewYear, viewMonth, 1);
        const offset = (firstDay.getDay() + 6) % 7;
        const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
        const cells = [];
        for (let i = 0; i < offset; i += 1) {
          cells.push(`<button type="button" class="ss-datetime-day is-empty" tabindex="-1" aria-hidden="true"></button>`);
        }
        for (let day = 1; day <= daysInMonth; day += 1) {
          const isSelected = selectedDate.getFullYear() === viewYear && selectedDate.getMonth() === viewMonth && selectedDate.getDate() === day;
          cells.push(`<button type="button" class="ss-datetime-day${isSelected ? " is-selected" : ""}" data-ss-day="${day}">${day}</button>`);
        }

        picker.innerHTML = `
          <div class="ss-datetime-head">
            <button type="button" class="ss-datetime-nav" data-ss-nav="-1" aria-label="Prethodni mjesec">‹</button>
            <select class="form-select" data-ss-datetime-month>${monthOptions}</select>
            <select class="form-select" data-ss-datetime-year>${yearOptions.join("")}</select>
            <button type="button" class="ss-datetime-nav" data-ss-nav="1" aria-label="Sljedeci mjesec">›</button>
          </div>
          <div class="ss-datetime-weekdays">${weekdays.map((d) => `<span>${d}</span>`).join("")}</div>
          <div class="ss-datetime-days">${cells.join("")}</div>
          ${timeMarkup}
          <div class="ss-datetime-actions">
            <button type="button" class="ss-btn ss-btn-secondary" data-ss-datetime-clear>Ocisti</button>
            ${dateOnly ? "" : `<button type="button" class="ss-btn" data-ss-datetime-apply>Primijeni</button>`}
          </div>
        `;

        picker.querySelector("[data-ss-datetime-month]")?.addEventListener("change", (event) => {
          viewMonth = Number(event.target.value);
          renderPicker();
        });
        picker.querySelector("[data-ss-datetime-year]")?.addEventListener("change", (event) => {
          viewYear = Number(event.target.value);
          renderPicker();
        });
        picker.querySelectorAll("[data-ss-nav]").forEach((navBtn) => {
          navBtn.addEventListener("click", () => {
            const delta = Number(navBtn.dataset.ssNav ?? "0");
            const next = new Date(viewYear, viewMonth + delta, 1);
            viewYear = next.getFullYear();
            viewMonth = next.getMonth();
            renderPicker();
          });
        });
        picker.querySelectorAll("[data-ss-day]").forEach((dayBtn) => {
          dayBtn.addEventListener("click", () => {
            const day = Number(dayBtn.dataset.ssDay ?? "1");
            selectedDate = new Date(viewYear, viewMonth, day, selectedDate.getHours(), selectedDate.getMinutes(), 0, 0);
            if (dateOnly) {
              input.value = formatDateValue(selectedDate, true);
              syncHidden();
              closePicker();
              return;
            }
            renderPicker();
          });
        });
        picker.querySelector("[data-ss-datetime-clear]")?.addEventListener("click", () => {
          hidden.value = "";
          input.value = "";
          input.classList.remove("is-invalid");
          hidden.dispatchEvent(new Event("change", { bubbles: true }));
          closePicker();
        });
        picker.querySelector("[data-ss-datetime-apply]")?.addEventListener("click", () => {
          const timeInput = picker.querySelector("[data-ss-datetime-time]");
          if (timeInput && !dateOnly) {
            const [hours, minutes] = String(timeInput.value || "00:00").split(":");
            selectedDate.setHours(Number(hours ?? "0"), Number(minutes ?? "0"), 0, 0);
          }
          input.value = formatDateValue(selectedDate, dateOnly);
          syncHidden();
          closePicker();
        });
      };

      syncVisible();
      input.addEventListener("blur", syncHidden);
      field.closest("form")?.addEventListener("submit", syncHidden);
      field.addEventListener("ss:sync-datetime", () => syncHidden(false));
      input.addEventListener("focus", () => {
        renderPicker();
        openPicker();
      });
      toggle?.addEventListener("click", () => {
        if (picker.hidden) {
          renderPicker();
          openPicker();
        } else {
          closePicker();
        }
      });
      document.addEventListener("click", (event) => {
        if (!field.contains(event.target)) {
          closePicker();
        }
      });
    });
  };

  const initializeAutocompleteFields = () => {
    document.querySelectorAll("[data-ss-autocomplete]").forEach((field) => {
      if (field.dataset.ssBound === "true") {
        return;
      }

      const hidden = field.querySelector("[data-ss-autocomplete-id]");
      const input = field.querySelector("[data-ss-autocomplete-input]");
      const menu = field.querySelector("[data-ss-autocomplete-menu]");
      const endpoint = field.dataset.endpoint;
      const minimumLength = Number(field.dataset.minLength ?? "0");
      const routeMode = field.dataset.routeMode;
      const routePeerName = field.dataset.routePeer;

      if (!hidden || !input || !menu || !endpoint) {
        return;
      }

      field.dataset.ssBound = "true";

      const closeMenu = () => {
        menu.hidden = true;
        input.setAttribute("aria-expanded", "false");
      };

      const renderMenu = (items) => {
        if (!items.length) {
          menu.innerHTML = '<div class="ss-autocomplete-empty">Nema rezultata.</div>';
          menu.hidden = false;
          return;
        }

        menu.innerHTML = items.map((item) => `
          <button type="button" class="ss-autocomplete-option" data-id="${escapeHtml(item.id)}" data-text="${escapeHtml(item.text)}">
            <span class="ss-autocomplete-title">${escapeHtml(item.text)}</span>
            ${item.subtext ? `<span class="ss-autocomplete-subtitle">${escapeHtml(item.subtext)}</span>` : ""}
          </button>
        `).join("");
        menu.hidden = false;
        input.setAttribute("aria-expanded", "true");
      };

      const fetchItems = debounce(async () => {
        const query = input.value.trim();

        if (query.length < minimumLength) {
          menu.innerHTML = "";
          menu.hidden = true;
          return;
        }

        const url = new URL(endpoint, window.location.origin);
        url.searchParams.set("q", query);
        if (routeMode === "from" || routeMode === "to") {
          url.searchParams.set("mode", routeMode);
          if (routePeerName) {
            const peerHidden = document.querySelector(`input[data-ss-autocomplete-id][name="${routePeerName}"]`);
            const peerInput = peerHidden?.closest("[data-ss-autocomplete]")?.querySelector("[data-ss-autocomplete-input]");
            const peerValue = peerHidden ? String(peerHidden.value || "").trim() : "";
            if (peerValue.length > 0) {
              url.searchParams.set(routePeerName, peerValue);
            } else {
              url.searchParams.delete(routePeerName);
            }
            const peerTextValue = peerInput ? String(peerInput.value || "").trim() : "";
            if (peerTextValue.length > 0) {
              url.searchParams.set(`${routePeerName}Text`, peerTextValue);
            } else {
              url.searchParams.delete(`${routePeerName}Text`);
            }
          }
        }

        try {
          const response = await fetch(url.toString(), {
            headers: {
              "X-Requested-With": "XMLHttpRequest"
            }
          });

          if (!response.ok) {
            return;
          }

          const items = await response.json();
          renderMenu(items);
        } catch {
          closeMenu();
        }
      }, 220);

      input.addEventListener("input", () => {
        hidden.value = "";
        input.classList.remove("is-invalid");
        fetchItems();
      });
      input.addEventListener("focus", () => {
        if (menu.innerHTML) {
          menu.hidden = false;
          return;
        }

        fetchItems();
      });
      input.addEventListener("blur", () => {
        window.setTimeout(closeMenu, 140);
      });
      input.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
          closeMenu();
        }
      });
      menu.addEventListener("mousedown", (event) => {
        const option = event.target.closest("[data-id]");
        if (!option) {
          return;
        }

        event.preventDefault();
        hidden.value = option.dataset.id ?? "";
        input.value = option.dataset.text ?? "";
        input.classList.remove("is-invalid");
        hidden.dispatchEvent(new Event("change", { bubbles: true }));
        input.dispatchEvent(new Event("change", { bubbles: true }));

        closeMenu();
      });

      const ensureValidSelection = () => {
        const isRequired = input.hasAttribute("required");
        if (!isRequired) {
          return;
        }

        if (!hidden.value) {
          input.classList.add("is-invalid");
        } else {
          input.classList.remove("is-invalid");
        }
      };

      input.addEventListener("blur", ensureValidSelection);
      field.closest("form")?.addEventListener("submit", ensureValidSelection);
    });
  };

  const initializeAjaxLists = () => {
    document.querySelectorAll("[data-ss-list-page]").forEach((page) => {
      const searchInput = page.querySelector("[data-ss-list-search]");
      if (!searchInput || searchInput.dataset.ssBound === "true") {
        return;
      }

      searchInput.dataset.ssBound = "true";

      const refreshList = debounce(async () => {
        const url = new URL(window.location.href);
        const query = searchInput.value.trim();
        const pageSizeElem = page.querySelector("[data-ss-list-pagesize]");
        const pageSizeValue = pageSizeElem ? String(pageSizeElem.value) : null;

        if (query.length > 0) {
          url.searchParams.set("search", query);
        } else {
          url.searchParams.delete("search");
        }
        if (pageSizeValue && pageSizeValue.length > 0) {
          url.searchParams.set("pageSize", pageSizeValue);
        } else {
          url.searchParams.delete("pageSize");
        }
        page.querySelectorAll("input[name], select[name], textarea[name]").forEach((control) => {
          const name = control.getAttribute("name");
          if (!name || name === "search" || name === "pageSize") {
            return;
          }
          if (control.type === "checkbox" && !control.checked) {
            url.searchParams.delete(name);
            return;
          }
          const value = String(control.value ?? "").trim();
          if (value.length > 0) {
            url.searchParams.set(name, value);
          } else {
            url.searchParams.delete(name);
          }
        });

        try {
          const response = await fetch(url.toString(), {
            headers: {
              "X-Requested-With": "XMLHttpRequest"
            }
          });

          if (!response.ok) {
            return;
          }

          const html = await response.text();
          const parser = new DOMParser();
          const documentFragment = parser.parseFromString(html, "text/html");
          const updatedPage = documentFragment.querySelector("[data-ss-list-page]");
          const currentPage = page;

          // Preserve focus and selection for the search input
          const active = document.activeElement;
          const wasFocusedInPage = currentPage ? currentPage.contains(active) : false;
          const wasSearch = active === searchInput;
          const selection = wasSearch && typeof active.selectionStart === "number"
            ? { start: active.selectionStart, end: active.selectionEnd }
            : null;

          if (!updatedPage || !currentPage) {
            return;
          }

          const currentListRegion = currentPage.querySelector("[data-ss-list-content], .ss-empty");
          const updatedListRegion = updatedPage.querySelector("[data-ss-list-content], .ss-empty");
          if (!currentListRegion || !updatedListRegion) {
            return;
          }

          currentListRegion.outerHTML = updatedListRegion.outerHTML;

          if (pageSizeElem) {
            pageSizeElem.value = pageSizeValue ?? "";
          }

          window.history.replaceState({}, "", url);

          if (wasFocusedInPage && wasSearch) {
            searchInput.focus();
            if (selection && typeof searchInput.setSelectionRange === "function") {
              try { searchInput.setSelectionRange(selection.start, selection.end); } catch {}
            }
          }

          const flashTarget = currentPage.querySelector("[data-ss-list-content]");
          if (flashTarget) {
            flashTarget.classList.remove("ss-list-flash");
            window.setTimeout(() => {
              flashTarget.classList.add("ss-list-flash");
            }, 10);
          }
        } catch {
          // Keep the current content if an AJAX refresh fails.
        }
      }, 250);

      const pageSizeControl = page.querySelector("[data-ss-list-pagesize]");
      if (pageSizeControl) {
        pageSizeControl.addEventListener("change", refreshList);
      }

      page.querySelectorAll("input[name], select[name], textarea[name]").forEach((control) => {
        if (control === searchInput || control === pageSizeControl) {
          return;
        }
        if (control.dataset.ssListBound === "true") {
          return;
        }
        control.dataset.ssListBound = "true";
        const eventName = control.tagName === "SELECT" || control.type === "hidden" ? "change" : "input";
        control.addEventListener(eventName, refreshList);
      });

      searchInput.addEventListener("input", refreshList);
      searchInput.addEventListener("keydown", (event) => {
        if (event.key === "Escape") {
          searchInput.value = "";
          searchInput.dispatchEvent(new Event("input", { bubbles: true }));
        }
      });
    });
  };

  const initializeAjaxForms = () => {
    document.querySelectorAll("[data-ss-ajax-form]").forEach((form) => {
      if (form.dataset.ssBound === "true") {
        return;
      }

      const targetSelector = form.dataset.ssAjaxTarget;
      if (!targetSelector) {
        return;
      }

      const target = document.querySelector(targetSelector);
      if (!target) {
        return;
      }

      form.dataset.ssBound = "true";

      const refreshForm = debounce(async () => {
        const url = new URL(form.action || window.location.href, window.location.origin);
        form.querySelectorAll("[data-ss-datetime]").forEach((field) => {
          field.dispatchEvent(new Event("ss:sync-datetime"));
        });
        const formData = new FormData(form);

        for (const [key, value] of formData.entries()) {
          if (typeof value === "string") {
            const trimmed = value.trim();
            if (trimmed.length > 0) {
              url.searchParams.set(key, trimmed);
            } else {
              url.searchParams.delete(key);
            }
          }
        }

        try {
          const response = await fetch(url.toString(), {
            headers: {
              "X-Requested-With": "XMLHttpRequest"
            }
          });

          if (!response.ok) {
            return;
          }

          const html = await response.text();
          const parser = new DOMParser();
          const documentFragment = parser.parseFromString(html, "text/html");
          const updatedTarget = documentFragment.querySelector(targetSelector);
          const currentTarget = document.querySelector(targetSelector);

          if (!updatedTarget || !currentTarget) {
            return;
          }

          currentTarget.outerHTML = updatedTarget.outerHTML;
          if (form.dataset.ajaxPushState !== "false") {
            window.history.replaceState({}, "", url);
          }
          initializeEnhancedUi();

          const flashTarget = document.querySelector(targetSelector);
          if (flashTarget) {
            flashTarget.classList.remove("ss-list-flash");
            window.setTimeout(() => {
              flashTarget.classList.add("ss-list-flash");
            }, 10);
          }
        } catch {
          // Keep the current content if an AJAX refresh fails.
        }
      }, 250);

      form.addEventListener("submit", (event) => {
        event.preventDefault();
        refreshForm();
      });

      form.querySelectorAll("input, select, textarea").forEach((control) => {
        control.addEventListener("input", refreshForm);
        control.addEventListener("change", refreshForm);
      });
    });
  };

  const initializeEnhancedUi = () => {
    initializeDateFields();
    initializeAutocompleteFields();
    initializeAjaxLists();
    initializeAjaxForms();
  };

  initializeEnhancedUi();
})();
