import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

export interface GeocodingResult {
  lat: number;
  lon: number;
  displayName: string;
  address?: {
    city?: string;
    town?: string;
    village?: string;
    municipality?: string;
    country?: string;
  };
}

export interface LocationCoordinates {
  lat: number;
  lng: number;
  displayName: string;
}

@Injectable({
  providedIn: 'root'
})
export class GeocodingService {
  private readonly nominatimUrl = 'https://nominatim.openstreetmap.org';

  constructor(private http: HttpClient) {}

  /**
   * Search for a location by address/name
   * @param query - The search query (e.g., "Belgrade", "Trg Republike, Belgrade")
   * @returns Observable of geocoding results
   */
  searchLocation(query: string): Observable<GeocodingResult[]> {
    if (!query || query.trim().length < 3) {
      return of([]);
    }

    const params = new HttpParams()
      .set('q', query)
      .set('format', 'json')
      .set('addressdetails', '1')
      .set('limit', '5')
      .set('countrycodes', 'rs,hr,ba,me,si'); // Focus on Balkans region

    return this.http.get<any[]>(`${this.nominatimUrl}/search`, { params }).pipe(
      map(results => results.map(result => ({
        lat: parseFloat(result.lat),
        lon: parseFloat(result.lon),
        displayName: result.display_name,
        address: result.address
      }))),
      catchError(error => {
        console.error('Geocoding error:', error);
        return of([]);
      })
    );
  }

  /**
   * Reverse geocode coordinates to get address
   * @param lat - Latitude
   * @param lng - Longitude
   * @returns Observable of location name
   */
  reverseGeocode(lat: number, lng: number): Observable<GeocodingResult | null> {
    const params = new HttpParams()
      .set('lat', lat.toString())
      .set('lon', lng.toString())
      .set('format', 'json')
      .set('addressdetails', '1');

    return this.http.get<any>(`${this.nominatimUrl}/reverse`, { params }).pipe(
      map(result => ({
        lat: parseFloat(result.lat),
        lon: parseFloat(result.lon),
        displayName: result.display_name,
        address: result.address
      })),
      catchError(error => {
        console.error('Reverse geocoding error:', error);
        return of(null);
      })
    );
  }

  /**
   * Get a simplified location name from geocoding result
   * @param result - Geocoding result
   * @returns Simplified location name (e.g., "Belgrade, Serbia")
   */
  getSimplifiedName(result: GeocodingResult): string {
    if (!result.address) {
      return result.displayName.split(',')[0];
    }

    const city = result.address.city ||
                 result.address.town ||
                 result.address.village ||
                 result.address.municipality;
    const country = result.address.country;

    if (city && country) {
      return `${city}, ${country}`;
    } else if (city) {
      return city;
    } else {
      return result.displayName.split(',').slice(0, 2).join(',');
    }
  }
}
