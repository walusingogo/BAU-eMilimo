import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly churchName$ = new BehaviorSubject<string>(environment.congregationName);

  getChurchName(): string {
    return this.churchName$.value;
  }

  setChurchName(name: string): void {
    this.churchName$.next(name);
  }
}
