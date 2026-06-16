# AuctionNest — Angular Frontend: Full Development Prompt

---

## 1. Project Context

You are building the **Angular 18 frontend** for **AuctionNest** — a real-time auction platform.

The backend is a production ASP.NET Core 8 REST API deployed at:
- **Production:** `https://auctionnest-api.onrender.com`
- **Local dev:** `http://localhost:5165`
- **Swagger docs:** `https://auctionnest-api.onrender.com/swagger`

The frontend lives in the **same GitHub repository** as the backend:
```
AuctionNest/
├── backend/     ← existing ASP.NET Core 8 API
└── frontend/    ← Angular 18 app (you are building this)
```

---

## 2. Tech Stack

| Concern | Technology |
|---|---|
| Framework | Angular **18** |
| Component style | **Standalone components** (no NgModules) |
| State management | **Angular Signals + Services** (no NgRx) |
| UI library | **Angular Material 18** |
| CSS | **Tailwind CSS** (alongside Angular Material) |
| Icons | **Material Icons** |
| Date utilities | **date-fns** |
| Countdown timer | **ngx-countdown** |
| Toast / Snackbar | **Angular Material Snackbar** (MatSnackBar) |
| HTTP | **Angular HttpClient** |
| Pagination | **Angular Material Paginator** (MatPaginator) |
| Real-time | **@microsoft/signalr** |
| Translations | **@ngx-translate/core** (en.json only, i18n-ready architecture) |
| Date pickers | **Angular Material Datepicker** |
| Charts | **Chart.js + ng2-charts** (low priority, scaffold only) |

---

## 3. Architecture Decisions

### 3.1 Standalone Components
All components, directives, and pipes use the standalone API:
```typescript
@Component({
  standalone: true,
  imports: [CommonModule, MatButtonModule, ...],
  ...
})
```
No `app.module.ts`. Bootstrap via `bootstrapApplication()` in `main.ts`.

### 3.2 State Management — Signals + Services
Use Angular `signal()`, `computed()`, and `effect()` for reactive state.
Services are the single source of truth. No NgRx, no BehaviorSubject (prefer signals).

```typescript
// Example service pattern
@Injectable({ providedIn: 'root' })
export class AuctionStore {
  private _auctions = signal<Auction[]>([]);
  readonly auctions = this._auctions.asReadonly();
  readonly activeCount = computed(() => this._auctions().filter(a => a.status === 'Active').length);
}
```

### 3.3 JWT Authentication — Silent Refresh Pattern

**Access token** → stored in memory only (in `AuthService` signal). Lost on F5.
**Refresh token** → stored in `localStorage`. Survives F5.

**F5 / App init flow:**
```
App starts → AppInitializer reads refreshToken from localStorage
           → calls POST /api/auth/refresh
           → if success: stores new accessToken in memory → user stays logged in
           → if failure: clears localStorage → user sees login page
```

**HTTP Interceptor** (`auth.interceptor.ts`):
- Injects `Authorization: Bearer <accessToken>` on every request
- On 401 response → calls refresh endpoint → retries original request
- On refresh failure → logout → redirect to `/auth/login`

### 3.4 SignalR — Dual Connection Strategy

**Global connection** (when user is authenticated):
- Connects on login, disconnects on logout
- Joins personal group `user:{userId}` automatically
- Receives: `NewNotification` events → updates notification badge

**Local connection** (per auction page):
- Connects when user opens `/auctions/:id`
- Calls `JoinAuction(auctionId)` on connect
- Calls `LeaveAuction(auctionId)` on destroy
- Receives: `BidPlaced`, `AuctionExtended`, `AuctionEnded`, `AuctionStarted`, `AuctionCancelled`

SignalR token (for authenticated WebSocket):
```typescript
const connection = new HubConnectionBuilder()
  .withUrl(`${environment.apiUrl}/hubs/auction`, {
    accessTokenFactory: () => this.authService.accessToken() ?? ''
  })
  .withAutomaticReconnect()
  .build();
```

### 3.5 Lazy Loading Routes

