/* ---------------------------------------------------------------------------
   Dashboard for the neighbourhood energy simulation.

   Vanilla JS, no build step, no CDN. It polls GET /api/simulation every 250 ms
   and repaints. Two rules run through the whole file:

     1. Nothing renders undefined or NaN. Every number from the wire goes
        through num() first, every list through arr().
     2. A percentage is never shown without the window it was measured over.
        "Since simulation start" and "last 24 h" are different measurements and
        are labelled as such everywhere they appear.
--------------------------------------------------------------------------- */
'use strict';

const $ = id => document.getElementById(id);

/* --- safety helpers ------------------------------------------------------ */
const num = v => (typeof v === 'number' && Number.isFinite(v)) ? v : 0;
const arr = v => Array.isArray(v) ? v : [];
const f = (v, d) => num(v).toFixed(d);
const esc = s => String(s == null ? '' : s)
  .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

const COLOR = { s1: '#3987e5', s2: '#d95926', s3: '#199e70', grid: '#1f2942', axis: '#2b3a5c', ink: '#8794b0', surface: '#121829' };

const ASSET_GLYPH = { Pv: '☀', HeatPump: '♨', HomeEvCharger: '⚡', BaseLoad: '' };

let running = true;
let meterFilter = '';
let snapshot = null;
let hoverX = null;      // pointer position inside the main chart, in svg px
let hoverIndex = null;  // index the crosshair snapped to
let builtHouses = 0, builtChargers = 0, builtBattery = null;
// The snapshot carries the battery's live state but not its nameplate limits,
// so those are read from the configuration endpoint rather than guessed.
let config = null;

/* --- clock / sky --------------------------------------------------------- */
const utc = iso => { const d = new Date(iso); return Number.isNaN(d.getTime()) ? null : d; };
const hhmm = d => d ? String(d.getUTCHours()).padStart(2, '0') + ':' + String(d.getUTCMinutes()).padStart(2, '0') : '--:--';

/** Sky colour follows the simulated hour, so the page visibly breathes day/night. */
function skyFor(hour, irradiance) {
  const stops = [
    [0, '#050813', '#0a1024'], [5, '#131a3a', '#2a2350'], [7, '#3b3566', '#8a5a72'],
    [9, '#2b5f96', '#4a92c4'], [13, '#2f7fc4', '#63b6e8'], [17, '#2b5f96', '#c4794a'],
    [19, '#6b3f63', '#c76a4e'], [21, '#1a1b3d', '#2a2350'], [24, '#050813', '#0a1024'],
  ];
  let a = stops[0], b = stops[stops.length - 1];
  for (let i = 0; i < stops.length - 1; i++)
    if (hour >= stops[i][0] && hour <= stops[i + 1][0]) { a = stops[i]; b = stops[i + 1]; break; }
  const t = (hour - a[0]) / Math.max(0.001, b[0] - a[0]);
  const mix = (x, y) => {
    const p = c => [parseInt(c.slice(1, 3), 16), parseInt(c.slice(3, 5), 16), parseInt(c.slice(5, 7), 16)];
    const [r1, g1, b1] = p(x), [r2, g2, b2] = p(y);
    return `rgb(${Math.round(r1 + (r2 - r1) * t)},${Math.round(g1 + (g2 - g1) * t)},${Math.round(b1 + (b2 - b1) * t)})`;
  };
  const glow = 0.25 + 0.75 * num(irradiance);
  return `radial-gradient(120% 90% at 50% 0%, ${mix(a[1], b[1])} 0%, ${mix(a[2], b[2])} ${40 * glow + 25}%, transparent 100%)`;
}

function weatherIcon(cloud, irradiance) {
  if (num(irradiance) <= 0.01) return num(cloud) > 0.6 ? '☁' : '☽';
  if (num(cloud) > 0.75) return '☁';
  if (num(cloud) > 0.35) return '⛅';
  return '☀';
}

/* --- chart plumbing ------------------------------------------------------ */
function box(svg, pad) {
  const r = svg.getBoundingClientRect();
  const W = Math.max(320, Math.round(r.width) || 1000);
  const H = Math.max(80, Math.round(r.height) || 200);
  svg.setAttribute('viewBox', `0 0 ${W} ${H}`);
  svg.setAttribute('preserveAspectRatio', 'none');
  return { W, H, iw: W - pad.l - pad.r, ih: H - pad.t - pad.b };
}

