import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';

export interface ModalConfig {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: 'confirm' | 'prompt' | 'alert';
  promptLabel?: string;
  promptPlaceholder?: string;
  danger?: boolean;
}

export interface ModalResult {
  confirmed: boolean;
  inputValue?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ModalService {
  private modalSubject = new BehaviorSubject<ModalConfig | null>(null);
  public modal$ = this.modalSubject.asObservable();

  private resultSubject = new Subject<ModalResult>();

  /**
   * Shows a confirmation modal dialog with Yes/No buttons.
   * @param config Modal configuration (title, message, button texts, danger flag)
   * @returns Observable<boolean> - true if user confirmed, false if cancelled
   * @example
   * this.modalService.confirm({
   *   title: 'Delete Item',
   *   message: 'Are you sure?',
   *   confirmText: 'Yes, Delete',
   *   danger: true
   * }).subscribe(confirmed => {
   *   if (confirmed) { // delete logic }
   * });
   */
  confirm(config: Omit<ModalConfig, 'type'>): Observable<boolean> {
    const fullConfig: ModalConfig = {
      ...config,
      type: 'confirm',
      confirmText: config.confirmText || 'Confirm',
      cancelText: config.cancelText || 'Cancel'
    };

    this.modalSubject.next(fullConfig);

    return new Observable(observer => {
      const sub = this.resultSubject.subscribe(result => {
        observer.next(result.confirmed);
        observer.complete();
        sub.unsubscribe();
      });
    });
  }

  /**
   * Shows a prompt modal dialog with text input field.
   * @param config Modal configuration including input label and placeholder
   * @returns Observable<string | null> - input value if confirmed, null if cancelled
   * @example
   * this.modalService.prompt({
   *   title: 'Reject Booking',
   *   message: 'Please provide a reason:',
   *   promptLabel: 'Reason',
   *   promptPlaceholder: 'e.g., Ride is full'
   * }).subscribe(reason => {
   *   if (reason !== null) { // reject with reason }
   * });
   */
  prompt(config: Omit<ModalConfig, 'type'>): Observable<string | null> {
    const fullConfig: ModalConfig = {
      ...config,
      type: 'prompt',
      confirmText: config.confirmText || 'Submit',
      cancelText: config.cancelText || 'Cancel',
      promptLabel: config.promptLabel || 'Enter value',
      promptPlaceholder: config.promptPlaceholder || ''
    };

    this.modalSubject.next(fullConfig);

    return new Observable(observer => {
      const sub = this.resultSubject.subscribe(result => {
        observer.next(result.confirmed ? (result.inputValue || '') : null);
        observer.complete();
        sub.unsubscribe();
      });
    });
  }

  /**
   * Shows an alert modal dialog with single OK button.
   * @param config Modal configuration (title, message, confirm button text)
   * @returns Observable<void> - completes when user clicks OK
   */
  alert(config: Omit<ModalConfig, 'type' | 'cancelText'>): Observable<void> {
    const fullConfig: ModalConfig = {
      ...config,
      type: 'alert',
      confirmText: config.confirmText || 'OK'
    };

    this.modalSubject.next(fullConfig);

    return new Observable(observer => {
      const sub = this.resultSubject.subscribe(() => {
        observer.next();
        observer.complete();
        sub.unsubscribe();
      });
    });
  }

  /**
   * Closes the currently open modal and emits result to subscribers.
   * Called internally by ConfirmationModalComponent when user clicks button.
   * @param result User's action (confirmed/cancelled) and optional input value
   */
  close(result: ModalResult): void {
    this.modalSubject.next(null);
    this.resultSubject.next(result);
  }
}
