import { Component } from '@angular/core';
import { NotificationService } from './shared/services/notification.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'RideBuddy';

  constructor(private notificationService: NotificationService) {}
}
