export interface NavigationItem {
  label: string;
  path: string;
}

export const authenticatedNavigation: NavigationItem[] = [
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Clients', path: '/clients' },
  { label: 'Projects', path: '/projects' },
  { label: 'Tasks', path: '/tasks' },
  { label: 'My Tasks', path: '/my-tasks' },
  { label: 'Users', path: '/users' }
];