
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { QuestionService } from '../../../core/question.service';
import { AdminService } from '../../../core/admin.service';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  standalone: true,
  selector: 'app-ask',
  imports: [
    CommonModule, ReactiveFormsModule, MatCardModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatSnackBarModule
  ],
  templateUrl: './ask.component.html',
  // styles: [`.full{width:100%}`]
  styleUrls: ['./ask.component.scss']
  
})
export class AskComponent {
  f: ReturnType<FormBuilder['group']>;
  files: File[] = [];

  constructor(
    private fb: FormBuilder,
    private qs: QuestionService,
    private admin: AdminService,
    private router: Router,
    private snack: MatSnackBar
  ) {
    this.f = this.fb.group({ title: ['', Validators.required], body: ['', Validators.required] });
  }

  onFiles(e: Event) {
    const el = e.target as HTMLInputElement;
    this.files = el.files ? Array.from(el.files) : [];
  }

  submit() {
    if (this.f.invalid) return;
    const { title, body } = this.f.value as { title: string; body: string };

    // Build one FormData payload both endpoints accept
    const fd = new FormData();
    fd.append('Title', title);
    fd.append('Text', body);
    this.files.forEach(f => fd.append('Files', f, f.name));

    // 1) Try admin endpoint (auto-approved)
    this.admin.createQuestionForm(fd).subscribe({
      next: () => {
        this.snack.open('Question posted (approved)', 'ok', { duration: 1400 });
        this.router.navigate(['/questions']);
      },
      error: (err: HttpErrorResponse) => {
        // 2) If forbidden/unauthorized, fallback to user endpoint (pending)
        if (err.status === 401 || err.status === 403) {
          this.qs.createQuestion({ title, body }, this.files).subscribe({
            next: () => {
              this.snack.open('Question posted (pending review)', 'ok', { duration: 1600 });
              this.router.navigate(['/questions']);
            },
            error: e2 => {
              console.error(e2);
              this.snack.open('Failed to post question', 'ok', { duration: 2000 });
            }
          });
        } else {
          console.error(err);
          this.snack.open('Failed to post question', 'ok', { duration: 2000 });
        }
      }
    });
  }
}
