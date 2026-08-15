import { Component, input } from '@angular/core';

@Component({
  selector: 'app-error-state',
  standalone: true,
  templateUrl: './error-state.component.html'
})
export class ErrorStateComponent {
  title = input('Something went wrong.');
  message = input('Please try again.');
  actionLabel = input('Retry');
  retry = input<() => void>(() => void 0);
}
