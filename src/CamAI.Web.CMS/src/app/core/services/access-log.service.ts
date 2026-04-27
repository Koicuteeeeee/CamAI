import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AccessLog {
  id: string;
  logTime: string;
  profileId: string | null;
  fullName: string;
  recognitionStatus: string;
  similarity: number;
  minioLogImage: string;
  deviceImpacted: string;
}

export interface ApiResponse {
  success: boolean;
  data: AccessLog[];
}

@Injectable({
  providedIn: 'root'
})
export class AccessLogService {
  private apiUrl = 'http://localhost:5282/api/AccessLogs';

  constructor(private http: HttpClient) { }

  getLogs(page: number = 1, pageSize: number = 20): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`);
  }
}
