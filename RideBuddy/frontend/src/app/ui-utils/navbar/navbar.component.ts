import { Component, OnInit } from '@angular/core';
import { AuthService, User } from '../../shared/services/auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent implements OnInit {
  currentUser$: Observable<User | null>;

  constructor(public authService: AuthService) {
    this.currentUser$ = authService.currentUser$;
  }

  ngOnInit(): void {
  }

  logout(): void {
    this.authService.logout();
  }
}
