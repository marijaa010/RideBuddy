import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserProfile, UpdateProfileRequest } from '../domain/user-profile.model';

/**
 * Service for managing user profile operations.
 * Handles communication with User Service API for profile viewing and editing.
 */
@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  /**
   * Retrieves complete user profile information.
   * Used in profile component to display user details and statistics.
   * @param id User ID to fetch profile for
   * @returns Observable with user profile data
   */
  getUserProfile(id: string): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.apiUrl}/${id}`);
  }

  /**
   * Updates user profile information (name, phone number).
   * After successful update, AuthService.updateCurrentUser() should be called
   * to refresh navbar and other UI components.
   * @param data Profile update request (firstName, lastName, phoneNumber)
   * @returns Observable with updated user profile
   */
  updateProfile(data: UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>(`${this.apiUrl}/profile`, data);
  }
}