import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-skeleton-loader',
  templateUrl: './skeleton-loader.component.html',
  styleUrls: ['./skeleton-loader.component.scss']
})
export class SkeletonLoaderComponent {
  @Input() type: 'card' | 'list' | 'text' = 'card';
  @Input() count: number = 3;

  get items(): number[] {
    return Array(this.count).fill(0).map((_, i) => i);
  }
}
