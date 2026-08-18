const $ = id => document.getElementById(id);
const ICONS = { Pv: '☀', HeatPump: '♨', HomeEvCharger: '⚡', BaseLoad: '' };
let running = true, filter = '';

/** Sky colour follows the simulated hour, so the page visibly breathes day/night. */
function skyFor(hour, irradiance) {
  const stops = [
    [0,  '#050813', '#0a1024'], [5,  '#131a3a', '#2a2350'], [7,  '#3b3566', '#8a5a72'],
    [9,  '#2b5f96', '#4a92c4'], [13, '#2f7fc4', '#63b6e8'], [17, '#2b5f96', '#c4794a'],
    [19, '#6b3f63', '#c76a4e'], [21, '#1a1b3d', '#2a2350'], [24, '#050813', '#0a1024'],
  ];
  let a = stops[0], b = stops[stops.length - 1];
  for (let i = 0; i < stops.length - 1; i++)
    if (hour >= stops[i][0] && hour <= stops[i + 1][0]) { a = stops[i]; b = stops[i + 1]; break; }
  const t = (hour - a[0]) / Math.max(0.001, b[0] - a[0]);
  const mix = (x, y) => {
    const p = c => [parseInt(c.slice(1,3),16), parseInt(c.slice(3,5),16), parseInt(c.slice(5,7),16)];
    const [r1,g1,b1] = p(x), [r2,g2,b2] = p(y);
    return `rgb(${Math.round(r1+(r2-r1)*t)},${Math.round(g1+(g2-g1)*t)},${Math.round(b1+(b2-b1)*t)})`;
  };
  const glow = 0.25 + 0.75 * irradiance;
  return `radial-gradient(120% 90% at 50% 0%, ${mix(a[1],b[1])} 0%, ${mix(a[2],b[2])} ${40*glow+25}%, transparent 100%)`;
}

function weatherIcon(hour, cloud, irradiance) {
  const night = irradiance <= 0.01;
  if (night) return cloud > 0.6 ? '☁' : '🌙';
  if (cloud > 0.75) return '☁';
  if (cloud > 0.35) return '⛅';
  return '☀';
}

/** 24h chart: consumption above the axis, generation below, net load as a line. */
function drawChart(points) {
  const svg = $('chart'), W = 1000, H = 260, mid = H / 2;
  if (!points.length) { svg.innerHTML = ''; return; }
  const peak = Math.max(10, ...points.map(p => Math.max(p.consumptionKw, p.generationKw, Math.abs(p.netKw))));
  const x = i => (i / Math.max(1, points.length - 1)) * W;
  const y = v => mid - (v / peak) * (mid - 12);

  const area = (sel, sign) => {
    let d = `M 0 ${mid}`;
    points.forEach((p, i) => { d += ` L ${x(i).toFixed(1)} ${y(sign * sel(p)).toFixed(1)}`; });
    return d + ` L ${W} ${mid} Z`;
  };
  const line = points.map((p, i) => `${i ? 'L' : 'M'} ${x(i).toFixed(1)} ${y(p.netKw).toFixed(1)}`).join(' ');

  const grid = [0.5, 0.25, -0.25, -0.5].map(f =>
    `<line x1="0" y1="${y(peak*f).toFixed(1)}" x2="${W}" y2="${y(peak*f).toFixed(1)}" stroke="#1f2942" stroke-width="1"/>`).join('');

  svg.innerHTML = `
    <defs>
      <linearGradient id="gc" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0%" stop-color="#f97362" stop-opacity=".55"/><stop offset="100%" stop-color="#f97362" stop-opacity="0"/>
      </linearGradient>
      <linearGradient id="gg" x1="0" y1="1" x2="0" y2="0">
        <stop offset="0%" stop-color="#3ddc97" stop-opacity=".55"/><stop offset="100%" stop-color="#3ddc97" stop-opacity="0"/>
      </linearGradient>
    </defs>
    ${grid}
    <path d="${area(p => p.consumptionKw, 1)}" fill="url(#gc)"/>
    <path d="${area(p => p.generationKw, -1)}" fill="url(#gg)"/>
    <line x1="0" y1="${mid}" x2="${W}" y2="${mid}" stroke="#3a4a6b" stroke-width="1"/>
    <path d="${line}" fill="none" stroke="#e8edf7" stroke-width="2" stroke-linejoin="round"/>
    <circle cx="${x(points.length-1).toFixed(1)}" cy="${y(points[points.length-1].netKw).toFixed(1)}" r="4" fill="#e8edf7"/>
    <text x="6" y="14" fill="#8794b0" font-size="11">${peak.toFixed(0)} kW</text>
    <text x="6" y="${H-6}" fill="#8794b0" font-size="11">-${peak.toFixed(0)} kW</text>`;

  const fmt = t => new Date(t).toUTCString().slice(17, 22);
  $('axis').innerHTML = [0, 0.25, 0.5, 0.75, 1]
    .map(f => `<span>${fmt(points[Math.round(f * (points.length - 1))].instant)}</span>`).join('');
}

