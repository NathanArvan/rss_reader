import { Component, OnInit, inject, signal } from '@angular/core';
import { ItemsService } from '../../services/items.service';
import { ItemWithSource } from '../../models/api.models';
import { ItemListComponent } from '../item-list/item-list.component';

/** Combined "All items" feed across every source — the default landing view. */
@Component({
  selector: 'app-all-items',
  imports: [ItemListComponent],
  templateUrl: './all-items.component.html',
})
export class AllItemsComponent implements OnInit {
  private readonly items = inject(ItemsService);

  readonly feed = signal<ItemWithSource[] | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.items.listAll().subscribe({
      next: (items) => this.feed.set(items),
      error: (err) => this.error.set(err.message ?? 'Could not load items'),
    });
  }
}
