import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardOverview, TimeRange } from '../models/dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardApi {
  private readonly http = inject(HttpClient);

  overview(timeRange: TimeRange): Observable<DashboardOverview> {
    const params = new HttpParams().set('timeRange', timeRange);
    return this.http.get<DashboardOverview>('/api/dashboard/overview', { params });
  }
}