All feature modules use lazy loading:
```typescript
{
  path: 'auctions',
  loadChildren: () => import('./features/auctions/auctions.routes').then(m => m.AUCTIONS_ROUTES)
}
```

### 3.6 i18n — ngx-translate, English Only (Architecture-Ready)

Install `@ngx-translate/core` + `@ngx-translate/http-loader`.
Single file: `assets/i18n/en.json`.
All user-facing strings go through the translate pipe:
```html
{{ 'AUCTIONS.PLACE_BID' | translate }}
```
Adding Polish (`pl.json`) in the future requires zero component changes.

---

## 4. Folder Structure

```
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── guards/              # auth.guard.ts, guest.guard.ts
│   │   │   ├── interceptors/        # auth.interceptor.ts
│   │   │   ├── initializers/        # app.initializer.ts (silent refresh)
│   │   │   ├── models/              # auction.model.ts, user.model.ts, bid.model.ts, etc.
│   │   │   └── services/
│   │   │       ├── auth.service.ts
│   │   │       ├── auction.service.ts
│   │   │       ├── notification.service.ts
│   │   │       ├── watchlist.service.ts
│   │   │       ├── signalr.service.ts       # global SignalR connection
│   │   │       └── theme.service.ts         # dark/light toggle
│   │   │
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   │   ├── login/
│   │   │   │   ├── register/
│   │   │   │   └── auth.routes.ts
│   │   │   ├── auctions/
│   │   │   │   ├── auction-list/            # catalog page
│   │   │   │   ├── auction-card/            # reusable card component
│   │   │   │   ├── auction-detail/          # detail page with SignalR
│   │   │   │   ├── auction-create/          # create form
│   │   │   │   └── auctions.routes.ts
│   │   │   └── profile/
│   │   │       ├── profile-overview/
│   │   │       ├── watchlist/
│   │   │       └── profile.routes.ts
│   │   │
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   ├── countdown-timer/         # ngx-countdown wrapper with color thresholds
│   │   │   │   ├── auction-status-badge/    # Draft/Active/Extending/Ended/Cancelled chip
│   │   │   │   ├── price-display/           # formatted price component
│   │   │   │   ├── loading-spinner/
│   │   │   │   └── empty-state/
│   │   │   ├── pipes/
│   │   │   │   └── time-ago.pipe.ts
│   │   │   └── material.module.ts           # re-export all used Material modules
│   │   │
│   │   ├── layout/
│   │   │   ├── header/                      # navbar with notification bell + dark toggle
│   │   │   └── footer/
│   │   │
│   │   ├── app.component.ts
│   │   ├── app.config.ts                    # provideRouter, provideHttpClient, etc.
│   │   └── app.routes.ts
│   │
│   ├── assets/
│   │   ├── i18n/
│   │   │   └── en.json                      # all translation strings
│   │   └── images/
│   │
│   ├── environments/
│   │   ├── environment.ts                   # { apiUrl: 'http://localhost:5165', production: false }
│   │   └── environment.prod.ts              # { apiUrl: 'https://auctionnest-api.onrender.com', production: true }
│   │
│   ├── styles/
│   │   ├── _theme.scss                      # Angular Material custom theme
│   │   └── _tailwind.scss                   # Tailwind directives
│   │
│   └── index.html
│
├── .github/
│   └── workflows/
│       └── frontend-ci.yml
├── angular.json
├── tailwind.config.js
├── package.json
└── tsconfig.json
```

---

## 5. Theme & Design System

### 5.1 Color Palette

| Role | Color | Hex | Usage |
|---|---|---|---|
| Primary | Slate-900 / Slate-50 | `#0f172a` / `#f8fafc` | Header, main buttons, cards |
| Accent | Emerald-600 | `#059669` | Current price, Place Bid button, success states |
| Warn | Rose-600 | `#e11d48` | Errors, outbid alerts, countdown warning |
| Timer — amber | Amber-500 | `#f59e0b` | Countdown < 5 minutes |
| Timer — danger | Rose-600 | `#e11d48` | Countdown < 30 seconds |

