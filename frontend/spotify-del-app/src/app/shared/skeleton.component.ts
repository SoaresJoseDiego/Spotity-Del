import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  template: `<div class="sk" [style.width]="width()" [style.height]="height()" [style.border-radius]="radius()"></div>`,
  styles: [`
    :host { display: block; line-height: 0; }
    .sk {
      background: linear-gradient(90deg, var(--app-hover) 0%, var(--app-border) 50%, var(--app-hover) 100%);
      background-size: 200% 100%;
      animation: shimmer 1.4s infinite linear;
      will-change: background-position;
    }
    @keyframes shimmer {
      0%   { background-position: 200% 0; }
      100% { background-position: -200% 0; }
    }
  `],
})
export class SkeletonComponent {
  readonly width  = input<string>('100%');
  readonly height = input<string>('16px');
  readonly radius = input<string>('4px');
}
