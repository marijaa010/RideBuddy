import { Component, Input, Output, EventEmitter, forwardRef, OnDestroy } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { GeocodingService, GeocodingResult } from '../../services/geocoding.service';

export interface LocationSelection {
  name: string;
  latitude: number;
  longitude: number;
}

@Component({
  selector: 'app-location-autocomplete',
  templateUrl: './location-autocomplete.component.html',
  styleUrls: ['./location-autocomplete.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => LocationAutocompleteComponent),
      multi: true
    }
  ]
})
export class LocationAutocompleteComponent implements ControlValueAccessor, OnDestroy {
  @Input() placeholder: string = 'Search for a location...';
  @Input() label: string = '';
  @Output() locationSelected = new EventEmitter<LocationSelection | null>();

  searchQuery: string = '';
  searchResults: GeocodingResult[] = [];
  showResults: boolean = false;
  isSearching: boolean = false;
  selectedLocation: LocationSelection | null = null;

  private searchSubject = new Subject<string>();
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(private geocodingService: GeocodingService) {
    this.setupSearchDebounce();
  }

  ngOnDestroy(): void {
    this.searchSubject.complete();
  }

  // ControlValueAccessor implementation
  writeValue(value: string): void {
    this.searchQuery = value || '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  private setupSearchDebounce(): void {
    this.searchSubject
      .pipe(
        debounceTime(500),
        distinctUntilChanged()
      )
      .subscribe(query => {
        this.performSearch(query);
      });
  }

  onSearchInput(event: Event): void {
    const query = (event.target as HTMLInputElement).value;
    this.searchQuery = query;
    this.searchSubject.next(query);
    this.onChange(query);

    // Clear selection if user modifies text
    if (this.selectedLocation && query !== this.selectedLocation.name) {
      this.selectedLocation = null;
      this.locationSelected.emit(null);
    }
  }

  private performSearch(query: string): void {
    if (!query || query.trim().length < 3) {
      this.searchResults = [];
      this.showResults = false;
      return;
    }

    this.isSearching = true;
    this.geocodingService.searchLocation(query).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.showResults = results.length > 0;
        this.isSearching = false;
      },
      error: () => {
        this.isSearching = false;
        this.searchResults = [];
        this.showResults = false;
      }
    });
  }

  selectSearchResult(result: GeocodingResult): void {
    const name = this.geocodingService.getSimplifiedName(result);
    this.searchQuery = name;
    this.selectedLocation = {
      name,
      latitude: result.lat,
      longitude: result.lon
    };
    this.showResults = false;
    this.searchResults = [];
    this.onChange(name);
    this.locationSelected.emit(this.selectedLocation);
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchResults = [];
    this.showResults = false;
    this.selectedLocation = null;
    this.onChange('');
    this.locationSelected.emit(null);
  }

  onBlur(): void {
    this.onTouched();
    // Delay to allow click on result
    setTimeout(() => {
      this.showResults = false;
    }, 300);
  }

  onResultMouseDown(result: GeocodingResult): void {
    this.selectSearchResult(result);
  }
}
