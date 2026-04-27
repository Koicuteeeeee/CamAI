import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, User, Lock, Eye, EyeOff, ShieldCheck, Cpu, ArrowRight } from 'lucide-angular';
import { CustomAuthService } from '../../core/auth/custom-auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  template: `
    <div class="login-container relative min-h-screen flex items-center justify-center overflow-hidden bg-[#f8fafc] font-['Inter',sans-serif]">
      <!-- Soft Ambient Accents -->
      <div class="absolute inset-0 z-0">
        <div class="absolute top-[-10%] right-[-5%] w-[400px] h-[400px] bg-emerald-500/5 rounded-full blur-[100px]"></div>
        <div class="absolute bottom-[-10%] left-[-5%] w-[400px] h-[400px] bg-blue-500/5 rounded-full blur-[100px]"></div>
      </div>

      <!-- Main Login Card -->
      <div class="relative z-10 w-full max-w-[440px] px-6">
        <div class="bg-white rounded-[40px] p-10 shadow-[0_20px_50px_rgba(0,0,0,0.04)] border border-gray-100 flex flex-col items-center">
          
          <!-- Branding Area -->
          <div class="flex flex-col items-center mb-10 w-full">
            <div class="w-16 h-16 rounded-3xl bg-emerald-500 flex items-center justify-center mb-6 shadow-lg shadow-emerald-500/20">
              <lucide-icon [name]="CpuIcon" class="w-8 h-8 text-white"></lucide-icon>
            </div>
            <h1 class="text-4xl font-black text-gray-900 tracking-tight mb-2 italic">
              CAMAI<span class="text-emerald-500 not-italic ml-1">.CMS</span>
            </h1>
            <p class="text-gray-400 text-[10px] font-black uppercase tracking-[0.3em]">Hệ thống quản trị an ninh</p>
          </div>

          <!-- Form Area -->
          <form (submit)="onSubmit()" class="w-full space-y-6">
            <!-- Username -->
            <div class="space-y-2 text-center">
              <div class="relative group">
                <div class="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none text-gray-300 group-focus-within:text-emerald-500 transition-colors">
                  <lucide-icon [name]="UserIcon" class="w-4 h-4"></lucide-icon>
                </div>
                <input 
                  type="text" 
                  [(ngModel)]="username" 
                  name="username"
                  placeholder="USERNAME"
                  class="w-full bg-gray-50 border border-gray-200 rounded-2xl py-4 pl-12 pr-4 text-gray-900 text-xs font-bold tracking-widest placeholder:text-gray-300 focus:outline-none focus:border-emerald-500/50 focus:bg-white focus:ring-4 focus:ring-emerald-500/5 transition-all"
                >
              </div>
            </div>

            <!-- Password -->
            <div class="space-y-2 text-center">
              <div class="relative group">
                <div class="absolute inset-y-0 left-0 pl-5 flex items-center pointer-events-none text-gray-300 group-focus-within:text-emerald-500 transition-colors">
                  <lucide-icon [name]="LockIcon" class="w-4 h-4"></lucide-icon>
                </div>
                <input 
                  [type]="showPassword ? 'text' : 'password'" 
                  [(ngModel)]="password" 
                  name="password"
                  placeholder="PASSWORD"
                  class="w-full bg-gray-50 border border-gray-200 rounded-2xl py-4 pl-12 pr-12 text-gray-900 text-xs font-bold tracking-widest placeholder:text-gray-300 focus:outline-none focus:border-emerald-500/50 focus:bg-white focus:ring-4 focus:ring-emerald-500/5 transition-all"
                >
                <button 
                  type="button"
                  (click)="showPassword = !showPassword"
                  class="absolute inset-y-0 right-0 pr-5 flex items-center text-gray-300 hover:text-emerald-500 transition-colors"
                >
                  <lucide-icon [name]="showPassword ? EyeOffIcon : EyeIcon" class="w-4 h-4"></lucide-icon>
                </button>
              </div>
            </div>

            <!-- Error Feedback -->
            <div *ngIf="authService.error()" class="text-center animate-shake">
               <p class="text-red-500 text-[10px] font-bold uppercase tracking-widest">{{ authService.error() }}</p>
            </div>

            <!-- CTA Button -->
            <div class="pt-4">
              <button 
                type="submit" 
                [disabled]="authService.isLoading()"
                class="w-full bg-gray-900 hover:bg-emerald-600 text-white font-bold py-4 rounded-2xl shadow-xl shadow-gray-200 hover:shadow-emerald-500/20 transition-all flex items-center justify-center gap-3 active:scale-[0.98] disabled:opacity-50 disabled:grayscale"
              >
                <span *ngIf="!authService.isLoading()" class="tracking-[0.2em] uppercase text-[11px]">Đăng nhập hệ thống</span>
                <span *ngIf="authService.isLoading()" class="w-4 h-4 border-2 border-white/20 border-t-white rounded-full animate-spin"></span>
                <lucide-icon *ngIf="!authService.isLoading()" [name]="ArrowIcon" class="w-4 h-4"></lucide-icon>
              </button>
            </div>

            <!-- FaceID Simulation -->
            <div class="flex flex-col items-center gap-4 pt-6 opacity-30 hover:opacity-100 transition-opacity cursor-pointer group">
                <div class="w-10 h-10 rounded-full border border-gray-200 flex items-center justify-center group-hover:border-emerald-500/50 group-hover:bg-emerald-500/5 transition-all">
                  <lucide-icon [name]="ShieldIcon" class="w-4 h-4 text-gray-400 group-hover:text-emerald-500"></lucide-icon>
                </div>
                <span class="text-[9px] text-gray-400 font-bold tracking-[0.2em] uppercase">Xác thực khuôn mặt</span>
            </div>
          </form>
        </div>

        <!-- Footer -->
        <p class="text-center mt-12 text-gray-300 text-[10px] font-bold uppercase tracking-widest leading-loose">
          &copy; 2026 CamAI Security Ecosystem<br>
          <span class="opacity-50 text-[8px]">Ad Astra Per Aspera</span>
        </p>
      </div>
    </div>
  `,
  styles: [`
    @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700;900&display=swap');

    .animate-shake {
      animation: shake 0.5s cubic-bezier(.36,.07,.19,.97) both;
    }

    @keyframes shake {
      10%, 90% { transform: translate3d(-1px, 0, 0); }
      20%, 80% { transform: translate3d(2px, 0, 0); }
      30%, 50%, 70% { transform: translate3d(-2px, 0, 0); }
      40%, 60% { transform: translate3d(2px, 0, 0); }
    }
  `]
})
export class LoginComponent {
  username = '';
  password = '';
  showPassword = false;

  readonly UserIcon = User;
  readonly LockIcon = Lock;
  readonly EyeIcon = Eye;
  readonly EyeOffIcon = EyeOff;
  readonly ShieldIcon = ShieldCheck;
  readonly CpuIcon = Cpu;
  readonly ArrowIcon = ArrowRight;

  constructor(public authService: CustomAuthService) {}

  onSubmit() {
    if (!this.username || !this.password) return;
    this.authService.loginDirect(this.username, this.password).subscribe();
  }
}
