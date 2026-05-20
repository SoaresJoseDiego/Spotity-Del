import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import html2canvas from 'html2canvas-pro';

import { DashboardOverview } from '../../core/models/dashboard.model';

interface ShareDialogData {
  overview: DashboardOverview;
  userName: string | null;
}

@Component({
  selector: 'app-share-dialog',
  imports: [MatButtonModule, MatDialogModule, MatIconModule, MatProgressBarModule],
  template: `
    <h2 mat-dialog-title>Compartilhar seu retrato musical</h2>

    <mat-dialog-content class="dlg-content">
      @if (generating()) { <mat-progress-bar mode="indeterminate" /> }

      <div class="card-wrap">
        <div class="share-card" #shareCard>
          <div class="bg-blur"></div>

          <header class="hdr">
            <div class="brand">
              <span class="dot"></span>
              SpotifyDel
            </div>
            <div class="range">{{ rangeLabel() }}</div>
          </header>

          <h1 class="title">Meu retrato<br><span class="accent">musical</span></h1>

          @if (data.overview.topArtists.length > 0) {
            <div class="grid">
              @for (a of data.overview.topArtists.slice(0, 9); track a.id; let i = $index) {
                <div class="cell">
                  @if (a.imageUrl) {
                    <img [src]="a.imageUrl" [alt]="a.name" crossorigin="anonymous" />
                  } @else {
                    <div class="fallback">{{ initials(a.name) }}</div>
                  }
                  <div class="rank">#{{ i + 1 }}</div>
                  <div class="name">{{ a.name }}</div>
                </div>
              }
            </div>
          }

          @if (topGenre()) {
            <div class="genre-block">
              <div class="genre-label">gênero dominante</div>
              <div class="genre-value">{{ topGenre() }}</div>
            </div>
          }

          <footer class="ftr">
            <span>{{ data.userName ?? 'me' }} · gerado por SpotifyDel</span>
          </footer>
        </div>
      </div>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Fechar</button>
      <button mat-flat-button color="primary" (click)="download()" [disabled]="generating()">
        <mat-icon>download</mat-icon>
        Baixar imagem
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    :host { display: block; }
    .dlg-content {
      padding: 0 1rem 1rem;
      max-height: 78vh;
      max-width: 95vw;
    }
    .card-wrap {
      display: flex;
      justify-content: center;
      padding: 1rem 0;
    }

    /* Fixed dimensions so html2canvas-pro gets a stable, high-res capture */
    .share-card {
      position: relative;
      width: 1080px;
      height: 1350px;
      padding: 80px 80px 60px;
      background:
        radial-gradient(60% 40% at 100% 0%,   rgba(29,185,84,0.55),  transparent 60%),
        radial-gradient(50% 35% at 0% 100%,   rgba(124,92,255,0.45), transparent 60%),
        linear-gradient(135deg, #0a0a0c 0%, #15161a 100%);
      color: #fff;
      display: flex;
      flex-direction: column;
      font-family: Roboto, "Helvetica Neue", sans-serif;
      overflow: hidden;

      /* Scaled-down preview in the dialog (var so we can override responsively) */
      --preview-scale: 0.55;
      transform: scale(var(--preview-scale));
      transform-origin: top left;
    }
    .bg-blur {
      position: absolute; inset: 0;
      background: radial-gradient(40% 30% at 50% 40%, rgba(29,185,84,0.10), transparent);
      pointer-events: none;
    }

    .hdr {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 28px;
      letter-spacing: 0.02em;
    }
    .brand {
      display: inline-flex;
      align-items: center;
      gap: 14px;
      font-weight: 600;
      .dot {
        width: 18px; height: 18px;
        border-radius: 50%;
        background: #1db954;
        box-shadow: 0 0 24px rgba(29,185,84,0.8);
      }
    }
    .range {
      opacity: 0.7;
      font-size: 22px;
      text-transform: uppercase;
      letter-spacing: 0.18em;
    }

    .title {
      font-size: 132px;
      line-height: 0.95;
      font-weight: 700;
      letter-spacing: -0.03em;
      margin: 60px 0 60px;
      .accent {
        background: linear-gradient(135deg, #1db954, #7c5cff);
        background-clip: text;
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
      }
    }

    .grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 24px;
      margin-bottom: 50px;
    }
    .cell {
      position: relative;
      border-radius: 16px;
      overflow: hidden;
      aspect-ratio: 1 / 1;
      background: rgba(255,255,255,0.08);
      isolation: isolate;
    }
    .cell img, .cell .fallback {
      width: 100%; height: 100%; object-fit: cover;
      display: block;
    }
    .cell .fallback {
      display: grid; place-items: center;
      background: linear-gradient(135deg, #1db954, #7c5cff);
      font-size: 80px;
      font-weight: 700;
    }
    .cell::after {
      content: '';
      position: absolute; inset: 0;
      background: linear-gradient(180deg, transparent 50%, rgba(0,0,0,0.85) 100%);
    }
    .cell .rank {
      position: absolute;
      top: 14px; left: 14px;
      background: #fff;
      color: #000;
      font-weight: 700;
      font-size: 22px;
      padding: 4px 12px;
      border-radius: 999px;
      z-index: 2;
    }
    .cell .name {
      position: absolute;
      bottom: 16px; left: 18px; right: 18px;
      font-size: 26px;
      font-weight: 600;
      line-height: 1.1;
      z-index: 2;
      text-shadow: 0 2px 4px rgba(0,0,0,0.6);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .genre-block {
      margin-top: auto;
    }
    .genre-label {
      font-size: 22px;
      text-transform: uppercase;
      letter-spacing: 0.2em;
      opacity: 0.6;
      margin-bottom: 8px;
    }
    .genre-value {
      font-size: 60px;
      font-weight: 600;
      background: linear-gradient(135deg, #1db954, #1ed760);
      background-clip: text;
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .ftr {
      margin-top: 30px;
      font-size: 22px;
      opacity: 0.55;
      letter-spacing: 0.05em;
    }

    /* Reserve the right amount of space for the scaled-down preview */
    .card-wrap {
      width: calc(1080px * 0.55);
      height: calc(1350px * 0.55);
    }
    @media (max-width: 720px) {
      .share-card { --preview-scale: 0.32; }
      .card-wrap  { width: calc(1080px * 0.32); height: calc(1350px * 0.32); }
    }
  `],
})
export class ShareDialogComponent {
  protected readonly data = inject<ShareDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ShareDialogComponent>);

  readonly shareCard = viewChild<ElementRef<HTMLElement>>('shareCard');
  readonly generating = signal(false);

  rangeLabel(): string {
    switch (this.data.overview.timeRange) {
      case 'short_term':  return '4 SEMANAS';
      case 'medium_term': return '6 MESES';
      case 'long_term':   return 'DESDE SEMPRE';
    }
  }

  topGenre(): string | null {
    const g = this.data.overview.genres[0];
    if (!g) return null;
    return g.genre.charAt(0).toUpperCase() + g.genre.slice(1);
  }

  initials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  close() { this.ref.close(); }

  async download() {
    const el = this.shareCard()?.nativeElement;
    if (!el) return;
    this.generating.set(true);

    const previousTransform = el.style.transform;
    el.style.transform = 'none';

    try {
      const canvas = await html2canvas(el, {
        useCORS: true,
        allowTaint: false,
        backgroundColor: null,
        width: 1080,
        height: 1350,
        scale: 1,
      });

      const blob: Blob | null = await new Promise(r => canvas.toBlob(r, 'image/png'));
      if (!blob) throw new Error('Failed to encode PNG');

      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `spotifydel-${this.data.overview.timeRange}-${this.dateStamp()}.png`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Image generation failed', err);
      alert('Não foi possível gerar a imagem. Algumas imagens do Spotify podem estar bloqueando CORS.');
    } finally {
      el.style.transform = previousTransform;
      this.generating.set(false);
    }
  }

  private dateStamp(): string {
    const d = new Date();
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}`;
  }
}
