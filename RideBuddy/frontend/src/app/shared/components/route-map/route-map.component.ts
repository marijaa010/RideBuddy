import { Component, OnInit, OnDestroy, AfterViewInit, Input, ViewChild, ElementRef, OnChanges, SimpleChanges } from '@angular/core';
import * as L from 'leaflet';

export interface RoutePoint {
  name: string;
  latitude: number;
  longitude: number;
}

@Component({
  selector: 'app-route-map',
  templateUrl: './route-map.component.html',
  styleUrls: ['./route-map.component.scss']
})
export class RouteMapComponent implements OnInit, AfterViewInit, OnDestroy, OnChanges {
  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef;

  @Input() origin!: RoutePoint;
  @Input() destination!: RoutePoint;
  @Input() height: string = '400px';

  private map!: L.Map;
  private originMarker?: L.Marker;
  private destinationMarker?: L.Marker;
  private routeLine?: L.Polyline;

  ngOnInit(): void {
    // Initialize map after a short delay to ensure DOM is ready
    setTimeout(() => {
      this.initMap();
      if (this.origin && this.destination) {
        this.displayRoute();
      }
    }, 100);
  }

  ngAfterViewInit(): void {
    // Ensure map size is correct after view is initialized
    setTimeout(() => {
      if (this.map) {
        this.map.invalidateSize();
      }
    }, 200);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.map && (changes['origin'] || changes['destination'])) {
      if (this.origin && this.destination) {
        this.displayRoute();
        // Invalidate size to ensure proper rendering
        setTimeout(() => {
          this.map.invalidateSize();
        }, 100);
      }
    }
  }

  ngOnDestroy(): void {
    if (this.map) {
      this.map.remove();
    }
  }

  private initMap(): void {
    this.map = L.map(this.mapContainer.nativeElement, {
      center: [44.7866, 20.4489],
      zoom: 7
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

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

  private displayRoute(): void {
    // Clear existing markers and lines
    this.clearRoute();

    // Create custom icons for origin and destination
    const originIcon = L.divIcon({
      className: 'custom-marker',
      html: '<div class="marker-pin origin-marker"><i class="fa-solid fa-location-dot"></i></div>',
      iconSize: [30, 42],
      iconAnchor: [15, 42]
    });

    const destinationIcon = L.divIcon({
      className: 'custom-marker',
      html: '<div class="marker-pin destination-marker"><i class="fa-solid fa-flag-checkered"></i></div>',
      iconSize: [30, 42],
      iconAnchor: [15, 42]
    });

    // Add origin marker
    this.originMarker = L.marker(
      [this.origin.latitude, this.origin.longitude],
      { icon: originIcon }
    ).addTo(this.map);
    this.originMarker.bindPopup(`<b>From:</b> ${this.origin.name}`);

    // Add destination marker
    this.destinationMarker = L.marker(
      [this.destination.latitude, this.destination.longitude],
      { icon: destinationIcon }
    ).addTo(this.map);
    this.destinationMarker.bindPopup(`<b>To:</b> ${this.destination.name}`);

    // Draw line between points
    this.routeLine = L.polyline(
      [
        [this.origin.latitude, this.origin.longitude],
        [this.destination.latitude, this.destination.longitude]
      ],
      {
        color: '#3498db',
        weight: 3,
        opacity: 0.7,
        dashArray: '10, 10'
      }
    ).addTo(this.map);

    // Fit map bounds to show both points with better padding and max zoom
    const bounds = L.latLngBounds([
      [this.origin.latitude, this.origin.longitude],
      [this.destination.latitude, this.destination.longitude]
    ]);
    this.map.fitBounds(bounds, {
      padding: [80, 80],
      maxZoom: 10  // Prevent zooming in too much
    });

    // Force map to recalculate size
    setTimeout(() => {
      this.map.invalidateSize();
    }, 100);

    // Calculate and display distance
    const distance = this.calculateDistance(
      this.origin.latitude,
      this.origin.longitude,
      this.destination.latitude,
      this.destination.longitude
    );

    // Add distance popup on the line
    const midPoint: L.LatLngExpression = [
      (this.origin.latitude + this.destination.latitude) / 2,
      (this.origin.longitude + this.destination.longitude) / 2
    ];

    L.popup()
      .setLatLng(midPoint)
      .setContent(`<b>Distance:</b> ${distance.toFixed(1)} km`)
      .openOn(this.map);
  }

  private clearRoute(): void {
    if (this.originMarker) {
      this.map.removeLayer(this.originMarker);
    }
    if (this.destinationMarker) {
      this.map.removeLayer(this.destinationMarker);
    }
    if (this.routeLine) {
      this.map.removeLayer(this.routeLine);
    }
  }

  /**
   * Calculate distance between two points using Haversine formula
   */
  private calculateDistance(lat1: number, lon1: number, lat2: number, lon2: number): number {
    const R = 6371; // Radius of Earth in kilometers
    const dLat = this.toRad(lat2 - lat1);
    const dLon = this.toRad(lon2 - lon1);

    const a =
      Math.sin(dLat / 2) * Math.sin(dLat / 2) +
      Math.cos(this.toRad(lat1)) *
        Math.cos(this.toRad(lat2)) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);

    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return R * c;
  }

  private toRad(degrees: number): number {
    return degrees * (Math.PI / 180);
  }
}
