let _interacted = false;
let _interactionTime = 0;

export function onInteraction(): void {
  if (!_interacted) {
    _interacted = true;
    _interactionTime = Date.now();
  }
}

export function getInteractionTimestamp(): number {
  return _interactionTime;
}

export function resetInteraction(): void {
  _interacted = false;
  _interactionTime = 0;
}

let _hpBaseSeed: number | null = null;
let _hpCallCount = 0;
function cryptoSeed(): number {
  const buf = new Uint32Array(1);
  window.crypto.getRandomValues(buf);
  return buf[0] >>> 0;
}
export function getHoneypotField(): { name: string; value: string; seed: number } {
  if (_hpBaseSeed === null) {
    _hpBaseSeed = cryptoSeed();
  }
  _hpCallCount++;
  const seed = (_hpBaseSeed ^ Math.floor(Date.now() / (1000 * 60 * 60))) + _hpCallCount;
  const hash = ((seed * 9301 + 49297) % 233280) & 0xffff;
  return { name: `hp_${hash.toString(36)}`, value: '', seed };
}
