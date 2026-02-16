import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { RideService } from '../services/ride.service';
import { getCityCoordinates } from '../services/city-coordinates';

@Component({
  selector: 'app-ride-create',
  templateUrl: './ride-create.component.html',
  styleUrls: ['./ride-create.component.scss']
})
export class RideCreateComponent implements OnInit {
  rideForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private rideService: RideService,
    public router: Router
  ) {
    this.rideForm = this.fb.group({
      originName: ['', Validators.required],
      destinationName: ['', Validators.required],
      departureTime: ['', Validators.required],
      availableSeats: [1, [Validators.required, Validators.min(1), Validators.max(8)]],
      pricePerSeat: [0, [Validators.required, Validators.min(0)]],
      currency: ['RSD', Validators.required],
      autoConfirmBookings: [true]
    });
  }

  ngOnInit(): void {}

  onSubmit(): void {
    if (this.rideForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const formValue = this.rideForm.value;
    
    // Get coordinates for cities
    const originCoords = getCityCoordinates(formValue.originName);
    const destCoords = getCityCoordinates(formValue.destinationName);

    const rideData = {
      originName: formValue.originName,
      originLatitude: originCoords.lat,
      originLongitude: originCoords.lng,
      destinationName: formValue.destinationName,
      destinationLatitude: destCoords.lat,
      destinationLongitude: destCoords.lng,
      departureTime: new Date(formValue.departureTime).toISOString(),
      availableSeats: formValue.availableSeats,
      pricePerSeat: formValue.pricePerSeat,
      currency: formValue.currency,
      autoConfirmBookings: formValue.autoConfirmBookings
    };

    this.rideService.createRide(rideData).subscribe({
      next: (ride) => {
        this.router.navigate(['/rides', ride.id]);
      },
      error: (error) => {
        this.isLoading = false;
        this.errorMessage = error.error?.error || 'Failed to create ride. Please try again.';
      }
    });
  }
}
