import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationsRoutingModule } from './notifications-routing.module';
import { NotificationsListComponent } from './notifications-list/notifications-list.component';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    NotificationsListComponent
  ],
  imports: [
    CommonModule,
    NotificationsRoutingModule,
    SharedModule
  ]
})
export class NotificationsModule { }