import { Injectable } from '@angular/core';
import Swal, { SweetAlertOptions, SweetAlertResult } from 'sweetalert2';

@Injectable({ providedIn: 'root' })
export class FeedbackAlertService {
  private readonly primaryColor = '#5b47f3';
  private readonly dangerColor = '#d33';

  async success(title: string, text: string): Promise<SweetAlertResult> {
    return Swal.fire({
      icon: 'success',
      title,
      text,
      showConfirmButton: false,
      timer: 1800,
      timerProgressBar: true,
      allowOutsideClick: false
    });
  }

  async error(title: string, text: string): Promise<SweetAlertResult> {
    return Swal.fire({
      icon: 'error',
      title,
      text,
      confirmButtonText: 'OK',
      confirmButtonColor: this.primaryColor
    });
  }

  async confirmDestructive(title: string, text: string, confirmButtonText = 'Delete'): Promise<boolean> {
    const result = await Swal.fire({
      icon: 'warning',
      title,
      text,
      showCancelButton: true,
      confirmButtonText,
      cancelButtonText: 'Cancel',
      confirmButtonColor: this.dangerColor,
      cancelButtonColor: '#64748b'
    });

    return result.isConfirmed;
  }

  async confirmAction(title: string, text: string, confirmButtonText = 'Confirm'): Promise<boolean> {
    const result = await Swal.fire({
      icon: 'question',
      title,
      text,
      showCancelButton: true,
      confirmButtonText,
      cancelButtonText: 'Cancel',
      confirmButtonColor: this.primaryColor,
      cancelButtonColor: '#64748b'
    });

    return result.isConfirmed;
  }

  async notify(options: SweetAlertOptions): Promise<SweetAlertResult> {
    return Swal.fire({
      confirmButtonColor: this.primaryColor,
      ...options
    });
  }
}