/** Round tick values so the axis reads 0 / 25 / 50 rather than 0 / 23.7 / 47.4. */
function ticks(min, max, count) {
  const span = Math.max(1e-6, max - min);
  const mag = Math.pow(10, Math.floor(Math.log10(span / count)));
  const n = (span / count) / mag;
  const step = (n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * mag;
  const out = [];
  for (let v = Math.ceil(min / step) * step; v <= max + 1e-9; v += step) out.push(Math.abs(v) < 1e-9 ? 0 : v);
  return out;
}

const path = pts => pts.map((p, i) => `${i ? 'L' : 'M'}${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join(' ');

/* --- 1. the peak shaving chart ------------------------------------------ */
function drawPeakChart(points, s) {
  const svg = $('chart');
  const pad = { l: 56, r: 16, t: 20, b: 26 };
  const { W, H, iw, ih } = box(svg, pad);
  if (points.length < 2) { svg.innerHTML = ''; $('tip').hidden = true; return; }

  const withB = points.map(p => num(p.netKw));
  const noB = points.map(p => num(p.netWithoutBatteryKw));
  const hi = Math.max(10, ...withB, ...noB);
  const lo = Math.min(0, ...withB, ...noB);
  const yMax = hi * 1.08, yMin = lo < 0 ? lo * 1.12 : 0;

  const x = i => pad.l + (i / (points.length - 1)) * iw;
  const y = v => pad.t + ((yMax - num(v)) / Math.max(1e-6, yMax - yMin)) * ih;

  const grid = ticks(yMin, yMax, 5).map(v =>
    `<line x1="${pad.l}" y1="${y(v).toFixed(1)}" x2="${W - pad.r}" y2="${y(v).toFixed(1)}" stroke="${COLOR.grid}" stroke-width="1"/>` +
    `<text x="${pad.l - 8}" y="${(y(v) + 3.5).toFixed(1)}" fill="${COLOR.ink}" font-size="10" text-anchor="end">${v.toFixed(0)}</text>`
  ).join('');

  // The band between the two lines, split by sign: one subpath per interval, so
  // a crossing costs at most one mis-tinted sliver instead of crossing algebra.
  let shaved = '', added = '';
  for (let i = 0; i < points.length - 1; i++) {
    const quad = `M${x(i).toFixed(1)} ${y(withB[i]).toFixed(1)} L${x(i).toFixed(1)} ${y(noB[i]).toFixed(1)} ` +
                 `L${x(i + 1).toFixed(1)} ${y(noB[i + 1]).toFixed(1)} L${x(i + 1).toFixed(1)} ${y(withB[i + 1]).toFixed(1)} Z `;
    const diff = (noB[i] - withB[i]) + (noB[i + 1] - withB[i + 1]);
    if (diff > 0.02) shaved += quad; else if (diff < -0.02) added += quad;
  }

  const lineWith = path(points.map((p, i) => [x(i), y(withB[i])]));
  const lineNo = path(points.map((p, i) => [x(i), y(noB[i])]));
  const areaWith = `${lineWith} L${x(points.length - 1).toFixed(1)} ${y(0).toFixed(1)} L${pad.l.toFixed(1)} ${y(0).toFixed(1)} Z`;

  // Reference hairlines for the window peaks - the visual proof of the shave.
  const winNo = Math.max(...noB), winWith = Math.max(...withB);
  let yNo = y(winNo), yWith = y(winWith);
  if (Math.abs(yNo - yWith) < 13) yWith = yNo + 13;
  const ref = (yy, v, color, label) =>
    `<line x1="${pad.l}" y1="${yy.toFixed(1)}" x2="${W - pad.r}" y2="${yy.toFixed(1)}" stroke="${color}" stroke-width="1" stroke-dasharray="2 3" opacity=".75"/>` +
    `<text x="${W - pad.r - 4}" y="${(yy - 5).toFixed(1)}" fill="${COLOR.ink}" font-size="9.5" text-anchor="end">${label} ${v.toFixed(1)} kW</text>`;

  const ceiling = num(s && s.peakShavingThresholdKw);
  const ceilLine = ceiling > 0 && ceiling < yMax
    ? `<line x1="${pad.l}" y1="${y(ceiling).toFixed(1)}" x2="${W - pad.r}" y2="${y(ceiling).toFixed(1)}" stroke="${COLOR.ink}" stroke-width="1.5" stroke-dasharray="8 4"/>` +
      `<text x="${pad.l + 6}" y="${(y(ceiling) - 5).toFixed(1)}" fill="${COLOR.ink}" font-size="10">hard ceiling ${ceiling.toFixed(0)} kW</text>`
    : '';

  // Crosshair, snapped to the nearest sample.
  hoverIndex = null;
  let cross = '';
  if (hoverX != null) {
    const i = Math.round(((hoverX - pad.l) / Math.max(1, iw)) * (points.length - 1));
    if (i >= 0 && i < points.length) {
      hoverIndex = i;
      cross =
        `<line x1="${x(i).toFixed(1)}" y1="${pad.t}" x2="${x(i).toFixed(1)}" y2="${(H - pad.b).toFixed(1)}" stroke="${COLOR.axis}" stroke-width="1"/>` +
        `<circle cx="${x(i).toFixed(1)}" cy="${y(noB[i]).toFixed(1)}" r="4.5" fill="${COLOR.s2}" stroke="${COLOR.surface}" stroke-width="2"/>` +
        `<circle cx="${x(i).toFixed(1)}" cy="${y(withB[i]).toFixed(1)}" r="4.5" fill="${COLOR.s1}" stroke="${COLOR.surface}" stroke-width="2"/>`;
    }
  }

  const last = points.length - 1;
  const hours = [0, 0.25, 0.5, 0.75, 1].map(fr => {
    const i = Math.round(fr * last);
    const anchor = fr === 0 ? 'start' : fr === 1 ? 'end' : 'middle';
    return `<text x="${x(i).toFixed(1)}" y="${H - 8}" fill="${COLOR.ink}" font-size="10" text-anchor="${anchor}">${hhmm(utc(points[i].instant))}</text>`;
  }).join('');

  svg.innerHTML = `
    <defs><linearGradient id="withFill" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0%" stop-color="${COLOR.s1}" stop-opacity=".22"/>
      <stop offset="100%" stop-color="${COLOR.s1}" stop-opacity=".02"/>
    </linearGradient></defs>
    ${grid}
    <line x1="${pad.l}" y1="${y(0).toFixed(1)}" x2="${W - pad.r}" y2="${y(0).toFixed(1)}" stroke="${COLOR.axis}" stroke-width="1"/>
    <path d="${areaWith}" fill="url(#withFill)"/>
    <path d="${shaved}" fill="${COLOR.s2}" fill-opacity=".30"/>
    <path d="${added}" fill="${COLOR.s1}" fill-opacity=".30"/>
    ${ceilLine}
    ${ref(yNo, winNo, COLOR.s2, '24 h peak without battery')}
    ${ref(yWith, winWith, COLOR.s1, '24 h peak with battery')}
    <path d="${lineNo}" fill="none" stroke="${COLOR.s2}" stroke-width="2" stroke-dasharray="6 4" stroke-linejoin="round" stroke-linecap="round"/>
    <path d="${lineWith}" fill="none" stroke="${COLOR.s1}" stroke-width="2" stroke-linejoin="round" stroke-linecap="round"/>
    <circle cx="${x(last).toFixed(1)}" cy="${y(withB[last]).toFixed(1)}" r="4.5" fill="${COLOR.s1}" stroke="${COLOR.surface}" stroke-width="2"/>
    ${cross}
    <text x="4" y="${pad.t - 8}" fill="${COLOR.ink}" font-size="10">kW</text>
    ${hours}`;

  paintTip(points, x, y, pad, W);
}

function paintTip(points, x, y, pad, W) {
  const tip = $('tip');
  if (hoverIndex == null) { tip.hidden = true; return; }
  const p = points[hoverIndex];
  const row = (color, dash, name, value) =>
    `<div class="t-row"><span class="n"><i class="t-key" style="border-color:${color};border-top-style:${dash}"></i>${name}</span><span class="v">${value}</span></div>`;
  tip.innerHTML =
    `<div class="t-when">${esc(hhmm(utc(p.instant)))} UTC</div>` +
    row(COLOR.s2, 'dashed', 'without battery', f(p.netWithoutBatteryKw, 1) + ' kW') +
    row(COLOR.s1, 'solid', 'with battery', f(p.netKw, 1) + ' kW') +
    row(COLOR.s3, 'solid', 'battery', (num(p.batteryKw) > 0 ? '+' : '') + f(p.batteryKw, 1) + ' kW') +
    row(COLOR.s3, 'solid', 'state of charge', f(p.socPercent, 0) + ' %');
  tip.hidden = false;
  const px = x(hoverIndex), tw = tip.offsetWidth || 200;
  const left = px + 14 + tw > W ? px - 14 - tw : px + 14;
  tip.style.left = Math.max(2, left) + 'px';
  tip.style.top = Math.max(2, Math.min(y(num(p.netKw)) - 18, ($('chart').clientHeight || 300) - (tip.offsetHeight || 100) - 4)) + 'px';
}

/* --- 2. state of charge sparkline --------------------------------------- */
function drawSocChart(points) {
  const svg = $('socChart');
  if (!svg) return;
  const pad = { l: 30, r: 10, t: 8, b: 14 };
  const { W, H, iw, ih } = box(svg, pad);
  if (points.length < 2) { svg.innerHTML = ''; return; }
  const x = i => pad.l + (i / (points.length - 1)) * iw;
  const y = v => pad.t + ((100 - Math.max(0, Math.min(100, num(v)))) / 100) * ih;
  const pts = points.map((p, i) => [x(i), y(p.socPercent)]);
  const line = path(pts);
  const last = points.length - 1;
  const guide = [0, 50, 100].map(v =>
    `<line x1="${pad.l}" y1="${y(v).toFixed(1)}" x2="${W - pad.r}" y2="${y(v).toFixed(1)}" stroke="${COLOR.grid}" stroke-width="1"/>` +
    `<text x="${pad.l - 6}" y="${(y(v) + 3.5).toFixed(1)}" fill="${COLOR.ink}" font-size="9" text-anchor="end">${v}</text>`).join('');
  svg.innerHTML = `${guide}
    <path d="${line} L${x(last).toFixed(1)} ${y(0).toFixed(1)} L${pad.l.toFixed(1)} ${y(0).toFixed(1)} Z" fill="${COLOR.s3}" fill-opacity=".14"/>
    <path d="${line}" fill="none" stroke="${COLOR.s3}" stroke-width="2" stroke-linejoin="round"/>
    <circle cx="${x(last).toFixed(1)}" cy="${y(points[last].socPercent).toFixed(1)}" r="4" fill="${COLOR.s3}" stroke="${COLOR.surface}" stroke-width="2"/>
    <text x="2" y="${H - 3}" fill="${COLOR.ink}" font-size="9">24 h ago</text>
    <text x="${W - pad.r}" y="${H - 3}" fill="${COLOR.ink}" font-size="9" text-anchor="end">now</text>`;
}

/* --- 3. demand vs PV, battery excluded ---------------------------------- */
function drawDemandChart(points) {
  const svg = $('cgChart');
  const pad = { l: 44, r: 12, t: 14, b: 20 };
  const { W, H, iw, ih } = box(svg, pad);
  if (points.length < 2) { svg.innerHTML = ''; return; }
  // consumptionKw includes battery charging and generationKw includes battery
  // discharge - both verified against the wire. Strip them so this chart is
  // households and chargers only.
  const demand = points.map(p => Math.max(0, num(p.consumptionKw) - Math.max(0, num(p.batteryKw))));
  const pv = points.map(p => Math.max(0, num(p.generationKw) - Math.max(0, -num(p.batteryKw))));
  const yMax = Math.max(10, ...demand, ...pv) * 1.08;
  const x = i => pad.l + (i / (points.length - 1)) * iw;
  const y = v => pad.t + ((yMax - num(v)) / yMax) * ih;
  const grid = ticks(0, yMax, 3).map(v =>
    `<line x1="${pad.l}" y1="${y(v).toFixed(1)}" x2="${W - pad.r}" y2="${y(v).toFixed(1)}" stroke="${COLOR.grid}" stroke-width="1"/>` +
    `<text x="${pad.l - 7}" y="${(y(v) + 3.5).toFixed(1)}" fill="${COLOR.ink}" font-size="9.5" text-anchor="end">${v.toFixed(0)}</text>`).join('');
  const last = points.length - 1;
  const series = (vals, color) => {
    const l = path(vals.map((v, i) => [x(i), y(v)]));
    return `<path d="${l} L${x(last).toFixed(1)} ${y(0).toFixed(1)} L${pad.l.toFixed(1)} ${y(0).toFixed(1)} Z" fill="${color}" fill-opacity=".12"/>` +
           `<path d="${l}" fill="none" stroke="${color}" stroke-width="2" stroke-linejoin="round"/>`;
  };
  svg.innerHTML = `${grid}${series(demand, COLOR.s2)}${series(pv, COLOR.s3)}
    <text x="2" y="${pad.t - 4}" fill="${COLOR.ink}" font-size="9.5">kW</text>
    <text x="${pad.l}" y="${H - 5}" fill="${COLOR.ink}" font-size="9.5">${esc(hhmm(utc(points[0].instant)))}</text>
    <text x="${W - pad.r}" y="${H - 5}" fill="${COLOR.ink}" font-size="9.5" text-anchor="end">${esc(hhmm(utc(points[last].instant)))}</text>`;
}

/* --- peak shaving figures ----------------------------------------------- */
function renderPeaks(s, points) {
  const hasBattery = !!s.battery;

  // `scope` is not decoration: the two pairs are different measurements, so the
  // percentage carries the window it was measured over rather than relying on
  // the heading above it to supply the meaning.
  const fill = (withoutId, withId, deltaId, without, withB, scope) => {
    $(withoutId).textContent = f(without, 1);
    $(withId).textContent = f(withB, 1);
    const el = $(deltaId);
    const cut = without - withB;
    if (!hasBattery) { el.className = 'delta none'; el.textContent = `no battery installed (${scope})`; return; }
    if (without <= 0.01 || cut <= 0.05) { el.className = 'delta none'; el.textContent = `no reduction yet (${scope})`; return; }
    el.className = 'delta';
    el.innerHTML = `<span class="arrow">▼</span>${f(cut, 1)} kW lower`
      + `<span class="pct">${f(100 * cut / without, 1)} % below the no-battery peak, ${esc(scope)}</span>`;
  };

  fill('pkStartWithout', 'pkStartWith', 'pkStartDelta',
    num(s.peakWithoutBatteryKw), num(s.peakWithBatteryKw), 'since simulation start');

  const winNo = points.length ? Math.max(...points.map(p => num(p.netWithoutBatteryKw))) : 0;
  const winWith = points.length ? Math.max(...points.map(p => num(p.netKw))) : 0;
  fill('pkWinWithout', 'pkWinWith', 'pkWinDelta', winNo, winWith, 'last 24 h');

  $('battStateBadge').textContent = hasBattery
    ? `${f(s.battery.capacityKwh, 0)} kWh battery installed`
    : 'battery disabled in the configuration';

  const ceiling = num(s.peakShavingThresholdKw);
  $('peakNote').textContent = hasBattery
    ? 'These two pairs are measured over different windows and will not agree: the left pair is cumulative from the first tick of this run '
      + '(it only ever grows, and resets when the configuration is saved), the right pair is the maximum inside the 24 h window drawn below. '
      + (ceiling > 0
        ? `The strategy also holds a hard ceiling of ${f(ceiling, 0)} kW on top of its percentile band.`
        : 'No hard ceiling is set, so the strategy is percentile-only.')
    : 'Enable the battery on the configuration page to see a peak shaving result.';
}

/* --- battery panel ------------------------------------------------------- */
function batteryLayout(has) {
  if (builtBattery === has) return;
  builtBattery = has;
  $('batteryBody').innerHTML = has ? `
    <div class="batt-top">
      <div class="batt-power">
        <span class="muted" style="font-size:10.5px;text-transform:uppercase;letter-spacing:.7px">Battery power now</span>
        <span class="v"><b id="bPower">0.0</b><i>kW</i></span>
      </div>
      <span class="mode" id="bMode"><span class="glyph">●</span><span id="bModeText">idle</span></span>
    </div>
    <div class="soc-wrap">
      <div class="soc-head"><span>State of charge <b id="bSocPct">0</b>%</span><span id="bSocKwh">0 / 0 kWh</span></div>
      <div class="soc-track"><div class="soc-fill" id="bSocFill" style="width:0%"></div></div>
    </div>
    <div class="batt-facts">
      <div class="fact"><span class="k">Capacity</span><span class="v"><b id="bCap">0</b><i>kWh</i></span></div>
      <div class="fact"><span class="k">Max power</span><span class="v"><b id="bMax">&mdash;</b><i>kW</i></span></div>
      <div class="fact"><span class="k">Round trip</span><span class="v"><b id="bEff">&mdash;</b><i>%</i></span></div>
      <div class="fact"><span class="k"><i class="key s1"></i>Charged</span><span class="v"><b id="bCharged">0</b><i>kWh</i></span></div>
      <div class="fact"><span class="k"><i class="key s3"></i>Discharged</span><span class="v"><b id="bDischarged">0</b><i>kWh</i></span></div>
    </div>
    <div class="strategy">Strategy: <b id="bStrategy">&mdash;</b></div>
    <div style="margin-top:14px">
      <div class="soc-head"><span>State of charge, last 24 simulated hours</span><span class="muted">0 &ndash; 100 %</span></div>
      <div class="chart-wrap"><svg id="socChart" class="chart" role="img" aria-label="Battery state of charge over the last 24 simulated hours."></svg></div>
    </div>`
    : `<div class="empty">The battery is switched off in the configuration.<br/>
       Turn it on to see charge state, throughput and peak shaving.<br/><br/>
       <a class="btn" href="config.html">Open configuration</a></div>`;
}

function renderBattery(s, points) {
  const b = s.battery;
  batteryLayout(!!b);
  $('battMeter').textContent = b ? 'meter neighbourhood/battery' : '';
  if (!b) return;

  const power = num(b.powerKw);
  $('bPower').textContent = (power > 0 ? '+' : '') + f(power, 1);
  const mode = (b.mode === 'charging' || b.mode === 'discharging') ? b.mode : 'idle';
  $('bMode').className = 'mode ' + mode;
  $('bModeText').textContent = mode === 'charging' ? 'charging - storing surplus'
    : mode === 'discharging' ? 'discharging - shaving the peak' : 'idle - load inside the band';

  const pct = Math.max(0, Math.min(100, num(b.stateOfChargePercent)));
  $('bSocPct').textContent = f(pct, 1);
  $('bSocFill').style.width = pct + '%';
  $('bSocKwh').textContent = `${f(b.stateOfChargeKwh, 1)} / ${f(b.capacityKwh, 0)} kWh stored`;
  $('bCap').textContent = f(b.capacityKwh, 0);
  $('bMax').textContent = config ? f(config.batteryMaxPowerKw, 0) : '—';
  $('bEff').textContent = config ? f(num(config.batteryRoundTripEfficiency) * 100, 0) : '—';
  $('bCharged').textContent = f(b.chargedKwh, 1);
  $('bDischarged').textContent = f(b.dischargedKwh, 1);
  $('bStrategy').textContent = b.strategy || 'unknown';
  drawSocChart(points);
}

/* --- houses & chargers (built once, then updated in place so CSS animates) */
function renderHouses(houses) {
  const host = $('houses');
  if (builtHouses !== houses.length) {
    builtHouses = houses.length;
    host.innerHTML = houses.map((h, i) => `<div class="house" id="h${i}">
      <div class="hid" id="h${i}n"></div>
      <div class="hkw"><b id="h${i}p">0.00</b><i>kW</i></div>
      <div class="icons" id="h${i}a"></div>
      <div class="hkwh" id="h${i}e"></div></div>`).join('');
  }
  houses.forEach((h, i) => {
    const p = num(h.netPowerKw);
    $('h' + i).className = 'house ' + (p < 0 ? 'exp' : 'imp');
    $('h' + i + 'n').textContent = String(h.id || '').replace('house-', 'House ');
    $('h' + i + 'p').textContent = f(p, 2);
    $('h' + i + 'a').textContent = arr(h.assets).map(a => ASSET_GLYPH[a] || '').join('');
    $('h' + i + 'e').textContent = f(h.netKwh, 1) + ' kWh net';
  });
}

function renderChargers(list) {
  const host = $('chargers');
  if (builtChargers !== list.length) {
    builtChargers = list.length;
    host.innerHTML = list.map((c, i) => `<div class="charger" id="c${i}">
      <div class="cid" id="c${i}n"></div>
      <div class="ckw"><b id="c${i}p">0.0</b><i>kW</i></div>
      <div class="ckwh" id="c${i}e"></div></div>`).join('');
  }
  list.forEach((c, i) => {
    $('c' + i).className = 'charger' + (c.busy ? ' busy' : '');
    $('c' + i + 'n').textContent = String(c.id || '').replace('public-charger-', 'Point ');
    $('c' + i + 'p').textContent = f(c.powerKw, 1);
    $('c' + i + 'e').textContent = f(c.consumedKwh, 1) + ' kWh total';
  });
}

/* --- tables -------------------------------------------------------------- */
const CAT_KEY = { Pv: 's3', Battery: 's3', BaseLoad: 's2', HeatPump: 's2', HomeEvCharger: 's2', PublicEvCharger: 's1' };

function renderMeters(meters) {
  const rows = meters.filter(m => !meterFilter
    || String(m.meterId).toLowerCase().includes(meterFilter)
    || String(m.ownerId).toLowerCase().includes(meterFilter)
    || String(m.category).toLowerCase().includes(meterFilter));
  $('meters').tBodies[0].innerHTML = rows.map(m => `<tr>
    <td>${esc(m.meterId)}</td><td>${esc(m.ownerId)}</td>
    <td><span class="cat"><i class="key ${CAT_KEY[m.category] || 's1'}"></i>${esc(m.category)}</span></td>
    <td class="num">${f(m.consumedKwh, 2)}</td>
    <td class="num">${num(m.generatedKwh) ? f(m.generatedKwh, 2) : '—'}</td>
    <td class="num">${f(m.netKwh, 2)}</td>
    <td class="num">${f(m.lastPowerKw, 2)}</td></tr>`).join('');
}

function renderSeriesTable(points) {
  const details = document.querySelector('details.data-table');
  if (!details || !details.open) return;
  $('seriesTable').tBodies[0].innerHTML = points.map(p => `<tr>
    <td>${esc(hhmm(utc(p.instant)))}</td>
    <td class="num">${f(p.netWithoutBatteryKw, 2)}</td>
    <td class="num">${f(p.netKw, 2)}</td>
    <td class="num">${f(p.batteryKw, 2)}</td>
    <td class="num">${f(p.socPercent, 1)}</td></tr>`).join('');
}

/* --- main render --------------------------------------------------------- */
function render(s) {
  snapshot = s;
  const points = arr(s.last24Hours);
  const d = utc(s.instant);
  const hour = d ? d.getUTCHours() + d.getUTCMinutes() / 60 : 0;

  $('sky').style.background = skyFor(hour, s.irradianceFactor);
  $('date').textContent = d ? d.toUTCString().slice(0, 16) : '—';
  $('time').textContent = hhmm(d);
  $('season').textContent = s.season || '—';
  $('temp').textContent = f(s.temperatureC, 1) + '°C';
  $('cloud').textContent = `${Math.round(num(s.cloudCover) * 100)}% cloud · ${Math.round(num(s.irradianceFactor) * 100)}% sun`;
  $('wxIcon').textContent = weatherIcon(s.cloudCover, s.irradianceFactor);
  $('seed').textContent = s.seed != null ? s.seed : '—';
  $('tickIndex').textContent = s.tickIndex != null ? s.tickIndex : '—';

  $('net').textContent = f(s.netPowerKw, 1);
  $('cons').textContent = f(s.consumptionKw, 1);
  $('gen').textContent = f(s.generationKw, 1);
  $('imp').textContent = f(s.totalImportedKwh, 0);
  $('exp').textContent = f(s.totalExportedKwh, 0);
  $('totals').textContent = `${f(s.totalConsumedKwh, 0)} / ${f(s.totalGeneratedKwh, 0)}`;

  const exporting = num(s.netPowerKw) < 0;
  $('netCard').className = 'stat big ' + (exporting ? 'exporting' : 'importing');
  $('netHint').textContent = exporting
    ? `exporting ${f(Math.abs(num(s.netPowerKw)), 1)} kW to the grid · ${f(s.netPowerWithoutBatteryKw, 1)} kW without the battery`
    : `importing ${f(s.netPowerKw, 1)} kW from the grid · ${f(s.netPowerWithoutBatteryKw, 1)} kW without the battery`;

  renderPeaks(s, points);
  drawPeakChart(points, s);
  drawDemandChart(points);
  renderBattery(s, points);
  renderHouses(arr(s.houses));
  renderChargers(arr(s.publicChargers));
  renderMeters(arr(s.meters));
  renderSeriesTable(points);

  running = !!s.running;
  $('toggle').textContent = running ? 'Pause' : 'Resume';
  $('speedVal').textContent = f(s.ticksPerSecond, 0) + '×';
  if (document.activeElement !== $('speed')) $('speed').value = num(s.ticksPerSecond);
}

/* --- polling & controls -------------------------------------------------- */
async function loadConfig() {
  try {
    const r = await fetch('/api/simulation/configuration');
    if (r.ok) config = await r.json();
  } catch { /* keep the previous copy */ }
}

let pollCount = 0;
async function poll() {
  try {
    const r = await fetch('/api/simulation');
    if (r.ok) render(await r.json());
  } catch { /* transient while the engine is being reconfigured */ }
  if (pollCount++ % 20 === 0) loadConfig();
}

$('toggle').onclick = async () => {
  try { await fetch(running ? '/api/simulation/pause' : '/api/simulation/resume', { method: 'POST' }); } catch { }
  poll();
};
$('filter').oninput = e => { meterFilter = e.target.value.trim().toLowerCase(); if (snapshot) renderMeters(arr(snapshot.meters)); };
$('speed').onchange = async e => {
  const value = Number(e.target.value);
  try {
    const cfg = await (await fetch('/api/simulation/configuration')).json();
    cfg.ticksPerSecond = value;
    await fetch('/api/simulation/configuration', {
      method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cfg),
    });
  } catch { }
  poll();
};

const chart = $('chart');
chart.addEventListener('pointermove', e => {
  const r = chart.getBoundingClientRect();
  hoverX = ((e.clientX - r.left) / Math.max(1, r.width)) * (chart.viewBox.baseVal.width || r.width);
  if (snapshot) drawPeakChart(arr(snapshot.last24Hours), snapshot);
});
chart.addEventListener('pointerleave', () => {
  hoverX = null; hoverIndex = null; $('tip').hidden = true;
  if (snapshot) drawPeakChart(arr(snapshot.last24Hours), snapshot);
});
chart.addEventListener('keydown', e => {
  const points = arr(snapshot && snapshot.last24Hours);
  if (!points.length || (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight')) return;
  e.preventDefault();
  const w = chart.viewBox.baseVal.width || 1000, step = (w - 72) / (points.length - 1);
  hoverX = hoverX == null ? w - 16 : hoverX + (e.key === 'ArrowRight' ? step : -step);
  drawPeakChart(points, snapshot);
});
chart.addEventListener('blur', () => { hoverX = null; $('tip').hidden = true; });
document.querySelector('details.data-table').addEventListener('toggle', () => {
  if (snapshot) renderSeriesTable(arr(snapshot.last24Hours));
});

let resizeTimer = 0;
window.addEventListener('resize', () => {
  clearTimeout(resizeTimer);
  resizeTimer = setTimeout(() => { if (snapshot) render(snapshot); }, 120);
});

loadConfig().then(poll);
setInterval(poll, 250);
