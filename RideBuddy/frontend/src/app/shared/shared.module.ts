import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingSpinnerComponent } from './components/loading-spinner/loading-spinner.component';
import { SkeletonLoaderComponent } from './components/skeleton-loader/skeleton-loader.component';
import { ToastComponent } from './components/toast/toast.component';

@NgModule({
  declarations: [
    LoadingSpinnerComponent,
    SkeletonLoaderComponent,
    ToastComponent
  ],
  imports: [
    CommonModule
  ],
  exports: [
    LoadingSpinnerComponent,
    SkeletonLoaderComponent,
    ToastComponent
  ]
})
export class SharedModule { }
