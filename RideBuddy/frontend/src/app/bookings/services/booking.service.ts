import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Booking, CreateBookingRequest } from '../domain/booking.model';

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private readonly apiUrl = `${environment.apiUrl}/bookings`;

  constructor(private http: HttpClient) {}

  getMyBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/my-bookings`);
  }

  getBookingsByRide(rideId: string): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/by-ride/${rideId}`);
  }

  createBooking(booking: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>(this.apiUrl, booking);
  }

  cancelBooking(id: string, reason?: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/cancel`, { reason: reason || '' });
  }

  confirmBooking(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/confirm`, {});
  }

  rejectBooking(id: string, reason?: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/reject`, { reason: reason || '' });
  }
}
