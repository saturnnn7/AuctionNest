import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserProfileDto } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);

  getProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${environment.apiUrl}/api/users/me`);
  }

  updateDisplayName(displayName: string): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/api/users/me/display-name`, { displayName });
  }
}
