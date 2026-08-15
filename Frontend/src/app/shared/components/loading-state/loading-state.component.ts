import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  standalone: true,
  templateUrl: './loading-state.component.html'
})
export class LoadingStateComponent {
  message = input('Loading...');
}
