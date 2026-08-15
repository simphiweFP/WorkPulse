import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig).catch(() => {
  document.body.replaceChildren();
  const message = document.createElement('p');
  message.style.padding = '16px';
  message.style.fontFamily = 'sans-serif';
  message.textContent = 'WorkPulse could not start. Please refresh the page.';
  document.body.appendChild(message);
});
