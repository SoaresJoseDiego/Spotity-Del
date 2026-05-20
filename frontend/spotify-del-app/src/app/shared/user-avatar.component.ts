import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-user-avatar',
  template: `
    @if (url()) {
      <img [src]="url()!" [alt]="name() ?? ''"
           [style.width.px]="size()" [style.height.px]="size()" class="img" />
    } @else {
      <div class="placeholder"
           [style.width.px]="size()"
           [style.height.px]="size()"
           [style.font-size.px]="size() / 2.4"
           [style.background]="bg()">
        {{ initials() }}
      </div>
    }
  `,
  styles: [`
    :host { display: inline-block; line-height: 0; }
    .img {
      border-radius: 50%;
      object-fit: cover;
    }
    .placeholder {
      border-radius: 50%;
      display: grid;
      place-items: center;
      color: #fff;
      font-weight: 600;
      user-select: none;
      letter-spacing: 0.02em;
    }
  `],
})
export class UserAvatarComponent {
  readonly url = input<string | null>(null);
  readonly name = input<string | null>(null);
  readonly size = input<number>(32);

  readonly initials = computed(() => {
    const n = (this.name() ?? '').trim();
    if (!n) return '?';
    const parts = n.split(/\s+/).filter(Boolean);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  });

  // Hash name to a stable hue so each user gets a consistent color.
  readonly bg = computed(() => {
    const n = this.name() ?? 'unknown';
    let hash = 0;
    for (let i = 0; i < n.length; i++) hash = (hash * 31 + n.charCodeAt(i)) >>> 0;
    const hue = hash % 360;
    return `linear-gradient(135deg, hsl(${hue}, 65%, 45%), hsl(${(hue + 40) % 360}, 70%, 55%))`;
  });
}
