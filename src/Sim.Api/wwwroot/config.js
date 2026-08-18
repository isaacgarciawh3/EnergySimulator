/* ---------------------------------------------------------------------------
   Page 2 of 2 - the configuration editor.

   GET  /api/simulation/configuration -> populate the form
   PUT  /api/simulation/configuration <- the WHOLE configuration record

   The server clamps everything it receives and returns what it actually stored,
   so the form is repopulated from the response rather than from what was typed:
   if a value was clamped, the page shows the clamped value.
--------------------------------------------------------------------------- */
'use strict';

const $ = id => document.getElementById(id);
const num = v => (typeof v === 'number' && Number.isFinite(v)) ? v : 0;

/** field id -> [min, max]. Mirrors SimulationConfiguration.Validated() exactly. */
const LIMITS = {
  tickMinutes: [1, 60],
  ticksPerSecond: [0.5, 240],
  batteryCapacityKwh: [0, 100000],
  batteryMaxPowerKw: [0, 10000],
  peakShavingThresholdKw: [0, 100000],
};

const PERCENT_FIELDS = ['pvShare', 'heatPumpShare', 'homeEvShare', 'batteryRoundTripEfficiency'];

let loaded = null;   // the last configuration the server confirmed

/* --- instant <-> datetime-local ----------------------------------------- */
function toInput(iso) {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '';
  const p = n => String(n).padStart(2, '0');
  return `${d.getUTCFullYear()}-${p(d.getUTCMonth() + 1)}-${p(d.getUTCDate())}T${p(d.getUTCHours())}:${p(d.getUTCMinutes())}`;
}
function fromInput(value) {
  if (!value) return null;
  const d = new Date(value + ':00Z');   // the field is explicitly labelled UTC
  return Number.isNaN(d.getTime()) ? null : d.toISOString();
}

/* --- form <-> record ----------------------------------------------------- */
function populate(cfg) {
  loaded = cfg;
  $('seed').value = num(cfg.seed);
  $('startInstant').value = toInput(cfg.startInstant);
  $('tickMinutes').value = num(cfg.tickMinutes);
  $('ticksPerSecond').value = num(cfg.ticksPerSecond);
  $('batteryCapacityKwh').value = num(cfg.batteryCapacityKwh);
  $('batteryMaxPowerKw').value = num(cfg.batteryMaxPowerKw);
  $('peakShavingThresholdKw').value = num(cfg.peakShavingThresholdKw);
  $('batteryEnabled').checked = !!cfg.batteryEnabled;
  PERCENT_FIELDS.forEach(id => { $(id).value = Math.round(num(cfg[id]) * 100); syncOutput(id); });
  syncBatteryEnabled();
  clearErrors();
}

function collect() {
  const errors = [];
  const fail = (id, message) => { errors.push(id); $(id + 'Err').textContent = message; };
  clearErrors();

  const seed = Number($('seed').value);
  if (!Number.isFinite(seed) || !Number.isInteger(seed)) fail('seed', 'Whole number required.');

  const startInstant = fromInput($('startInstant').value);
  if (!startInstant) fail('startInstant', 'Pick a valid date and time.');

  const plain = {};
  for (const id of Object.keys(LIMITS)) {
    const v = Number($(id).value);
    const [lo, hi] = LIMITS[id];
    if (!Number.isFinite(v)) fail(id, 'Number required.');
    else if (v < lo || v > hi) fail(id, `Must be between ${lo} and ${hi}. The server would clamp it.`);
    plain[id] = v;
  }

  if (errors.length) { $(errors[0]).focus(); return null; }

  // Spread the record the server last confirmed, then override only the fields
  // this page edits. The PUT is a whole-record replace, so anything the API
  // sends that this form does not show (tickDuration today, whatever the API
  // grows tomorrow) is echoed back untouched instead of being silently dropped.
  return {
    ...loaded,
    seed,
    startInstant,
    tickMinutes: Math.round(plain.tickMinutes),
    ticksPerSecond: plain.ticksPerSecond,
    pvShare: Number($('pvShare').value) / 100,
    heatPumpShare: Number($('heatPumpShare').value) / 100,
    homeEvShare: Number($('homeEvShare').value) / 100,
    batteryCapacityKwh: plain.batteryCapacityKwh,
    batteryMaxPowerKw: plain.batteryMaxPowerKw,
    batteryRoundTripEfficiency: Number($('batteryRoundTripEfficiency').value) / 100,
    peakShavingThresholdKw: plain.peakShavingThresholdKw,
    batteryEnabled: $('batteryEnabled').checked,
  };
}

function clearErrors() {
  document.querySelectorAll('.err').forEach(e => { e.textContent = ''; });
}

/* --- small interactions -------------------------------------------------- */
function syncOutput(id) { $(id + 'Out').textContent = $(id).value + ' %'; }
PERCENT_FIELDS.forEach(id => { $(id).addEventListener('input', () => syncOutput(id)); });

function syncBatteryEnabled() {
  const on = $('batteryEnabled').checked;
  $('batteryFields').style.opacity = on ? '1' : '.45';
  $('batteryFields').querySelectorAll('input').forEach(i => { i.disabled = !on; });
}
$('batteryEnabled').addEventListener('change', syncBatteryEnabled);

$('randomise').addEventListener('click', () => {
  $('seed').value = Math.floor(Math.random() * 2000000000) + 1;
  state('Seed randomised. Nothing is saved until you press "Save and restart".', '');
});

function state(text, kind) {
  const el = $('saveState');
  el.textContent = text;
  el.className = 'save-state' + (kind ? ' ' + kind : '');
}

/* --- load / save --------------------------------------------------------- */
async function load() {
  try {
    const r = await fetch('/api/simulation/configuration');
    if (!r.ok) throw new Error('HTTP ' + r.status);
    populate(await r.json());
    state('', '');
  } catch (e) {
    state('Could not read the configuration: ' + e.message, 'bad');
  }
}

$('form').addEventListener('submit', async e => {
  e.preventDefault();
  const body = collect();
  if (!body) { state('Fix the highlighted fields first. Nothing was sent.', 'bad'); return; }

  $('save').disabled = true;
  state('Saving...', '');
  try {
    const r = await fetch('/api/simulation/configuration', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!r.ok) throw new Error('HTTP ' + r.status);
    populate(await r.json());
    state(`Saved. The simulation restarted from seed ${loaded.seed}, with the ledger and both peak counters back at zero.`, 'ok');
  } catch (err) {
    state('Save failed: ' + err.message + '. Nothing changed.', 'bad');
  } finally {
    $('save').disabled = false;
  }
});

$('revert').addEventListener('click', () => {
  if (loaded) { populate(loaded); state('Reverted to the configuration currently running.', ''); }
  else load();
});

load();
