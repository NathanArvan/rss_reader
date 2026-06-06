import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Item, ItemWithSource } from '../../models/api.models';

/**
 * Presentational list of items, reused by the combined feed and the per-source
 * view. Each row links to the item detail. Set `showSource` to display the
 * originating feed's title (combined feed only).
 */
@Component({
  selector: 'app-item-list',
  imports: [DatePipe, RouterLink],
  templateUrl: './item-list.component.html',
  styleUrl: './item-list.component.scss',
})
export class ItemListComponent {
  @Input({ required: true }) items: Array<Item | ItemWithSource> = [];
  @Input() showSource = false;

  sourceTitle(item: Item | ItemWithSource): string | null {
    return 'sourceTitle' in item ? item.sourceTitle : null;
  }
}
