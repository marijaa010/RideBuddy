import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { RideService } from '../services/ride.service';
import { Ride } from '../domain/ride.model';
import { DateFormatterService } from '../../shared/services/date-formatter.service';
import { ToastService } from '../../shared/services/toast.service';
import { LocationSelection } from '../../shared/components/location-autocomplete/location-autocomplete.component';

@Component({
  selector: 'app-ride-list',
  templateUrl: './ride-list.component.html',
  styleUrls: ['./ride-list.component.scss']
})
export class RideListComponent implements OnInit {
  rides: Ride[] = [];
  searchForm: FormGroup;
  isLoading = false;

  // Store selected locations from autocomplete
  originLocation: LocationSelection | null = null;
  destinationLocation: LocationSelection | null = null;

  constructor(
    private rideService: RideService,
    private fb: FormBuilder,
    public dateFormatter: DateFormatterService,
    private toastService: ToastService
  ) {
    this.searchForm = this.fb.group({
      origin: [''],
      destination: [''],
      date: ['']
    });
  }

  ngOnInit(): void {
    this.searchRides();
  }

  /**
   * Searches for rides based on form filters (origin, destination, date).
   * Called on component init (loads all rides) and when user clicks search button.
   * Shows info toast if no rides match criteria.
   */
  searchRides(): void {
    this.isLoading = true;

    const { origin, destination, date } = this.searchForm.value;

    this.rideService.searchRides(origin, destination, date).subscribe({
      next: (rides) => {
        this.rides = rides;
        this.isLoading = false;
        if (rides.length === 0) {
          this.toastService.info('No rides found. Try adjusting your search criteria.');
        }
      },
      error: (error) => {
        this.toastService.error('Failed to load rides. Please try again.');
        this.isLoading = false;
      }
    });
  }

  /**
   * Formats date to relative format (e.g., "today", "tomorrow", "in 3 days").
   * @param dateStr ISO date string
   * @returns Human-readable date string
   */
  formatDate(dateStr: string): string {
    return this.dateFormatter.formatRelativeDate(dateStr);
  }

  /**
   * Calculates countdown to departure time.
   * @param dateStr ISO date string for departure time
   * @returns Countdown string (e.g., "2h 30m")
   */
  getCountdown(dateStr: string): string {
    return this.dateFormatter.getCountdown(dateStr);
  }

  /**
   * Handler for origin location selection from autocomplete component.
   * Updates both internal state and form control for backend query.
   * @param location Selected location with name and coordinates, or null if cleared
   */
  onOriginSelected(location: LocationSelection | null): void {
    this.originLocation = location;
    this.searchForm.patchValue({ origin: location?.name || '' });
  }

  /**
   * Handler for destination location selection from autocomplete component.
   * Updates both internal state and form control for backend query.
   * @param location Selected location with name and coordinates, or null if cleared
   */
  onDestinationSelected(location: LocationSelection | null): void {
    this.destinationLocation = location;
    this.searchForm.patchValue({ destination: location?.name || '' });
  }
}
