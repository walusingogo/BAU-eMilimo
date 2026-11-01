import { ChangeDetectionStrategy, Component, OnDestroy, computed, inject, signal } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { EMPTY, Subscription, catchError, exhaustMap, finalize, takeWhile, tap, timer } from 'rxjs';
import { apiErrorMessage } from '../../core/utils/api-error';
import { AuthService } from '../../core/services/auth.service';
import { DigitalGivingService } from './digital-giving.service';
import {
  DigitalPaymentIntentResult,
  DigitalPaymentStatus,
  PublicDigitalGivingConfiguration
} from './digital-giving.models';

function createBrowserUuid(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, value => value.toString(16).padStart(2, '0')).join('');
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

@Component({
  selector: 'app-public-digital-giving',
  imports: [CommonModule, ReactiveFormsModule, NgOptimizedImage],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex min-h-screen flex-col bg-[#F5F6F7] text-slate-900 antialiased">
      <header class="border-b border-slate-200 bg-white">
        <div class="mx-auto flex max-w-xl items-center justify-between gap-4 px-5 py-3.5">
          <div class="flex min-w-0 items-center gap-3">
            <img ngSrc="/images/ucz-logo.png" width="36" height="36" priority alt="UCZ logo" class="h-9 w-9 shrink-0 object-contain" />
            <p class="truncate text-[15px] font-semibold text-[#173E55]">{{ churchName() }}</p>
          </div>
          <p class="flex shrink-0 items-center gap-1.5 text-xs text-slate-500">
            <svg class="h-3.5 w-3.5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M10 1a4 4 0 0 0-4 4v2H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V9a2 2 0 0 0-2-2h-1V5a4 4 0 0 0-4-4Zm2 6V5a2 2 0 1 0-4 0v2h4Z" clip-rule="evenodd"/></svg>
            Secure payment
          </p>
        </div>
      </header>

      <main class="mx-auto w-full max-w-xl flex-1 px-5 py-8 sm:py-12">
        @if (loading()) {
          <div class="animate-pulse space-y-5" aria-label="Loading giving page"><div class="h-8 w-48 rounded bg-slate-200"></div><div class="h-4 w-full rounded bg-slate-200"></div><div class="h-72 rounded-xl bg-white"></div></div>
        } @else if (loadError()) {
          <section class="rounded-xl border border-slate-200 bg-white p-8" role="alert">
            <h1 class="text-xl font-semibold">This giving page isn't available</h1>
            <p class="mt-2 text-sm leading-6 text-slate-600">{{ loadError() }}</p>
          </section>
        } @else if (intent(); as payment) {
          <section class="rounded-xl border border-slate-200 bg-white">
            <div class="border-b border-slate-200 p-6 sm:p-8">
              @if (paymentStatus()?.status === 'SUCCEEDED') {
                <div class="flex h-11 w-11 items-center justify-center rounded-full bg-emerald-600 text-white"><svg class="h-6 w-6" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M16.7 5.3a1 1 0 0 1 0 1.4l-7.5 7.5a1 1 0 0 1-1.4 0L3.3 9.7a1 1 0 1 1 1.4-1.4l3.8 3.8 6.8-6.8a1 1 0 0 1 1.4 0Z" clip-rule="evenodd"/></svg></div>
                <h1 class="mt-4 text-2xl font-semibold tracking-tight">{{ payment.isSandbox ? 'Test payment completed' : 'Thank you for your gift' }}</h1>
                <p class="mt-1.5 text-sm leading-6 text-slate-600">{{ payment.isSandbox ? 'MTN verified the sandbox transaction. The contribution, allocation and ledger records were posted through the full workflow.' : 'Your contribution has been verified and recorded.' }}</p>
              } @else if (isTerminalStatus(paymentStatus()?.status)) {
                <div class="flex h-11 w-11 items-center justify-center rounded-full bg-red-600 text-lg font-bold text-white" aria-hidden="true">!</div>
                <h1 class="mt-4 text-2xl font-semibold tracking-tight">Payment not completed</h1>
                <p class="mt-1.5 text-sm leading-6 text-slate-600">The status is {{ paymentStatus()?.status }}. You can start a new contribution below. If money left your account, contact the finance office with the reference.</p>
              } @else {
                <div class="h-11 w-11 animate-spin rounded-full border-[3px] border-slate-200 border-t-[#173E55]" role="status" aria-label="Waiting for payment"></div>
                <h1 class="mt-4 text-2xl font-semibold tracking-tight">{{ payment.isSandbox ? 'Test payment in progress' : 'Approve the payment on your phone' }}</h1>
                <p class="mt-1.5 text-sm leading-6 text-slate-600">{{ payment.isSandbox ? 'This runs against MTN MoMo Sandbox, then follows the same contribution, ledger, receipt and reporting workflow as production.' : 'Complete the ' + selectedPaymentMethodName() + ' prompt. Please do not start another payment while this one is pending.' }}</p>
              }
            </div>

            <div class="space-y-5 p-6 sm:p-8">
              <dl class="divide-y divide-slate-100 text-sm">
                <div class="flex items-baseline justify-between gap-4 py-2.5"><dt class="text-slate-500">Reference</dt><dd class="font-mono font-semibold text-[#173E55]">{{ payment.paymentReference }}</dd></div>
                <div class="flex items-baseline justify-between gap-4 py-2.5"><dt class="text-slate-500">Amount</dt><dd class="text-base font-semibold">{{ payment.currencyCode }} {{ payment.grossAmount | number:'1.2-2' }}</dd></div>
                @if (paymentStatus(); as status) { <div class="flex items-baseline justify-between gap-4 py-2.5"><dt class="text-slate-500">Status</dt><dd class="font-semibold">{{ status.status }}</dd></div> }
              </dl>

              @if (payment.providerMessage) { <p class="rounded-lg bg-slate-50 p-3 text-sm text-slate-700">{{ payment.providerMessage }}</p> }
              @if (!isTerminalStatus(paymentStatus()?.status)) { <p class="text-sm text-slate-500" role="status">Checking with MTN automatically. This page updates by itself.</p> }
              @if (statusRefreshMessage()) { <p class="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">{{ statusRefreshMessage() }}</p> }

              <div class="flex flex-col gap-3 pt-1">
                @if (paymentStatus()?.status === 'SUCCEEDED') { <button type="button" (click)="openReceipt()" class="w-full rounded-lg bg-[#173E55] px-5 py-3 font-semibold text-white hover:bg-[#102f42] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#173E55]">View receipt</button> }
                <button type="button" (click)="startAnother()" class="w-full rounded-lg border border-slate-300 px-5 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#173E55]">Give again</button>
              </div>
            </div>
          </section>
        } @else if (configuration(); as config) {
          <div>
            <h1 class="text-3xl font-semibold tracking-tight text-[#173E55] sm:text-4xl">Give to {{ churchName() }}</h1>
            <p class="mt-2 max-w-md text-[15px] leading-6 text-slate-600">{{ config.welcomeMessage || 'Give with your mobile money account. It takes about a minute.' }}</p>

            <form [formGroup]="form" (ngSubmit)="submit()" class="mt-7 rounded-xl border border-slate-200 bg-white">
              <div class="border-b border-slate-200 px-5 pb-4 pt-5 sm:px-8" aria-label="Giving progress">
                <div class="flex gap-1.5" aria-hidden="true">
                  @for (label of stepLabels; track label; let index = $index) { <span class="h-1 flex-1 rounded-full" [class.bg-[#173E55]]="currentStep() >= index + 1" [class.bg-slate-200]="currentStep() < index + 1"></span> }
                </div>
                <p class="mt-2.5 text-sm text-slate-500">Step {{ currentStep() }} of {{ stepLabels.length }}: <span class="font-semibold text-slate-800">{{ stepLabels[currentStep() - 1] }}</span></p>
              </div>

              <div class="space-y-6 p-5 sm:p-8">
                @if (currentStep() === 1) {
                <fieldset>
                  <legend class="text-lg font-semibold">What would you like to give?</legend>
                  <p class="mt-1 text-sm text-slate-500">Enter an amount next to each category. You can split one payment across several.</p>
                  <div class="mt-3 divide-y divide-slate-100">
                    @for (type of config.contributionTypes; track type.contributionTypeId) {
                      <div class="py-3.5">
                        <div class="flex items-center gap-4">
                          <label class="min-w-0 flex-1" [for]="'amount-' + type.contributionTypeId"><span class="block font-medium text-slate-800">{{ type.name }}</span></label>
                          <div class="flex w-36 items-center rounded-lg border border-slate-300 bg-white focus-within:border-[#173E55] focus-within:ring-2 focus-within:ring-[#173E55]/20">
                            <span class="pl-3 text-sm text-slate-500">{{ config.currencyCode }}</span>
                            <input [id]="'amount-' + type.contributionTypeId" type="number" min="0" step="0.01" inputmode="decimal" placeholder="0" [value]="amountFor(type.contributionTypeId) || ''" (input)="setAmount(type.contributionTypeId, numberValue($event))" class="w-full min-w-0 rounded-lg bg-transparent px-2 py-2 text-right font-semibold tabular-nums outline-none" [attr.aria-label]="'Amount for ' + type.name" />
                          </div>
                        </div>
                        @if (amountFor(type.contributionTypeId) > 0 && (type.requireDestinationSelection || type.destinations.length > 1)) {
                          <label class="mt-3 block">
                            <span class="text-sm text-slate-600">Where should this go?</span>
                            <select [value]="destinationKeyFor(type.contributionTypeId)" (change)="setDestination(type.contributionTypeId, $event)" class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm focus:border-[#173E55] focus:outline-none focus:ring-2 focus:ring-[#173E55]/20">
                              @if (type.requireDestinationSelection) { <option value="">Choose a destination</option> }
                              @for (destination of type.destinations; track destination.destinationType + ':' + destination.id) {
                                <option [value]="destinationKey(destination.destinationType, destination.id)">{{ destination.name }}</option>
                              }
                            </select>
                          </label>
                        }
                      </div>
                    }
                  </div>
                  <div class="flex items-baseline justify-between border-t-2 border-slate-900 pt-4"><span class="font-semibold">Total</span><span class="text-2xl font-semibold tabular-nums text-[#173E55]">{{ config.currencyCode }} {{ total() | number:'1.2-2' }}</span></div>
                </fieldset>
                @if (formError()) { <div class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800" role="alert">{{ formError() }}</div> }
                <button type="button" (click)="nextFromAmounts()" class="w-full rounded-lg bg-[#173E55] px-5 py-3.5 font-semibold text-white hover:bg-[#102f42] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#173E55]">Continue</button>
                }

                @if (currentStep() === 2) {
                <fieldset>
                  <legend class="text-lg font-semibold">How should we record your gift?</legend>
                  <div class="mt-3 grid grid-cols-3 rounded-lg bg-slate-100 p-1">
                    @for (choice of giverChoices; track choice.value) {
                      <label class="cursor-pointer rounded-md px-2 py-2 text-center text-sm font-semibold focus-within:outline focus-within:outline-2 focus-within:outline-[#173E55]" [class.bg-white]="form.controls.giverType.value === choice.value" [class.shadow-sm]="form.controls.giverType.value === choice.value" [class.text-[#173E55]]="form.controls.giverType.value === choice.value" [class.text-slate-500]="form.controls.giverType.value !== choice.value">
                        <input type="radio" formControlName="giverType" [value]="choice.value" class="sr-only" />{{ choice.label }}
                      </label>
                    }
                  </div>
                </fieldset>

                @if (form.controls.giverType.value === 'MEMBER') {
                  <section aria-labelledby="member-verification-heading">
                    <h2 id="member-verification-heading" class="font-semibold text-slate-800">Find your membership record</h2>
                    <p class="mt-1 text-sm leading-5 text-slate-500">Search by membership number, registered phone number or full registered name. This doesn't sign you in.</p>
                    @if (!memberVerified()) {
                      <div class="mt-3 flex flex-col gap-2 sm:flex-row"><input formControlName="memberLookup" autocomplete="off" class="min-w-0 flex-1 rounded-lg border border-slate-300 bg-white px-3 py-2.5 focus:border-[#173E55] focus:outline-none focus:ring-2 focus:ring-[#173E55]/20" placeholder="Membership number, phone or full name" aria-label="Membership number, phone or full name" /><button type="button" (click)="identifyMember()" [disabled]="identifying()" class="rounded-lg bg-[#173E55] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#102f42] disabled:opacity-60">{{ identifying() ? 'Searching…' : 'Find record' }}</button></div>
                      @if (needsAdditionalIdentifier()) { <div class="mt-3 rounded-lg border border-amber-200 bg-amber-50 p-3"><p class="text-sm leading-5 text-amber-900">{{ verificationMessage() }}</p><div class="mt-2 flex flex-col gap-2 sm:flex-row"><input formControlName="additionalIdentifier" autocomplete="off" class="min-w-0 flex-1 rounded-lg border border-amber-300 bg-white px-3 py-2" placeholder="Registered phone or membership number" aria-label="Registered phone or membership number" /><button type="button" (click)="identifyMember()" [disabled]="identifying()" class="rounded-lg border border-amber-700 px-4 py-2 text-sm font-semibold text-amber-900 hover:bg-amber-100">Narrow search</button></div></div> }
                      @if (memberNotFound()) { <div class="mt-3 rounded-lg bg-slate-50 p-3 text-sm text-slate-700"><p>{{ verificationMessage() }}</p><button type="button" (click)="continueAsGuest()" class="mt-2 font-semibold text-[#173E55] underline underline-offset-2">Give as a guest instead</button></div> }
                    } @else {
                      <div class="mt-3 flex gap-3 rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-emerald-950">
                        <svg class="mt-0.5 h-5 w-5 shrink-0 text-emerald-600" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true"><path fill-rule="evenodd" d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm3.7-9.3a1 1 0 0 0-1.4-1.4L9 10.6 7.7 9.3a1 1 0 0 0-1.4 1.4l2 2a1 1 0 0 0 1.4 0l4-4Z" clip-rule="evenodd"/></svg>
                        <div class="text-sm"><p class="font-semibold">{{ matchedMemberName() }}</p><p class="mt-0.5 text-emerald-800">Membership ID {{ matchedMembershipNumber() }}</p>@if (matchedPhoneNumber()) { <p class="text-emerald-800">Registered mobile {{ matchedPhoneNumber() }}</p> }</div>
                      </div>
                    }
                  </section>
                } @else if (form.controls.giverType.value === 'GUEST') {
                  <div class="space-y-4">
                    <label class="block"><span class="text-sm font-medium text-slate-700">Full name</span><input formControlName="contributorName" autocomplete="name" class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 focus:border-[#173E55] focus:outline-none focus:ring-2 focus:ring-[#173E55]/20" /></label>
                    <label class="block"><span class="text-sm font-medium text-slate-700">Email <span class="font-normal text-slate-400">(optional, for your receipt)</span></span><input type="email" formControlName="email" autocomplete="email" class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 focus:border-[#173E55] focus:outline-none focus:ring-2 focus:ring-[#173E55]/20" /></label>
                  </div>
                } @else {
                  <p class="text-sm leading-6 text-slate-600">Your name won't appear in regular contribution reports. Payment details are kept only for provider reconciliation and compliance.</p>
                }
                @if (formError()) { <div class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800" role="alert">{{ formError() }}</div> }
                <div class="flex gap-3"><button type="button" (click)="previousStep()" class="flex-1 rounded-lg border border-slate-300 px-5 py-3 font-semibold text-slate-700 hover:bg-slate-50">Back</button><button type="button" (click)="nextFromIdentity()" class="flex-[2] rounded-lg bg-[#173E55] px-5 py-3 font-semibold text-white hover:bg-[#102f42] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#173E55]">Continue</button></div>
                }

                @if (currentStep() === 3) {
                <fieldset>
                  <legend class="text-lg font-semibold">Payment details</legend>
                  <div class="mt-3 space-y-4">
                    <label class="block"><span class="text-sm font-medium text-slate-700">Payment method</span><select formControlName="paymentMethodDefinitionId" class="mt-1 w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 focus:border-[#173E55] focus:outline-none focus:ring-2 focus:ring-[#173E55]/20"><option value="">Choose a method</option>@for (method of config.paymentMethods; track method.id) { <option [value]="method.id">{{ method.name }}{{ method.isSandbox ? ' (sandbox test)' : '' }}</option> }</select></label>
                    <label class="block"><span class="text-sm font-medium text-slate-700">{{ selectedPaymentMethod()?.isSandbox ? 'MTN test scenario' : 'Mobile money number' }}</span>@if (selectedPaymentMethod()?.isSandbox) { <select formControlName="paymentPhone" class="mt-1 w-full rounded-lg border border-amber-300 bg-white px-3 py-2.5 focus:outline-none focus:ring-2 focus:ring-amber-400/40"><option value="">Choose a test scenario</option>@for (scenario of config.sandboxScenarios; track scenario.id) { <option [value]="scenario.msisdn">{{ scenario.name }}, {{ scenario.msisdn }}, {{ scenario.expectedTransactionStatus }}</option> }</select> } @else { <input formControlName="paymentPhone" autocomplete="tel" inputmode="tel" class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2.5 focus:border-[#173E55] focus:outline-none focus:ring-2 focus:ring-[#173E55]/20" placeholder="0977 000 000" /> }<span class="mt-1.5 block text-sm text-slate-500">{{ selectedPaymentMethod()?.isSandbox ? 'Pick an MTN test number to simulate success, rejection, delay or another outcome.' : (memberVerified() && matchedPhoneNumber() ? 'We filled in your registered number. You can use a different one.' : 'This number will receive the payment prompt.') }}</span></label>
                  </div>
                </fieldset>

                @if (selectedPaymentMethod()?.isSandbox) { <div class="rounded-lg border border-amber-300 bg-amber-50 p-4 text-sm leading-5 text-amber-950" role="note"><p class="font-semibold">Sandbox test</p><p class="mt-1">The amount is sent as {{ effectiveCurrency() }} using MTN's test numbers. No real money moves, but a verified success creates the same contribution, ledger, receipt and report records as production.</p></div> }

                @if (formError()) { <div class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800" role="alert">{{ formError() }}</div> }
                <div class="flex gap-3"><button type="button" (click)="previousStep()" class="flex-1 rounded-lg border border-slate-300 px-5 py-3 font-semibold text-slate-700 hover:bg-slate-50">Back</button><button type="submit" [disabled]="submitting()" class="flex-[2] rounded-lg bg-[#173E55] px-5 py-3.5 font-semibold text-white hover:bg-[#102f42] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[#173E55] disabled:cursor-wait disabled:opacity-60">{{ submitting() ? 'Sending request…' : 'Pay ' + effectiveCurrency() + ' ' + (total() | number:'1.2-2') }}</button></div>
                <p class="text-center text-sm leading-5 text-slate-500">Your gift is recorded only after your mobile money provider confirms it.</p>
                }
              </div>
            </form>

            <p class="mt-5 text-sm leading-6 text-slate-500">If a payment stays pending, contact the church finance office before trying again so you aren't charged twice.</p>
          </div>
        }
      </main>

      <footer class="px-5 py-6 text-center text-xs text-slate-500">Powered by <a href="https://www.bausolutions.africa" target="_blank" rel="noopener" class="font-semibold text-slate-700 underline underline-offset-2">BAU Solutions Limited</a></footer>

      @if (submitting()) { <div class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 p-4" role="status" aria-live="polite"><div class="rounded-xl bg-white p-6 text-center shadow-xl"><div class="mx-auto h-9 w-9 animate-spin rounded-full border-[3px] border-slate-200 border-t-[#173E55]"></div><p class="mt-4 font-semibold text-slate-900">Sending your payment request</p><p class="mt-1 text-sm text-slate-500">Please keep this page open.</p></div></div> }
    </div>
  `
})
export class PublicDigitalGivingComponent implements OnDestroy {
  private readonly service = inject(DigitalGivingService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly subscriptions = new Subscription();
  private readonly slug = this.route.snapshot.paramMap.get('slug');
  private idempotencyKey = createBrowserUuid();
  private statusPolling: Subscription | null = null;

  readonly churchName = signal(this.auth.getChurchName());
  readonly configuration = signal<PublicDigitalGivingConfiguration | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal('');
  readonly submitting = signal(false);
  readonly identifying = signal(false);
  readonly checkingStatus = signal(false);
  readonly statusRefreshMessage = signal('');
  readonly challengeId = signal('');
  readonly verificationToken = signal('');
  readonly verificationMessage = signal('');
  readonly memberVerified = signal(false);
  readonly needsAdditionalIdentifier = signal(false);
  readonly memberNotFound = signal(false);
  readonly matchedMemberName = signal('');
  readonly matchedMembershipNumber = signal('');
  readonly matchedPhoneNumber = signal('');
  readonly amounts = signal<Record<string, number>>({});
  readonly destinations = signal<Record<string, { destinationType: string; destinationId: string | null }>>({});
  readonly intent = signal<DigitalPaymentIntentResult | null>(null);
  readonly paymentStatus = signal<DigitalPaymentStatus | null>(null);
  readonly formError = signal('');
  readonly currentStep = signal(1);
  readonly stepLabels = ['Amount', 'Your details', 'Payment'];
  readonly total = computed(() => Object.values(this.amounts()).reduce((sum, value) => sum + (Number.isFinite(value) ? value : 0), 0));
  readonly selectedPaymentMethod = computed(() => this.configuration()?.paymentMethods.find(item => item.id === this.form.controls.paymentMethodDefinitionId.value) ?? null);
  readonly selectedPaymentMethodName = computed(() => this.selectedPaymentMethod()?.name ?? 'mobile money');
  readonly effectiveCurrency = computed(() => this.selectedPaymentMethod()?.currencyCode ?? this.configuration()?.currencyCode ?? 'ZMW');
  readonly giverChoices = [
    { value: 'MEMBER', label: 'Member' },
    { value: 'GUEST', label: 'Guest' },
    { value: 'ANONYMOUS', label: 'Anonymous' }
  ] as const;

  readonly form = this.fb.nonNullable.group({
    giverType: 'GUEST',
    memberLookup: '',
    additionalIdentifier: '',
    contributorName: '',
    email: ['', Validators.email],
    paymentMethodDefinitionId: ['', Validators.required],
    paymentPhone: ['', [Validators.required, Validators.minLength(9), Validators.maxLength(20)]]
  });

  constructor() {
    this.load();
    this.subscriptions.add(this.form.controls.giverType.valueChanges.subscribe(() => {
      this.memberVerified.set(false);
      this.challengeId.set('');
      this.verificationToken.set('');
      this.needsAdditionalIdentifier.set(false);
      this.memberNotFound.set(false);
      this.matchedMemberName.set('');
      this.matchedMembershipNumber.set('');
      this.matchedPhoneNumber.set('');
      this.formError.set('');
    }));
    this.subscriptions.add(this.form.controls.paymentMethodDefinitionId.valueChanges.subscribe(() => {
      const method = this.selectedPaymentMethod();
      if (method?.isSandbox) this.form.controls.paymentPhone.setValue('');
      else if (this.memberVerified() && this.matchedPhoneNumber()) this.form.controls.paymentPhone.setValue(this.matchedPhoneNumber());
    }));
  }

  ngOnDestroy(): void { this.statusPolling?.unsubscribe(); this.subscriptions.unsubscribe(); }

  private load(): void {
    this.service.getPublicConfiguration(this.slug).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: configuration => {
        this.configuration.set(configuration);
        this.churchName.set(configuration.churchName);
        this.auth.setChurchName(configuration.churchName);
        this.amounts.set(Object.fromEntries(configuration.contributionTypes.map(item => [item.contributionTypeId, 0])));
        this.destinations.set(Object.fromEntries(configuration.contributionTypes.map(item => {
          const preferred = item.requireDestinationSelection && item.destinations.length > 1
            ? null
            : item.destinations.find(destination => destination.destinationType === item.defaultDestinationType)
              ?? (item.destinations.length === 1 ? item.destinations[0] : null);
          return [item.contributionTypeId, preferred
            ? { destinationType: preferred.destinationType, destinationId: preferred.id }
            : { destinationType: '', destinationId: null }];
        })));
        if (configuration.paymentMethods.length === 1) this.form.controls.paymentMethodDefinitionId.setValue(configuration.paymentMethods[0].id);
      },
      error: error => this.loadError.set(apiErrorMessage(error, 'This giving page is not published or is temporarily unavailable.'))
    });
  }

  amountFor(id: string): number { return this.amounts()[id] ?? 0; }
  setAmount(id: string, amount: number): void { this.amounts.update(values => ({ ...values, [id]: Math.max(0, amount) })); }
  destinationKey(type: string, id: string | null): string { return `${type}:${id ?? ''}`; }
  destinationKeyFor(contributionTypeId: string): string {
    const value = this.destinations()[contributionTypeId];
    return value?.destinationType ? this.destinationKey(value.destinationType, value.destinationId) : '';
  }
  setDestination(contributionTypeId: string, event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const separator = value.indexOf(':');
    const destinationType = separator >= 0 ? value.slice(0, separator) : '';
    const destinationId = separator >= 0 && value.slice(separator + 1) ? value.slice(separator + 1) : null;
    this.destinations.update(items => ({ ...items, [contributionTypeId]: { destinationType, destinationId } }));
  }

  nextFromAmounts(): void {
    const config = this.configuration();
    if (!config) return;
    const total = Number(this.total().toFixed(2));
    if (total < config.minimumAmount || config.maximumAmount !== null && total > config.maximumAmount) {
      this.formError.set(`Enter a total between ${config.currencyCode} ${config.minimumAmount.toFixed(2)}${config.maximumAmount === null ? '' : ` and ${config.maximumAmount.toFixed(2)}`}.`);
      return;
    }
    const missing = config.contributionTypes.find(item => (this.amounts()[item.contributionTypeId] ?? 0) > 0 && item.requireDestinationSelection && !this.destinations()[item.contributionTypeId]?.destinationType);
    if (missing) { this.formError.set(`Select a destination for ${missing.name}.`); return; }
    this.formError.set('');
    this.currentStep.set(2);
  }

  nextFromIdentity(): void {
    const value = this.form.getRawValue();
    if (value.giverType === 'MEMBER' && !this.memberVerified()) { this.formError.set('Match your active membership record before continuing.'); return; }
    if (value.giverType === 'GUEST' && value.contributorName.trim().length < 2) { this.formError.set('Enter your full name.'); return; }
    if (value.giverType === 'GUEST' && this.form.controls.email.invalid) { this.formError.set('Enter a valid email address or leave it blank.'); return; }
    this.formError.set('');
    this.currentStep.set(3);
  }

  previousStep(): void { this.formError.set(''); this.currentStep.update(step => Math.max(1, step - 1)); }

  identifyMember(): void {
    const lookup = this.form.controls.memberLookup.value.trim();
    if (lookup.length < 3) { this.formError.set('Enter your membership number, registered phone number or full name.'); return; }
    this.formError.set('');
    this.identifying.set(true);
    this.service.identifyMember(lookup, this.form.controls.additionalIdentifier.value.trim()).pipe(finalize(() => this.identifying.set(false))).subscribe({
      next: result => {
        this.verificationMessage.set(result.message);
        this.needsAdditionalIdentifier.set(result.status === 'MULTIPLE');
        this.memberNotFound.set(result.status === 'NOT_FOUND');
        if (result.status !== 'MATCHED' || !result.challengeId || !result.verificationToken) return;
        this.challengeId.set(result.challengeId);
        this.verificationToken.set(result.verificationToken);
        this.matchedMemberName.set(result.fullName ?? 'Registered member');
        this.matchedMembershipNumber.set(result.membershipNumber ?? '—');
        this.matchedPhoneNumber.set(result.phoneNumber ?? '');
        if (result.phoneNumber && !this.form.controls.paymentPhone.value.trim()) this.form.controls.paymentPhone.setValue(result.phoneNumber);
        this.memberVerified.set(true);
      },
      error: error => this.formError.set(apiErrorMessage(error, 'The membership record could not be checked.'))
    });
  }

  continueAsGuest(): void { this.form.controls.giverType.setValue('GUEST'); }

  submit(): void {
    const config = this.configuration();
    if (!config) return;
    this.form.markAllAsTouched();
    const total = Number(this.total().toFixed(2));
    if (total < config.minimumAmount || config.maximumAmount !== null && total > config.maximumAmount) {
      this.formError.set(`Enter a total between ${config.currencyCode} ${config.minimumAmount.toFixed(2)}${config.maximumAmount === null ? '' : ` and ${config.maximumAmount.toFixed(2)}`}.`);
      return;
    }
    if (this.form.invalid) { this.formError.set('Complete the payment method and mobile money number.'); return; }
    const value = this.form.getRawValue();
    if (value.giverType === 'MEMBER' && !this.memberVerified()) { this.formError.set('Match your active membership record before continuing.'); return; }
    if (value.giverType === 'GUEST' && value.contributorName.trim().length < 2) { this.formError.set('Enter your full name.'); return; }
    const selectedTypes = config.contributionTypes.filter(item => (this.amounts()[item.contributionTypeId] ?? 0) > 0);
    const missingDestination = selectedTypes.find(item => item.requireDestinationSelection && !this.destinations()[item.contributionTypeId]?.destinationType);
    if (missingDestination) { this.formError.set(`Select a destination for ${missingDestination.name}.`); return; }
    const lines = selectedTypes
      .map(item => ({
        contributionTypeId: item.contributionTypeId,
        amount: Number((this.amounts()[item.contributionTypeId] ?? 0).toFixed(2)),
        destinationType: this.destinations()[item.contributionTypeId]?.destinationType || item.defaultDestinationType || 'ORGANIZATION',
        destinationId: this.destinations()[item.contributionTypeId]?.destinationId ?? null
      }))
      .filter(item => item.amount > 0);
    this.formError.set('');
    this.submitting.set(true);
    this.service.createPaymentIntent(config.publicSlug, {
      publicSlug: config.publicSlug,
      giverType: value.giverType,
      paymentMethodDefinitionId: value.paymentMethodDefinitionId,
      totalAmount: total,
      currencyCode: this.effectiveCurrency(),
      paymentPhone: value.paymentPhone.trim(),
      contributorName: value.giverType === 'GUEST' ? value.contributorName.trim() : null,
      email: value.giverType === 'GUEST' && value.email.trim() ? value.email.trim() : null,
      memberVerificationChallengeId: value.giverType === 'MEMBER' ? this.challengeId() : null,
      memberVerificationToken: value.giverType === 'MEMBER' ? this.verificationToken() : null,
      idempotencyKey: this.idempotencyKey,
      lines
    }).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: result => {
        this.intent.set(result);
        this.paymentStatus.set({ churchName: config.churchName, paymentReference: result.paymentReference, status: result.status, settlementStatus: 'PENDING', grossAmount: result.grossAmount, providerFee: 0, netSettlementAmount: 0, currencyCode: result.currencyCode, createdAtUtc: new Date().toISOString(), completedAtUtc: null, lines: [], isSandbox: result.isSandbox });
        this.startStatusPolling();
      },
      error: error => this.formError.set(apiErrorMessage(error, 'The payment request could not be created.'))
    });
  }

  private startStatusPolling(): void {
    const payment = this.intent();
    if (!payment) return;
    this.statusPolling?.unsubscribe();
    this.checkingStatus.set(true);
    this.statusPolling = timer(750, 4000).pipe(
      exhaustMap(() => this.service.refreshPaymentStatus(payment.paymentReference, payment.publicStatusToken).pipe(
        catchError(() => {
          this.statusRefreshMessage.set('MTN could not be reached just now. eMilimo will keep checking safely. Do not submit another payment.');
          return EMPTY;
        })
      )),
      tap(status => { this.paymentStatus.set(status); this.statusRefreshMessage.set(''); }),
      takeWhile(status => !this.isTerminalStatus(status.status), true),
      finalize(() => this.checkingStatus.set(false))
    ).subscribe();
  }

  isTerminalStatus(status: string | null | undefined): boolean {
    return !!status && ['SUCCEEDED', 'FAILED', 'CANCELLED', 'EXPIRED', 'REVERSED'].includes(status);
  }

  startAnother(): void {
    this.statusPolling?.unsubscribe();
    this.statusPolling = null;
    this.checkingStatus.set(false);
    this.statusRefreshMessage.set('');
    this.intent.set(null);
    this.paymentStatus.set(null);
    this.idempotencyKey = createBrowserUuid();
    this.amounts.update(values => Object.fromEntries(Object.keys(values).map(key => [key, 0])));
    this.currentStep.set(1);
    this.formError.set('');
  }

  openReceipt(): void {
    const payment = this.intent();
    if (!payment) return;
    const path = `/receipt/${encodeURIComponent(payment.paymentReference)}?token=${encodeURIComponent(payment.publicStatusToken)}`;
    window.open(path, '_blank', 'noopener');
  }

  numberValue(event: Event): number { return Number((event.target as HTMLInputElement).value) || 0; }
}