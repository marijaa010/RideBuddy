import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { LoadingSpinnerComponent } from './components/loading-spinner/loading-spinner.component';
import { SkeletonLoaderComponent } from './components/skeleton-loader/skeleton-loader.component';
import { ToastComponent } from './components/toast/toast.component';
import { MapPickerComponent } from './components/map-picker/map-picker.component';
import { RouteMapComponent } from './components/route-map/route-map.component';
import { LocationAutocompleteComponent } from './components/location-autocomplete/location-autocomplete.component';

@NgModule({
  declarations: [
    LoadingSpinnerComponent,
    SkeletonLoaderComponent,
    ToastComponent,
    MapPickerComponent,
    RouteMapComponent,
    LocationAutocompleteComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    HttpClientModule
  ],
  exports: [
    LoadingSpinnerComponent,
    SkeletonLoaderComponent,
    ToastComponent,
    MapPickerComponent,
    RouteMapComponent,
    LocationAutocompleteComponent
  ]
})
export class SharedModule { }
