import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { ModalService, ModalConfig } from '../../services/modal.service';

@Component({
  selector: 'app-confirmation-modal',
  templateUrl: './confirmation-modal.component.html',
  styleUrls: ['./confirmation-modal.component.scss']
})
export class ConfirmationModalComponent implements OnInit, OnDestroy {
  config: ModalConfig | null = null;
  isVisible = false;
  inputValue = '';
  private subscription?: Subscription;

  constructor(private modalService: ModalService) {}

  ngOnInit(): void {
    this.subscription = this.modalService.modal$.subscribe(config => {
      this.config = config;
      this.isVisible = !!config;
      this.inputValue = '';
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  confirm(): void {
    this.modalService.close({
      confirmed: true,
      inputValue: this.inputValue
    });
  }

  cancel(): void {
    this.modalService.close({
      confirmed: false
    });
  }

  onBackdropClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.cancel();
    }
  }
}
