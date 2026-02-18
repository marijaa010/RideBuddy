import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { RideService } from '../services/ride.service';
import { ToastService } from '../../shared/services/toast.service';
import { LocationData } from '../../shared/components/map-picker/map-picker.component';

@Component({
  selector: 'app-ride-create',
  templateUrl: './ride-create.component.html',
  styleUrls: ['./ride-create.component.scss']
})
export class RideCreateComponent implements OnInit {
  rideForm: FormGroup;
  isLoading = false;

  // Store selected locations
  originLocation?: LocationData;
  destinationLocation?: LocationData;

  constructor(
    private fb: FormBuilder,
    private rideService: RideService,
    public router: Router,
    private toastService: ToastService
  ) {
    this.rideForm = this.fb.group({
      departureTime: ['', Validators.required],
      availableSeats: [1, [Validators.required, Validators.min(1), Validators.max(8)]],
      pricePerSeat: [0, [Validators.required, Validators.min(0)]],
      currency: ['RSD', Validators.required],
      autoConfirmBookings: [true]
    });
  }

  ngOnInit(): void {}

  onOriginSelected(location: LocationData): void {
    this.originLocation = location;
  }

  onDestinationSelected(location: LocationData): void {
    this.destinationLocation = location;
  }

  onSubmit(): void {
    if (this.rideForm.invalid) {
      this.toastService.warning('Please fill in all required fields correctly.');
      return;
    }

    if (!this.originLocation) {
      this.toastService.warning('Please select an origin location.');
      return;
    }

    if (!this.destinationLocation) {
      this.toastService.warning('Please select a destination location.');
      return;
    }

    this.isLoading = true;

    const formValue = this.rideForm.value;

    const rideData = {
      originName: this.originLocation.name,
      originLatitude: this.originLocation.latitude,
      originLongitude: this.originLocation.longitude,
      destinationName: this.destinationLocation.name,
      destinationLatitude: this.destinationLocation.latitude,
      destinationLongitude: this.destinationLocation.longitude,
      departureTime: new Date(formValue.departureTime).toISOString(),
      availableSeats: formValue.availableSeats,
      pricePerSeat: formValue.pricePerSeat,
      currency: formValue.currency,
      autoConfirmBookings: formValue.autoConfirmBookings
    };

    this.rideService.createRide(rideData).subscribe({
      next: (ride) => {
        this.toastService.success('Ride created successfully!');
        this.router.navigate(['/rides', ride.id]);
      },
      error: (error) => {
        this.isLoading = false;
        const errorMsg = error.error?.error || 'Failed to create ride. Please try again.';
        this.toastService.error(errorMsg);
      }
    });
  }

  get isFormValid(): boolean {
    return this.rideForm.valid && !!this.originLocation && !!this.destinationLocation;
  }
}
