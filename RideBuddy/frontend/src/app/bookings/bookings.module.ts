import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { BookingsRoutingModule } from './bookings-routing.module';
import { MyBookingsComponent } from './my-bookings/my-bookings.component';

@NgModule({
  declarations: [
    MyBookingsComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    BookingsRoutingModule
  ]
})
export class BookingsModule { }
