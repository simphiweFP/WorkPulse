import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  templateUrl: './empty-state.component.html'
})
export class EmptyStateComponent {
  title = input('You are clear for today.');
  message = input('No urgent or upcoming tasks need your attention.');
}
