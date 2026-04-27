import { Component, ElementRef, OnDestroy, OnInit, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LucideAngularModule, Camera, ArrowLeft, CheckCircle2, User, ChevronRight, XCircle, MonitorSmartphone } from 'lucide-angular';
import { FaceRegistrationService } from './face-registration.service';

@Component({
  selector: 'app-face-registration',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  template: `
    <div class="flex flex-col h-screen bg-[#f8fafc] font-['Inter',sans-serif]">
      <!-- Header -->
      <header class="bg-white border-b px-6 py-4 flex items-center justify-between shadow-[0_4px_20px_rgba(0,0,0,0.02)]">
        <div class="flex items-center gap-4">
          <button (click)="goBack()" class="p-2 mr-2 rounded-full hover:bg-gray-100 text-gray-500 transition-colors">
            <lucide-icon [name]="ArrowLeftIcon" class="w-5 h-5"></lucide-icon>
          </button>
          <div class="bg-emerald-600 p-2 rounded-xl text-white shadow-lg shadow-emerald-500/20">
            <lucide-icon [name]="UserIcon" class="w-6 h-6"></lucide-icon>
          </div>
          <div>
            <h1 class="text-gray-900 font-bold text-xl leading-none">Đăng ký khuôn mặt</h1>
            <p class="text-[10px] text-gray-500 font-bold uppercase tracking-widest mt-1">Hệ thống nhận diện CamAI</p>
          </div>
        </div>
      </header>

      <!-- Main Content -->
      <main class="flex-1 overflow-y-auto p-8 flex justify-center">
        <div class="w-full max-w-5xl grid grid-cols-1 lg:grid-cols-2 gap-8">
          
          <!-- Left: Scanner (Webcam) -->
          <div class="bg-white rounded-3xl p-6 shadow-[0_20px_50px_rgba(0,0,0,0.03)] border border-gray-100 flex flex-col">
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-sm font-bold text-gray-800 uppercase tracking-widest">Live Scanner</h2>
              
              <!-- Nguồn Camera Selector -->
              <div class="flex items-center gap-2">
                <lucide-icon [name]="MonitorDeviceIcon" class="w-4 h-4 text-gray-400"></lucide-icon>
                <select [(ngModel)]="selectedDeviceId" (change)="switchCamera()" class="text-xs border-gray-200 rounded-lg bg-gray-50 py-1 pl-2 pr-6 focus:ring-emerald-500 outline-none max-w-[150px] truncate text-gray-700 font-medium">
                   <option *ngFor="let cam of videoDevices" [value]="cam.deviceId">{{ cam.label || 'Camera chưa rõ tên' }}</option>
                </select>
                
                <div class="flex items-center gap-2 text-xs font-semibold text-emerald-600 ml-2" *ngIf="streamActive()">
                  <span class="relative flex h-2 w-2">
                    <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                    <span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
                  </span>
                  LIVE
                </div>
              </div>
            </div>

            <!-- Webcam Container -->
            <div class="relative flex-1 bg-black rounded-2xl overflow-hidden aspect-[4/3] border-4 border-gray-100 shadow-inner group">
              <video #videoElement autoplay playsinline class="absolute inset-0 w-full h-full object-cover transform -scale-x-100"></video>
              <canvas #canvasElement class="hidden"></canvas>
              
              <!-- Scanning Grid Overlay -->
              <div class="absolute inset-0 pointer-events-none border-2 border-emerald-500/30 m-4 rounded-xl">
                 <!-- Corners -->
                 <div class="absolute top-0 left-0 w-8 h-8 border-t-4 border-l-4 border-emerald-500 -mt-1 -ml-1"></div>
                 <div class="absolute top-0 right-0 w-8 h-8 border-t-4 border-r-4 border-emerald-500 -mt-1 -mr-1"></div>
                 <div class="absolute bottom-0 left-0 w-8 h-8 border-b-4 border-l-4 border-emerald-500 -mb-1 -ml-1"></div>
                 <div class="absolute bottom-0 right-0 w-8 h-8 border-b-4 border-r-4 border-emerald-500 -mb-1 -mr-1"></div>
              </div>

              <!-- Start Camera Button -->
              <div *ngIf="!streamActive()" class="absolute inset-0 flex items-center justify-center bg-gray-900/80 backdrop-blur-sm z-10">
                <button (click)="startCamera()" class="bg-white text-gray-900 px-6 py-3 rounded-xl font-bold flex items-center gap-2 hover:scale-105 transition-transform shadow-xl">
                  <lucide-icon [name]="CameraIcon" class="w-5 h-5"></lucide-icon>
                  Bật Camera
                </button>
              </div>
            </div>

            <p class="text-center text-[11px] text-gray-400 mt-4 font-medium uppercase tracking-widest">
              Đảm bảo ánh sáng rõ ràng và không đeo kính râm
            </p>
          </div>

          <!-- Right: Registration Form -->
          <div class="bg-white rounded-3xl p-8 shadow-[0_20px_50px_rgba(0,0,0,0.03)] border border-gray-100 flex flex-col">
            <h2 class="text-sm font-bold text-gray-800 uppercase tracking-widest mb-6">Thông tin hồ sơ</h2>
            
            <div class="space-y-6 flex-1">
              <!-- Name Input -->
              <div>
                <label class="block text-xs font-black text-gray-400 uppercase tracking-widest mb-2">Họ và tên nhân sự</label>
                <input type="text" [(ngModel)]="fullName" placeholder="VD: Nguyễn Văn A" class="w-full bg-gray-50 border border-gray-200 rounded-xl px-4 py-3 text-sm font-bold text-gray-800 focus:bg-white focus:outline-none focus:border-emerald-500 focus:ring-4 focus:ring-emerald-500/10 transition-all">
              </div>

              <!-- Required Angles -->
              <div>
                <label class="block text-xs font-black text-gray-400 uppercase tracking-widest mb-3" *ngIf="!isSubmitting()">Sẵn sàng thu thập</label>
                <label class="block text-xs font-black text-emerald-600 uppercase tracking-widest mb-3" *ngIf="isSubmitting()">Đang thu thập: {{ currentAngles() }} / {{ minRequired() }} góc (Tối đa {{ maxAllowed() }})</label>
                
                <div class="relative w-full h-10 bg-gray-100 rounded-2xl overflow-hidden border border-gray-200">
                  <!-- Progress Fill -->
                  <div class="absolute top-0 left-0 h-full bg-gradient-to-r from-emerald-400 to-emerald-500 transition-all duration-500 shadow-[inset_0_2px_4px_rgba(255,255,255,0.3)]" 
                       [style.width]="(currentAngles() / (minRequired() || 1)) * 100 + '%'"></div>
                  
                  <!-- Percent Text -->
                  <div class="absolute inset-0 flex items-center justify-center text-xs font-bold font-mono z-10" 
                       [ngClass]="currentAngles() > (minRequired() / 2) ? 'text-white' : 'text-gray-600'">
                    {{ Math.round((currentAngles() / (minRequired() || 1)) * 100) }}%
                  </div>
                </div>
                <p class="text-xs text-gray-400 mt-2 italic font-medium" *ngIf="isSubmitting()">Vui lòng quay mặt sang trái, phải từ từ trước Camera AI...</p>
              </div>
            </div>

            <!-- Status Alert -->
            <div *ngIf="message()" class="mb-4 mt-4 p-3 rounded-xl border text-xs font-bold flex items-center gap-2" [ngClass]="status() === 'success' ? 'bg-emerald-50 border-emerald-200 text-emerald-700' : 'bg-red-50 border-red-200 text-red-700'">
              <lucide-icon [name]="status() === 'success' ? CheckCircleIcon : XCircleIcon" class="w-4 h-4"></lucide-icon>
              {{ message() }}
            </div>

            <!-- Submit Button -->
            <button (click)="submit()" 
                    [disabled]="isSubmitting() || !isFormValid()"
                    class="w-full mt-6 bg-gray-900 hover:bg-emerald-600 text-white font-bold py-4 rounded-xl shadow-xl shadow-gray-200 hover:shadow-emerald-500/30 transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:grayscale">
              <span *ngIf="!isSubmitting()" class="uppercase tracking-widest text-[11px]">Đồng bộ dữ liệu AI</span>
              <span *ngIf="isSubmitting()" class="w-4 h-4 border-2 border-white/20 border-t-white rounded-full animate-spin"></span>
              <lucide-icon *ngIf="!isSubmitting()" [name]="ChevronRightIcon" class="w-4 h-4"></lucide-icon>
            </button>
          </div>

        </div>
      </main>
    </div>
  `
})
export class FaceRegistrationComponent implements OnInit, OnDestroy {
  @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasElement') canvasElement!: ElementRef<HTMLCanvasElement>;

