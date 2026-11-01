# Adding or deriving a business module

## Add a process to this product

1. Add a module manifest under `Platform:Modules` with a stable key and capability list.
2. Create the module's domain entities and EF configuration in its own folder.
3. Put workflow rules behind a module service interface; controllers should only translate HTTP requests.
4. Use capability permissions (`payroll.run`, `purchase-orders.approve`) rather than role names.
5. Use `IFileStorage` for attachments and shared audit/notification/proof services for cross-cutting behavior.
6. Add lazy-loaded Angular routes and let the module catalog control whether navigation is visible.
7. Add contract, authorization, workflow and migration tests.

To remove a process, set its manifest to `Enabled: false`. Data is retained, navigation is hidden and the module API returns a neutral not-available response. Physical removal can happen in a later product-specific build without losing data first.

## Derive Payroll or Purchase Orders

Keep eMilimo-specific business entities out of the reusable platform kernel. The target solution structure is:

```text
BAU.Platform.Core            shared contracts and primitives
BAU.Platform.Infrastructure identity, audit, storage, jobs, notifications
BAU.Platform.API            middleware, health and module discovery
Modules.Payroll             payroll domain/API
Modules.PurchaseOrders      purchasing domain/API
Products.eMilimo            eMilimo composition root
Products.Payroll            payroll composition root
```

Each product selects modules in configuration and provides branding, licence claims and navigation metadata. The composition root is the only place that should know the complete product.

Before extracting projects, new functionality should follow these boundaries inside eMilimo. Once two products consume a capability, extract it into `BAU.Platform.*`; avoid premature copying or a shared project containing church-specific entities.
