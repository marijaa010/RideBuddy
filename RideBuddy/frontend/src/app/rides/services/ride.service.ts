import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Ride, CreateRideRequest } from '../domain/ride.model';

/**
 * Service for managing ride operations.
 * Handles communication with Ride Service API for searching, creating, and managing rides.
 */
@Injectable({
  providedIn: 'root'
})
export class RideService {
  private readonly apiUrl = `${environment.apiUrl}/rides`;

  constructor(private http: HttpClient) {}

  /**
   * Searches for available rides based on optional filters.
   * Used in ride-list component for finding rides matching user criteria.
   * @param origin Optional origin location filter
   * @param destination Optional destination location filter
   * @param date Optional departure date filter (ISO format)
   * @returns Observable array of rides matching search criteria
   */
  searchRides(origin?: string, destination?: string, date?: string): Observable<Ride[]> {
    let params = new HttpParams();
    if (origin) params = params.set('origin', origin);
    if (destination) params = params.set('destination', destination);
    if (date) params = params.set('date', date);

    return this.http.get<Ride[]>(`${this.apiUrl}/search`, { params });
  }

  /**
   * Retrieves detailed information for a specific ride.
   * Used in ride-details component to display full ride info and booking button.
   * @param id Ride ID to fetch
   * @returns Observable with complete ride details including availability
   */
  getRideById(id: string): Observable<Ride> {
    return this.http.get<Ride>(`${this.apiUrl}/${id}`);
  }

  /**
   * Retrieves all rides created by the current logged-in driver.
   * Used in my-rides component for driver dashboard.
   * @returns Observable array of driver's rides with booking information
   */
  getMyRides(): Observable<Ride[]> {
    return this.http.get<Ride[]>(`${this.apiUrl}/my-rides`);
  }

  /**
   * Creates a new ride offer (driver action).
   * Requires origin, destination, departure time, seats, and price information.
   * @param ride Ride creation request data
   * @returns Observable with created ride details
   */
  createRide(ride: CreateRideRequest): Observable<Ride> {
    return this.http.post<Ride>(this.apiUrl, ride);
  }
}
