import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuctionDetailDto,
  AuctionDto,
  AuctionFilterParams,
  BidDto,
  CreateAuctionRequest,
  PagedResponse,
} from '../models/auction.model';

@Injectable({ providedIn: 'root' })
export class AuctionService {
  private http = inject(HttpClient);

  getAuctions(filters: AuctionFilterParams = {}): Observable<PagedResponse<AuctionDto>> {
    let params = new HttpParams();
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http.get<PagedResponse<AuctionDto>>(`${environment.apiUrl}/api/auctions`, { params });
  }

  getById(id: string): Observable<AuctionDetailDto> {
    return this.http.get<AuctionDetailDto>(`${environment.apiUrl}/api/auctions/${id}`);
  }

  create(request: CreateAuctionRequest): Observable<AuctionDto> {
    return this.http.post<AuctionDto>(`${environment.apiUrl}/api/auctions`, request);
  }

  placeBid(auctionId: string, amount: number, idempotencyKey: string): Observable<BidDto> {
    return this.http.post<BidDto>(
      `${environment.apiUrl}/api/auctions/${auctionId}/bids`,
      { amount },
      { headers: { 'X-Idempotency-Key': idempotencyKey } }
    );
  }

  buyItNow(auctionId: string): Observable<BidDto> {
    return this.http.post<BidDto>(`${environment.apiUrl}/api/auctions/${auctionId}/buy-it-now`, {});
  }

  cancel(auctionId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/api/auctions/${auctionId}/cancel`);
  }
}
