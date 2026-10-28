// HUD, menus, story panels and touch controls. Plain DOM over the canvas.
import { view } from './view.js';
const $ = (id) => document.getElementById(id);
const lbl = (long, short) => `<span class="lbl-long">${long}</span><span class="lbl-short">${short}</span>`;
const CAM_LABEL = { '3d': lbl('Kamera 3D', '3D'), '25d': lbl('Kamera 2.5D', '2.5D') };
const wait = (ms) => new Promise(r => setTimeout(r, ms));

export class UI {
  constructor({ isTouch, quality, qKey, sound, moves }) {
    this.onAction = () => { }; this.onHold = () => { }; this.onJoy = () => { }; this.onLook = () => { }; this.onZoom = () => { };
    this.onTitle = () => { }; this.onSelectHero = () => { }; this.onConfirmHero = () => { };
    this.quality = quality; this.qKey = qKey; this.isTouch = isTouch;
    document.body.classList.toggle('touch', isTouch);
    this.bannerT = 0; this.keysT = 0; this.sayT = 0; this.moneyShown = 0;
    const act = (id, a) => { const el = $(id); if (el) el.addEventListener('click', (e) => { e.preventDefault(); this.onAction(a); el.blur(); }); };
    act('bCam', 'cam'); act('bMoves', 'moves'); act('bSnd', 'sound'); act('bIso', 'iso-left'); act('bOrient', 'orient'); act('bFull', 'fullscreen'); act('bTime', 'timefast'); act('bMenu', 'menu'); act('bResume', 'resume');
    $('pause').addEventListener('click', (e) => { if (e.target.id === 'pause') this.onAction('resume'); });
    const d = document; if (d.fullscreenEnabled || d.webkitFullscreenEnabled) $('bFull').hidden = false;
    if (isTouch && !(d.fullscreenEnabled || d.webkitFullscreenEnabled)) $('bOrient').hidden = false;
    $('bQual').addEventListener('click', () => { const ks = Object.keys(this.quality); const n = ks[(ks.indexOf(this.qKey) + 1) % ks.length]; this.onAction('q-' + n); });
    $('movesClose').addEventListener('click', () => this.toggleMoves(false));
    $('moves').addEventListener('click', (e) => { if (e.target.id === 'moves') this.toggleMoves(false); });
    $('btnPlay').addEventListener('click', () => this.onTitle());
    $('shopClose').addEventListener('click', () => this.shop(false));
    $('mapClose').addEventListener('click', () => this.showMap(false));
    $('map').addEventListener('click', (e) => { if (e.target.id === 'map') this.showMap(false); });
    const list = $('movesList');
    for (const [group, items] of moves) {
      const sec = document.createElement('section');
      const h = document.createElement('h3'); h.textContent = group; sec.appendChild(h);
      const grid = document.createElement('div'); grid.className = 'mgrid';
      for (const [id, label, key] of items) {
        const b = document.createElement('button'); b.className = 'mv'; b.type = 'button';
        b.innerHTML = `<span>${label}</span>${key && !isTouch ? `<kbd>${key}</kbd>` : ''}`;
        b.addEventListener('click', () => { this.toggleMoves(false); this.onAction(id); });
        grid.appendChild(b);
      }
      sec.appendChild(grid); list.appendChild(sec);
    }
    this.setSound(sound.on);
    this.initPointer();
    for (const b of document.querySelectorAll('[data-hold]')) {
      const on = (e) => { e.preventDefault(); e.stopPropagation(); b.classList.add('down'); this.onHold(b.dataset.hold, true); };
      const off = () => { b.classList.remove('down'); this.onHold(b.dataset.hold, false); };
      b.addEventListener('pointerdown', on); b.addEventListener('pointerup', off); b.addEventListener('pointercancel', off); b.addEventListener('pointerleave', off);
    }
    for (const b of document.querySelectorAll('[data-act]')) {
      b.addEventListener('pointerdown', (e) => { e.preventDefault(); e.stopPropagation(); b.classList.add('down'); this.onAction(b.dataset.act); });
      const up = () => b.classList.remove('down');
      b.addEventListener('pointerup', up); b.addEventListener('pointercancel', up); b.addEventListener('pointerleave', up);
    }
    if (!isTouch) { this.keysT = 9; if (window.innerHeight < 640) $('keys').classList.add('min'); }
  }

