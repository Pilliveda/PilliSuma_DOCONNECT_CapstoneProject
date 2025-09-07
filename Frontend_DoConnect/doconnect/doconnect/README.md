# DoConnect – Frontend (Angular)

Angular app for the **DoConnect** Q&A platform. It implements questions list/detail/ask,
authentication with JWT, admin actions (guarded), image upload + lightbox, and an **Ask from AI**
experience (dialog or dedicated page) that supports plain text, images (Vision) and files
(PDF/DOC/CSV) for grounded answers.

> The backend base URL is configured in `src/environments/environment.ts`.

---

## Features

- Questions: list (search + pagination), detail, ask
- Answers: view & post (with images)
- Auth: login/register, JWT storage, role-aware UI (admin)
- Admin: delete/approve (guarded via role)
- Media: image uploads, absolute URL builder, lightbox
- **AI**: ask via dialog or `/ai` page; supports text + images/files
- Robust HTTP interceptors (JWT + global errors)
- Angular Material + Bootstrap 5 + Bootstrap Icons

---

## Requirements

- **Node.js** 18+ (20+ recommended)
- **Angular CLI** 16+ (project uses standalone components; Angular 19 works great)
- A running backend API (see backend README for ports/CORS)

---

## Quick Start

```bash
# from the frontend project root
cd Frontend_DoConnect/doconnect/doconnect

# 1) install dependencies
npm install
npm i bootstrap icons

# 2) point frontend to your backend
#   src/environments/environment.ts
# export const environment = {
#   production: false,
#   apiUrl: 'http://localhost:5108/api',
#   apiOrigin: 'http://localhost:5108'
# };

# 3) run dev server
ng serve    # http://localhost:4200
```

HMR is enabled by default in Angular’s dev server.

---

## Project Structure

```
doconnect/
 ├─ angular.json
 ├─ package.json
 └─ src/
     ├─ app/
     │   ├─ app.component.(ts|html|scss)     # navbar + <router-outlet>
     │   ├─ app.routes.ts                    # top-level routes
     │   ├─ core/
     │   │   ├─ auth.service.ts              # login/register/me, token storage
     │   │   ├─ question.service.ts          # questions & answers API
     │   │   ├─ admin.service.ts             # admin-only calls
     │   │   ├─ models.ts
     │   │   ├─ jwt.interceptor.ts           # adds Authorization header
     │   │   └─ error.interceptor.ts         # global HTTP error handling
     │   ├─ features/
     │   │   ├─ questions/
     │   │   │   ├─ list/                    # search, pagination, lightbox
     │   │   │   ├─ ask/
     │   │   │   └─ detail/
     │   │   └─ ai/ai-chat/                  # AI page/dialog (optional)
     ├─ assets/
     ├─ environments/environment.ts
     ├─ index.html
     ├─ main.ts                              # boot + providers/interceptors
     └─ styles.scss
```

---

## Environment

`src/environments/environment.ts`

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5108/api', // REST base URL used by services
  apiOrigin: 'http://localhost:5108'   // builds absolute URLs for uploaded images
};
```

- `apiUrl` → used by services (Auth, Questions, Admin)
- `apiOrigin` → used when API returns relative paths like `/uploads/...`

Make sure your backend CORS policy allows `http://localhost:4200`.

---

## Routing

`src/app/app.routes.ts` (typical):

```ts
export const routes: Routes = [
  { path: 'questions', loadChildren: () => import('./features/questions/questions.routes').then(m => m.QUESTIONS_ROUTES) },

  // Optional AI page route (enable if you use a full page instead of dialog)
  // { path: 'ai', loadComponent: () => import('./features/ai/ai-chat/ai-chat.component').then(m => m.AiChatComponent) },

  { path: '', redirectTo: 'questions', pathMatch: 'full' },
  { path: '**', redirectTo: 'questions' }
];
```

`features/questions/questions.routes.ts`:

```ts
export const QUESTIONS_ROUTES: Routes = [
  { path: '', component: ListComponent },
  { path: 'ask', component: AskComponent },
  { path: ':id', component: DetailComponent },
];
```

If you have an admin area, guard it with a route guard that calls `/auth/me`.

---

## Interceptors (wired in `main.ts`)

```ts
providers: [
  { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
  { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
  provideRouter(routes),
  importProvidersFrom(HttpClientModule),
  provideAnimations()
]
```

- **JwtInterceptor**: attaches `Authorization: Bearer <token>` when present
- **ErrorInterceptor**: handles 401/403 and surfaces server errors (e.g., MatSnackBar)

