import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { DigitalPaymentStatus } from './digital-giving.models';
import { DigitalGivingService } from './digital-giving.service';
import { apiErrorMessage } from '../../core/utils/api-error';
import { ReportExportService } from '../../../../../web/src/app/core/services/report-export.service';
import { DocumentViewerComponent } from '../../../../../web/src/app/shared/document-viewer/document-viewer.component';

@Component({
  selector: 'app-digital-giving-receipt',
  imports: [CommonModule, DocumentViewerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="proof-page min-h-screen bg-slate-100 p-4 md:p-8">
      @if (loading()) {
        <div class="proof-screen-width mx-auto p-12 text-center text-sm text-slate-600" role="status">Loading receipt...</div>
      } @else if (error()) {
        <div class="proof-screen-width mx-auto bg-white p-12 text-center" role="alert">
          <h1 class="text-xl font-bold">Receipt unavailable</h1>
          <p class="mt-2 text-sm text-slate-600">{{ error() }}</p>
        </div>
      } @else if (receipt(); as item) {
        <div class="proof-screen-width mx-auto p-12 text-center text-sm text-slate-600" role="status">
          @if (rendering()) {
            Preparing your receipt…
          } @else if (opened()) {
            <button type="button" (click)="openDocument()" class="font-semibold text-[#173E55] underline underline-offset-2">View receipt again</button>
          }
        </div>

        <div class="receipt-source" aria-hidden="true">
          <div class="page-preview">
            <div #receiptPage class="paper report-output-surface first-page" data-report-orientation="portrait">

              <!-- ===== HEADER ===== -->
              <header class="text-center border-b-2 border-[#241C15] pb-5">
                <img src="/images/ucz-logo.png" [alt]="churchName()" class="h-16 w-auto mx-auto mb-3 object-contain" />
                <div class="text-xs uppercase tracking-[.3em] text-[#8A7F6A]">{{ churchName() }}</div>
                <h1 class="text-3xl font-bold mt-2" style="font-family:'Fraunces',serif">Giving Receipt</h1>
                <p class="mt-2 font-mono">{{ item.paymentReference }}</p>
                <span class="inline-block mt-3 px-3 py-1 border rounded-full text-xs font-semibold">{{ item.status }}</span>
              </header>

              <!-- ===== BODY ===== -->
              <div class="body-content">
                @if (item.isSandbox) {
                  <p class="sandbox-note mt-4 rounded border-2 border-amber-400 bg-amber-50 px-3 py-2 text-center text-sm font-semibold text-amber-900">MTN MoMo Sandbox test. No real money moved; the full contribution workflow was exercised.</p>
                }

                <section class="grid grid-cols-4 gap-4 py-6 border-b text-sm">
                  <div>
                    <p class="label">Payment date</p>
                    <p>{{ (item.completedAtUtc || item.createdAtUtc) | date:'mediumDate' }}</p>
                  </div>
                  <div>
                    <p class="label">Settlement</p>
                    <p>{{ item.settlementStatus }}</p>
                  </div>
                  <div>
                    <p class="label">Currency</p>
                    <p>{{ item.currencyCode }}</p>
                  </div>
                  <div>
                    <p class="label">Amount</p>
                    <p class="font-bold">{{ item.currencyCode }} {{ item.grossAmount | number:'1.2-2' }}</p>
                  </div>
                </section>

                <section class="detail-section py-6">
                  <h2 class="section-title">Contribution allocation</h2>
                  <table class="w-full text-sm">
                    <thead>
                      <tr class="border-b text-left">
                        <th class="py-2">Purpose</th>
                        <th class="py-2">Contribution number</th>
                        <th class="text-right">Amount</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (line of item.lines; track $index) {
                        <tr class="detail-row border-b">
                          <td class="py-2">{{ line.contributionType }}</td>
                          <td class="py-2 font-mono">{{ line.contributionNumber || 'Posting pending' }}</td>
                          <td class="text-right">{{ item.currencyCode }} {{ line.amount | number:'1.2-2' }}</td>
                        </tr>
                      }
                    </tbody>
                    <tfoot>
                      <tr class="grand-total border-t-2 border-[#241C15] font-bold">
                        <td colspan="2" class="py-2 text-right uppercase">Grand Total</td>
                        <td class="text-right py-2">{{ item.currencyCode }} {{ item.grossAmount | number:'1.2-2' }}</td>
                      </tr>
                    </tfoot>
                  </table>
                </section>

                @if (item.status !== 'SUCCEEDED') {
                  <p class="mt-2 rounded border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800"><strong>This is not a final receipt.</strong> Funds are recorded as received only after provider verification succeeds.</p>
                }
              </div>

              <!-- ===== FOOTER ===== -->
              <footer class="border-t pt-4 mt-6 grid grid-cols-3 items-center text-[10px] text-[#8A7F6A]">
                <span>System-generated receipt</span>
                <span class="flex flex-col items-center justify-center gap-1 text-[8px] text-[#64748B]">
                  <span>Powered by BAU Solutions Limited</span>
                </span>
                <span class="text-right">Page 1 of 1</span>
              </footer>
            </div>
          </div>
        </div>
        <app-document-viewer #viewer></app-document-viewer>
      }
    </div>
  `,
  styles: [`
    .label { font-size: .7rem; text-transform: uppercase; color: #8A7F6A; margin-bottom: .2rem; }
    .section-title { font-weight: 700; margin-bottom: 1rem; text-transform: uppercase; font-size: 12pt; letter-spacing: .08em; }

    .receipt-source { position: fixed; left: -10000px; top: 0; width: 210mm; pointer-events: none; }
    .proof-screen-width { width: 100%; max-width: 56rem; }

    .page-preview { width: 100%; display: flex !important; justify-content: center !important; align-items: flex-start; }

    .paper {
      background: white;
      border: 1px solid #e2e8f0;
      border-radius: 0.5rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
      display: flex;
      flex-direction: column;
      width: 210mm;
      height: 297mm;
      min-height: 297mm;
      padding: 12mm;
      margin-left: auto !important;
      margin-right: auto !important;
      box-sizing: border-box;
      font-size: 11pt;
      line-height: 1.25;
      transform-origin: top center;
    }
    .paper > header { flex: 0 0 auto; }
    .paper .body-content { flex: 1; min-height: 0; }
    .paper .body-content section { padding-top: 8px; padding-bottom: 8px; }
    .paper.first-page .body-content section { padding-top: 6px; padding-bottom: 6px; }
    .paper .grand-total { font-size: 10pt; }
    .paper table th, .paper table td { padding-top: 4px !important; padding-bottom: 4px !important; line-height: 1.2; }
    .paper footer { flex: 0 0 auto; margin-top: 12px; padding-top: 8px; }
    .paper h1 { font-size: 18pt; line-height: 1.2; }
    .paper .body-content .text-sm { font-size: 10pt; }
    .paper .label, .paper .text-xs { font-size: 9pt; }
  `]
})
export class DigitalGivingReceiptComponent implements OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly service = inject(DigitalGivingService);
  private readonly auth = inject(AuthService);
  private readonly exporter = inject(ReportExportService);
  @ViewChild('viewer') private viewer?: DocumentViewerComponent;
  @ViewChild('receiptPage') private receiptPage?: ElementRef<HTMLElement>;
  readonly churchName = signal(this.auth.getChurchName());
  readonly receipt = signal<DigitalPaymentStatus | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly rendering = signal(false);
  readonly opened = signal(false);
  private objectUrl?: string;

  constructor() {
    this.auth.churchName$.subscribe(name => this.churchName.set(name));
    const reference = this.route.snapshot.paramMap.get('reference') ?? '';
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!reference || !token) { this.loading.set(false); this.error.set('The secure receipt link is incomplete.'); return; }
    this.service.getPaymentStatus(reference, token).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: item => {
        this.receipt.set(item);
        this.churchName.set(item.churchName);
        this.auth.setChurchName(item.churchName);
        void this.openDocument();
      },
      error: error => this.error.set(apiErrorMessage(error, 'The secure receipt link is invalid or no longer available.'))
    });
  }

  ngOnDestroy(): void { if (this.objectUrl) URL.revokeObjectURL(this.objectUrl); }

  async openDocument(): Promise<void> {
    if (!this.receipt() || this.rendering()) return;
    this.rendering.set(true);
    try {
      if (!(await this.whenRendered())) throw new Error('Receipt view was not ready.');
      await this.waitForImages(this.receiptPage!.nativeElement);
      const blob = await this.exporter.pdfBlobFromPages([this.receiptPage!.nativeElement], { orientation: 'portrait' });
      if (this.objectUrl) URL.revokeObjectURL(this.objectUrl);
      this.objectUrl = URL.createObjectURL(blob);
      this.viewer!.open(this.objectUrl, 'application/pdf', `Giving-Receipt-${this.receipt()!.paymentReference}.pdf`);
      this.opened.set(true);
    } catch {
      this.error.set('The receipt could not be prepared for viewing.');
    } finally { this.rendering.set(false); }
  }

  /** The sheet and viewer only exist after the data loads and the view renders, so wait for both. */
  private async whenRendered(): Promise<boolean> {
    for (let attempt = 0; attempt < 120; attempt++) {
      if (this.receiptPage && this.viewer) return true;
      await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));
    }
    return false;
  }

  /** Make sure the logo has loaded before the sheet is captured. */
  private async waitForImages(root: HTMLElement): Promise<void> {
    await Promise.all(Array.from(root.querySelectorAll('img')).map(image =>
      image.complete ? Promise.resolve() : new Promise<void>(resolve => { image.onload = () => resolve(); image.onerror = () => resolve(); })));
  }
}