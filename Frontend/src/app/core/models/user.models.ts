export interface CurrentUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: 'Admin' | 'Developer' | string;
}