import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { RideService } from '../services/ride.service';
import { Ride } from '../domain/ride.model';
import { DateFormatterService } from '../../shared/services/date-formatter.service';
import { ToastService } from '../../shared/services/toast.service';

@Component({
  selector: 'app-ride-list',
  templateUrl: './ride-list.component.html',
  styleUrls: ['./ride-list.component.scss']
})
export class RideListComponent implements OnInit {
  rides: Ride[] = [];
  searchForm: FormGroup;
  isLoading = false;

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

  formatDate(dateStr: string): string {
    return this.dateFormatter.formatRelativeDate(dateStr);
  }

  getCountdown(dateStr: string): string {
    return this.dateFormatter.getCountdown(dateStr);
  }
}