---

## UI Libraries

Install once:

```bash
npm i bootstrap bootstrap-icons
```

Wire them either in **angular.json**:

```json
"styles": [
  "@angular/material/prebuilt-themes/azure-blue.css",
  "src/styles.scss",
  "node_modules/bootstrap/dist/css/bootstrap.min.css",
  "node_modules/bootstrap-icons/font/bootstrap-icons.css"
],
"scripts": [
  "node_modules/bootstrap/dist/js/bootstrap.bundle.min.js"
]
```

Or import in **styles.scss**:

```scss
@import "bootstrap/dist/css/bootstrap.min.css";
@import "bootstrap-icons/font/bootstrap-icons.css";
```

> Use **package paths** in SASS; do **not** prefix with `node_modules/...`.

---

## Features in Detail

### Questions List
- Debounced search & MatPaginator
- **Smart thumbnails**: computes a “question‑only” image by fetching the detail and
  filtering out images that also appear in answers.
- Lightbox viewer (full screen)
- Admin-only **Delete** (role detected from JWT)

### Ask / Detail
- Create a question with images (multipart/form-data: `Title`, `Text`, `Files[]`)
- View a question and its answers; post answers (multipart: `Text`, `Files[]`)

### Ask from AI
Two options:
1) **Dialog**: `this.dialog.open(AiChatComponent, { width: '720px', maxHeight: '90vh' })`
2) **Full page**: add route `/ai` → `AiChatComponent`

Supports:
- **Text** → `POST /api/ai/chat` with `{ prompt }`
- **Images & Files** → `POST /api/ai/chat-uploads` (multipart `prompt` + `files[]`)

**Scrollable answers** – sample styles:
```css
.answer-box { max-height: 46vh; overflow: auto; position: relative; }
.scroll-fab { position: absolute; right: 12px; bottom: 12px; }
```

---

## Services → API Mapping

Base: `environment.apiUrl` (e.g., `http://localhost:5108/api`)

- **AuthService**
  - `POST /auth/login`
  - `POST /auth/register`
  - `GET /auth/me`
- **QuestionService**
  - `GET /questions?q=&page=&pageSize=`
  - `GET /questions/{id}`
  - `POST /questions` (multipart: `Title`, `Text`, `Files[]`)
  - `GET /questions/{id}/answers`
  - `POST /questions/{id}/answers` (multipart: `Text`, `Files[]`)
- **AdminService**
  - Approve/Reject/Delete (as exposed by backend)

> Use **PascalCase** keys (`Title`, `Text`, `Files`) when posting multipart to .NET.

---

## Image URLs

Build absolute URLs for uploaded images:

```ts
asUrl(path: string): string {
  if (!path) return '';
  if (path.startsWith('http')) return path;
  const rel = path.startsWith('/') ? path : `/${path}`;
  return `${environment.apiOrigin}${rel}`;
}
```

Use a safe error handler to swap in a fallback avatar:

```html
<img [src]="thumb" (error)="onImgError($event)" alt="image">
```

```ts
onImgError(e: Event) {
  (e.target as HTMLImageElement).src = 'assets/default-avatar.png';
}
```

---

## NPM Scripts

```bash
npm start            # ng serve
npm run build        # ng build --configuration production
```

Build output: `dist/doconnect/`

---

## Common Issues & Fixes

- **Bootstrap Icons cannot resolve**  
  Install + import via package path:
  ```bash
  npm i bootstrap-icons
  ```
  ```scss
  @import "bootstrap-icons/font/bootstrap-icons.css";
  ```

- **`routerLink` not a known property**  
  Import `RouterModule` in any **standalone** component that uses `routerLink`.

- **Material “not a known element”**  
  Import the specific Material module in the component’s `imports:`
  (`MatCardModule`, `MatIconModule`, `MatButtonModule`, `MatDialogModule`, etc.).

- **Angular parser error for `(error)="($event.target as HTMLImageElement).src=..."`**  
  Move it to a method (`onImgError`) instead of inline type casting in the template.

- **CORS errors**  
  Ensure backend allows `http://localhost:4200` and that you’re calling `/api/...`.

- **AI answer not scrolling**  
  Give the answer container a `max-height` in `vh` and `overflow:auto` (see styles above).

---

## Production

```bash
ng build --configuration production
```


Set production URLs in `environment.*.ts` as needed.

---

## Contributing

- Keep `environment.*.ts` synced with backend URLs and CORS.
- Use services for API calls; keep components lean.
- Import only the Material modules each component needs.


---

