import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ItemsService } from '../../services/items.service';
import { Item } from '../../models/api.models';
import { ItemListComponent } from '../item-list/item-list.component';

/** Items belonging to a single source. */
@Component({
  selector: 'app-source-items',
  imports: [ItemListComponent],
  templateUrl: './source-items.component.html',
})
export class SourceItemsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly items = inject(ItemsService);

  readonly list = signal<Item[] | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.items.listBySource(id).subscribe({
      next: (items) => this.list.set(items),
      error: (err) => this.error.set(err.message ?? 'Could not load items'),
    });
  }
}