  readonly ArrowLeftIcon = ArrowLeft;
  readonly CameraIcon = Camera;
  readonly UserIcon = User;
  readonly CheckCircleIcon = CheckCircle2;
  readonly XCircleIcon = XCircle;
  readonly ChevronRightIcon = ChevronRight;
  readonly MonitorDeviceIcon = MonitorSmartphone;

  streamActive = signal(false);
  fullName = '';
  
  currentAngles = signal<number>(0);
  minRequired = signal<number>(5);
  maxAllowed = signal<number>(10);
  Math = Math;

  isSubmitting = signal(false);
  message = signal<string>('');
  status = signal<'success' | 'error' | ''>('');

  private mediaStream: MediaStream | null = null;

  // Quản lý thiết bị
  videoDevices: MediaDeviceInfo[] = [];
  selectedDeviceId: string = '';

  constructor(private router: Router, private regService: FaceRegistrationService) {}

  ngOnInit() {
    this.enumerateDevices();
  }

  async enumerateDevices() {
    try {
      if (!navigator.mediaDevices || !navigator.mediaDevices.enumerateDevices) {
        console.warn('enumerateDevices is not supported by this browser.');
        return;
      }
      // Phải yêu cầu quyền trước để trình duyệt trả tên thiết bị thực tế
      await navigator.mediaDevices.getUserMedia({ video: true, audio: false });
      
      const devices = await navigator.mediaDevices.enumerateDevices();
      this.videoDevices = devices.filter(device => device.kind === 'videoinput');
      
      if (this.videoDevices.length > 0) {
        this.selectedDeviceId = this.videoDevices[0].deviceId;
      }
    } catch (err) {
      console.error('Lấy thiết bị thất bại:', err);
    }
  }

