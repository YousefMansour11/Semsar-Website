let _openCount = 0;
let _prevOverflow = '';
let _prevPaddingRight = '';

export function lockBodyScroll(): void {
  if (_openCount === 0) {
    _prevOverflow = document.body.style.overflow;
    _prevPaddingRight = document.body.style.paddingRight;
    document.body.style.overflow = 'hidden';
    document.body.style.paddingRight = 'var(--scrollbar-width, 0px)';
  }
  _openCount++;
}

export function unlockBodyScroll(): void {
  _openCount = Math.max(0, _openCount - 1);
  if (_openCount === 0) {
    document.body.style.overflow = _prevOverflow;
    document.body.style.paddingRight = _prevPaddingRight;
  }
}
