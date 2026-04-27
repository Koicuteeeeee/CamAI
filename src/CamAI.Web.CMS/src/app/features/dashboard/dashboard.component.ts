import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { LucideAngularModule, Camera, Users, Car, RefreshCw, Settings, ChevronLeft, LogOut, User } from 'lucide-angular';
import { AccessLogService } from '../../core/services/access-log.service';
import { KeycloakService } from 'keycloak-angular';
import { CustomAuthService } from '../../core/auth/custom-auth.service';
import { RouterModule } from '@angular/router';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    LucideAngularModule,
    RouterModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  constructor(
    private sanitizer: DomSanitizer,
    private accessLogService: AccessLogService,
    private keycloak: KeycloakService,
    private authService: CustomAuthService
  ) {}
  readonly CameraIcon = Camera;
  readonly UsersIcon = Users;
  readonly UserIcon = User;
  readonly CarIcon = Car; 
  readonly RefreshIcon = RefreshCw;
  readonly SettingsIcon = Settings;
  readonly BackIcon = ChevronLeft;
  readonly LogoutIcon = LogOut;

  userName = signal<string>('Guest');
  userEmail = signal<string>('');
  private readonly minioBaseUrl = 'http://localhost:9000';
  private readonly accessLogStreamUrl = 'http://localhost:5282/api/AccessLogs/stream';
  private accessLogStream: EventSource | null = null;
  private refreshTimer: ReturnType<typeof setTimeout> | null = null;
  streamConnected = signal(false);

  // Dùng Signals để đảm bảo giao diện cập nhật ngay lập tức
  detections = signal<any[]>([]);
  readonly liveFallbackImage = '/images/no-image.svg';
  readonly registeredFallbackImage = '/images/unknown-face.svg';
  
  stats = signal<any[]>([
    { label: 'LƯỢT NHẬN DIỆN HÔM NAY', value: '0', icon: Users, color: 'bg-[#065F46]' },
    { label: 'CẢNH BÁO NGƯỜI LẠ', value: '0', icon: Settings, color: 'bg-orange-600' }
  ]);

  cameras = [
    { 
      name: 'Camera Sảnh chính (FaceID AI)', 
      status: 'online', 
      time: '15:30:01', 
      url: 'http://localhost:5120/api/FaceStream/live-clean' 
    }
  ];

  ngOnInit(): void {
    this.loadUserProfile();
    this.loadDetections();
    this.startRealtimeStream();
  }

  async loadUserProfile() {
    if (await this.keycloak.isLoggedIn()) {
      const profile = await this.keycloak.loadUserProfile();
      this.userName.set(`${profile.firstName} ${profile.lastName}`);
      this.userEmail.set(profile.email || '');
    }
  }

  logout() {
    this.authService.logout();
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
    }

    if (this.accessLogStream) {
      this.accessLogStream.close();
      this.accessLogStream = null;
    }
  }

  loadDetections(): void {
    console.log('Đang gọi API lấy Log...');
    forkJoin({
      logs: this.accessLogService.getLogs(),
      faces: this.accessLogService.getFaceRecords()
    }).subscribe({
      next: (res) => {
        if (res && res.logs?.data) {
          // Logic V2: res.faces.data contains multiple rows per profileId.
          // We take one representative image (preferably "front") for each profile.
          const faceImageMap = new Map<string, string>();
          (res.faces?.data ?? []).forEach((face: any) => {
            const pid = face.profileId?.toLowerCase();
            if (!pid || !face.minioImageUrl) return;
            
            // If we don't have an image for this profile yet, or if this one is "front", update it.
            if (!faceImageMap.has(pid) || face.angleLabel === 'front') {
              faceImageMap.set(pid, face.minioImageUrl);
            }
          });

          const mappedData = res.logs.data.map((log: any) => ({
            id: log.id,
            name: log.fullName || 'Người lạ (Unknown)',
            department: log.profileId ? 'Nhân viên' : 'Khách vãng lai',
            logTimeRaw: log.logTime ?? null,
            time: log.logTime ? new Date(log.logTime).toLocaleTimeString('vi-VN') : 'N/A',
            location: log.deviceImpacted || 'Camera AI',
            confidence: Number.isFinite(log.similarity) ? Math.round(log.similarity * 100) : 0,
            status: this.normalizeRecognitionStatus(log.recognitionStatus),
            type: log.profileId ? 'Employee' : 'Stranger',
            image: this.resolveMinioImageUrl(log.minioLogImage, this.liveFallbackImage),
            regImage: this.resolveRegisteredImage(log.profileId, faceImageMap)
          }));
          
          this.detections.set(mappedData);
          
          // Cập nhật stats
          const today = new Date().toDateString();
          const todayLogs = mappedData.filter(d => d.logTimeRaw && new Date(d.logTimeRaw).toDateString() === today);

          this.stats.update(s => {
            s[0].value = todayLogs.length.toString();
            s[1].value = todayLogs.filter(d => d.type === 'Stranger').length.toString();
            return [...s];
          });
          
          console.log('Đã cập nhật Signal detections. Số lượng:', this.detections().length);
        }
      },
      error: (err) => {
        console.error('LỖI KHI GỌI API:', err);
      }
    });
  }

  getSafeUrl(url: string | undefined): any {
    if (!url) return null;
    return this.sanitizer.bypassSecurityTrustUrl(url);
  }

  refreshLogs(): void {
    this.loadDetections();
  }

  private startRealtimeStream(): void {
    this.streamConnected.set(false);
    this.accessLogStream = new EventSource(this.accessLogStreamUrl);

    this.accessLogStream.onopen = () => {
      this.streamConnected.set(true);
    };

    this.accessLogStream.addEventListener('access-log', () => {
      this.scheduleRefresh();
    });

    this.accessLogStream.onerror = () => {
      this.streamConnected.set(false);
      // Let browser auto-reconnect; no manual retry needed.
      console.warn('Mất kết nối realtime stream, hệ thống đang tự reconnect...');
    };
  }

  private scheduleRefresh(): void {
    // Debounce nhẹ để tránh spam call khi AI đẩy nhiều event liên tiếp.
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
    }

    this.refreshTimer = setTimeout(() => {
      this.loadDetections();
      this.refreshTimer = null;
    }, 300);
  }

  onImageError(event: Event, fallback: string): void {
    const img = event.target as HTMLImageElement | null;
    if (!img) {
      return;
    }

    if (img.src.endsWith(fallback)) {
      return;
    }

    img.src = fallback;
  }

  private normalizeRecognitionStatus(rawStatus?: string): 'approved' | 'denied' {
    const status = (rawStatus ?? '').toLowerCase();
    return status === 'approved' || status === 'identified' ? 'approved' : 'denied';
  }

  private resolveMinioImageUrl(rawImagePath?: string, fallback: string = this.liveFallbackImage): string {
    if (!rawImagePath) {
      return fallback;
    }

    const trimmed = rawImagePath.trim();
    if (!trimmed) {
      return fallback;
    }

    // DB có thể lưu full URL, hoặc chỉ lưu object key trong bucket faces.
    if (/^https?:\/\//i.test(trimmed)) {
      return trimmed;
    }

    const normalizedPath = trimmed.replace(/^\/+/, '');
    const pathWithBucket = normalizedPath.startsWith('faces/') ? normalizedPath : `faces/${normalizedPath}`;
    return `${this.minioBaseUrl}/${pathWithBucket}`;
  }

  private resolveRegisteredImage(profileId?: string, faceImageMap?: Map<string, string>): string {
    if (!profileId || !faceImageMap) {
      return this.registeredFallbackImage;
    }

    const minioFrontPath = faceImageMap.get(profileId.toLowerCase());
    return this.resolveMinioImageUrl(minioFrontPath, this.registeredFallbackImage);
  }

}
