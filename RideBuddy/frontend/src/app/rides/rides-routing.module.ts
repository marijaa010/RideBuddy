import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RideListComponent } from './ride-list/ride-list.component';
import { RideDetailsComponent } from './ride-details/ride-details.component';
import { RideCreateComponent } from './ride-create/ride-create.component';
import { MyRidesComponent } from './my-rides/my-rides.component';
import { AuthGuard } from '../shared/guards/auth.guard';

const routes: Routes = [
  {
    path: '',
    component: RideListComponent
  },
  {
    path: 'create',
    component: RideCreateComponent,
    canActivate: [AuthGuard]
  },
  {
    path: 'my-rides',
    component: MyRidesComponent,
    canActivate: [AuthGuard]
  },
  {
    path: ':id',
    component: RideDetailsComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class RidesRoutingModule { }