  async startCamera() {
    try {
      this.stopCamera();

      const constraints: MediaStreamConstraints = {
        video: this.selectedDeviceId ? { deviceId: { exact: this.selectedDeviceId }, width: 640, height: 480 } : { facingMode: 'user', width: 640, height: 480 }
      };

      this.mediaStream = await navigator.mediaDevices.getUserMedia(constraints);
      if (this.videoElement) {
        this.videoElement.nativeElement.srcObject = this.mediaStream;
        this.streamActive.set(true);
      }
    } catch (err) {
      console.error('Lỗi khi mở camera:', err);
      this.showMessage('Không thể truy cập Camera. Vui lòng cấp quyền hoặc kiểm tra kết nối.', 'error');
    }
  }

  switchCamera() {
    if (this.streamActive()) {
      this.startCamera();
    }
  }

  isFormValid(): boolean {
    return this.fullName.trim().length > 0;
  }

  private pollInterval: any;

  async submit() {
    if (!this.isFormValid()) return;
    
    this.isSubmitting.set(true);
    this.message.set('Đang kích hoạt thu thập từ Camera AI...');
    this.status.set('');
    this.currentAngles.set(0);

    this.regService.startEnroll(this.fullName).subscribe({
      next: (res) => {
        this.showMessage('Đã bắt đầu. Vui lòng nhìn vào Camera AI và quay mặt từ từ.', 'success');
        
        // Bắt đầu poll
        this.pollInterval = setInterval(() => {
          this.regService.getEnrollStatus().subscribe({
            next: (statusData) => {
              if (statusData && statusData.progress) {
                const prog = statusData.progress;
                this.currentAngles.set(prog.currentAngles);
                this.minRequired.set(prog.minRequired);
                this.maxAllowed.set(prog.maxAllowed);

                if (!statusData.active || prog.isComplete || prog.currentAngles >= prog.minRequired) {
                  clearInterval(this.pollInterval);
                  this.isSubmitting.set(false);
                  this.showMessage('Đã thu thập đủ góc và đăng ký thành công!', 'success');
                  this.fullName = '';
                }
              } else if (statusData && !statusData.active && this.currentAngles() > 0) {
                  clearInterval(this.pollInterval);
                  this.isSubmitting.set(false);
                  this.showMessage('Đăng ký kết thúc nhưng không rõ trạng thái.', 'error');
              }
            },
            error: () => {
              // Ignore poll errors to avoid spam
            }
          });
        }, 1500);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err?.error?.message || 'Không thể kết nối dịch vụ AI.';
        this.showMessage(msg, 'error');
      }
    });
  }

  private showMessage(msg: string, type: 'success' | 'error') {
    this.message.set(msg);
    this.status.set(type);
  }

  goBack() {
    this.router.navigate(['/']);
  }

  stopCamera() {
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop());
      this.streamActive.set(false);
    }
  }

  ngOnDestroy() {
    this.stopCamera();
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
    }
  }
}
