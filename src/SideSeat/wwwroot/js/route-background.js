(() => {
  "use strict";

  if (!window.L) {
    return;
  }

  const root = document.querySelector("[data-ss-route-bg]");
  if (!root) {
    return;
  }
  const mapEl = root.querySelector("[data-ss-route-bg-map]");
  const dataEl = root.querySelector("[data-ss-route-bg-data]");
  if (!mapEl) {
    return;
  }

  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const body = document.body;
  const tileUrl = body.dataset.mapTileUrl || "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
  const attribution = body.dataset.mapAttribution ||
    "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors";
  const routeUrl = body.dataset.mapRouteUrl || "/api/maps/route";

  const CAR_SPEED_MPS = 1000;      // auto se mice najvise 1 km/s po stvarnoj duljini rute
  const MIN_ROUTE_SECONDS = 5;     // i jako kratku rutu auto prelazi najmanje 5 sekundi
  const RESPAWN_GAP = 2;           // 2 s nakon dolaska na kraj nastaje novi auto na istoj ruti
  const FOCUS_SECONDS = 5;         // 5 s kamera ostaje na autu (zoom in)
  const TRAVEL_SECONDS = 4;        // polagani prijelaz na sljedeci auto (zoom out pa zoom in)
  const CAMERA_FOLLOW = 0.08;      // glatko pracenje sredista kamere
  const ZOOM_IN = 10;              // dosta blizu na autu
  const ZOOM_OUT = 7;              // odzumirano (pregled Hrvatske) pri prelasku
  const COLORS = ["#25c97a", "#2f7fd0", "#e58a2a", "#c2496b"];
  const CROATIA = L.latLngBounds([42.3, 13.4], [46.6, 19.5]);

  const isFinite = Number.isFinite;
  const valid = (route) =>
    route &&
    isFinite(Number(route.startLat)) && isFinite(Number(route.startLng)) &&
    isFinite(Number(route.endLat)) && isFinite(Number(route.endLng));

  let routes = [];
  try {
    routes = JSON.parse(dataEl?.textContent || "[]");
  } catch {
    routes = [];
  }
  if (!Array.isArray(routes)) {
    routes = [];
  }
  routes = routes.filter(valid).map((route) => ({
    startLat: Number(route.startLat),
    startLng: Number(route.startLng),
    endLat: Number(route.endLat),
    endLng: Number(route.endLng)
  }));
  if (routes.length === 0) {
    routes = [
      { startLat: 45.815, startLng: 15.9819, endLat: 43.5081, endLng: 16.4402 },
      { startLat: 45.815, startLng: 15.9819, endLat: 45.3271, endLng: 14.4422 },
      { startLat: 45.815, startLng: 15.9819, endLat: 45.555, endLng: 18.6955 },
      { startLat: 43.5081, startLng: 16.4402, endLat: 42.6507, endLng: 18.0944 },
      { startLat: 45.3271, startLng: 14.4422, endLat: 44.1194, endLng: 15.2314 },
      { startLat: 45.815, startLng: 15.9819, endLat: 46.3057, endLng: 16.3366 }
    ];
  }
  routes = routes.slice(0, 7);

  const map = L.map(mapEl, {
    zoomControl: false,
    attributionControl: false,
    dragging: false,
    scrollWheelZoom: false,
    doubleClickZoom: false,
    boxZoom: false,
    keyboard: false,
    touchZoom: false,
    tap: false,
    inertia: false,
    zoomSnap: 0,
    fadeAnimation: true
  });
  L.tileLayer(tileUrl, {
    attribution,
    maxZoom: 19,
    detectRetina: true,
    keepBuffer: 8,            // zadrzi vise pločica oko vidnog polja da nema crnog ekrana
    updateWhenZooming: false  // ne osvjezavaj pločice tijekom zumiranja, tek kad se smiri
  }).addTo(map);
  map.fitBounds(CROATIA);

  const CAR_HTML =
    "<div class=\"ss-bg-car-rot\">" +
    "<svg viewBox=\"-22 -14 92 28\" width=\"58\" height=\"22\">" +
    "<path class=\"ss-bg-car-beam\" d=\"M16 -6 L70 -22 L70 22 L16 6 Z\"/>" +
    "<rect class=\"ss-bg-car-wheel\" x=\"-14\" y=\"-12\" width=\"9\" height=\"5\" rx=\"2\"/>" +
    "<rect class=\"ss-bg-car-wheel\" x=\"-14\" y=\"7\" width=\"9\" height=\"5\" rx=\"2\"/>" +
    "<rect class=\"ss-bg-car-wheel\" x=\"6\" y=\"-12\" width=\"9\" height=\"5\" rx=\"2\"/>" +
    "<rect class=\"ss-bg-car-wheel\" x=\"6\" y=\"7\" width=\"9\" height=\"5\" rx=\"2\"/>" +
    "<rect class=\"ss-bg-car-body\" x=\"-18\" y=\"-9\" width=\"36\" height=\"18\" rx=\"7\"/>" +
    "<path class=\"ss-bg-car-glass\" d=\"M-2 -7 L11 -5 L11 5 L-2 7 Z\"/>" +
    "<circle class=\"ss-bg-car-lamp\" cx=\"16\" cy=\"-5\" r=\"1.8\"/>" +
    "<circle class=\"ss-bg-car-lamp\" cx=\"16\" cy=\"5\" r=\"1.8\"/>" +
    "</svg></div>";

  const carIcon = (index) => L.divIcon({
    className: `ss-bg-car-icon ss-bg-car-${index % COLORS.length}`,
    html: CAR_HTML,
    iconSize: [58, 22],
    iconAnchor: [14, 11]
  });

  const computeCumulative = (entry) => {
    const cum = [0];
    let total = 0;
    for (let i = 1; i < entry.latlngs.length; i += 1) {
      total += entry.latlngs[i - 1].distanceTo(entry.latlngs[i]);
      cum.push(total);
    }
    entry.cum = cum;
    entry.total = total;
  };

  const positionAt = (entry, fraction) => {
    if (entry.total <= 0 || entry.latlngs.length < 2) {
      return entry.latlngs[0];
    }
    const target = Math.min(entry.total, Math.max(0, fraction * entry.total));
    const cum = entry.cum;
    let i = 1;
    while (i < cum.length - 1 && cum[i] < target) {
      i += 1;
    }
    const a = entry.latlngs[i - 1];
    const b = entry.latlngs[i];
    const segLength = (cum[i] - cum[i - 1]) || 1;
    const localT = (target - cum[i - 1]) / segLength;
    return L.latLng(a.lat + (b.lat - a.lat) * localT, a.lng + (b.lng - a.lng) * localT);
  };

  const fetchExact = (route) => {
    const query = new URLSearchParams({
      startLat: route.startLat.toFixed(6),
      startLng: route.startLng.toFixed(6),
      endLat: route.endLat.toFixed(6),
      endLng: route.endLng.toFixed(6)
    });
    const controller = new AbortController();
    const timer = window.setTimeout(() => controller.abort(), 5000);
    return fetch(`${routeUrl}?${query}`, {
      headers: { Accept: "application/json" },
      credentials: "same-origin",
      cache: "force-cache",
      signal: controller.signal
    })
      .then((response) => (response.ok ? response.json() : null))
      .then((payload) => {
        if (!payload || !Array.isArray(payload.points)) {
          return null;
        }
        const points = payload.points
          .filter((point) => Array.isArray(point) && isFinite(point[0]) && isFinite(point[1]))
          .map((point) => L.latLng(point[0], point[1]));
        return points.length >= 2 ? points : null;
      })
      .catch(() => null)
      .finally(() => window.clearTimeout(timer));
  };

  const MAX_ROUTE_ATTEMPTS = 4;
  const loadExactRoute = (entry, route, attempt, delay) => {
    window.setTimeout(() => {
      if (!root.isConnected) {
        return;
      }
      fetchExact(route).then((points) => {
        if (points) {
          entry.line.setLatLngs(points);
          if (entry.halo) {
            entry.halo.setLatLngs(points);
          }
          entry.latlngs = points;
          computeCumulative(entry);
          return;
        }
        if (attempt + 1 < MAX_ROUTE_ATTEMPTS && root.isConnected) {
          loadExactRoute(entry, route, attempt + 1, 1200 * (attempt + 1));
        }
      });
    }, delay);
  };

  const animated = [];

  routes.forEach((route, index) => {
    const straight = [
      L.latLng(route.startLat, route.startLng),
      L.latLng(route.endLat, route.endLng)
    ];
    const color = COLORS[index % COLORS.length];

    const halo = L.polyline(straight, {
      color: "#ffffff", weight: 8, opacity: 0.5,
      lineCap: "round", lineJoin: "round", interactive: false
    }).addTo(map);
    const line = L.polyline(straight, {
      color, weight: 4, opacity: 0.95,
      lineCap: "round", lineJoin: "round", interactive: false,
      className: "ss-bg-route-line"
    }).addTo(map);
    L.circleMarker(straight[1], {
      radius: 4, color: "#ffffff", weight: 2, fillColor: color, fillOpacity: 1, interactive: false
    }).addTo(map);

    const marker = L.marker(straight[0], {
      icon: carIcon(index), interactive: false, keyboard: false, zIndexOffset: 1000, opacity: 0
    }).addTo(map);

    const entry = {
      latlngs: straight,
      cum: [0, 1],
      total: 1,
      line,
      halo,
      marker,
      rotEl: null,
      angle: null,
      offset: index * 1.7,
      visible: false,
      currentPos: straight[index % 2 !== 0 ? 1 : 0],
      reverse: index % 2 !== 0
    };
    computeCumulative(entry);
    animated.push(entry);

    if (!reduceMotion.matches) {
      // Staggerano i s ponovnim pokusajima da OSRM ne odbije paralelne zahtjeve.
      loadExactRoute(entry, route, 0, index * 450);
    }
  });

  const placeCar = (entry, progress) => {
    const fraction = entry.reverse ? 1 - progress : progress;
    const ahead = Math.min(1, Math.max(0, fraction + (entry.reverse ? -0.012 : 0.012)));
    const pos = positionAt(entry, fraction);
    const next = positionAt(entry, ahead);
    entry.currentPos = pos;
    entry.marker.setLatLng(pos);
    if (!entry.rotEl) {
      const node = entry.marker.getElement();
      entry.rotEl = node ? node.querySelector(".ss-bg-car-rot") : null;
    }
    if (entry.rotEl) {
      const p1 = map.latLngToContainerPoint(pos);
      const p2 = map.latLngToContainerPoint(next);
      let angle = Math.atan2(p2.y - p1.y, p2.x - p1.x) * 180 / Math.PI;
      // Drzi kut neprekidnim da CSS prijelaz uvijek skrene kracim putem (a ne 360deg na ±180).
      if (entry.angle != null) {
        let diff = ((angle - entry.angle + 540) % 360) - 180;
        angle = entry.angle + diff;
      }
      entry.angle = angle;
      entry.rotEl.style.transform = `rotate(${angle.toFixed(1)}deg)`;
    }
  };

  map.whenReady(() => window.requestAnimationFrame(() => map.invalidateSize(false)));

  if (reduceMotion.matches) {
    map.fitBounds(CROATIA);
    window.requestAnimationFrame(() => animated.forEach((entry) => {
      entry.marker.setOpacity(1);
      placeCar(entry, 0.5);
    }));
    return;
  }

  const lerp = (a, b, t) => a + (b - a) * t;
  const easeInOut = (t) => (t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2);
  const carStart = (entry) =>
    entry.reverse ? entry.latlngs[entry.latlngs.length - 1] : entry.latlngs[0];

  let clock = 0;
  let lastTime = 0;
  let frame = 0;
  let camPos = null;
  let camZoom = ZOOM_IN;

  const step = (time) => {
    if (!root.isConnected) {
      window.cancelAnimationFrame(frame);
      return;
    }
    const delta = lastTime ? Math.min((time - lastTime) / 1000, 0.05) : 0;
    lastTime = time;
    clock += delta;

    // Svaki auto vozi svoju rutu, dodje na kraj, 2 s pauze, pa novi na istoj ruti.
    animated.forEach((entry) => {
      const duration = entry.total > 0
        ? Math.max(entry.total / CAR_SPEED_MPS, MIN_ROUTE_SECONDS)
        : MIN_ROUTE_SECONDS;
      const period = duration + RESPAWN_GAP;
      const local = (((clock + entry.offset) % period) + period) % period;
      const driving = local < duration;
      if (driving !== entry.visible) {
        entry.visible = driving;
        entry.marker.setOpacity(driving ? 1 : 0);
      }
      if (driving) {
        placeCar(entry, local / duration);
      } else {
        entry.currentPos = carStart(entry);
      }
    });

    // Kamera: zoom in i prati auto, pa zoom out dok putuje na sljedeci auto.
    const count = animated.length;
    const slot = FOCUS_SECONDS + TRAVEL_SECONDS;
    const tour = (((clock % (slot * count)) + slot * count) % (slot * count));
    const index = Math.floor(tour / slot) % count;
    const slotT = tour - Math.floor(tour / slot) * slot;
    const current = animated[index];

    let targetCenter;
    let targetZoom;
    if (slotT < FOCUS_SECONDS) {
      targetCenter = current.currentPos;
      targetZoom = ZOOM_IN;
    } else {
      const linear = (slotT - FOCUS_SECONDS) / TRAVEL_SECONDS;
      const next = animated[(index + 1) % count];
      const from = current.currentPos;
      const to = next.currentPos;
      const moved = easeInOut(linear);
      targetCenter = L.latLng(lerp(from.lat, to.lat, moved), lerp(from.lng, to.lng, moved));
      targetZoom = ZOOM_IN - (ZOOM_IN - ZOOM_OUT) * Math.sin(linear * Math.PI);
    }

    camPos = camPos
      ? L.latLng(
          lerp(camPos.lat, targetCenter.lat, CAMERA_FOLLOW),
          lerp(camPos.lng, targetCenter.lng, CAMERA_FOLLOW))
      : L.latLng(targetCenter.lat, targetCenter.lng);
    camZoom = lerp(camZoom, targetZoom, CAMERA_FOLLOW);
    map.setView(camPos, camZoom, { animate: false });

    frame = window.requestAnimationFrame(step);
  };

  const start = () => {
    lastTime = 0;
    frame = window.requestAnimationFrame(step);
  };
  const stop = () => window.cancelAnimationFrame(frame);

  document.addEventListener("visibilitychange", () => {
    if (document.hidden) {
      stop();
    } else {
      start();
    }
  });

  start();
})();
