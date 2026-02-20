import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import * as L from 'leaflet';
import { GeocodingService, GeocodingResult } from '../../services/geocoding.service';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

export interface LocationData {
  name: string;
  latitude: number;
  longitude: number;
}

@Component({
  selector: 'app-map-picker',
  templateUrl: './map-picker.component.html',
  styleUrls: ['./map-picker.component.scss']
})
export class MapPickerComponent implements OnInit, OnDestroy {
  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef;

  @Input() label: string = 'Location';
  @Input() placeholder: string = 'Search for a location...';
  @Input() initialLocation?: LocationData;
  @Output() locationSelected = new EventEmitter<LocationData>();

  private map!: L.Map;
  private marker?: L.Marker;

  searchQuery: string = '';
  searchResults: GeocodingResult[] = [];
  showResults: boolean = false;
  isSearching: boolean = false;

  private searchSubject = new Subject<string>();

  // Default center (Belgrade, Serbia)
  private defaultCenter: L.LatLngExpression = [44.7866, 20.4489];
  private defaultZoom = 7;

  constructor(private geocodingService: GeocodingService) {}

  ngOnInit(): void {
    this.initMap();
    this.setupSearchDebounce();

    if (this.initialLocation) {
      this.setLocation(
        this.initialLocation.latitude,
        this.initialLocation.longitude,
        this.initialLocation.name
      );
      this.searchQuery = this.initialLocation.name;
    }
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
    this.searchSubject.complete();
  }

  private initMap(): void {
    // Initialize map
    this.map = L.map(this.mapContainer.nativeElement, {
      center: this.defaultCenter,
      zoom: this.defaultZoom
    });

    // Add OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    // Handle map clicks
    this.map.on('click', (e: L.LeafletMouseEvent) => {
      this.onMapClick(e.latlng.lat, e.latlng.lng);
    });

    // Fix marker icon issue with Leaflet in Angular
    this.fixMarkerIcons();
  }

  private fixMarkerIcons(): void {
    const iconRetinaUrl = 'assets/marker-icon-2x.png';
    const iconUrl = 'assets/marker-icon.png';
    const shadowUrl = 'assets/marker-shadow.png';

    const iconDefault = L.icon({
      iconRetinaUrl,
      iconUrl,
      shadowUrl,
      iconSize: [25, 41],
      iconAnchor: [12, 41],
      popupAnchor: [1, -34],
      tooltipAnchor: [16, -28],
      shadowSize: [41, 41]
    });

    L.Marker.prototype.options.icon = iconDefault;
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
    this.setLocation(result.lat, result.lon, name);
    this.searchQuery = name;
    this.showResults = false;
    this.searchResults = [];
  }

  private onMapClick(lat: number, lng: number): void {
    this.geocodingService.reverseGeocode(lat, lng).subscribe({
      next: (result) => {
        if (result) {
          const name = this.geocodingService.getSimplifiedName(result);
          this.setLocation(lat, lng, name);
          this.searchQuery = name;
        } else {
          this.setLocation(lat, lng, `${lat.toFixed(4)}, ${lng.toFixed(4)}`);
        }
      },
      error: () => {
        this.setLocation(lat, lng, `${lat.toFixed(4)}, ${lng.toFixed(4)}`);
      }
    });
  }

  private setLocation(lat: number, lng: number, name: string): void {
    // Remove existing marker
    if (this.marker) {
      this.map.removeLayer(this.marker);
    }

    // Add new DRAGGABLE marker
    this.marker = L.marker([lat, lng], { draggable: true }).addTo(this.map);
    this.marker.bindPopup(name).openPopup();

    // Handle marker drag end
    this.marker.on('dragend', (event: L.DragEndEvent) => {
      const position = event.target.getLatLng();
      this.onMarkerDragged(position.lat, position.lng);
    });

    // Center map on location
    this.map.setView([lat, lng], 13);

    // Emit location
    this.locationSelected.emit({
      name,
      latitude: lat,
      longitude: lng
    });
  }

  private onMarkerDragged(lat: number, lng: number): void {
    // Perform reverse geocoding to get location name
    this.geocodingService.reverseGeocode(lat, lng).subscribe({
      next: (result) => {
        if (result) {
          const name = this.geocodingService.getSimplifiedName(result);
          this.searchQuery = name;
          this.marker?.bindPopup(name).openPopup();

          // Emit updated location
          this.locationSelected.emit({
            name,
            latitude: lat,
            longitude: lng
          });
        }
      },
      error: () => {
        const name = `${lat.toFixed(4)}, ${lng.toFixed(4)}`;
        this.searchQuery = name;
        this.marker?.bindPopup(name).openPopup();

        // Emit updated location
        this.locationSelected.emit({
          name,
          latitude: lat,
          longitude: lng
        });
      }
    });
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchResults = [];
    this.showResults = false;
  }

  onBlur(): void {
    // Delay to allow click on result
    setTimeout(() => {
      this.showResults = false;
    }, 300);
  }

  onResultMouseDown(result: GeocodingResult): void {
    // Use mousedown instead of click to avoid blur race condition
    this.selectSearchResult(result);
  }
}
