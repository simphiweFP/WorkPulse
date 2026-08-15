import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, catchError, firstValueFrom, map, of, tap } from 'rxjs';
import { apiConfig } from '../services/api.config';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenStorageKey = 'workpulse_token';
  private readonly userStorageKey = 'workpulse_user';
  private readonly currentUserSubject = new BehaviorSubject<User | null>(null);

  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private readonly http: HttpClient) {
    this.restoreSession();
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${apiConfig.authBaseUrl}/register`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${apiConfig.authBaseUrl}/login`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.userStorageKey);
    this.currentUserSubject.next(null);
  }

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${apiConfig.authBaseUrl}/me`).pipe(
      tap((user) => {
        this.currentUserSubject.next(user);
        localStorage.setItem(this.userStorageKey, JSON.stringify(user));
      })
    );
  }

  isAuthenticated(): Observable<boolean> {
    return this.currentUser$.pipe(map(() => !!this.getToken()));
  }

  isAuthenticatedSnapshot(): boolean {
    return !!this.getToken();
  }

  hasRole(role: string): Observable<boolean> {
    return this.currentUser$.pipe(map((user) => user?.role === role));
  }

  getCurrentUserSnapshot(): User | null {
    return this.currentUserSubject.value;
  }

  initializeAuth(): Promise<void> {
    if (!this.getToken()) {
      return Promise.resolve();
    }

    this.restoreStoredUser();

    return firstValueFrom(
      this.getCurrentUser().pipe(
        catchError(() => {
          const storedUser = this.getStoredUser();
          if (storedUser) {
            this.currentUserSubject.next(storedUser);
            return of(storedUser);
          }

          this.logout();
          return of(null);
        }),
        map(() => void 0)
      )
    ).then(() => void 0);
  }

  getFullName(user: User | null | undefined): string {
    if (!user) {
      return '';
    }

    const fullName = [user.firstName, user.lastName]
      .map((value) => value?.trim())
      .filter((value): value is string => !!value)
      .join(' ')
      .trim();

    return fullName || user.email || '';
  }

  getInitials(user: User | null | undefined): string {
    if (!user) {
      return 'WP';
    }

    const first = user.firstName?.trim()?.[0] ?? '';
    const last = user.lastName?.trim()?.[0] ?? '';
    const initials = `${first}${last}`.toUpperCase();

    return initials || 'WP';
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  private restoreSession(): void {
    this.restoreStoredUser();

    if (!this.getToken()) {
      return;
    }

    this.getCurrentUser()
      .pipe(
        catchError(() => {
          const storedUser = this.getStoredUser();
          if (storedUser) {
            this.currentUserSubject.next(storedUser);
            return of(storedUser);
          }

          this.logout();
          return of(null);
        })
      )
      .subscribe();
  }

  private restoreStoredUser(): void {
    const storedUser = this.getStoredUser();
    if (storedUser) {
      this.currentUserSubject.next(storedUser);
    }
  }

  private getStoredUser(): User | null {
    const raw = localStorage.getItem(this.userStorageKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }

  private setSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenStorageKey, response.token);
    localStorage.setItem(this.userStorageKey, JSON.stringify(response.user));
    this.currentUserSubject.next(response.user);
  }
}