### 5.2 Angular Material Theme (`_theme.scss`)
```scss
@use '@angular/material' as mat;

$primary: mat.define-palette(mat.$blue-grey-palette, 900);
$accent:  mat.define-palette(mat.$green-palette, 600);
$warn:    mat.define-palette(mat.$red-palette, 600);

$light-theme: mat.define-light-theme((
  color: (primary: $primary, accent: $accent, warn: $warn)
));

$dark-theme: mat.define-dark-theme((
  color: (primary: $primary, accent: $accent, warn: $warn)
));

.light-theme { @include mat.all-component-themes($light-theme); }
.dark-theme  { @include mat.all-component-themes($dark-theme); }
```

### 5.3 Dark/Light Mode Toggle

`ThemeService` adds class `dark-theme` or `light-theme` to `<body>`. Preference saved to `localStorage`.

```typescript
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private _isDark = signal(localStorage.getItem('theme') === 'dark');
  readonly isDark = this._isDark.asReadonly();

  toggle() {
    this._isDark.update(v => !v);
    const theme = this._isDark() ? 'dark' : 'light';
    document.body.classList.toggle('dark-theme', this._isDark());
    localStorage.setItem('theme', theme);
  }
}
```

### 5.4 Tailwind Configuration
```js
// tailwind.config.js
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        emerald: { 600: '#059669' },
        slate:   { 900: '#0f172a' },
        rose:    { 600: '#e11d48' },
      }
    }
  }
}
```

---

## 6. Pages & Features Specification

### 6.1 `/` — Catalog Page (Home)

**Layout:**
- Compact hero: app name, tagline, total active auctions count (from API)
- Filter bar (sticky on scroll): search input (debounce 400ms), category dropdown, status filter (Active/Ending Soon/Ended), price range
- Auction grid (responsive: 1 col mobile / 2 col tablet / 3 col desktop)
- Angular Material Paginator (page size: 12)

**Auction Card** shows:
- Image (with lazy loading, fallback placeholder)
- Category chip (Angular Material Chip)
- Title
- Current price (highlighted in Emerald-600)
- Countdown timer (color changes: default → amber < 5min → rose < 30sec)
- Bid count
- Status badge
- Watchlist heart button (toggle, requires auth)

### 6.2 `/auctions/:id` — Auction Detail Page

**Left column (mobile: top):**
- Image (full width)
- Title + category + status badge
- Seller display name
- Description (expandable if long)

**Right column (mobile: below):**
- Current price (large, Emerald-600)
- Countdown timer (large, prominent)
- Reserve price met indicator (if applicable)
- **Place Bid form:** amount input (min = currentPrice + minBidIncrement), Submit button
- **Buy It Now button** (shown only if available and no bids placed)
- Watchlist toggle

**Bid History — Timeline (below main content):**
- Vertical timeline, newest bid on top
- Each entry: bidder display name (or "Anonymous"), amount, time ago (date-fns)
- **Real-time:** new bids animate in from top via Angular animations + fade-in
- SignalR `BidPlaced` event triggers update

**Real-time events on this page:**
- `BidPlaced` → add new entry to timeline (animated), update current price
- `AuctionExtended` → update countdown timer, show snackbar "Auction extended!"
- `AuctionEnded` → disable bid form, show winner banner
- `AuctionCancelled` → show cancellation notice

### 6.3 `/auctions/create` — Create Auction (Auth required)

**Form fields** (Angular Reactive Forms):
- Title (required, max 100 chars)
- Description (textarea, required, max 5000)
- Category (MatSelect from GET /api/categories)
- Start Price (number, min 0.01)
- Minimum Bid Increment (number, min 0.01)
- Starts At (Angular Material DateTimePicker)
- Ends At (Angular Material DateTimePicker)
- Reserve Price (optional)
- Buy It Now Price (optional)
- Image URL (optional text input)

Validation errors shown inline. Submit calls `POST /api/auctions`.
On success: navigate to `/auctions/:id` of created auction.

### 6.4 `/auth/login` — Login Page

- Email or username input
- Password input
- Submit → `POST /api/auth/login`
- On success: store access token in memory + refresh token in localStorage → redirect to `/`
- Link to register page

### 6.5 `/auth/register` — Register Page

