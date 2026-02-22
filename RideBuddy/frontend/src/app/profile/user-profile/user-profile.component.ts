import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../shared/services/auth.service';
import { UserService } from '../services/user.service';
import { BookingService } from '../../bookings/services/booking.service';
import { RideService } from '../../rides/services/ride.service';
import { DateFormatterService } from '../../shared/services/date-formatter.service';
import { ToastService } from '../../shared/services/toast.service';
import { UserProfile } from '../domain/user-profile.model';

@Component({
  selector: 'app-user-profile',
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.scss']
})
export class UserProfileComponent implements OnInit {
  profile: UserProfile | null = null;
  isLoading = true;
  isEditing = false;
  isSaving = false;

  editForm = {
    firstName: '',
    lastName: '',
    phoneNumber: ''
  };

  stats = {
    totalBookings: 0,
    completedBookings: 0,
    totalRides: 0,
    completedRides: 0
  };

  constructor(
    public authService: AuthService,
    private userService: UserService,
    private bookingService: BookingService,
    private rideService: RideService,
    private dateFormatter: DateFormatterService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadProfile();
  }

  /**
   * Loads user profile data for current logged-in user.
   * After loading profile, also fetches user statistics.
   */
  loadProfile(): void {
    const user = this.authService.getCurrentUser();
    if (!user) return;

    this.isLoading = true;
    this.userService.getUserProfile(user.id).subscribe({
      next: (profile) => {
        this.profile = profile;
        this.isLoading = false;
        this.loadStats();
      },
      error: () => {
        this.toastService.error('Failed to load profile.');
        this.isLoading = false;
      }
    });
  }

  /**
   * Loads user statistics (bookings, rides) from API.
   * For drivers, also fetches ride statistics.
   */
  loadStats(): void {
    this.bookingService.getMyBookings().subscribe({
      next: (bookings) => {
        this.stats.totalBookings = bookings.length;
        this.stats.completedBookings = bookings.filter(b => b.status === 3).length;
      }
    });

    if (this.authService.isDriver()) {
      this.rideService.getMyRides().subscribe({
        next: (rides) => {
          this.stats.totalRides = rides.length;
          this.stats.completedRides = rides.filter(r => r.status === 2).length;
        }
      });
    }
  }

  /**
   * Generates user initials from first and last name for avatar display.
   * @returns Two-letter initials in uppercase (e.g., "JD" for John Doe)
   */
  getInitials(): string {
    if (!this.profile) return '';
    return (this.profile.firstName[0] + this.profile.lastName[0]).toUpperCase();
  }

  /**
   * Formats account creation date to full date string.
   * @returns Formatted date string (e.g., "January 15, 2024")
   */
  formatMemberSince(): string {
    if (!this.profile) return '';
    return this.dateFormatter.formatFullDate(this.profile.createdAt);
  }

  /**
   * Enters edit mode and populates edit form with current profile data.
   */
  startEditing(): void {
    if (!this.profile) return;
    this.editForm.firstName = this.profile.firstName;
    this.editForm.lastName = this.profile.lastName;
    this.editForm.phoneNumber = this.profile.phoneNumber;
    this.isEditing = true;
  }

  /**
   * Cancels profile editing without saving changes.
   */
  cancelEditing(): void {
    this.isEditing = false;
  }

  /**
   * Saves updated profile information to backend.
   * After successful save, updates AuthService to refresh navbar display.
   */
  saveProfile(): void {
    if (!this.editForm.firstName.trim() || !this.editForm.lastName.trim()) {
      this.toastService.warning('First name and last name are required.');
      return;
    }

    this.isSaving = true;
    this.userService.updateProfile(this.editForm).subscribe({
      next: (updated) => {
        this.profile = updated;
        this.isEditing = false;
        this.isSaving = false;
        this.toastService.success('Profile updated successfully.');

        this.authService.updateCurrentUser({
          firstName: updated.firstName,
          lastName: updated.lastName
        });
      },
      error: () => {
        this.isSaving = false;
        this.toastService.error('Failed to update profile.');
      }
    });
  }
}