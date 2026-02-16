export interface Ride {
  id: string;
  driverId: string;
  driverName?: string;
  originName: string;
  originLatitude: number;
  originLongitude: number;
  destinationName: string;
  destinationLatitude: number;
  destinationLongitude: number;
  departureTime: string;
  availableSeats: number;
  totalSeats: number;
  pricePerSeat: number;
  currency: string;
  autoConfirmBookings: boolean;
  status: number;
  createdAt?: string;
}

export interface CreateRideRequest {
  originName: string;
  originLatitude: number;
  originLongitude: number;
  destinationName: string;
  destinationLatitude: number;
  destinationLongitude: number;
  departureTime: string;
  availableSeats: number;
  pricePerSeat: number;
  currency: string;
  autoConfirmBookings: boolean;
}