- Username, Email, Password, Display Name inputs
- Submit → `POST /api/auth/register`
- On success: auto-login (store tokens) → redirect to `/`
- Link to login page

### 6.6 `/profile` — User Profile

- Display Name (editable via `PATCH /api/users/me/display-name`)
- Username, Email (read-only)
- Role badge
- My auctions list (filtered from GET /api/auctions by seller)

### 6.7 `/profile/watchlist` — Watchlist

- Grid of watched auctions (same Auction Card component)
- Remove from watchlist button on each card
- Calls GET /api/watchlist, DELETE /api/watchlist/:id

---

## 7. Countdown Timer Component

`CountdownTimerComponent` wraps `ngx-countdown`.

**Color thresholds:**
- Normal: default (slate/gray text)
- < 5 minutes: **Amber-500** (`#f59e0b`) — pulsing animation
- < 30 seconds: **Rose-600** (`#e11d48`) — faster pulse, shake animation

```typescript
@Component({
  selector: 'app-countdown-timer',
  standalone: true,
  template: `
    <countdown
      [config]="countdownConfig()"
      [class]="timerClass()"
      (event)="onCountdownEvent($event)">
    </countdown>
  `
})
export class CountdownTimerComponent {
  endsAt = input.required<string>();  // ISO datetime string

  secondsRemaining = computed(() =>
    Math.max(0, Math.floor((new Date(this.endsAt()).getTime() - Date.now()) / 1000))
  );

  timerClass = computed(() => {
    const s = this.secondsRemaining();
    if (s <= 30) return 'timer-danger';
    if (s <= 300) return 'timer-warning';
    return 'timer-normal';
  });
}
```

When `AuctionExtended` SignalR event received → update `endsAt` input → timer resets.

---

## 8. Notifications

**Bell icon in header:**
- MatBadge shows unread count (from GET /api/notifications/unread-count)
- On click: MatMenu dropdown opens

**Notification dropdown:**
- Last 5 notifications (from GET /api/notifications?pageSize=5)
- Each item: icon (by type), title, message, time ago
- "Mark all as read" button
- Real-time: SignalR `NewNotification` → increment badge, prepend to list, show MatSnackbar

**Snackbar for real-time events:**
```typescript
// On BidPlaced (from global or auction room)
this.snackBar.open('You have been outbid!', 'View', { duration: 5000, panelClass: 'snack-warn' });

// On AuctionExtended
this.snackBar.open('Auction extended by 2 minutes!', null, { duration: 3000 });
```

---

## 9. HTTP Services Pattern

All API calls go through typed services:

```typescript
@Injectable({ providedIn: 'root' })
export class AuctionService {
  private http = inject(HttpClient);
  private apiUrl = inject(ENVIRONMENT).apiUrl;

  getAuctions(params: AuctionFilterParams): Observable<PagedResponse<AuctionDto>> {
    return this.http.get<PagedResponse<AuctionDto>>(`${this.apiUrl}/api/auctions`, { params });
  }

  getById(id: string): Observable<AuctionDetailDto> {
    return this.http.get<AuctionDetailDto>(`${this.apiUrl}/api/auctions/${id}`);
  }

  placeBid(auctionId: string, amount: number, idempotencyKey: string): Observable<BidDto> {
    return this.http.post<BidDto>(
      `${this.apiUrl}/api/auctions/${auctionId}/bids`,
      { amount },
      { headers: { 'X-Idempotency-Key': idempotencyKey } }
    );
  }
}
```

Use `inject()` instead of constructor injection (Angular 18 style).

---

## 10. Models (TypeScript Interfaces)

Match exactly the backend API response shapes:

```typescript
// core/models/auction.model.ts
export interface AuctionDto {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  sellerId: string;
  sellerDisplayName: string;
  categoryId: string;
  categoryName: string;
  startPrice: number;
  currentPrice: number;
  reservePrice: number | null;
  buyItNowPrice: number | null;
  minBidIncrement: number;
  status: AuctionStatus;
  startsAt: string;      // ISO string
  endsAt: string;        // ISO string
  extensionCount: number;
  bidCount: number;
  isReserveMet: boolean;
  isBuyItNowAvailable: boolean;
}

export type AuctionStatus = 'Draft' | 'Active' | 'Extending' | 'Ended' | 'Cancelled';

export interface BidDto {
  id: string;
  auctionId: string;
  bidderId: string;
  bidderDisplayName: string;
  amount: number;
  isWinning: boolean;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  username: string;
  displayName: string;
  role: string;
}
```