  initPointer() {
    const layer = $('c'); const ptrs = new Map(); const joyEl = $('joy'), knob = $('joyKnob'); const R = 58;
    layer.addEventListener('contextmenu', (e) => e.preventDefault());
    layer.addEventListener('pointerdown', (e) => {
      try { layer.setPointerCapture?.(e.pointerId); } catch { }
      const touch = e.pointerType !== 'mouse'; const L = view.toLocal(e.clientX, e.clientY);
      const role = touch && L.x < view.w() * 0.45 ? 'joy' : 'look';
      ptrs.set(e.pointerId, { role, x0: L.x, y0: L.y, x: L.x, y: L.y });
      if (role === 'joy') { joyEl.style.left = L.x + 'px'; joyEl.style.top = L.y + 'px'; joyEl.classList.add('on'); knob.style.transform = 'translate(-50%,-50%)'; this.onJoy(0, 0, true); $('joyHint').classList.add('gone'); }
    });
    layer.addEventListener('pointermove', (e) => {
      const p = ptrs.get(e.pointerId); if (!p) return; const L = view.toLocal(e.clientX, e.clientY);
      if (p.role === 'joy') { let dx = L.x - p.x0, dy = L.y - p.y0; const l = Math.hypot(dx, dy); if (l > R) { dx *= R / l; dy *= R / l; } knob.style.transform = `translate(calc(-50% + ${dx}px), calc(-50% + ${dy}px))`; this.onJoy(dx / R, -dy / R, true); }
      else this.onLook(L.x - p.x, L.y - p.y);
      p.x = L.x; p.y = L.y;
    });
    const end = (e) => { const p = ptrs.get(e.pointerId); if (!p) return; if (p.role === 'joy') { joyEl.classList.remove('on'); this.onJoy(0, 0, false); } ptrs.delete(e.pointerId); };
    layer.addEventListener('pointerup', end); layer.addEventListener('pointercancel', end);
    layer.addEventListener('wheel', (e) => { e.preventDefault(); this.onZoom(Math.sign(e.deltaY) * 0.08); }, { passive: false });
  }

  // ---------- menus ----------
  showTitle(v) { $('title').hidden = !v; document.body.classList.toggle('menu', v); }
  showSelect(v, heroes) {
    const el = $('select'); el.hidden = !v; document.body.classList.toggle('menu', v);
    if (v && heroes && !this.selBuilt) {
      this.selBuilt = true;
      const wrap = $('heroCards');
      for (const h of Object.values(heroes)) {
        const c = document.createElement('button'); c.type = 'button'; c.className = 'hero'; c.dataset.k = h.key;
        c.innerHTML = `<b>${h.name}</b><em>${h.title}</em><p>${h.desc}</p>` +
          Object.entries(h.stats).map(([k, v]) => `<div class="stat"><span>${k}</span><i style="--v:${v * 10}%"></i></div>`).join('') +
          `<small>Specjal: ${h.specialNames.join(' · ')}</small>`;
        c.addEventListener('click', () => { if (this.selKey === h.key) this.onConfirmHero(h.key); else this.onSelectHero(h.key); });
        wrap.appendChild(c);
      }
      $('btnFight').addEventListener('click', () => this.onConfirmHero(this.selKey));
    }
  }
  selectHero(k) { this.selKey = k; for (const c of document.querySelectorAll('.hero')) c.classList.toggle('on', c.dataset.k === k); $('btnFight').textContent = 'Walcz jako ' + (k === 'brax' ? 'Brax' : 'Nyxa'); }
  async story(panels) {
    const el = $('story'); el.hidden = false; document.body.classList.add('menu');
    for (let i = 0; i < panels.length; i++) {
      const p = panels[i];
      el.querySelector('h2').textContent = p.t; el.querySelector('p').textContent = p.s;
      el.querySelector('.count').textContent = `${i + 1} / ${panels.length}`;
      el.classList.remove('in'); void el.offsetWidth; el.classList.add('in');
      const r = await new Promise(res => { const n = $('storyNext'), s = $('storySkip'); const a = () => { cl(); res('next'); }, b = () => { cl(); res('skip'); }; const cl = () => { n.removeEventListener('click', a); s.removeEventListener('click', b); }; n.addEventListener('click', a); s.addEventListener('click', b); });
      if (r === 'skip') break;
    }
    el.hidden = true; document.body.classList.remove('menu');
  }
  async chapter(c) {
    const el = $('chapter'); el.querySelector('small').textContent = c.n; el.querySelector('strong').textContent = c.t; el.querySelector('span').textContent = c.s;
    el.classList.add('on'); await wait(2600); el.classList.remove('on'); await wait(400);
  }
  async fade(on) { const el = $('fade'); el.classList.toggle('on', on); await wait(450); }
  loading(on, msg = '') { const el = $('busy'); el.hidden = !on; el.querySelector('span').textContent = msg; }
  async ending(panels, st) {
    const el = $('ending'); el.hidden = false; document.body.classList.add('menu');
    for (const p of panels) { el.querySelector('h2').textContent = p.t; el.querySelector('p').textContent = p.s; el.classList.remove('in'); void el.offsetWidth; el.classList.add('in'); await wait(4200); }
    el.querySelector('h2').textContent = 'KONIEC'; el.querySelector('p').textContent = `${st.hero} oczyścił(a) Braxhorn City. Zarobione: $${st.money.toLocaleString('pl-PL')} · Pokonani: ${st.kills}`;
    $('endAgain').hidden = false;
    await new Promise(r => $('endAgain').addEventListener('click', r, { once: true }));
  }

