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
    try {
      const stored = localStorage.getItem(storageKey);
      if (stored === "light" || stored === "dark") {
        return stored;
      }
    } catch {}

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
  };

  const setTheme = (theme) => {
    root.setAttribute("data-theme", theme);
    try {
      localStorage.setItem(storageKey, theme);
    } catch {}

    if (toggleButton) {
      const isDark = theme === "dark";
      const icon = toggleButton.querySelector(".ss-theme-toggle-icon");
      const label = toggleButton.querySelector(".ss-theme-toggle-label");
      if (icon) {
        icon.textContent = isDark ? "☾" : "☀";
      }
      if (label) {
        label.textContent = isDark ? "Dark" : "Light";
      }
      toggleButton.title = isDark ? "Prebaci na svijetlu temu" : "Prebaci na tamnu temu";
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

  const initializeSurfaceTilt = () => {
    const canTilt = window.matchMedia("(hover: hover) and (pointer: fine)").matches &&
      !window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (!canTilt) {
      return;
    }

    const tiltSelector = [
      "[data-ss-tilt]",
      ".ss-ride-card",
      ".ss-cap-card",
      ".ss-flow-step",
      ".ss-hero-metric",
      ".ss-holo-row",
      ".ss-detail-grid > div",
      ".ss-home-card",
      ".ss-role-card",
      ".ss-list-row",
      ".ss-notification-item"
    ].join(", ");
    let activeElement = null;
    let animationFrame = 0;
    let pointerX = 0;
    let pointerY = 0;

    const resetTilt = (element) => {
      if (!element) {
        return;
      }

      element.style.removeProperty("--ss-tilt-x");
      element.style.removeProperty("--ss-tilt-y");
      element.style.removeProperty("--ss-tilt-pointer-x");
      element.style.removeProperty("--ss-tilt-pointer-y");
      delete element.dataset.ssTiltActive;
    };

    const renderTilt = () => {
      animationFrame = 0;
      if (!activeElement || !activeElement.isConnected) {
        return;
      }

      const rect = activeElement.getBoundingClientRect();
      const normalizedX = Math.min(1, Math.max(0, (pointerX - rect.left) / Math.max(rect.width, 1)));
      const normalizedY = Math.min(1, Math.max(0, (pointerY - rect.top) / Math.max(rect.height, 1)));
      const rotateX = (0.5 - normalizedY) * 6;
      const rotateY = (normalizedX - 0.5) * 8;

      activeElement.classList.add("ss-tilt-surface");
      activeElement.style.setProperty("--ss-tilt-x", `${rotateX.toFixed(2)}deg`);
      activeElement.style.setProperty("--ss-tilt-y", `${rotateY.toFixed(2)}deg`);
      activeElement.style.setProperty("--ss-tilt-pointer-x", `${(normalizedX * 100).toFixed(1)}%`);
      activeElement.style.setProperty("--ss-tilt-pointer-y", `${(normalizedY * 100).toFixed(1)}%`);
      activeElement.dataset.ssTiltActive = "true";
    };

    document.addEventListener("pointermove", (event) => {
      const nextElement = event.target.closest?.(tiltSelector) || null;
      if (nextElement !== activeElement) {
        resetTilt(activeElement);
        activeElement = nextElement;
      }
      if (!activeElement) {
        return;
      }

      pointerX = event.clientX;
      pointerY = event.clientY;
      if (!animationFrame) {
        animationFrame = window.requestAnimationFrame(renderTilt);
      }
    }, { passive: true });

    document.addEventListener("pointerout", (event) => {
      if (!activeElement || (event.relatedTarget && activeElement.contains(event.relatedTarget))) {
        return;
      }

      resetTilt(activeElement);
      activeElement = null;
      if (animationFrame) {
        window.cancelAnimationFrame(animationFrame);
        animationFrame = 0;
      }
    }, { passive: true });

    window.addEventListener("blur", () => {
      resetTilt(activeElement);
      activeElement = null;
    });
  };

  initializeSurfaceTilt();

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

  const ensureImagePreviewModal = () => {
    let modal = document.getElementById("ssImagePreviewModal");
    if (modal) {
      return modal;
    }

    modal = document.createElement("div");
    modal.className = "modal fade";
    modal.id = "ssImagePreviewModal";
    modal.tabIndex = -1;
    modal.setAttribute("aria-hidden", "true");
    modal.innerHTML = `
      <div class="modal-dialog modal-dialog-centered modal-xl">
        <div class="modal-content ss-auth-shell">
          <div class="d-flex justify-content-between align-items-center gap-3 mb-3">
            <h2 class="h5 m-0" id="ssImagePreviewTitle">Slika recenzije</h2>
            <button type="button" class="btn-close ss-auth-close" data-bs-dismiss="modal" aria-label="Zatvori"></button>
          </div>
          <div class="text-center">
            <img id="ssImagePreviewImage" class="ss-image-preview" src="" alt="Slika recenzije" />
          </div>
          <div class="ss-actions mt-3">
            <button type="button" class="ss-btn ss-btn-secondary" data-bs-dismiss="modal">Zatvori</button>
            <button type="button" class="ss-btn ss-image-delete-button" data-ss-image-delete hidden>Obriši sliku</button>
          </div>
        </div>
      </div>`;
    document.body.appendChild(modal);
    return modal;
  };

  document.addEventListener("click", (event) => {
    const trigger = event.target.closest("[data-ss-image-preview]");
    if (!trigger) {
      return;
    }

    const modal = ensureImagePreviewModal();
    const image = modal.querySelector("#ssImagePreviewImage");
    const title = modal.querySelector("#ssImagePreviewTitle");
    const imageUrl = trigger.getAttribute("data-ss-image-preview") || "";
    const imageTitle = trigger.getAttribute("data-ss-image-title") || "Slika recenzije";
    const deleteButton = modal.querySelector("[data-ss-image-delete]");
    const deleteUrl = trigger.getAttribute("data-ss-image-delete-url") || "";
    const deleteToken = trigger.getAttribute("data-ss-image-delete-token") || "";
    const imageId = trigger.getAttribute("data-ss-image-id") || "";

    image.src = imageUrl;
    image.alt = imageTitle;
    title.textContent = imageTitle;
    deleteButton.hidden = !deleteUrl;
    deleteButton.dataset.deleteUrl = deleteUrl;
    deleteButton.dataset.deleteToken = deleteToken;
    deleteButton.dataset.imageId = imageId;
    deleteButton.dataset.imageTitle = imageTitle;
    bootstrap.Modal.getOrCreateInstance(modal).show();
  });

  document.addEventListener("click", async (event) => {
    const deleteButton = event.target.closest("[data-ss-image-delete]");
    if (!deleteButton || deleteButton.hidden) {
      return;
    }

    const imageTitle = deleteButton.dataset.imageTitle || "ovu sliku";
    if (!window.confirm(`Želiš li sigurno obrisati sliku "${imageTitle}"?`)) {
      return;
    }

    deleteButton.disabled = true;
    try {
      const response = await fetch(deleteButton.dataset.deleteUrl, {
        method: "POST",
        headers: {
          "RequestVerificationToken": deleteButton.dataset.deleteToken || "",
          "X-Requested-With": "XMLHttpRequest"
        }
      });

      if (!response.ok) {
        throw new Error("Brisanje slike nije uspjelo.");
      }

      const imageId = deleteButton.dataset.imageId;
      document.querySelectorAll(`[data-ss-image-id="${CSS.escape(imageId)}"]`).forEach((element) => {
        const images = element.closest(".ss-review-images");
        element.remove();
        if (images && images.children.length === 0) {
          images.remove();
        }
      });

      bootstrap.Modal.getOrCreateInstance(document.getElementById("ssImagePreviewModal")).hide();
      document.dispatchEvent(new CustomEvent("ss:attachment-deleted"));
    } catch (error) {
      window.alert(error.message || "Brisanje slike nije uspjelo.");
    } finally {
      deleteButton.disabled = false;
    }
  });

  const initializeAttachmentLists = () => {
    document.querySelectorAll("[data-ss-attachment-list]").forEach((container) => {
      if (container.dataset.ssBound === "true") {
        return;
      }

      container.dataset.ssBound = "true";
      const loadAttachments = async () => {
        const response = await fetch(container.dataset.ssAttachmentListUrl, {
          headers: { "X-Requested-With": "XMLHttpRequest" }
        });
        if (!response.ok) {
          container.innerHTML = '<p class="text-danger">Privitke nije moguće učitati.</p>';
          return;
        }

        container.innerHTML = await response.text();
      };

      document.addEventListener("ss:attachment-deleted", loadAttachments);
      loadAttachments();
    });
  };

  initializeAttachmentLists();

  document.querySelectorAll("[data-ss-file-picker]").forEach((input) => {
    const container = input.closest(".ss-file-picker");
    const status = container?.querySelector("[data-ss-file-picker-status]");
    if (!status) {
      return;
    }

    input.addEventListener("change", () => {
      const count = input.files?.length ?? 0;
      status.textContent = count === 0
        ? "Nijedna slika nije odabrana."
        : count === 1
          ? input.files[0].name
          : `Odabrano slika: ${count}`;
    });
  });

  const paymentMethodInputs = document.querySelectorAll('input[name="NacinPlacanja"]');
  const cardFields = document.querySelector("[data-payment-card-fields]");
  const externalFields = document.querySelector("[data-payment-external-fields]");
  const externalProviderName = document.querySelector("[data-external-provider-name]");
  const externalAccountName = document.querySelector("[data-external-account-name]");
  const externalPaymentConfirmed = document.querySelector("[data-external-payment-confirmed]");
  if (paymentMethodInputs.length > 0 && cardFields && externalFields) {
    const updatePaymentFields = () => {
      const selected = document.querySelector('input[name="NacinPlacanja"]:checked');
      const isCard = selected?.value === "Kartica";
      cardFields.hidden = !isCard;
      externalFields.hidden = isCard;
      cardFields.querySelectorAll("input").forEach((input) => {
        input.disabled = !isCard;
      });
      externalFields.querySelectorAll("input").forEach((input) => {
        input.disabled = isCard;
      });
      if (externalProviderName && !isCard) {
        externalProviderName.textContent = `${selected?.value ?? "Vanjski"} račun`;
      }
      if (externalAccountName) {
        externalAccountName.required = !isCard;
      }
      if (externalPaymentConfirmed) {
        externalPaymentConfirmed.value = "false";
      }
    };

    paymentMethodInputs.forEach((input) => input.addEventListener("change", updatePaymentFields));
    updatePaymentFields();
  }

  const cardNumberInput = document.querySelector("[data-card-number]");
  cardNumberInput?.addEventListener("input", () => {
    const digits = cardNumberInput.value.replace(/\D/g, "").slice(0, 16);
    cardNumberInput.value = digits.replace(/(\d{4})(?=\d)/g, "$1 ");
  });

  const cardExpiryInput = document.querySelector("[data-card-expiry]");
  cardExpiryInput?.addEventListener("input", () => {
    const digits = cardExpiryInput.value.replace(/\D/g, "").slice(0, 4);
    cardExpiryInput.value = digits.length > 2
      ? `${digits.slice(0, 2)}/${digits.slice(2)}`
      : digits;
  });

  const cardCvvInput = document.querySelector("[data-card-cvv]");
  cardCvvInput?.addEventListener("input", () => {
    cardCvvInput.value = cardCvvInput.value.replace(/\D/g, "").slice(0, 4);
  });

  const paymentForm = document.querySelector("[data-payment-form]");
  const externalPaymentModalElement = document.querySelector("#externalPaymentModal");
  if (paymentForm && externalPaymentModalElement && externalPaymentConfirmed) {
    document.body.appendChild(externalPaymentModalElement);
    const modal = bootstrap.Modal.getOrCreateInstance(externalPaymentModalElement);
    const modalTitle = externalPaymentModalElement.querySelector("[data-external-modal-title]");
    const modalMessage = externalPaymentModalElement.querySelector("[data-external-modal-message]");
    const completeButton = externalPaymentModalElement.querySelector("[data-external-payment-complete]");
    let providerWindow = null;

    paymentForm.addEventListener("submit", (event) => {
      const selected = document.querySelector('input[name="NacinPlacanja"]:checked')?.value;
      if (selected === "Kartica" || externalPaymentConfirmed.value === "true") {
        return;
      }

      event.preventDefault();
      if (!paymentForm.reportValidity()) {
        return;
      }

      const providerUrl = selected === "PayPal"
        ? "https://www.paypal.com/"
        : "https://www.revolut.com/";
      const providerLabel = selected === "PayPal" ? "PayPal Pay" : "Revolut Pay";
      const popupWidth = 520;
      const popupHeight = 720;
      const popupLeft = Math.max(0, window.screenX + (window.outerWidth - popupWidth) / 2);
      const popupTop = Math.max(0, window.screenY + (window.outerHeight - popupHeight) / 2);
      const popupName = selected === "PayPal" ? "sideseat-paypal" : "sideseat-revolut";
      const popupFeatures = [
        "popup=yes",
        `width=${popupWidth}`,
        `height=${popupHeight}`,
        `left=${Math.round(popupLeft)}`,
        `top=${Math.round(popupTop)}`,
        "resizable=yes",
        "scrollbars=yes",
        "toolbar=no",
        "menubar=no",
        "location=no",
        "status=no"
      ].join(",");

      providerWindow = window.open(providerUrl, popupName, popupFeatures);
      providerWindow?.focus();
      modalTitle.textContent = `Otvoren ${providerLabel}`;
      modalMessage.textContent = providerWindow
        ? `${providerLabel} otvoren je u zasebnom popup prozoru.`
        : `Preglednik je blokirao ${providerLabel} popup. Dopusti popup prozore i pokušaj ponovno.`;
      modal.show();
    });

    completeButton?.addEventListener("click", () => {
      providerWindow?.close();
      providerWindow = null;
      externalPaymentConfirmed.value = "true";
      modal.hide();
      paymentForm.requestSubmit();
    });

    externalPaymentModalElement.addEventListener("hidden.bs.modal", () => {
      providerWindow?.close();
      providerWindow = null;
    });
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
        const path = typeof event.composedPath === "function" ? event.composedPath() : [];
        const clickedInsideField = path.length > 0
          ? path.includes(field)
          : field.contains(event.target);
        if (!clickedInsideField) {
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
          //AJAX UPIT
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

  const initializeTableCards = (rootElement = document) => {
    rootElement.querySelectorAll(".ss-table").forEach((table) => {
      const headers = Array.from(table.querySelectorAll("thead th"))
        .map((header) => header.textContent.trim());

      table.querySelectorAll("tbody tr").forEach((row) => {
        Array.from(row.cells).forEach((cell, index) => {
          if (cell.colSpan > 1) {
            cell.classList.add("ss-card-cell-wide");
            cell.removeAttribute("data-label");
            return;
          }

          const label = headers[index] || "";
          if (label) {
            cell.dataset.label = label;
          } else {
            cell.removeAttribute("data-label");
          }
        });
      });

      table.classList.add("is-card-table");
      table.closest(".ss-table-shell")?.classList.add("ss-card-table-shell");
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
          //AJAX UPIT
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
          initializeTableCards(currentPage);

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
          //AJAX UPIT
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

  const initializePerformativeUi = () => {
    document.querySelectorAll("[data-ss-word-roll]").forEach((element) => {
      if (element.dataset.ssBound === "true") {
        return;
      }

      const words = (element.dataset.words || "")
        .split(",")
        .map((word) => word.trim())
        .filter(Boolean);
      if (words.length < 2) {
        return;
      }

      element.dataset.ssBound = "true";
      let index = 0;
      const rotateWord = () => {
        if (document.hidden || !element.isConnected) {
          return;
        }

        element.classList.add("is-changing");
        window.setTimeout(() => {
          index = (index + 1) % words.length;
          element.textContent = words[index];
        }, 160);
        window.setTimeout(() => element.classList.remove("is-changing"), 360);
      };

      window.setInterval(rotateWord, 3600);
    });

    document.querySelectorAll("[data-ss-counter]").forEach((element) => {
      if (element.dataset.ssBound === "true") {
        return;
      }

      element.dataset.ssBound = "true";
      const target = Number.parseInt(element.dataset.ssCounter || "0", 10);
      if (!Number.isFinite(target) || target <= 0) {
        element.textContent = "0";
        return;
      }

      const animate = () => {
        const startedAt = performance.now();
        const duration = Math.min(1200, 500 + target * 18);
        const frame = (time) => {
          const progress = Math.min(1, (time - startedAt) / duration);
          const eased = 1 - Math.pow(1 - progress, 3);
          element.textContent = Math.round(target * eased).toLocaleString("hr-HR");
          if (progress < 1) {
            requestAnimationFrame(frame);
          }
        };
        requestAnimationFrame(frame);
      };

      if ("IntersectionObserver" in window) {
        const observer = new IntersectionObserver((entries) => {
          if (entries.some((entry) => entry.isIntersecting)) {
            observer.disconnect();
            animate();
          }
        }, { threshold: 0.35 });
        observer.observe(element);
      } else {
        animate();
      }
    });

  };

  const initializeAiAssistant = () => {
    const assistant = document.querySelector("[data-ss-ai]");
    if (!assistant || assistant.dataset.ssBound === "true") {
      return;
    }

    assistant.dataset.ssBound = "true";
    const panel = assistant.querySelector(".ss-ai-panel");
    const toggle = assistant.querySelector("[data-ss-ai-toggle]");
    const close = assistant.querySelector("[data-ss-ai-close]");
    const form = assistant.querySelector("[data-ss-ai-form]");
    const input = assistant.querySelector("[data-ss-ai-input]");
    const messagesElement = assistant.querySelector("[data-ss-ai-messages]");
    const sendButton = assistant.querySelector("[data-ss-ai-send]");
    const configured = assistant.dataset.configured === "true";
    const history = [];

    if (!panel || !toggle || !form || !input || !messagesElement || !sendButton) {
      return;
    }

    const isSafeInternalLink = (href) => {
      if (!href || !href.startsWith("/") || href.startsWith("//")) {
        return false;
      }

      try {
        return new URL(href, window.location.origin).origin === window.location.origin;
      } catch {
        return false;
      }
    };

    const appendInlineMarkdown = (parent, text) => {
      const tokenPattern = /(\[([^\]]+)\]\(([^)]+)\)|\*\*([^*]+)\*\*|`([^`]+)`|\*([^*\n]+)\*)/g;
      let cursor = 0;
      let match;

      while ((match = tokenPattern.exec(text)) !== null) {
        if (match.index > cursor) {
          parent.appendChild(document.createTextNode(text.slice(cursor, match.index)));
        }

        if (match[2] !== undefined) {
          const label = match[2];
          const href = match[3].trim();
          if (isSafeInternalLink(href)) {
            const link = document.createElement("a");
            link.href = href;
            link.className = "ss-ai-link";
            link.textContent = label;
            parent.appendChild(link);
          } else {
            parent.appendChild(document.createTextNode(label));
          }
        } else if (match[4] !== undefined) {
          const strong = document.createElement("strong");
          strong.textContent = match[4];
          parent.appendChild(strong);
        } else if (match[5] !== undefined) {
          const code = document.createElement("code");
          code.textContent = match[5];
          parent.appendChild(code);
        } else if (match[6] !== undefined) {
          const emphasis = document.createElement("em");
          emphasis.textContent = match[6];
          parent.appendChild(emphasis);
        }

        cursor = tokenPattern.lastIndex;
      }

      if (cursor < text.length) {
        parent.appendChild(document.createTextNode(text.slice(cursor)));
      }
    };

    const renderMarkdown = (container, markdown) => {
      const lines = String(markdown || "").replace(/\r\n?/g, "\n").split("\n");
      let list = null;
      let listType = "";
      let paragraphLines = [];
      let codeBlock = null;

      const closeList = () => {
        list = null;
        listType = "";
      };

      const flushParagraph = () => {
        if (paragraphLines.length === 0) {
          return;
        }

        const paragraph = document.createElement("p");
        appendInlineMarkdown(paragraph, paragraphLines.join(" "));
        container.appendChild(paragraph);
        paragraphLines = [];
      };

      lines.forEach((line) => {
        if (line.trim().startsWith("```")) {
          flushParagraph();
          closeList();
          if (codeBlock) {
            container.appendChild(codeBlock);
            codeBlock = null;
          } else {
            codeBlock = document.createElement("pre");
            codeBlock.appendChild(document.createElement("code"));
          }
          return;
        }

        if (codeBlock) {
          const code = codeBlock.querySelector("code");
          code.textContent += `${code.textContent ? "\n" : ""}${line}`;
          return;
        }

        const headingMatch = line.match(/^(#{1,3})\s+(.+)$/);
        const unorderedMatch = line.match(/^\s*[-*]\s+(.+)$/);
        const orderedMatch = line.match(/^\s*\d+\.\s+(.+)$/);
        const quoteMatch = line.match(/^\s*>\s?(.+)$/);

        if (headingMatch) {
          flushParagraph();
          closeList();
          const heading = document.createElement(`h${Math.min(headingMatch[1].length + 2, 5)}`);
          appendInlineMarkdown(heading, headingMatch[2]);
          container.appendChild(heading);
          return;
        }

        if (unorderedMatch || orderedMatch) {
          flushParagraph();
          const nextListType = unorderedMatch ? "ul" : "ol";
          if (!list || listType !== nextListType) {
            closeList();
            listType = nextListType;
            list = document.createElement(nextListType);
            container.appendChild(list);
          }

          const item = document.createElement("li");
          appendInlineMarkdown(item, (unorderedMatch || orderedMatch)[1]);
          list.appendChild(item);
          return;
        }

        if (quoteMatch) {
          flushParagraph();
          closeList();
          const quote = document.createElement("blockquote");
          appendInlineMarkdown(quote, quoteMatch[1]);
          container.appendChild(quote);
          return;
        }

        if (!line.trim()) {
          flushParagraph();
          closeList();
          return;
        }

        closeList();
        paragraphLines.push(line.trim());
      });

      flushParagraph();
      if (codeBlock) {
        container.appendChild(codeBlock);
      }
    };

    const setOpen = (open) => {
      panel.hidden = !open;
      toggle.setAttribute("aria-expanded", open ? "true" : "false");
      if (open && configured) {
        window.setTimeout(() => input.focus(), 80);
      }
    };

    const appendMessage = (role, text, extraClass = "") => {
      const message = document.createElement("article");
      message.className = `ss-ai-message is-${role}${extraClass ? ` ${extraClass}` : ""}`;

      if (role === "assistant") {
        const icon = document.createElement("span");
        icon.className = "ss-ai-message-icon";
        icon.setAttribute("aria-hidden", "true");
        icon.textContent = "✦";
        message.appendChild(icon);
      }

      const content = document.createElement("div");
      if (role === "assistant") {
        content.className = "ss-ai-markdown";
        renderMarkdown(content, text);
      } else {
        const paragraph = document.createElement("p");
        paragraph.textContent = text;
        content.appendChild(paragraph);
      }
      message.appendChild(content);
      messagesElement.appendChild(message);
      messagesElement.scrollTop = messagesElement.scrollHeight;
      return message;
    };

    const appendTyping = () => {
      const message = document.createElement("article");
      message.className = "ss-ai-message is-assistant";
      message.innerHTML = '<span class="ss-ai-message-icon" aria-hidden="true">✦</span><span class="ss-ai-typing" aria-label="AI priprema odgovor"><i></i><i></i><i></i></span>';
      messagesElement.appendChild(message);
      messagesElement.scrollTop = messagesElement.scrollHeight;
      return message;
    };

    const askAi = async (prompt) => {
      const text = (prompt || "").trim();
      if (!configured || !text || sendButton.disabled) {
        return;
      }

      setOpen(true);
      appendMessage("user", text);
      history.push({ role: "user", content: text });
      input.value = "";
      input.style.height = "";
      sendButton.disabled = true;
      const typing = appendTyping();

      try {
        const response = await fetch(assistant.dataset.endpoint, {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "RequestVerificationToken": assistant.dataset.token || "",
            "X-Requested-With": "XMLHttpRequest"
          },
          body: JSON.stringify({
            messages: history.slice(-12),
            pageTitle: document.body.dataset.pageTitle || document.title,
            pagePath: `${window.location.pathname}${window.location.search}`
          })
        });

        const payload = await response.json().catch(() => ({}));
        if (!response.ok) {
          throw new Error(payload.detail || payload.message || payload.title || "AI odgovor nije dostupan.");
        }

        const answer = payload.message || "AI nije vratio tekstualni odgovor.";
        history.push({ role: "assistant", content: answer });
        appendMessage("assistant", answer);
      } catch (error) {
        appendMessage("assistant", error.message || "Veza sa SideSeat AI servisom nije uspjela.", "is-error");
      } finally {
        typing.remove();
        sendButton.disabled = false;
        input.focus();
      }
    };

    toggle.addEventListener("click", () => setOpen(panel.hidden));
    close?.addEventListener("click", () => setOpen(false));

    form.addEventListener("submit", (event) => {
      event.preventDefault();
      askAi(input.value);
    });

    input.addEventListener("keydown", (event) => {
      if (event.key === "Enter" && !event.shiftKey) {
        event.preventDefault();
        form.requestSubmit();
      }
    });

    input.addEventListener("input", () => {
      input.style.height = "";
      input.style.height = `${Math.min(input.scrollHeight, 130)}px`;
    });

    assistant.querySelectorAll("[data-ss-ai-suggestion]").forEach((button) => {
      button.addEventListener("click", () => askAi(button.dataset.ssAiSuggestion || ""));
    });

    document.querySelectorAll("[data-ss-ai-launch-form]").forEach((launchForm) => {
      launchForm.addEventListener("submit", (event) => {
        event.preventDefault();
        const launchInput = launchForm.querySelector("input");
        const prompt = launchInput?.value.trim() || "Objasni mi kako SideSeat može pomoći s mojom sljedećom vožnjom.";
        if (launchInput) {
          launchInput.value = "";
        }
        askAi(prompt);
      });
    });

    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape" && !panel.hidden) {
        setOpen(false);
      }
    });
  };

  const initializeEnhancedUi = () => {
    initializeDateFields();
    initializeAutocompleteFields();
    initializeTableCards();
    initializeAjaxLists();
    initializeAjaxForms();
    initializePerformativeUi();
  };

  initializeAiAssistant();
  initializeEnhancedUi();
})();
