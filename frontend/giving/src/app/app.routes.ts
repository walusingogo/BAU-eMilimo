import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/digital-giving/public-digital-giving.component').then(x => x.PublicDigitalGivingComponent) },
  { path: 'receipt/:reference', loadComponent: () => import('./features/digital-giving/digital-giving-receipt.component').then(x => x.DigitalGivingReceiptComponent) },
  { path: ':slug', loadComponent: () => import('./features/digital-giving/public-digital-giving.component').then(x => x.PublicDigitalGivingComponent) },
  { path: '**', redirectTo: '' }
];
