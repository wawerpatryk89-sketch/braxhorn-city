// Screen orientation helper. On phones held upright the whole game can be turned 90° in CSS
// ("landscape inside a portrait app"), so the Claude app does not need to rotate.
export const view = {
  rotated: false,
  w() { return this.rotated ? window.innerHeight : window.innerWidth; },
  h() { return this.rotated ? window.innerWidth : window.innerHeight; },
  toLocal(cx, cy) { return this.rotated ? { x: cy, y: window.innerWidth - cx } : { x: cx, y: cy }; },
};
export function applyOrientation(prefLandscape, isTouch) {
  const portrait = window.innerHeight > window.innerWidth;
  view.rotated = !!(isTouch && prefLandscape && portrait);
  const b = document.body, r = document.documentElement;
  b.classList.toggle('rot', view.rotated);
  r.style.setProperty('--appW', view.w() + 'px');
  r.style.setProperty('--appH', view.h() + 'px');
  b.classList.toggle('compact', !!(isTouch && view.w() > view.h() && view.h() < 600));
  b.classList.toggle('slim', !!(isTouch && view.w() <= view.h()));
  return view.rotated;
}
