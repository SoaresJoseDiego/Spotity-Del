import { Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-empty-state',
  imports: [MatIconModule],
  template: `
    <div class="empty">
      <div class="art">
        <div class="pulse"></div>
        <mat-icon [class]="'icon ' + tone()">{{ icon() }}</mat-icon>
      </div>
      <h3>{{ title() }}</h3>
      @if (description()) { <p>{{ description() }}</p> }
      <ng-content></ng-content>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      text-align: center;
      gap: 0.75rem;
      padding: 3rem 1rem;
      color: var(--app-text-muted);
    }
    .art {
      position: relative;
      width: 96px; height: 96px;
      display: grid;
      place-items: center;
      margin-bottom: 0.5rem;
    }
    .pulse {
      position: absolute;
      inset: 0;
      border-radius: 50%;
      background: radial-gradient(circle, rgba(29,185,84,0.15), transparent 65%);
      animation: pulse 2.5s ease-in-out infinite;
    }
    .icon {
      position: relative;
      font-size: 48px; width: 48px; height: 48px;
      color: var(--app-accent);
      &.muted { color: var(--app-text-muted); opacity: 0.7; }
    }
    h3 { margin: 0; color: var(--app-text); font-weight: 500; }
    p  { margin: 0; max-width: 380px; line-height: 1.5; }
    @keyframes pulse {
      0%, 100% { transform: scale(1);   opacity: 0.6; }
      50%      { transform: scale(1.15); opacity: 1;   }
    }
  `],
})
export class EmptyStateComponent {
  readonly icon = input<string>('inbox');
  readonly title = input<string>('Nada por aqui');
  readonly description = input<string | null>(null);
  readonly tone = input<'accent' | 'muted'>('accent');
}