---

## 11. Route Guards

```typescript
// core/guards/auth.guard.ts
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated()
    ? true
    : router.createUrlTree(['/auth/login']);
};

// core/guards/guest.guard.ts (redirect logged-in users away from login/register)
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated()
    ? inject(Router).createUrlTree(['/'])
    : true;
};
```

---

## 12. Routes Configuration

```typescript
// app.routes.ts
export const APP_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./features/auctions/auction-list/auction-list.component') },
  {
    path: 'auctions',
    loadChildren: () => import('./features/auctions/auctions.routes').then(m => m.AUCTIONS_ROUTES)
  },
  {
    path: 'auth',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadChildren: () => import('./features/profile/profile.routes').then(m => m.PROFILE_ROUTES)
  },
  { path: '**', redirectTo: '' }
];

// features/auctions/auctions.routes.ts
export const AUCTIONS_ROUTES: Routes = [
  { path: ':id', loadComponent: () => import('./auction-detail/auction-detail.component') },
  { path: 'create', canActivate: [authGuard], loadComponent: () => import('./auction-create/auction-create.component') }
];
```

---

## 13. App Initialization (Silent Refresh)

```typescript
// core/initializers/app.initializer.ts
export function appInitializer(auth: AuthService): () => Promise<void> {
  return async () => {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return;
    try {
      await auth.silentRefresh(refreshToken);
    } catch {
      localStorage.removeItem('refreshToken');
    }
  };
}

// Register in app.config.ts
providers: [
  {
    provide: APP_INITIALIZER,
    useFactory: (auth: AuthService) => appInitializer(auth),
    deps: [AuthService],
    multi: true
  }
]
```

---

## 14. i18n — Translation Keys Structure (en.json)

```json
{
  "NAV": {
    "HOME": "Auctions",
    "CREATE": "Create Auction",
    "LOGIN": "Login",
    "REGISTER": "Register",
    "PROFILE": "Profile",
    "WATCHLIST": "Watchlist",
    "LOGOUT": "Logout"
  },
  "AUCTION": {
    "CURRENT_PRICE": "Current Price",
    "PLACE_BID": "Place Bid",
    "BUY_NOW": "Buy It Now",
    "TIME_LEFT": "Time Left",
    "BID_COUNT": "{{count}} bids",
    "NO_BIDS": "No bids yet",
    "RESERVE_MET": "Reserve met",
    "RESERVE_NOT_MET": "Reserve not met",
    "EXTENDED": "Auction extended by 2 minutes!",
    "OUTBID": "You've been outbid!"
  },
  "AUTH": {
    "LOGIN_TITLE": "Welcome Back",
    "REGISTER_TITLE": "Create Account",
    "EMAIL": "Email or Username",
    "PASSWORD": "Password"
  },
  "ERRORS": {
    "GENERIC": "Something went wrong. Please try again.",
    "UNAUTHORIZED": "Please log in to continue.",
    "NOT_FOUND": "Not found."
  }
}
```

---

## 15. Backend API Reference

All endpoints (base URL = `environment.apiUrl`):

