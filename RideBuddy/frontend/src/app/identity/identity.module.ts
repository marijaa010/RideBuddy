import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { IdentityRoutingModule } from './identity-routing.module';
import { LoginComponent } from './feature-authentication/login/login.component';
import { RegisterComponent } from './feature-authentication/register/register.component';

@NgModule({
  declarations: [
    LoginComponent,
    RegisterComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    IdentityRoutingModule
  ]
})
export class IdentityModule { }
