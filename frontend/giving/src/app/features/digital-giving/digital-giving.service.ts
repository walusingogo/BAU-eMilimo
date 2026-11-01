import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import {
  DigitalPaymentIntentResult,
  DigitalPaymentStatus,
  MemberIdentificationResult,
  PublicDigitalGivingConfiguration
} from './digital-giving.models';

@Injectable({ providedIn: 'root' })
export class DigitalGivingService {
  private readonly api = inject(ApiService);

  getPublicConfiguration(slug?: string | null): Observable<PublicDigitalGivingConfiguration> {
    const suffix = slug ? `/${encodeURIComponent(slug)}` : '';
    return this.api.get<PublicDigitalGivingConfiguration>(`public/digital-giving${suffix}`);
  }

  identifyMember(lookup: string, additionalIdentifier?: string): Observable<MemberIdentificationResult> {
    return this.api.post<MemberIdentificationResult>('public/digital-giving/member-identification', { lookup, additionalIdentifier: additionalIdentifier || null });
  }

  createPaymentIntent(slug: string, request: object): Observable<DigitalPaymentIntentResult> {
    return this.api.post<DigitalPaymentIntentResult>(`public/digital-giving/${encodeURIComponent(slug)}/payment-intents`, request);
  }

  getPaymentStatus(reference: string, token: string): Observable<DigitalPaymentStatus> {
    return this.api.get<DigitalPaymentStatus>(
      `public/digital-giving/payment-intents/${encodeURIComponent(reference)}/status?token=${encodeURIComponent(token)}`
    );
  }

  refreshPaymentStatus(reference: string, token: string): Observable<DigitalPaymentStatus> {
    return this.api.post<DigitalPaymentStatus>(
      `public/digital-giving/payment-intents/${encodeURIComponent(reference)}/refresh-status?token=${encodeURIComponent(token)}`,
      {}
    );
  }
}
