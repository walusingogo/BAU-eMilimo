export interface DigitalGivingOption {
  id: string;
  code: string;
  name: string;
}

export interface PaymentMethodOption extends DigitalGivingOption {
  category: string;
  providerName: string | null;
  providerEnvironment: string | null;
  currencyCode: string | null;
  isSandbox: boolean;
}

export interface PublicSandboxScenario {
  id: string;
  name: string;
  description: string;
  msisdn: string;
  expectedTransactionStatus: string;
  expectedProcessingBehaviour: string;
}

export interface PublicDigitalGivingConfiguration {
  id: string;
  churchName: string;
  isEnabled: boolean;
  publicSlug: string;
  currencyCode: string;
  minimumAmount: number;
  maximumAmount: number | null;
  contributionSourceId: string;
  welcomeMessage: string | null;
  contributionTypes: Array<{
    contributionTypeId: string;
    code: string;
    name: string;
    displayOrder: number;
    defaultDestinationType: string | null;
    requireDestinationSelection: boolean;
    destinations: Array<{ id: string | null; name: string; destinationType: string }>;
  }>;
  paymentMethods: PaymentMethodOption[];
  sandboxScenarios: PublicSandboxScenario[];
}

export interface MemberIdentificationResult {
  status: 'MATCHED' | 'NOT_FOUND' | 'MULTIPLE';
  challengeId: string | null;
  verificationToken: string | null;
  message: string;
  fullName: string | null;
  membershipNumber: string | null;
  phoneNumber: string | null;
}

export interface DigitalPaymentIntentResult {
  id: string;
  paymentReference: string;
  status: string;
  grossAmount: number;
  currencyCode: string;
  publicStatusToken: string;
  providerMessage: string | null;
  expiresAtUtc: string | null;
  isIdempotentReplay: boolean;
  isSandbox: boolean;
}

export interface DigitalPaymentStatus {
  churchName: string;
  paymentReference: string;
  status: string;
  settlementStatus: string;
  grossAmount: number;
  providerFee: number;
  netSettlementAmount: number;
  currencyCode: string;
  createdAtUtc: string;
  completedAtUtc: string | null;
  lines: Array<{ contributionType: string; amount: number; contributionNumber: string | null }>;
  isSandbox: boolean;
}
