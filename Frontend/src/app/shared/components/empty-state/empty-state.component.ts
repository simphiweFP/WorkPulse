import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <section class="empty-state">
      <h2>{{ title() }}</h2>
      <p>{{ message() }}</p>
    </section>
  `
})
export class EmptyStateComponent {
  title = input('You are clear for today.');
  message = input('No urgent or upcoming tasks need your attention.');
}