import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CategoryDto } from '../models/category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private http = inject(HttpClient);
  private _categories = signal<CategoryDto[]>([]);
  readonly categories = this._categories.asReadonly();

  getCategories(): Observable<CategoryDto[]> {
    if (this._categories().length > 0) {
      return of(this._categories());
    }
    return this.http.get<CategoryDto[]>(`${environment.apiUrl}/api/categories`).pipe(
      tap(cats => this._categories.set(cats))
    );
  }
}
