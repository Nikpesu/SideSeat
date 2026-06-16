(() => {
  "use strict";

  if (!window.L) {
    return;
  }

  const body = document.body;
  const tileUrl = body.dataset.mapTileUrl || "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
  const attribution = body.dataset.mapAttribution ||
    "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors";
  const routeUrl = body.dataset.mapRouteUrl || "/api/maps/route";
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const hoverCapable = window.matchMedia("(hover: hover) and (pointer: fine)");
  const mapStates = new WeakMap();
  const routeRequests = new Map();
  const persistentRoots = new Set();
  const croatiaBounds = L.latLngBounds([42.2, 13.2], [46.7, 19.6]);
  let previewTimer = 0;
  let activePreviewSource = null;

  const closest = (target, selector) =>
    target instanceof Element ? target.closest(selector) : null;

  const numberFrom = (value) => {
    if (value === undefined || value === null || value === "") {
      return null;
    }
    const parsed = Number.parseFloat(value);
    return Number.isFinite(parsed) ? parsed : null;
  };

  const routeFrom = (element) => ({
    startName: element.dataset.routeStartName || "",
    startLat: numberFrom(element.dataset.routeStartLat),
    startLng: numberFrom(element.dataset.routeStartLng),
    endName: element.dataset.routeEndName || "",
    endLat: numberFrom(element.dataset.routeEndLat),
    endLng: numberFrom(element.dataset.routeEndLng)
  });

  const routeAvailable = (route) =>
    route.startLat !== null &&
    route.startLng !== null &&
    route.endLat !== null &&
    route.endLng !== null;

  const routeKey = (route) =>
    `${route.startLat?.toFixed(6)},${route.startLng?.toFixed(6)}:` +
    `${route.endLat?.toFixed(6)},${route.endLng?.toFixed(6)}`;

  const routeSeed = (route) => {
    const text = `${route.startName}|${route.endName}`;
    let hash = 0;
    for (let index = 0; index < text.length; index += 1) {
      hash = ((hash << 5) - hash + text.charCodeAt(index)) | 0;
    }
    return hash;
  };

  const approximateRoutePoints = (route) => {
    const start = { lat: route.startLat, lng: route.startLng };
    const end = { lat: route.endLat, lng: route.endLng };
    const deltaLat = end.lat - start.lat;
    const deltaLng = end.lng - start.lng;
    const distance = Math.hypot(deltaLat, deltaLng);
    const bend = Math.min(Math.max(distance * 0.16, 0.08), 0.48);
    const length = distance || 1;
    const perpendicular = {
      lat: -deltaLng / length,
      lng: deltaLat / length
    };
    const direction = routeSeed(route) % 2 === 0 ? 1 : -1;
    const controlOne = {
      lat: start.lat + deltaLat * 0.28 + perpendicular.lat * bend * direction,
      lng: start.lng + deltaLng * 0.28 + perpendicular.lng * bend * direction
    };
    const controlTwo = {
      lat: start.lat + deltaLat * 0.72 - perpendicular.lat * bend * direction * 0.55,
      lng: start.lng + deltaLng * 0.72 - perpendicular.lng * bend * direction * 0.55
    };
    const points = [];
    for (let index = 0; index <= 36; index += 1) {
      const t = index / 36;
      const inverse = 1 - t;
      points.push([
        inverse ** 3 * start.lat +
          3 * inverse ** 2 * t * controlOne.lat +
          3 * inverse * t ** 2 * controlTwo.lat +
          t ** 3 * end.lat,
        inverse ** 3 * start.lng +
          3 * inverse ** 2 * t * controlOne.lng +
          3 * inverse * t ** 2 * controlTwo.lng +
          t ** 3 * end.lng
      ]);
    }
    return points;
  };

  const exactRoutePoints = (route) => {
    const key = routeKey(route);
    const cached = routeRequests.get(key);
    if (cached && cached.expiresAt > Date.now()) {
      return cached.promise;
    }

    const controller = new AbortController();
    const timeout = window.setTimeout(() => controller.abort(), 950);
    const query = new URLSearchParams({
      startLat: route.startLat.toFixed(6),
      startLng: route.startLng.toFixed(6),
      endLat: route.endLat.toFixed(6),
      endLng: route.endLng.toFixed(6)
    });
    const entry = {
      expiresAt: Date.now() + 30000,
      promise: null
    };
    entry.promise = fetch(`${routeUrl}?${query}`, {
      headers: { Accept: "application/json" },
      credentials: "same-origin",
      cache: "force-cache",
      signal: controller.signal
    })
      .then(async (response) => {
        if (!response.ok || response.status === 204) {
          return null;
        }
        const payload = await response.json();
        if (!Array.isArray(payload.points)) {
          return null;
        }
        const points = payload.points
          .filter((point) =>
            Array.isArray(point) &&
            point.length >= 2 &&
            Number.isFinite(point[0]) &&
            Number.isFinite(point[1]))
          .map((point) => [point[0], point[1]]);
        return points.length >= 2 ? points : null;
      })
      .catch(() => null)
      .then((points) => {
        entry.expiresAt = points ? Number.MAX_SAFE_INTEGER : Date.now() + 30000;
        return points;
      })
      .finally(() => window.clearTimeout(timeout));
    routeRequests.set(key, entry);
    return entry.promise;
  };

  const createState = (root, interactive) => {
    const canvas = root.querySelector("[data-ss-route-map-canvas]");
    if (!canvas) {
      return null;
    }

    const map = L.map(canvas, {
      attributionControl: true,
      zoomControl: interactive,
      dragging: interactive,
      scrollWheelZoom: interactive,
      doubleClickZoom: interactive,
      boxZoom: interactive,
      keyboard: interactive,
      tap: interactive
    });
    L.tileLayer(tileUrl, {
      attribution,
      maxZoom: 19
    }).addTo(map);
    const state = {
      map,
      layer: L.layerGroup().addTo(map),
      route: null,
      routeKey: null,
      points: null,
      quality: null,
      generation: 0,
      interactive
    };
    mapStates.set(root, state);
    return state;
  };

  const colors = () => {
    const dark = document.documentElement.dataset.theme === "dark";
    return {
      route: "#25d879",
      halo: dark ? "#07130c" : "#ffffff",
      start: "#ef4444",
      end: "#16c96b"
    };
  };

  const animateLine = (line) => {
    if (reducedMotion.matches || !line._path || !line._map || !line._path.isConnected) {
      return;
    }
    const path = line._path;
    const length = path.getTotalLength();
    path.style.transition = "none";
    path.style.strokeDasharray = `${length} ${length}`;
    path.style.strokeDashoffset = `${length}`;
    path.getBoundingClientRect();
    path.style.transition = "stroke-dashoffset 1.8s cubic-bezier(.2,.75,.25,1)";
    path.style.strokeDashoffset = "0";
  };

  const drawRouteGeometry = (
    root,
    state,
    route,
    points,
    interactive,
    quality) => {
    state.layer.clearLayers();
    state.points = points;
    state.quality = quality;
    root.dataset.routeQuality = quality;
    const palette = colors();
    L.polyline(points, {
      color: palette.halo,
      weight: 9,
      opacity: 0.78,
      interactive: false,
      lineCap: "round",
      lineJoin: "round"
    }).addTo(state.layer);
    const line = L.polyline(points, {
      color: palette.route,
      weight: 4,
      opacity: 1,
      interactive: false,
      lineCap: "round",
      lineJoin: "round",
      className: "ss-route-map-line"
    }).addTo(state.layer);
    L.circleMarker([route.startLat, route.startLng], {
      radius: 8,
      color: palette.halo,
      weight: 3,
      fillColor: palette.start,
      fillOpacity: 1
    }).bindTooltip(route.startName || "Polazište").addTo(state.layer);
    L.circleMarker([route.endLat, route.endLng], {
      radius: 8,
      color: palette.halo,
      weight: 3,
      fillColor: palette.end,
      fillOpacity: 1
    }).bindTooltip(route.endName || "Odredište").addTo(state.layer);
    state.map.fitBounds(L.latLngBounds(points), {
      padding: interactive ? [42, 42] : [24, 24],
      maxZoom: interactive ? 10 : 9,
      animate: !reducedMotion.matches
    });
    window.requestAnimationFrame(() => {
      if (!root.isConnected || !line._map) {
        return;
      }
      state.map.invalidateSize(false);
      animateLine(line);
    });
  };

  const renderRoute = (root, route, interactive = true) => {
    const fallback = root.querySelector("[data-ss-route-map-fallback]");
    const canvas = root.querySelector("[data-ss-route-map-canvas]");
    const label = root.querySelector("[data-ss-route-map-label], [data-ss-route-preview-label]");
    if (label) {
      label.textContent = `${route.startName || "Polazište"} → ${route.endName || "Odredište"}`;
    }

    let state = mapStates.get(root);
    if (!state) {
      state = createState(root, interactive);
    }
    if (!state || !canvas || !fallback) {
      return;
    }

    const key = routeAvailable(route) ? routeKey(route) : null;
    if (
      key &&
      state.routeKey === key &&
      state.route &&
      state.points &&
      state.layer.getLayers().length > 0
    ) {
      return;
    }

    const generation = ++state.generation;
    state.route = route;
    state.routeKey = key;
    state.points = null;
    state.quality = null;
    state.layer.clearLayers();
    if (!routeAvailable(route)) {
      canvas.hidden = true;
      fallback.hidden = false;
      return;
    }

    canvas.hidden = false;
    fallback.hidden = true;
    drawRouteGeometry(
      root,
      state,
      route,
      approximateRoutePoints(route),
      interactive,
      "approximate");
    exactRoutePoints(route).then((points) => {
      if (
        !points ||
        !root.isConnected ||
        state.generation !== generation ||
        state.routeKey !== key
      ) {
        return;
      }
      drawRouteGeometry(root, state, route, points, interactive, "exact");
    });
  };

  const initializePersistentMaps = (scope = document) => {
    scope.querySelectorAll("[data-ss-route-map]").forEach((root) => {
      persistentRoots.add(root);
      if (!mapStates.has(root)) {
        renderRoute(root, routeFrom(root), true);
      }
    });
  };

  const updateHomeMap = (source) => {
    const schedule = source.closest("[data-ss-home-schedule]");
    const mapRoot = schedule?.querySelector("[data-ss-home-route-map] [data-ss-route-map]");
    if (!mapRoot) {
      return;
    }
    schedule.querySelectorAll("[data-ss-home-route-source].is-route-active")
      .forEach((item) => item.classList.remove("is-route-active"));
    const carouselKey = source.dataset.ssRouteCarouselKey;
    if (carouselKey) {
      schedule.querySelectorAll(
        `[data-ss-home-route-source][data-ss-route-carousel-key="${carouselKey}"]`)
        .forEach((item) => item.classList.add("is-route-active"));
    } else {
      source.classList.add("is-route-active");
    }
    renderRoute(mapRoot, routeFrom(source), true);
  };

  const showCroatiaOverview = (mapRoot) => {
    const state = mapStates.get(mapRoot) || createState(mapRoot, true);
    if (!state) {
      return;
    }
    state.generation += 1;
    state.route = null;
    state.routeKey = null;
    state.points = null;
    state.quality = null;
    state.layer.clearLayers();
    const fallback = mapRoot.querySelector("[data-ss-route-map-fallback]");
    const canvas = mapRoot.querySelector("[data-ss-route-map-canvas]");
    if (fallback) {
      fallback.hidden = true;
    }
    if (canvas) {
      canvas.hidden = false;
    }
    const label = mapRoot.querySelector("[data-ss-route-map-label]");
    if (label) {
      label.textContent = "Pregled Hrvatske";
    }
    state.map.fitBounds(croatiaBounds, { padding: [20, 20] });
    window.requestAnimationFrame(() => state.map.invalidateSize(false));
  };

  const selectFirstHomeRoute = (scope = document) => {
    scope.querySelectorAll("[data-ss-home-schedule]").forEach((schedule) => {
      const source = schedule.querySelector(
        "[data-ss-route-carousel-primary] [data-ss-home-route-source]") ||
        schedule.querySelector("[data-ss-home-route-source]");
      const mapRoot = schedule.querySelector("[data-ss-home-route-map] [data-ss-route-map]");
      if (source) {
        updateHomeMap(source);
      } else if (mapRoot) {
        showCroatiaOverview(mapRoot);
      }
    });
  };

  const makeCarouselClone = (source) => {
    const clone = source.cloneNode(true);
    clone.dataset.ssRouteCarouselClone = "true";
    delete clone.dataset.ssRouteCarouselPrimary;
    clone.setAttribute("aria-hidden", "true");
    if (clone.hasAttribute("tabindex")) {
      clone.setAttribute("tabindex", "-1");
    }
    clone.querySelectorAll("[data-ss-route-carousel-item]")
      .forEach((item) => {
        item.dataset.ssRouteCarouselClone = "true";
        item.setAttribute("aria-hidden", "true");
      });
    clone.querySelectorAll("a, button, input, select, textarea, [tabindex]")
      .forEach((item) => item.setAttribute("tabindex", "-1"));
    return clone;
  };

  const initializeRouteCarousels = (scope = document) => {
    scope.querySelectorAll("[data-ss-route-carousel]").forEach((carousel) => {
      if (carousel.dataset.ssRouteCarouselReady === "true") {
        return;
      }

      const viewport = carousel.querySelector("[data-ss-route-carousel-viewport]");
      const track = carousel.querySelector("[data-ss-route-carousel-track]");
      const items = track
        ? Array.from(track.children).filter((item) =>
          item.matches("[data-ss-route-carousel-item]"))
        : [];
      if (!viewport || !track || items.length === 0) {
        return;
      }

      carousel.dataset.ssRouteCarouselReady = "true";
      const primaryGroup = document.createElement("div");
      primaryGroup.className = "ss-route-carousel-group";
      primaryGroup.dataset.ssRouteCarouselPrimary = "true";
      items.forEach((item, index) => {
        item.dataset.ssRouteCarouselKey = String(index + 1);
        primaryGroup.append(item);
      });
      track.append(primaryGroup);

      const fillGroup = () => {
        let appended = items.length;
        while (
          primaryGroup.scrollWidth < viewport.clientWidth * 1.25 &&
          appended < 20
        ) {
          items.forEach((item) => {
            if (appended >= 20) {
              return;
            }
            primaryGroup.append(makeCarouselClone(item));
            appended += 1;
          });
        }
      };
      fillGroup();

      const beforeGroup = makeCarouselClone(primaryGroup);
      const afterGroup = makeCarouselClone(primaryGroup);
      track.prepend(beforeGroup);
      track.append(afterGroup);

      let loopWidth = primaryGroup.getBoundingClientRect().width;
      let paused = false;
      let resumeTimer = 0;
      let lastFrame = 0;
      let frameId = 0;

      const recalculate = () => {
        const nextWidth = primaryGroup.getBoundingClientRect().width;
        if (nextWidth <= 0) {
          return;
        }
        if (loopWidth > 0 && Math.abs(nextWidth - loopWidth) > 1) {
          const offset = viewport.scrollLeft - loopWidth;
          viewport.scrollLeft = nextWidth + Math.max(0, offset);
        }
        loopWidth = nextWidth;
      };

      const normalize = () => {
        recalculate();
        if (loopWidth <= 0) {
          return;
        }
        if (viewport.scrollLeft >= loopWidth * 2) {
          viewport.scrollLeft -= loopWidth;
        } else if (viewport.scrollLeft <= 0) {
          viewport.scrollLeft += loopWidth;
        }
      };

      const pause = () => {
        paused = true;
        window.clearTimeout(resumeTimer);
      };

      const resume = (delay = 0) => {
        window.clearTimeout(resumeTimer);
        resumeTimer = window.setTimeout(() => {
          paused = false;
          lastFrame = 0;
        }, delay);
      };

      const scrollByCard = (direction) => {
        pause();
        const card = primaryGroup.querySelector("[data-ss-route-carousel-item]");
        const groupStyle = window.getComputedStyle(primaryGroup);
        const gap = Number.parseFloat(groupStyle.columnGap || groupStyle.gap) || 0;
        const distance = (card?.getBoundingClientRect().width || 320) + gap;
        viewport.scrollBy({
          left: direction * distance,
          behavior: reducedMotion.matches ? "auto" : "smooth"
        });
        resume(1800);
      };

      const animate = (time) => {
        if (!carousel.isConnected) {
          window.cancelAnimationFrame(frameId);
          return;
        }
        if (!paused && !reducedMotion.matches) {
          if (lastFrame > 0) {
            viewport.scrollLeft += Math.min((time - lastFrame) * 0.032, 2.2);
            normalize();
          }
        }
        lastFrame = time;
        frameId = window.requestAnimationFrame(animate);
      };

      carousel.querySelector("[data-ss-route-carousel-prev]")
        ?.addEventListener("click", () => scrollByCard(-1));
      carousel.querySelector("[data-ss-route-carousel-next]")
        ?.addEventListener("click", () => scrollByCard(1));
      viewport.addEventListener("scroll", normalize, { passive: true });
      viewport.addEventListener("keydown", (event) => {
        if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
          event.preventDefault();
          scrollByCard(event.key === "ArrowLeft" ? -1 : 1);
        }
      });
      carousel.addEventListener("pointerenter", pause);
      carousel.addEventListener("pointerleave", () => resume(500));
      carousel.addEventListener("focusin", pause);
      carousel.addEventListener("focusout", (event) => {
        if (!carousel.contains(event.relatedTarget)) {
          resume(500);
        }
      });
      carousel.addEventListener("touchstart", pause, { passive: true });
      carousel.addEventListener("touchend", () => resume(1400), { passive: true });
      carousel.addEventListener("wheel", () => {
        pause();
        resume(1400);
      }, { passive: true });

      window.requestAnimationFrame(() => {
        recalculate();
        viewport.scrollLeft = loopWidth;
        frameId = window.requestAnimationFrame(animate);
      });
    });
  };

  const preview = document.querySelector("[data-ss-route-preview]");

  const placePreview = (source) => {
    if (!preview) {
      return;
    }
    const rect = source.getBoundingClientRect();
    const width = Math.min(360, window.innerWidth - 24);
    const height = 240;
    const left = Math.min(
      Math.max(12, rect.left + rect.width / 2 - width / 2),
      window.innerWidth - width - 12);
    const preferredTop = rect.bottom + 12;
    const top = preferredTop + height <= window.innerHeight - 12
      ? preferredTop
      : Math.max(12, rect.top - height - 12);
    preview.style.width = `${width}px`;
    preview.style.left = `${left}px`;
    preview.style.top = `${top}px`;
  };

  const openPreview = (source, mobile = false) => {
    if (!preview) {
      return;
    }
    window.clearTimeout(previewTimer);
    activePreviewSource = source;
    preview.hidden = false;
    preview.classList.toggle("is-mobile-open", mobile);
    placePreview(source);
    renderRoute(preview, routeFrom(source), false);
  };

  const closePreview = () => {
    if (!preview) {
      return;
    }
    window.clearTimeout(previewTimer);
    preview.hidden = true;
    preview.classList.remove("is-mobile-open");
    activePreviewSource = null;
  };

  document.addEventListener("pointerover", (event) => {
    const homeSource = closest(event.target, "[data-ss-home-route-source]");
    const previousHomeSource = closest(event.relatedTarget, "[data-ss-home-route-source]");
    if (homeSource && hoverCapable.matches && previousHomeSource !== homeSource) {
      updateHomeMap(homeSource);
    }

    const source = closest(event.target, "[data-ss-route-preview-source]");
    const previousSource = closest(event.relatedTarget, "[data-ss-route-preview-source]");
    if (!source || !hoverCapable.matches || previousSource === source) {
      return;
    }
    previewTimer = window.setTimeout(() => openPreview(source, false), 140);
  });

  document.addEventListener("pointerout", (event) => {
    const source = closest(event.target, "[data-ss-route-preview-source]");
    if (!source || !hoverCapable.matches || source.contains(event.relatedTarget)) {
      return;
    }
    closePreview();
  });

  document.addEventListener("focusin", (event) => {
    const homeSource = closest(event.target, "[data-ss-home-route-source]");
    if (homeSource) {
      updateHomeMap(homeSource);
    }
    const source = closest(event.target, "[data-ss-route-preview-source]");
    if (source && hoverCapable.matches) {
      previewTimer = window.setTimeout(() => openPreview(source, false), 100);
    }
  });

  document.addEventListener("focusout", (event) => {
    const source = closest(event.target, "[data-ss-route-preview-source]");
    if (source && hoverCapable.matches && !source.contains(event.relatedTarget)) {
      closePreview();
    }
  });

  document.addEventListener("click", (event) => {
    if (closest(event.target, "[data-ss-route-preview-close]")) {
      closePreview();
      return;
    }
    const button = closest(event.target, "[data-ss-route-preview-button]");
    if (button) {
      event.preventDefault();
      event.stopPropagation();
      const source = closest(
        button,
        "[data-ss-route-preview-source], [data-ss-home-route-source]");
      if (source) {
        openPreview(source, true);
      }
      return;
    }
    const homeSource = closest(event.target, "[data-ss-home-route-source]");
    if (homeSource) {
      updateHomeMap(homeSource);
    }
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && activePreviewSource) {
      closePreview();
    }
  });

  document.addEventListener("ss:content-updated", (event) => {
    const scope = event.detail?.target instanceof Element ? event.detail.target : document;
    initializePersistentMaps(scope);
    initializeRouteCarousels(scope);
    selectFirstHomeRoute(document);
  });

  new MutationObserver(() => {
    persistentRoots.forEach((root) => {
      if (!root.isConnected) {
        persistentRoots.delete(root);
        return;
      }
      const state = mapStates.get(root);
      if (state?.route && state.points) {
        drawRouteGeometry(
          root,
          state,
          state.route,
          state.points,
          state.interactive,
          state.quality || "approximate");
      }
    });
    if (preview && !preview.hidden) {
      const state = mapStates.get(preview);
      if (state?.route && state.points) {
        drawRouteGeometry(
          preview,
          state,
          state.route,
          state.points,
          false,
          state.quality || "approximate");
      }
    }
  }).observe(document.documentElement, {
    attributes: true,
    attributeFilter: ["data-theme"]
  });

  initializePersistentMaps();
  initializeRouteCarousels();
  selectFirstHomeRoute();
})();
