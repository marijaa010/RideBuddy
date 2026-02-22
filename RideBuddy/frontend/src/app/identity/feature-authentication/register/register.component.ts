import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../shared/services/auth.service';
import { ToastService } from '../../../shared/services/toast.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  registerForm: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastService: ToastService
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?\d{10,15}$/)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]],
      role: ['Passenger', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/rides']);
    }
  }

  /**
   * Custom validator to ensure password and confirmPassword fields match.
   * Applied at form level (not control level) to compare two fields.
   * @param form FormGroup containing password fields
   * @returns null if passwords match, error object if mismatch
   */
  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');
    return password && confirmPassword && password.value === confirmPassword.value
      ? null : { passwordMismatch: true };
  }

  /**
   * Handles registration form submission.
   * Validates form data, calls AuthService to create new user account.
   * On success: automatically logs in user and redirects to /rides.
   * On error: displays error message via toast notification.
   */
  onSubmit(): void {
    if (this.registerForm.invalid) {
      if (this.registerForm.errors?.['passwordMismatch']) {
        this.toastService.warning('Passwords do not match.');
      } else {
        this.toastService.warning('Please fill in all required fields correctly.');
      }
      return;
    }

    this.isLoading = true;

    const { confirmPassword, ...registerData } = this.registerForm.value;

    this.authService.register(registerData).subscribe({
      next: () => {
        this.toastService.success('Registration successful! Welcome aboard.');
        this.router.navigate(['/rides']);
      },
      error: (error) => {
        this.isLoading = false;
        const errorMsg = error.error?.error || 'Registration failed. Please try again.';
        this.toastService.error(errorMsg);
      }
    });
  }

  /**
   * Getter for firstName form control (used for validation in template).
   */
  get firstName() { return this.registerForm.get('firstName'); }

  /**
   * Getter for lastName form control (used for validation in template).
   */
  get lastName() { return this.registerForm.get('lastName'); }

  /**
   * Getter for email form control (used for validation in template).
   */
  get email() { return this.registerForm.get('email'); }

  /**
   * Getter for phoneNumber form control (used for validation in template).
   */
  get phoneNumber() { return this.registerForm.get('phoneNumber'); }

  /**
   * Getter for password form control (used for validation in template).
   */
  get password() { return this.registerForm.get('password'); }

  /**
   * Getter for confirmPassword form control (used for validation in template).
   */
  get confirmPassword() { return this.registerForm.get('confirmPassword'); }

  /**
   * Getter for role form control (used for validation in template).
   */
  get role() { return this.registerForm.get('role'); }
}
