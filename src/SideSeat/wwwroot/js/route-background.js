(() => {
  "use strict";

  const root = document.querySelector("[data-ss-route-bg]");
  if (!root) {
    return;
  }

  const svg = root.querySelector(".ss-route-bg-svg");
  const gridLayer = root.querySelector("[data-ss-bg-grid]");
  const routeLayer = root.querySelector("[data-ss-bg-routes]");
  const nodeLayer = root.querySelector("[data-ss-bg-nodes]");
  const carLayer = root.querySelector("[data-ss-bg-cars]");
  const template = root.querySelector("#ssBgCarTemplate");
  const dataEl = root.querySelector("[data-ss-route-bg-data]");
  if (!svg || !routeLayer || !nodeLayer || !carLayer || !template) {
    return;
  }

  const NS = "http://www.w3.org/2000/svg";
  const VB_W = 1440;
  const VB_H = 900;
  const PAD_X = 150;
  const PAD_Y = 120;
  const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  const routeUrl = document.body.dataset.mapRouteUrl || "/api/maps/route";

  // Stabilni okvir Hrvatske; širi se ako rute izlaze izvan njega.
  const BASE_BOUNDS = { minLat: 42.35, maxLat: 46.55, minLng: 13.4, maxLng: 19.45 };

  const DEFAULT_ROUTES = [
    { startName: "Zagreb", startLat: 45.815, startLng: 15.9819, endName: "Split", endLat: 43.5081, endLng: 16.4402 },
    { startName: "Zagreb", startLat: 45.815, startLng: 15.9819, endName: "Rijeka", endLat: 45.3271, endLng: 14.4422 },
    { startName: "Zagreb", startLat: 45.815, startLng: 15.9819, endName: "Osijek", endLat: 45.555, endLng: 18.6955 },
    { startName: "Split", startLat: 43.5081, startLng: 16.4402, endName: "Dubrovnik", endLat: 42.6507, endLng: 18.0944 },
    { startName: "Rijeka", startLat: 45.3271, startLng: 14.4422, endName: "Zadar", endLat: 44.1194, endLng: 15.2314 },
    { startName: "Zagreb", startLat: 45.815, startLng: 15.9819, endName: "Varaždin", endLat: 46.3057, endLng: 16.3366 }
  ];

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
    startName: route.startName || "Polazište",
    startLat: Number(route.startLat),
    startLng: Number(route.startLng),
    endName: route.endName || "Odredište",
    endLat: Number(route.endLat),
    endLng: Number(route.endLng)
  }));
  if (routes.length === 0) {
    routes = DEFAULT_ROUTES.slice();
  }
  routes = routes.slice(0, 7);

  // --- Projekcija geografskih koordinata u SVG viewBox ---
  const bounds = {
    minLat: BASE_BOUNDS.minLat,
    maxLat: BASE_BOUNDS.maxLat,
    minLng: BASE_BOUNDS.minLng,
    maxLng: BASE_BOUNDS.maxLng
  };
  routes.forEach((route) => {
    bounds.minLat = Math.min(bounds.minLat, route.startLat, route.endLat);
    bounds.maxLat = Math.max(bounds.maxLat, route.startLat, route.endLat);
    bounds.minLng = Math.min(bounds.minLng, route.startLng, route.endLng);
    bounds.maxLng = Math.max(bounds.maxLng, route.startLng, route.endLng);
  });
  const midLatRad = ((bounds.minLat + bounds.maxLat) / 2) * Math.PI / 180;
  const cosLat = Math.max(Math.cos(midLatRad), 0.2);
  const geoW = Math.max((bounds.maxLng - bounds.minLng) * cosLat, 0.0001);
  const geoH = Math.max(bounds.maxLat - bounds.minLat, 0.0001);
  const scale = Math.min((VB_W - 2 * PAD_X) / geoW, (VB_H - 2 * PAD_Y) / geoH);
  const offsetX = (VB_W - geoW * scale) / 2;
  const offsetY = (VB_H - geoH * scale) / 2;

  const project = (lat, lng) => [
    offsetX + (lng - bounds.minLng) * cosLat * scale,
    offsetY + (bounds.maxLat - lat) * scale
  ];

  const el = (name, attrs) => {
    const node = document.createElementNS(NS, name);
    for (const key in attrs) {
      node.setAttribute(key, attrs[key]);
    }
    return node;
  };

  // --- Pozadinska mreža (graticule) ---
  for (let x = 0; x <= VB_W; x += 180) {
    gridLayer?.append(el("line", { x1: x, y1: 0, x2: x, y2: VB_H, class: "ss-bg-grid-line" }));
  }
  for (let y = 0; y <= VB_H; y += 150) {
    gridLayer?.append(el("line", { x1: 0, y1: y, x2: VB_W, y2: y, class: "ss-bg-grid-line" }));
  }

  const approxPath = (a, b, direction) => {
    const dx = b[0] - a[0];
    const dy = b[1] - a[1];
    const dist = Math.hypot(dx, dy) || 1;
    const bend = Math.min(Math.max(dist * 0.18, 28), 150) * direction;
    const nx = -dy / dist;
    const ny = dx / dist;
    const cx = (a[0] + b[0]) / 2 + nx * bend;
    const cy = (a[1] + b[1]) / 2 + ny * bend;
    return `M ${a[0].toFixed(1)} ${a[1].toFixed(1)} Q ${cx.toFixed(1)} ${cy.toFixed(1)} ${b[0].toFixed(1)} ${b[1].toFixed(1)}`;
  };

  const exactPath = (points) => {
    let d = "";
    points.forEach((point, index) => {
      const [x, y] = project(point[0], point[1]);
      d += `${index === 0 ? "M" : "L"} ${x.toFixed(1)} ${y.toFixed(1)} `;
    });
    return d.trim();
  };

  const fetchExact = (route) => {
    const query = new URLSearchParams({
      startLat: route.startLat.toFixed(6),
      startLng: route.startLng.toFixed(6),
      endLat: route.endLat.toFixed(6),
      endLng: route.endLng.toFixed(6)
    });
    const controller = new AbortController();
    const timer = window.setTimeout(() => controller.abort(), 1200);
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
        const points = payload.points.filter(
          (point) => Array.isArray(point) && isFinite(point[0]) && isFinite(point[1]));
        return points.length >= 2 ? points : null;
      })
      .catch(() => null)
      .finally(() => window.clearTimeout(timer));
  };

  const nodeSeen = new Set();
  const addNode = (point, name, isDestination) => {
    const key = `${point[0].toFixed(0)},${point[1].toFixed(0)}`;
    if (nodeSeen.has(key)) {
      return;
    }
    nodeSeen.add(key);
    const group = el("g", { transform: `translate(${point[0].toFixed(1)} ${point[1].toFixed(1)})` });
    if (isDestination) {
      group.append(el("circle", { r: 6, class: "ss-bg-node-pulse" }));
    }
    group.append(el("circle", { r: 5.5, class: "ss-bg-node-halo" }));
    group.append(el("circle", { r: 3, class: "ss-bg-node-core" }));
    nodeLayer.append(group);
  };

  const cars = [];

  routes.forEach((route, index) => {
    const start = project(route.startLat, route.startLng);
    const end = project(route.endLat, route.endLng);
    const direction = index % 2 === 0 ? 1 : -1;

    const path = el("path", {
      class: "ss-bg-route",
      d: approxPath(start, end, direction),
      style: `--ss-bg-route-delay:${(index * -1.3).toFixed(2)}s;opacity:${(0.9 - index * 0.07).toFixed(2)}`
    });
    routeLayer.append(path);

    addNode(start, route.startName, false);
    addNode(end, route.endName, true);

    const car = template.cloneNode(true);
    car.removeAttribute("id");
    car.setAttribute("class", `ss-bg-car ss-bg-car-${index % 4}`);
    carLayer.append(car);

    const entry = {
      path,
      car,
      length: path.getTotalLength(),
      distance: (index / routes.length),
      speed: 58 + (index % 3) * 16,
      reverse: direction < 0
    };
    entry.distance *= entry.length;
    cars.push(entry);

    if (!reduceMotion.matches) {
      fetchExact(route).then((points) => {
        if (!points) {
          return;
        }
        const ratio = entry.length > 0 ? entry.distance / entry.length : 0;
        path.setAttribute("d", exactPath(points));
        entry.length = path.getTotalLength();
        entry.distance = ratio * entry.length;
      });
    }
  });

  const placeCar = (entry) => {
    if (entry.length <= 0) {
      return;
    }
    const along = entry.reverse ? entry.length - entry.distance : entry.distance;
    const point = entry.path.getPointAtLength(along);
    const ahead = entry.path.getPointAtLength(
      Math.min(entry.length, Math.max(0, along + (entry.reverse ? -1 : 1))));
    const angle = Math.atan2(ahead.y - point.y, ahead.x - point.x) * 180 / Math.PI;
    entry.car.setAttribute(
      "transform",
      `translate(${point.x.toFixed(2)} ${point.y.toFixed(2)}) rotate(${angle.toFixed(1)})`);
  };

  if (reduceMotion.matches) {
    cars.forEach((entry) => {
      entry.distance = entry.length * 0.5;
      placeCar(entry);
    });
    return;
  }

  let lastTime = 0;
  let frame = 0;

  const step = (time) => {
    if (!root.isConnected) {
      window.cancelAnimationFrame(frame);
      return;
    }
    const delta = lastTime ? Math.min((time - lastTime) / 1000, 0.05) : 0;
    lastTime = time;
    cars.forEach((entry) => {
      entry.distance += entry.speed * delta;
      if (entry.distance >= entry.length) {
        entry.distance -= entry.length;
      }
      placeCar(entry);
    });
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

  cars.forEach(placeCar);
  start();
})();
