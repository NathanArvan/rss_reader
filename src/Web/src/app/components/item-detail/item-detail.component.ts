import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { ItemsService } from '../../services/items.service';
import { Item } from '../../models/api.models';

/**
 * Item detail view. Loads the item by id, auto-marks it read on open (with a
 * manual toggle to undo), renders the feed summary, and links out to the source.
 * The summary is bound via [innerHTML], which Angular sanitizes automatically —
 * do not bypass the sanitizer.
 */
@Component({
  selector: 'app-item-detail',
  imports: [DatePipe],
  templateUrl: './item-detail.component.html',
  styleUrl: './item-detail.component.scss',
})
export class ItemDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly items = inject(ItemsService);

  readonly item = signal<Item | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.items.get(id).subscribe({
      next: (item) => {
        this.item.set(item);
        if (!item.isRead) this.markRead(true);
      },
      error: (err) => this.error.set(err.message ?? 'Could not load item'),
    });
  }

  toggleRead(): void {
    const current = this.item();
    if (current) this.markRead(!current.isRead);
  }

  private markRead(read: boolean): void {
    const current = this.item();
    if (!current) return;
    this.items.markRead(current.id, read).subscribe({
      next: (updated) => this.item.set(updated),
    });
  }
}
