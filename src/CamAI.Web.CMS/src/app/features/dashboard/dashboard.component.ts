import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { LucideAngularModule, Camera, Users, Car, RefreshCw, Settings, ChevronLeft } from 'lucide-angular';
import { AccessLogService } from '../../core/services/access-log.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, 
    LucideAngularModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  constructor(
    private sanitizer: DomSanitizer,
    private accessLogService: AccessLogService
  ) {}
  readonly CameraIcon = Camera;
  readonly UsersIcon = Users;
  readonly CarIcon = Car; // Sẽ đổi thành icon khác phù hợp hơn
  readonly RefreshIcon = RefreshCw;
  readonly SettingsIcon = Settings;
  readonly BackIcon = ChevronLeft;

  // Dùng Signals để đảm bảo giao diện cập nhật ngay lập tức
  detections = signal<any[]>([]);
  
  stats = signal<any[]>([
    { label: 'LƯỢT NHẬN DIỆN HÔM NAY', value: '0', icon: Users, color: 'bg-[#065F46]' },
    { label: 'CẢNH BÁO NGƯỜI LẠ', value: '0', icon: Settings, color: 'bg-orange-600' }
  ]);

  cameras = [
    { 
      name: 'Camera Sảnh chính (FaceID AI)', 
      status: 'online', 
      time: '15:30:01', 
      url: 'http://localhost:5120/api/FaceStream/live' 
    }
  ];

  ngOnInit(): void {
    this.loadDetections();
  }

  loadDetections(): void {
    console.log('Đang gọi API lấy Log...');
    this.accessLogService.getLogs().subscribe({
      next: (res) => {
        if (res && res.data) {
          const mappedData = res.data.map((log: any) => ({
            id: log.id,
            name: log.fullName || 'Người lạ (Unknown)',
            department: log.profileId ? 'Nhân viên' : 'Khách vãng lai',
            time: log.logTime ? new Date(log.logTime).toLocaleTimeString('vi-VN') : 'N/A',
            location: log.deviceImpacted || 'Camera AI',
            confidence: log.similarity ? Math.round(log.similarity * 100) : 0,
            status: log.recognitionStatus === 'approved' ? 'approved' : 'denied',
            type: log.profileId ? 'Employee' : 'Stranger',
            image: log.minioLogImage ? `http://localhost:9000/access-logs/${log.minioLogImage}` : 'assets/images/no-image.png',
            regImage: log.profileId ? `http://localhost:9000/face-profiles/${log.profileId}_front.jpg` : 'assets/images/unknown-face.png'
          }));
          
          this.detections.set(mappedData);
          
          // Cập nhật stats
          this.stats.update(s => {
            s[0].value = mappedData.length.toString();
            s[1].value = mappedData.filter(d => d.type === 'Stranger').length.toString();
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

}
