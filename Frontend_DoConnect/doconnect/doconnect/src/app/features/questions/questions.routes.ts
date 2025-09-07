import { Routes } from '@angular/router';
import { ListComponent } from './list/list.component';
import { DetailComponent } from './detail/detail.component';
import { AskComponent } from './ask/ask.component';

export const QUESTIONS_ROUTES: Routes = [
  { path: '', component: ListComponent },
  { path: 'ask', component: AskComponent },
  { path: ':id', component: DetailComponent },
];
