import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  template: `
    <header class="page-header">
      <p class="eyebrow">{{ eyebrow() }}</p>
      <h1>{{ title() }}</h1>
      <p class="subheading">{{ subtitle() }}</p>
      @if (meta()) {
        <p class="meta">{{ meta() }}</p>
      }
    </header>
  `
})
export class PageHeaderComponent {
  eyebrow = input('');
  title = input.required<string>();
  subtitle = input('');
  meta = input('');
}