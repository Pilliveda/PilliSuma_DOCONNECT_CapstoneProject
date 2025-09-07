// src/app/core/admin.guard.ts
// import { Injectable } from '@angular/core';
// import { CanActivate, CanMatch, Router, UrlTree, Route, UrlSegment } from '@angular/router';
// import { AuthService } from './auth.service';
// import { HttpClient } from '@angular/common/http';
// import { environment } from '../../environments/environment';
// import { catchError, map, of, Observable } from 'rxjs';

// @Injectable({ providedIn: 'root' })
// export class AdminGuard implements CanActivate, CanMatch {
//   constructor(private auth: AuthService, private http: HttpClient, private router: Router) {}

//   private redirect(): UrlTree { return this.router.parseUrl('/questions'); }

//   // 🚫 Do NOT hit any /admin/* endpoint here
//   private isAdmin$(): Observable<boolean | UrlTree> {
//     // Fast local check if you already cache the user
//     const user = this.auth.getUser?.();
//     if (user?.role === 'Admin') return of(true);

//     // Server-authoritative but SAFE: /auth/me returns 200 for any authenticated user
//     return this.http.get<{ role?: string }>(`${environment.apiUrl}/auth/me`).pipe(
//       map(me => me?.role === 'Admin' ? true : this.redirect()),
//       catchError(() => of(this.redirect()))
//     );
//   }

//   canActivate() { return this.isAdmin$(); }
//   canMatch(_r: Route, _s: UrlSegment[]) { return this.isAdmin$(); }
// }
// src/app/core/admin.guard.ts
import { Injectable } from '@angular/core';
import { CanActivate, CanMatch, Router, UrlTree, Route, UrlSegment } from '@angular/router';
import { AuthService } from './auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { catchError, map, of, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AdminGuard implements CanActivate, CanMatch {
  constructor(private auth: AuthService, private http: HttpClient, private router: Router) {}

  private redirect(): UrlTree {
    return this.router.parseUrl('/questions');
  }

  private isAdmin$(): Observable<boolean | UrlTree> {
    // 1. Check local cache first
    const user = this.auth.getUser?.();
    if (user?.role === 'Admin') return of(true);

    // 2. If no token → definitely not admin, skip API call
    const token = this.auth.getToken();
    if (!token) return of(this.redirect());

    // 3. Ask server only if logged in
    return this.http.get<{ role?: string }>(`${environment.apiUrl}/auth/me`).pipe(
      map(me => me?.role === 'Admin' ? true : this.redirect()),
      catchError(() => of(this.redirect()))
    );
  }

  canActivate() { return this.isAdmin$(); }
  canMatch(_r: Route, _s: UrlSegment[]) { return this.isAdmin$(); }
}