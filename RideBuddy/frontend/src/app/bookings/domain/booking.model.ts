export interface Booking {
  id: string;
  rideId: string;
  passengerId: string;
  passengerName?: string;
  seatsBooked: number;
  totalPrice: number;
  currency: string;
  status: number;
  bookedAt: string;
  ride?: {
    rideId: string;
    driverId: string;
    origin: string;
    destination: string;
    departureTime: string;
    availableSeats: number;
    pricePerSeat: number;
    currency: string;
    isAvailable: boolean;
    autoConfirmBookings: boolean;
  };
}

export interface CreateBookingRequest {
  rideId: string;
  seatsToBook: number;
}
