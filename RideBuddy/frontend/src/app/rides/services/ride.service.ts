import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Ride, CreateRideRequest } from '../domain/ride.model';

@Injectable({
  providedIn: 'root'
})
export class RideService {
  private readonly apiUrl = `${environment.apiUrl}/rides`;

  constructor(private http: HttpClient) {}

  searchRides(origin?: string, destination?: string, date?: string): Observable<Ride[]> {
    let params = new HttpParams();
    if (origin) params = params.set('origin', origin);
    if (destination) params = params.set('destination', destination);
    if (date) params = params.set('date', date);

    return this.http.get<Ride[]>(`${this.apiUrl}/search`, { params });
  }

  getRideById(id: string): Observable<Ride> {
    return this.http.get<Ride>(`${this.apiUrl}/${id}`);
  }

  getMyRides(): Observable<Ride[]> {
    return this.http.get<Ride[]>(`${this.apiUrl}/my-rides`);
  }

  createRide(ride: CreateRideRequest): Observable<Ride> {
    return this.http.post<Ride>(this.apiUrl, ride);
  }
}
