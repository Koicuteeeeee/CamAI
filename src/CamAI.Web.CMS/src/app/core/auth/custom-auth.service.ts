import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { KeycloakService } from 'keycloak-angular';
import { Router } from '@angular/router';
import { catchError, tap, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CustomAuthService {
  private readonly keycloakUrl = 'http://192.168.1.144:8081';
  private readonly realm = 'CamAI';
  private readonly clientId = 'camai-web';

  isLoading = signal(false);
  error = signal<string | null>(null);

  constructor(
    private http: HttpClient,
    private keycloak: KeycloakService,
    private router: Router
  ) {}

  loginDirect(username: string, password: string) {
    this.isLoading.set(true);
    this.error.set(null);

    const body = new HttpParams()
      .set('grant_type', 'password')
      .set('client_id', this.clientId)
      .set('username', username)
      .set('password', password);

    const headers = new HttpHeaders({
      'Content-Type': 'application/x-www-form-urlencoded'
    });

    return this.http.post(`${this.keycloakUrl}/realms/${this.realm}/protocol/openid-connect/token`, body.toString(), { headers })
      .pipe(
        tap((res: any) => {
          // Lưu token và chuyển hướng
          localStorage.setItem('access_token', res.access_token);
          this.isLoading.set(false);
          this.router.navigate(['/']);
        }),
        catchError(err => {
          this.isLoading.set(false);
          this.error.set('Tài khoản hoặc mật khẩu không chính xác!');
          return throwError(() => err);
        })
      );
  }

  async logout() {
    localStorage.removeItem('access_token');
    await this.keycloak.logout(window.location.origin + '/login');
  }

  async isAuthenticated(): Promise<boolean> {
     // Kiểm tra token local
     const token = localStorage.getItem('access_token');
     return !!token;
  }
}
