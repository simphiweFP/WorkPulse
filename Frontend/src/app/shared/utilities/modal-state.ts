export function lockBodyScroll(): void {
  document.body.style.overflow = 'hidden';
}

export function unlockBodyScroll(): void {
  document.body.style.overflow = '';
}
