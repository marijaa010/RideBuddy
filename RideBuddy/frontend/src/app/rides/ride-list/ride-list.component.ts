import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { RideService } from '../services/ride.service';
import { Ride } from '../domain/ride.model';

@Component({
  selector: 'app-ride-list',
  templateUrl: './ride-list.component.html',
  styleUrls: ['./ride-list.component.scss']
})
export class RideListComponent implements OnInit {
  rides: Ride[] = [];
  searchForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor(
    private rideService: RideService,
    private fb: FormBuilder
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

  searchRides(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const { origin, destination, date } = this.searchForm.value;

    this.rideService.searchRides(origin, destination, date).subscribe({
      next: (rides) => {
        this.rides = rides;
        this.isLoading = false;
      },
      error: (error) => {
        this.errorMessage = 'Failed to load rides. Please try again.';
        this.isLoading = false;
      }
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
