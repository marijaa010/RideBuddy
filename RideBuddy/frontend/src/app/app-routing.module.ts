import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './shared/guards/auth.guard';

const routes: Routes = [
  {
    path: '',
    redirectTo: '/rides',
    pathMatch: 'full'
  },
  {
    path: 'identity',
    loadChildren: () => import('./identity/identity.module').then(m => m.IdentityModule)
  },
  {
    path: 'rides',
    loadChildren: () => import('./rides/rides.module').then(m => m.RidesModule)
  },
  {
    path: 'bookings',
    loadChildren: () => import('./bookings/bookings.module').then(m => m.BookingsModule),
    canActivate: [AuthGuard]
  },
  {
    path: '**',
    redirectTo: '/rides'
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
