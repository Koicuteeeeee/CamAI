import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FaceRegistrationService {
  private apiUrl = 'http://192.168.1.144:5120/api/face/register-multi';

  constructor(private http: HttpClient) {}

  registerMultiFace(fullName: string, frontFile: File | Blob, leftFile: File | Blob, rightFile: File | Blob): Observable<any> {
    const formData = new FormData();
    formData.append('fullName', fullName);
    formData.append('frontFile', frontFile, 'front.jpg');
    formData.append('leftFile', leftFile, 'left.jpg');
    formData.append('rightFile', rightFile, 'right.jpg');

    return this.http.post(this.apiUrl, formData);
  }

  startEnroll(fullName: string): Observable<any> {
    return this.http.post('http://192.168.1.144:5120/api/face/enroll/start', { fullName });
  }

  getEnrollStatus(): Observable<any> {
    return this.http.get('http://192.168.1.144:5120/api/face/enroll/status');
  }
}