  // ---------- HUD ----------
  setHero(h, partner) { $('meName').textContent = h.name; $('meTag').textContent = h.title || ''; this.partner = partner.name; $('oppName').textContent = partner.name; $('oppTag').textContent = 'Sparing'; document.body.classList.add('playing'); $('padSpecial').textContent = h.key === 'nyxa' ? 'MARTELO' : 'KOLANO'; }
  hp(which, f) {
    const k = which === 'me' ? 'Me' : 'Opp', el = $('hp' + k), gh = $('hp' + k + 'Ghost'), w = (Math.max(0, Math.min(1, f)) * 100).toFixed(1) + '%';
    const prev = this['hp' + k] ?? 1; this['hp' + k] = f;
    if (el.style.width === w) return;
    el.style.width = w;
    if (gh) { if (f >= prev) { gh.style.transition = 'none'; gh.style.width = w; void gh.offsetWidth; gh.style.transition = ''; } else gh.style.width = w; }
    el.parentElement.classList.toggle('low', which === 'me' && f < 0.3);
  }
  bossHp(f) { this.hp('opp', f); }
  boss(name, title) {
    const card = $('oppCard');
    if (name) { $('oppName').textContent = name; $('oppTag').textContent = title; card.classList.add('boss', 'on'); }
    else { card.classList.remove('boss', 'on'); if (this.partner) { $('oppName').textContent = this.partner; $('oppTag').textContent = 'Sparing'; } }
    this.bossOn = !!name;
  }
  showOpp(v) { if (!this.bossOn) $('oppCard').classList.toggle('on', !!v); }
  showPause(v, hint = '') { $('pause').hidden = !v; document.body.classList.toggle('menu', v); $('pauseHint').textContent = hint; if (!v) this.toggleMoves(false); }
  money(v, delta) {
    $('money').textContent = '$' + Math.round(v).toLocaleString('pl-PL');
    if (delta) { const p = $('moneyPop'); p.textContent = '+$' + Math.round(delta); p.classList.remove('on'); void p.offsetWidth; p.classList.add('on'); }
  }
  clock(s, night) { $('clock').textContent = s; $('clockIcon').textContent = night ? '☾' : '☀'; }
  setTimeFast(v) { $('bTime').classList.toggle('main', v); $('bTime').innerHTML = v ? lbl('Czas ×30', '×30') : lbl('Przyspiesz czas', 'Czas'); }
  objective(t) { const el = $('objective'); el.textContent = t || ''; el.classList.toggle('on', !!t); this.objT = t ? 10 : 0; }
  say(name, text, dur = 3) { const el = $('say'); el.querySelector('b').textContent = name; el.querySelector('b').hidden = !name; el.querySelector('span').textContent = text; el.classList.add('on'); this.sayT = dur; }
  shakes(n) { this.nShakes = n; const s = $('shakeBadge'); s.hidden = !(n > 0); s.textContent = 'SHAKE ×' + n; const b = $('padShake'); if (b) b.textContent = 'SHAKE'; }
  shakeNeeded(v) { const b = $('padShake'); if (b) b.hidden = !(v && this.nShakes > 0); }
  shop(open, items, upg, money) {
    const el = $('shop'); el.hidden = !open; if (!open) return;
    $('shopMoney').textContent = '$' + Math.round(money).toLocaleString('pl-PL');
    const list = $('shopList'); list.innerHTML = '';
    for (const it of items) {
      const lvl = it.id === 'bat' ? '' : `<span class="lvl">${upg[it.id] || 0}/${it.max}</span>`;
      const b = document.createElement('button'); b.type = 'button'; b.className = 'item';
      b.innerHTML = `<b>${it.name}</b>${lvl}<p>${it.desc}</p><span class="price">$${it.price}</span>`;
      b.addEventListener('click', () => this.onBuy && this.onBuy(it.id));
      list.appendChild(b);
    }
  }
  showMap(open, items = [], here = '') {
    const el = $('map'); el.hidden = !open; document.body.classList.toggle('menu', open && true);
    if (!open) { if ($('pause').hidden) document.body.classList.remove('menu'); return; }
    const list = $('mapList'); list.innerHTML = '';
    items.forEach((it, i) => {
      const b = document.createElement('button'); b.type = 'button'; b.className = 'dest' + (it.done ? ' done' : '') + (it.key === here ? ' here' : '');
      b.innerHTML = `<b>${it.title}</b><p>${it.sub}</p><span class="tag">${it.tag || ''}</span><i>${String(i + 1).padStart(2, '0')}</i>`;
      if (it.key !== here) b.addEventListener('click', () => this.onTravel && this.onTravel(it.key));
      list.appendChild(b);
    });
  }
  weapon(name, n) { const el = $('weapon'); el.hidden = !name; if (name) el.textContent = `${name} · ${n}`; }
  action(label) { const b = $('btnAct'); if (b) { b.hidden = !label; if (label) b.textContent = label; } const h = $('actHint'); if (h) { h.classList.toggle('on', !!label && !this.isTouch); if (label) h.innerHTML = `<kbd>O</kbd> ${label.toLowerCase()}`; } }
  combo(n) {
    const el = $('combo');
    if (n < 2) { el.classList.remove('on'); return; }
    el.querySelector('b').textContent = n; el.classList.remove('pop'); void el.offsetWidth; el.classList.add('on', 'pop');
  }
  flash() { }
  banner(title, sub, dur = 2.6) {
    const el = $('banner'); el.querySelector('strong').textContent = title || ''; el.querySelector('span').textContent = sub || '';
    el.classList.toggle('notitle', !title); el.classList.add('on'); this.bannerT = dur;
  }
  setCam(m) { $('bCam').innerHTML = CAM_LABEL[m]; $('bIso').hidden = m !== '25d'; }
  setQuality(k) { this.qKey = k; $('bQual').innerHTML = lbl('Jakość: ', '') + this.quality[k].name; }
  setSound(on) { $('bSnd').innerHTML = on ? lbl('Dźwięk: wł.', 'Dźwięk') : lbl('Dźwięk: wył.', 'Cisza'); }
  setOrient(landscape) { $('bOrient').textContent = landscape ? 'Pionowo' : 'Poziomo'; }
  throwReady(v) { $('btnThrow').classList.toggle('ready', v); }
  toggleMoves(force) { const m = $('moves'); const open = force ?? m.hidden; m.hidden = !open; }
  toggleKeys() { const k = $('keys'); k.classList.toggle('min'); this.keysT = 0; }
  hideKeysSoon() { if (this.keysT > 4) this.keysT = 4; }
  tick(dt) {
    if (this.bannerT > 0) { this.bannerT -= dt; if (this.bannerT <= 0) $('banner').classList.remove('on'); }
    if (this.sayT > 0) { this.sayT -= dt; if (this.sayT <= 0) $('say').classList.remove('on'); }
    if (this.objT > 0) { this.objT -= dt; if (this.objT <= 0) $('objective').classList.remove('on'); }
    if (this.keysT > 0) { this.keysT -= dt; if (this.keysT <= 0) $('keys').classList.add('min'); }
  }
}
