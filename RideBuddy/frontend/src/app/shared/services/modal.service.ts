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

  close(result: ModalResult): void {
    this.modalSubject.next(null);
    this.resultSubject.next(result);
  }
}