```
Auth:
  POST /api/auth/register     { username, email, password, displayName }
  POST /api/auth/login        { usernameOrEmail, password }
  POST /api/auth/refresh      { refreshToken }

Auctions:
  GET  /api/auctions          ?page&pageSize&search&categoryId&status&minPrice&maxPrice&sortBy&sortDescending
  GET  /api/auctions/:id
  POST /api/auctions          { categoryId, title, description, startPrice, minBidIncrement, startsAt, endsAt, reservePrice?, buyItNowPrice?, imageUrl? }
  POST /api/auctions/:id/bids  { amount }  Header: X-Idempotency-Key
  POST /api/auctions/:id/buy-it-now
  DELETE /api/auctions/:id/cancel

WatchList:
  GET    /api/watchlist
  POST   /api/watchlist/:auctionId
  DELETE /api/watchlist/:auctionId

Notifications:
  GET   /api/notifications    ?page&pageSize
  GET   /api/notifications/unread-count
  PATCH /api/notifications/:id/read
  PATCH /api/notifications/read-all

Users:
  GET   /api/users/me
  PATCH /api/users/me/display-name  { displayName }

Categories:
  GET /api/categories

SignalR Hub: /hubs/auction
  Client → Server: JoinAuction(auctionId), LeaveAuction(auctionId)
  Server → Client: BidPlaced, AuctionExtended, AuctionEnded, AuctionStarted, AuctionCancelled, NewNotification
```

---

## 16. Git Branch Strategy

```
main      ← production (Vercel auto-deploys from here)
develop   ← active development branch
feature/auth
feature/auction-list
feature/auction-detail
feature/auction-create
feature/profile
feature/signalr
feature/i18n-scaffold
```

All feature branches merge into `develop` via Pull Request.
`develop` → `main` when a milestone is stable.

---

## 17. GitHub Actions CI for Frontend

File: `.github/workflows/frontend-ci.yml`

```yaml
name: Frontend CI

on:
  push:
    branches: [main, develop]
    paths: ['frontend/**']
  pull_request:
    branches: [main, develop]
    paths: ['frontend/**']

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: frontend

    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Build
        run: npm run build -- --configuration production

      - name: Test
        run: npm run test -- --watch=false --browsers=ChromeHeadless
```

---

## 18. Vercel Deployment

1. Connect GitHub repo to Vercel
2. Set **Root Directory** to `frontend`
3. Framework preset: Angular
4. Build command: `npm run build -- --configuration production`
5. Output directory: `dist/frontend/browser`
6. **Environment variable in Vercel:** `NG_APP_API_URL=https://auctionnest-api.onrender.com`

For environment.prod.ts, use Vercel env var at build time:
```typescript
export const environment = {
  production: true,
  apiUrl: process.env['NG_APP_API_URL'] ?? 'https://auctionnest-api.onrender.com'
};
```

---

## 19. CORS (Backend — do when Vercel URL is known)

After first Vercel deploy, add the Vercel domain to backend CORS policy in `Program.cs`:
```csharp
builder.Services.AddCors(options =>
  options.AddDefaultPolicy(policy =>
    policy.WithOrigins(
      "http://localhost:4200",
      "https://your-app.vercel.app"  // ← replace after first Vercel deploy
    )
    .AllowAnyHeader()
    .AllowAnyMethod()
  )
);
```

---

## 20. Development Setup Commands

```bash
# In /frontend directory:
ng new frontend --routing --style=scss --standalone
cd frontend
npm install @angular/material @angular/cdk
npm install tailwindcss postcss autoprefixer
npm install @microsoft/signalr
npm install @ngx-translate/core @ngx-translate/http-loader
npm install date-fns
npm install ngx-countdown
npm install chart.js ng2-charts

npx tailwindcss init

# Run dev server
ng serve --proxy-config proxy.conf.json

# proxy.conf.json (dev CORS workaround)
{
  "/api": { "target": "http://localhost:5165", "secure": false },
  "/hubs": { "target": "http://localhost:5165", "secure": false, "ws": true }
}
```

---

## 21. Priority Order for Implementation

Build in this order (each step is shippable):

1. **Setup** — Angular project, Material + Tailwind, theme, routing skeleton
2. **Auth** — Login, Register, AuthService, Interceptor, Silent Refresh
3. **Auction List** — Catalog page, Auction Card, filters, pagination
4. **Auction Detail** — Detail page, Bid form, Bid History timeline
5. **SignalR** — Real-time bids, countdown updates, notifications
6. **Create Auction** — Form with validation
7. **Profile + Watchlist** — User profile, watchlist management
8. **Notifications** — Bell badge, dropdown, real-time
9. **Dark/Light toggle** — Theme switch in header
10. **Charts** — Bid history chart (low priority, do last)
