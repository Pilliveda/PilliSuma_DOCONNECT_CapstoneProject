import { Injectable } from '@angular/core';
import {
  HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from './auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(private router: Router, private snack: MatSnackBar, private auth: AuthService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((err: HttpErrorResponse) => {
        if (err.status === 401) {
          this.auth.logout();
          this.snack.open('Session expired. Please log in again.', 'ok', { duration: 2000 });
          this.router.navigate(['/auth/login']);
        } else if (err.status === 403) {
          this.snack.open('Access denied', 'ok', { duration: 2000 });
        } else if (err.error?.message) {
          this.snack.open(err.error.message, 'ok', { duration: 2000 });
        }
        return throwError(() => err);
      })
    );
  }
}
