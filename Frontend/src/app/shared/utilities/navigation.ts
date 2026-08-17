export interface NavigationItem {
  label: string;
  path: string;
}

// Keep Today as the first destination for authenticated users.
export const authenticatedNavigation: NavigationItem[] = [
  { label: 'Today', path: '/today' },
  { label: 'My Tasks', path: '/my-tasks' },
  { label: 'Clients', path: '/clients' },
  { label: 'Projects', path: '/projects' },
  { label: 'Tasks', path: '/tasks' },
  { label: 'Backlog', path: '/backlog' },
  { label: 'Sprints', path: '/sprints' },
  { label: 'Team', path: '/team' },
  { label: 'Profile', path: '/profile' }
];
