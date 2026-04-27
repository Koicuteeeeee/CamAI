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

export interface FaceRecord {
  profileId: string;
  minioFront?: string;
}

export interface FaceApiResponse {
  success: boolean;
  data: FaceRecord[];
}

@Injectable({
  providedIn: 'root'
})
export class AccessLogService {
  private apiUrl = 'http://localhost:5282/api/AccessLogs';
  private faceProfileUrl = 'http://localhost:5282/api/FaceProfiles/faces';

  constructor(private http: HttpClient) { }

  getLogs(page: number = 1, pageSize: number = 20): Observable<ApiResponse> {
    return this.http.get<ApiResponse>(`${this.apiUrl}?page=${page}&pageSize=${pageSize}`);
  }

  getFaceRecords(): Observable<FaceApiResponse> {
    return this.http.get<FaceApiResponse>(this.faceProfileUrl);
  }
}
