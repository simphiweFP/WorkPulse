import { Component } from '@angular/core';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  template: `
    <section class="screen">
      <div class="panel">
        <h1>Access Denied</h1>
        <p>You do not have permission to view this page.</p>
      </div>
    </section>
  `
})
export class AccessDeniedComponent {}