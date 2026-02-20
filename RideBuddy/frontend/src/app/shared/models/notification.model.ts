export interface AppNotification {
  id: string;
  userId: string;
  title: string;
  message: string;
  type: NotificationType;
  bookingId?: string;
  rideId?: string;
  isRead: boolean;
  createdAt: string;
}

export enum NotificationType {
  BookingCreated = 0,
  BookingConfirmed = 1,
  BookingRejected = 2,
  BookingCancelled = 3,
  BookingCompleted = 4
}
