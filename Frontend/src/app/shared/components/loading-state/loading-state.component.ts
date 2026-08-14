import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  standalone: true,
  template: `<p class="loading">{{ message() }}</p>`
})
export class LoadingStateComponent {
  message = input('Loading...');
}