import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { FilterRequest } from '../../core/models/track.model';

@Component({
  selector: 'app-filters-dialog',
  imports: [
    FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatChipsModule, MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>Filtros inteligentes</h2>
    <mat-dialog-content class="dialog-content">
      <p class="hint">
        O filtro varre toda a sua biblioteca e marca as músicas que batem com qualquer critério.
        Você revisa antes de remover.
      </p>

      <mat-form-field appearance="outline">
        <mat-label>Curtidas antes de</mat-label>
        <input matInput type="date" [(ngModel)]="addedBefore" />
        <mat-hint>Faixas adicionadas antes desta data</mat-hint>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Artista repete pelo menos</mat-label>
        <input matInput type="number" min="2" [(ngModel)]="minOccurrences" />
        <mat-hint>Marca faixas cujo artista aparece N+ vezes nas curtidas</mat-hint>
      </mat-form-field>

      <mat-form-field appearance="outline">
        <mat-label>Excluir gêneros (separados por vírgula)</mat-label>
        <input matInput type="text" [(ngModel)]="genresText" placeholder="ex.: country, sertanejo" />
      </mat-form-field>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button mat-button (click)="close()">Cancelar</button>
      <button mat-flat-button color="primary" (click)="apply()">
        <mat-icon>filter_alt</mat-icon>
        Aplicar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dialog-content {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      min-width: 360px;
    }
    .hint {
      color: rgba(0, 0, 0, 0.6);
      margin: 0 0 0.5rem;
    }
  `],
})
export class FiltersDialogComponent {
  private readonly ref = inject(MatDialogRef<FiltersDialogComponent, FilterRequest>);

  addedBefore = signal<string>('');
  minOccurrences = signal<number | null>(null);
  genresText = signal<string>('');

  close() { this.ref.close(); }

  apply() {
    const genres = this.genresText()
      .split(',')
      .map(s => s.trim())
      .filter(s => s.length > 0);

    const request: FilterRequest = {
      addedBefore: this.addedBefore() ? new Date(this.addedBefore()).toISOString() : undefined,
      minArtistOccurrences: this.minOccurrences() ?? undefined,
      excludeGenres: genres.length > 0 ? genres : undefined,
    };
    this.ref.close(request);
  }
}