function render(s) {
  const d = new Date(s.instant), hour = d.getUTCHours() + d.getUTCMinutes() / 60;

  $('sky').style.background = skyFor(hour, s.irradianceFactor);
  $('date').textContent = d.toUTCString().slice(0, 16);
  $('time').textContent = d.toUTCString().slice(17, 22);
  $('season').textContent = s.season;
  $('temp').textContent = s.temperatureC.toFixed(1) + '°C';
  $('cloud').textContent = `${Math.round(s.cloudCover * 100)}% cloud · ${Math.round(s.irradianceFactor * 100)}% sun`;
  $('wxIcon').textContent = weatherIcon(hour, s.cloudCover, s.irradianceFactor);
  $('seed').textContent = s.seed;

  $('net').textContent = s.netPowerKw.toFixed(1);
  $('cons').textContent = s.consumptionKw.toFixed(1);
  $('gen').textContent = s.generationKw.toFixed(1);
  $('imp').textContent = s.totalImportedKwh.toFixed(0);
  $('exp').textContent = s.totalExportedKwh.toFixed(0);
  $('totals').textContent = `${s.totalConsumedKwh.toFixed(0)} / ${s.totalGeneratedKwh.toFixed(0)}`;

  const exporting = s.netPowerKw < 0;
  $('netCard').className = 'stat big ' + (exporting ? 'exporting' : 'importing');
  $('netHint').textContent = exporting
    ? `exporting ${Math.abs(s.netPowerKw).toFixed(1)} kW to the grid`
    : `importing ${s.netPowerKw.toFixed(1)} kW from the grid`;

  drawChart(s.last24Hours);

  $('houses').innerHTML = s.houses.map(h => {
    const cls = h.netPowerKw < 0 ? 'exp' : 'imp';
    const icons = h.assets.filter(a => ICONS[a]).map(a => ICONS[a]).join('');
    return `<div class="house ${cls}">
      <div class="hid">${h.id.replace('house-', 'House ')}</div>
      <div class="hkw">${h.netPowerKw.toFixed(2)}<span style="font-size:9px;color:var(--muted)"> kW</span></div>
      <div class="icons">${icons}</div>
      <div class="hkwh">${h.netKwh.toFixed(1)} kWh net</div>
    </div>`;
  }).join('');

  $('chargers').innerHTML = s.publicChargers.map(c => `
    <div class="charger ${c.busy ? 'busy' : ''}">
      <div class="cid">${c.id.replace('public-charger-', 'Point ')}</div>
      <div class="ckw">${c.powerKw.toFixed(1)}<span style="font-size:9px;color:var(--muted)"> kW</span></div>
      <div class="ckwh">${c.consumedKwh.toFixed(1)} kWh total</div>
    </div>`).join('');

  const rows = s.meters.filter(m => !filter || m.meterId.includes(filter) || m.category.toLowerCase().includes(filter));
  $('meters').tBodies[0].innerHTML = rows.map(m => `<tr>
    <td>${m.meterId}</td><td>${m.ownerId}</td><td>${m.category}</td>
    <td class="num">${m.consumedKwh.toFixed(2)}</td>
    <td class="num gen">${m.generatedKwh ? m.generatedKwh.toFixed(2) : '—'}</td>
    <td class="num">${m.netKwh.toFixed(2)}</td>
    <td class="num">${m.lastPowerKw.toFixed(2)}</td></tr>`).join('');

  running = s.running;
  $('toggle').textContent = running ? 'Pause' : 'Resume';
  $('speedVal').textContent = s.ticksPerSecond + '×';
  if (document.activeElement !== $('speed')) $('speed').value = s.ticksPerSecond;
}

async function poll() {
  try { render(await (await fetch('/api/simulation')).json()); }
  catch { /* transient during reconfiguration */ }
}

$('toggle').onclick = async () => {
  await fetch(running ? '/api/simulation/pause' : '/api/simulation/resume', { method: 'POST' });
  poll();
};
$('filter').oninput = e => { filter = e.target.value.toLowerCase(); };
$('speed').onchange = async e => {
  const cfg = await (await fetch('/api/simulation/configuration')).json();
  cfg.ticksPerSecond = Number(e.target.value);
  await fetch('/api/simulation/configuration', {
    method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cfg),
  });
};

poll();
setInterval(poll, 250);
