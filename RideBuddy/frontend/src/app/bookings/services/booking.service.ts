import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Booking, CreateBookingRequest } from '../domain/booking.model';

/**
 * Service for managing booking operations.
 * Handles communication with Booking Service API for creating and managing ride bookings.
 */
@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private readonly apiUrl = `${environment.apiUrl}/bookings`;

  constructor(private http: HttpClient) {}

  /**
   * Retrieves all bookings for the current logged-in user (passenger view).
   * @returns Observable array of user's bookings with ride details
   */
  getMyBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/my-bookings`);
  }

  /**
   * Retrieves all bookings for a specific ride (driver view).
   * Used by drivers to see pending/confirmed bookings for their rides.
   * @param rideId Ride ID to fetch bookings for
   * @returns Observable array of bookings for the specified ride
   */
  getBookingsByRide(rideId: string): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.apiUrl}/by-ride/${rideId}`);
  }

  /**
   * Creates a new booking request for a ride.
   * Triggers orchestration flow: validates user, checks availability, reserves seats.
   * @param booking Booking request data (rideId, passengerId, seatsToBook)
   * @returns Observable with created booking details
   */
  createBooking(booking: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>(this.apiUrl, booking);
  }

  /**
   * Cancels an existing booking (passenger action).
   * Releases reserved seats back to ride availability.
   * @param id Booking ID to cancel
   * @param reason Optional cancellation reason
   * @returns Observable that completes when cancellation is successful
   */
  cancelBooking(id: string, reason?: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/cancel`, { reason: reason || '' });
  }

  /**
   * Confirms a pending booking request (driver action).
   * Only available for rides with AutoConfirmBookings = false.
   * @param id Booking ID to confirm
   * @returns Observable that completes when confirmation is successful
   */
  confirmBooking(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/confirm`, {});
  }

  /**
   * Rejects a pending booking request (driver action).
   * Releases reserved seats and notifies passenger.
   * @param id Booking ID to reject
   * @param reason Optional rejection reason (shown to passenger)
   * @returns Observable that completes when rejection is successful
   */
  rejectBooking(id: string, reason?: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/reject`, { reason: reason || '' });
  }
}
