import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { QuestionService, AnswerDto, QuestionDto } from '../../../core/question.service';
import { AdminService } from '../../../core/admin.service';
import { AuthService } from '../../../core/auth.service';
import { debounceTime, firstValueFrom } from 'rxjs';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-question-list',
  standalone: true,
  imports: [
    CommonModule, RouterModule, MatCardModule, MatButtonModule,
    MatInputModule, ReactiveFormsModule, MatListModule,
    MatPaginatorModule, MatIconModule, MatSnackBarModule
  ],
  templateUrl: './list.component.html',
  styleUrls: ['./list.component.scss']
})
export class ListComponent implements OnInit {
  search = new FormControl('');
  questions: any[] = [];
  total = 0;
  page = 1;
  pageSize = 5;
  isAdmin = false;

  /** Lightbox viewer url */
  viewerUrl: string | null = null;

  /**
   * Cache of computed question-only thumbnails:
   * key: question id -> value: image path (relative or absolute)
   */
  private thumbCache = new Map<string, string | null>();
  /** Prevent duplicate detail fetches per question id */
  private thumbRequested = new Set<string>();

  constructor(
    private qs: QuestionService,
    private admin: AdminService,
    private auth: AuthService,
    private snack: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  async ngOnInit() {
    this.isAdmin = this.checkIsAdminLocal();
    try {
      const ok = await firstValueFrom(this.admin.checkAdmin());
      if (ok) this.isAdmin = true;
    } catch { /* ignore */ }

    this.load();
    this.search.valueChanges.pipe(debounceTime(300))
      .subscribe(() => { this.page = 1; this.load(); });
  }

  load() {
    this.qs.getQuestions(this.search.value || '', this.page, this.pageSize)
      .subscribe((res: any) => {
        this.questions = res.items || [];
        this.total = res.total ?? this.questions.length;

        // Warm the thumb cache for visible items (non-blocking)
        for (const q of this.questions) this.ensureThumb(q);
      });
  }

  /**
   * Returns a URL to use for the card thumbnail.
   * Tries cache first. If absent and not requested yet, kicks off a detail fetch
   * that computes question-only images and updates the cache (UI updates after).
   */
  thumbUrl(q: any): string {
    const id = q?.id;
    if (!id) return '';

    const cached = this.thumbCache.get(id);
    if (typeof cached !== 'undefined') {
      return cached ? this.asUrl(cached) : '';
    }

    // If the list already has images, use its first as a temporary fallback
    // (it will be replaced after the detail fetch completes).
    const fallback = (q?.images?.[0] as string) || '';
    this.thumbCache.set(id, fallback || null);

    // Start lazy fetch if not already in-flight
    this.ensureThumb(q);

    return fallback ? this.asUrl(fallback) : '';
  }

  /** Fire a one-time detail fetch and compute the question-only thumbnail */
  private ensureThumb(q: any) {
    const id = q?.id;
    if (!id || this.thumbRequested.has(id)) return;
    this.thumbRequested.add(id);

    this.qs.getQuestion(id).subscribe((res: any) => {
      let question: QuestionDto;
      let answers: AnswerDto[] = [];

      if (res?.question) {
        question = res.question as QuestionDto;
        answers = res.answers || [];
      } else {
        question = res as QuestionDto;
      }

      // If answers not included, try separate fetch (best effort)
      const computeAndStore = (ans: AnswerDto[]) => {
        const qImgs = (question?.images || []) as string[];
        const ansImgs = new Set((ans || []).flatMap(a => (a?.images || []) as string[]));
        // Keep only images that are not present in any answer
        const filtered = qImgs.filter(p => !ansImgs.has(p));
        const chosen = (filtered[0] ?? qImgs[0] ?? null);
        this.thumbCache.set(id, chosen);
        this.cdr.markForCheck();
      };

      if (answers.length) {
        computeAndStore(answers);
      } else {
        this.qs.getAnswers(id).subscribe(a => {
          computeAndStore(a || []);
        }, () => {
          // If answers fetch fails, just keep what we have
          computeAndStore([]);
        });
      }
    }, () => {
      // If detail call fails, leave whatever is in cache
    });
  }

  asUrl(path: string): string {
    if (!path) return '';
    if (path.startsWith('http')) return path;
    const rel = path.startsWith('/') ? path : `/${path}`;
    return `${environment.apiOrigin}${rel}`;
  }

  onPage(e: PageEvent) { this.page = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }
  onSearchClick() { this.page = 1; this.load(); }

  onDelete(id: string, title: string) {
    if (!this.isAdmin) return;

    const ok = confirm(`Delete this question?\n\n${title}`);
    if (!ok) return;

    this.admin.deleteQuestion(id).subscribe({
      next: () => {
        this.questions = this.questions.filter(q => q.id !== id);
        this.total = Math.max(0, this.total - 1);
        this.thumbCache.delete(id);
        this.thumbRequested.delete(id);
        this.snack.open('Question deleted', 'ok', { duration: 1500 });
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 401 || err.status === 403) {
          this.isAdmin = false;
          return;
        }
        console.error(err);
        this.snack.open('Failed to delete question', 'ok', { duration: 2000 });
      }
    });
  }

  private checkIsAdminLocal(): boolean {
    try {
      const token = (this.auth as any).currentToken?.() ?? (this.auth as any).currentToken;
      if (!token) return false;
      const payload: any = jwtDecode(token);
      let roleClaim =
        payload?.role ??
        payload?.roles ??
        payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (typeof roleClaim === 'string' && roleClaim.trim().startsWith('[')) {
        try { roleClaim = JSON.parse(roleClaim); } catch {}
      }
      const roles: string[] = Array.isArray(roleClaim) ? roleClaim : roleClaim ? [roleClaim] : [];
      return roles.map(r => String(r).toLowerCase()).includes('admin');
    } catch { return false; }
  }

  /* ===== Lightbox controls ===== */
  openViewer(url: string) {
    this.viewerUrl = url;
    document.body.style.overflow = 'hidden';
  }

  closeViewer() {
    this.viewerUrl = null;
    document.body.style.overflow = '';
  }

  @HostListener('document:keydown.escape')
  onEsc() {
    if (this.viewerUrl) this.closeViewer();
  }
}