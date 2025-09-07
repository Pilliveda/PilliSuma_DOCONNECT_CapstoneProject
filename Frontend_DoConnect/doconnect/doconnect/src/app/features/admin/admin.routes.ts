// src/app/features/admin/admin.routes.ts
import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';

export const ADMIN_ROUTES: Routes = [
  { path: '', redirectTo: 'pending', pathMatch: 'full' },
  { path: 'pending', component: DashboardComponent },
];
