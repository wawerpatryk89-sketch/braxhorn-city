// Consume elapsed game time in bounded physics steps. A slow rendered frame must
// not turn walking into slow motion, while a suspended tab must not teleport us.
export function advanceFrame(elapsed, tick) {
  const duration = Math.max(0, Math.min(Number.isFinite(elapsed) ? elapsed : 0, 0.2));
  if (!duration) return 0;
  const count = Math.ceil(duration / (1 / 30));
  for (let i = 0; i < count; i++) tick(duration / count);
  return duration;
}
