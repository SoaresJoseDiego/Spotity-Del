import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
  styles: [':host { display: block; min-height: 100vh; }'],
})
export class AppComponent implements OnInit {
  private readonly auth = inject(AuthService);

  ngOnInit() {
    this.auth.initialize().subscribe();
  }
}
