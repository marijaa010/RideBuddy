import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

import { RidesRoutingModule } from './rides-routing.module';
import { RideListComponent } from './ride-list/ride-list.component';
import { RideDetailsComponent } from './ride-details/ride-details.component';
import { RideCreateComponent } from './ride-create/ride-create.component';
import { MyRidesComponent } from './my-rides/my-rides.component';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    RideListComponent,
    RideDetailsComponent,
    RideCreateComponent,
    MyRidesComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RidesRoutingModule,
    SharedModule
  ]
})
export class RidesModule { }
