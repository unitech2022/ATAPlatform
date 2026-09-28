# F11 المدفوعات + F13 الإشعارات (OneSignal / SMS / القوالب)

الطبقات: MySQL → API (وحدتا `Payments` و`Notifications` + توسعة `Wallet` و`Trips`) → Flutter (features `payments`, `driver_wallet`, توسعة `wallet` و`notifications` + `core/push`) → لوحة الإدارة (المدفوعات، الاستردادات، السحوبات، التسويات، المحافظ، القوالب، الحملات، سجل الإرسال). لا صفحات للموقع في هذه الميزة.

هذا المستند هو **المرجع الموحّد** لثلاثة أشياء تعتمد عليها بقية المستندات (09–12):
1. حسابات الدفتر المالي وأنواع الحركات والقيود (§F11.3).
2. كتالوج أكواد أحداث الإشعارات (§F13.2).
3. مخطط الروابط العميقة `ata://` (§F13.7).

الاصطلاحات كما في `04`/`05`: جداول `snake_case` بالجمع، `id CHAR(36)` UUID v7، `DATETIME(6)` UTC، المبالغ `DECIMAL(12,2)`، JSON `camelCase`، قيم الـenum في JSON بصيغة `snake_case` (مثل `partially_refunded`)، غلاف الأخطاء الموحد، شكل الصفحات `{ items, page, pageSize, total }`، كل كتابة إدارية تُسجَّل في `audit_logs` (العمود `action` بصيغة `<entity>.<verb>` مثل `refund.approve`).

---

# F11 — المدفوعات

## F11.1 تجريد بوابة الدفع

```csharp
namespace ATA.Api.Modules.Payments.Gateways;

public interface IPaymentGateway
{
    string Provider { get; }                       // "sandbox" | "moyasar"
    Task<GatewayResult> AuthorizeAsync(GatewayChargeRequest request, CancellationToken ct);  // capture يدوي
    Task<GatewayResult> PurchaseAsync(GatewayChargeRequest request, CancellationToken ct);   // capture فوري
    Task<GatewayResult> CaptureAsync(string gatewayPaymentId, decimal amount, CancellationToken ct);
    Task<GatewayResult> VoidAsync(string gatewayPaymentId, CancellationToken ct);
    Task<GatewayResult> RefundAsync(string gatewayPaymentId, decimal amount, string idempotencyKey, CancellationToken ct);
    Task<GatewayResult> FetchAsync(string gatewayPaymentId, CancellationToken ct);
    Task<GatewayCardResult> VerifyCardTokenAsync(string token, string returnUrl, CancellationToken ct); // حفظ بطاقة
    Task DeleteCardTokenAsync(string token, CancellationToken ct);
    WebhookParseResult ParseWebhook(string rawBody, IHeaderDictionary headers);  // يتحقق من التوقيع
}

public sealed record GatewayChargeRequest(
    Guid PaymentId, decimal Amount, string Currency, GatewaySource Source, string Description,
    string ReturnUrl, string IdempotencyKey, IReadOnlyDictionary<string, string> Metadata);

public sealed record GatewaySource(string Type /* token | apple_pay | sandbox */, string Value);

public sealed record GatewayResult(
    bool Success, string? GatewayPaymentId, GatewayStatus Status, decimal? AuthorizedAmount, decimal? CapturedAmount,
    string? ActionUrl, string? FailureCode, string? FailureMessage, bool IsTransient, string? RawStatus);

public enum GatewayStatus { Initiated, RequiresAction, Authorized, Captured, Failed, Voided, Refunded, PartiallyRefunded }

public sealed record GatewayCardResult(bool Success, string? Token, string? Brand, string? Last4, int? ExpiryMonth, int? ExpiryYear,
    string? HolderName, string? ActionUrl, string? FailureCode, string? FailureMessage);

public sealed record WebhookParseResult(bool SignatureValid, string? EventId, string? EventType, string? GatewayPaymentId, GatewayStatus? Status,
    decimal? Amount, string? FailureCode);
```

- التسجيل عبر `Payments:Provider` (`sandbox` افتراضي في التطوير). `IPaymentGatewayResolver.Get(provider)` يعيد التنفيذ لأن الدفعات القديمة تحتفظ بمزوّدها.
- كل استدعاء للبوابة يتم **خارج** معاملة قاعدة البيانات، بمهلة `Payments:GatewayTimeoutSeconds` (15)، وبـ`IdempotencyKey` = `payments.idempotency_key` (أو `refunds.id`).

### SandboxGateway (التطوير والاختبارات)
- يقبل رموزاً ثابتة (لا يوجد PAN إطلاقاً):

| الرمز | السلوك | brand / last4 |
|---|---|---|
| `tok_sandbox_mada` | نجاح فوري | `mada` / `4201` |
| `tok_sandbox_visa` | نجاح فوري | `visa` / `4242` |
| `tok_sandbox_mastercard` | نجاح فوري | `mastercard` / `5454` |
| `tok_sandbox_3ds` | `RequiresAction` مع `ActionUrl` = صفحة تحدٍّ محاكاة | `visa` / `1091` |
| `tok_sandbox_declined` | `Failed` بـ`failureCode = card_declined` | `visa` / `0002` |
| `tok_sandbox_insufficient` | `Failed` بـ`insufficient_funds` | `mada` / `9995` |
| `tok_sandbox_capture_fail` | التفويض ينجح، الـcapture يفشل (`card_declined`) | `visa` / `3220` |
| `tok_sandbox_timeout` | يتجاوز المهلة (`IsTransient = true`) | `visa` / `4000` |
| `applepay_sandbox` | مصدر Apple Pay ناجح | `visa` / `0000` |

- صفحة التحدي: `GET /api/v1/payments/sandbox/challenge/{paymentId}` تعيد HTML بسيطاً بزرّين (موافقة / رفض) يرسلان `POST /api/v1/payments/sandbox/challenge/{paymentId}` بحقل `outcome=approve|decline` → الخادم يحدّث الدفعة كما لو وصل Webhook ثم `302` إلى `returnUrl?paymentId={id}&status={status}`. متاحة فقط عندما `Payments:Provider=sandbox`.
- `Payments:SandboxSkipAuthorization=true` يجعل رحلات البطاقة لا تُفوَّض عند الطلب وتُحصَّل مباشرة عند الإكمال (`PurchaseAsync`).
- الطريقة القديمة `POST /wallet/topups { method: "sandbox" }` تبقى كما هي (بدون صف في `payments`) طالما `Payments:SandboxEnabled=true`.

### MoyasarGateway (تصميم المحوّل للمزوّد السعودي — نمط Moyasar)
- العميل (Flutter) يُنشئ **رمز البطاقة** عبر SDK المزوّد بالمفتاح العام (`publishableKey`) — الخادم لا يستقبل رقم البطاقة أبداً (PCI DSS SAQ-A).
- المصادر المدعومة: `token` (مدى/Visa/Mastercard محفوظة)، `apple_pay` (رمز Apple Pay من الجهاز — للشحن فقط في v1).
- إنشاء دفعة: `POST {BaseUrl}/v1/payments` بـ`amount` بالهللة (×100)، `currency=SAR`، `source`، `callback_url` = `{Payments:PublicBaseUrl}/api/v1/payments/return/moyasar`، `metadata.paymentId`، وللتفويض `source.manual=true`. عند 3DS يعيد المزوّد `source.transaction_url` → يصبح `action.url`.
- Capture: `POST /v1/payments/{id}/capture {amount}`، Void: `POST /v1/payments/{id}/void`، Refund: `POST /v1/payments/{id}/refund {amount}`، Fetch: `GET /v1/payments/{id}`.
- خريطة الحالات: `initiated→initiated`، `authorized→authorized`، `captured`/`paid→captured`، `failed→failed`، `voided→voided`، `refunded→refunded|partially_refunded` (حسب `refunded` مقابل `captured`).
- Webhooks: الخادم يتحقق من التوقيع قبل أي معالجة: HMAC-SHA256 للجسم الخام بـ`Payments:Moyasar:WebhookSecret` مقارنةً بالترويسة المعرّفة في `Payments:Moyasar:SignatureHeader`، أو (إن كان المزوّد يرسل سراً مشتركاً داخل الجسم مثل `secret_token`) مقارنة ثابتة الزمن. أي فشل → `401 webhook_signature_invalid` ويُحفظ الحدث بـ`signature_valid=false`.
- الإعدادات: `Payments:Moyasar:BaseUrl`, `Payments:Moyasar:PublishableKey`, `Payments:Moyasar:SecretKey` (سر)، `Payments:Moyasar:WebhookSecret` (سر)، `Payments:Moyasar:SignatureHeader`, `Payments:Moyasar:ApplePayMerchantId`.
- هذا المحوّل يُسلَّم كـ **placeholder** مع اختبارات عقد (تسجيل HTTP مزيّف)؛ تفعيله في الإنتاج يتطلب حساب تاجر وتوثيق المزوّد الفعلي.

## F11.2 الجداول (MySQL)

| جدول | الأعمدة |
|---|---|
| `payment_methods` | id, user_id (FK), provider (`sandbox`/`moyasar`), type (`card`), gateway_token VARCHAR(255) (رمز فقط — **لا PAN ولا CVV**)، brand (`mada`/`visa`/`mastercard`)، last4 CHAR(4), expiry_month TINYINT, expiry_year SMALLINT, holder_name NULL, fingerprint VARCHAR(64) NULL (لمنع تكرار نفس البطاقة)، status (`pending_verification`/`active`/`failed`/`removed`)، is_default BOOL, verified_at NULL, removed_at NULL, created_at, updated_at — INDEX(user_id, status), UNIQUE(user_id, fingerprint) |
| `payments` | id, user_id (FK), purpose (`trip`/`topup`/`cancellation_fee`)، trip_id NULL, wallet_id NULL (للشحن)، payment_method_id NULL, method (`card`/`apple_pay`/`sandbox`)، provider, currency (`SAR`)، amount (المطلوب)، authorized_amount, captured_amount, refunded_amount (افتراضي 0)، status (`initiated`/`authorized`/`captured`/`failed`/`voided`/`refunded`/`partially_refunded`)، capture_mode (`manual`/`auto`)، gateway_payment_id VARCHAR(100) NULL UNIQUE, gateway_status VARCHAR(40) NULL, action_url VARCHAR(1000) NULL, return_url VARCHAR(500) NULL, action_expires_at NULL, failure_code VARCHAR(60) NULL, failure_message VARCHAR(500) NULL, idempotency_key VARCHAR(100) UNIQUE, authorized_at, captured_at, failed_at, voided_at, metadata JSON NULL, created_at, updated_at — INDEX(user_id, created_at), INDEX(trip_id), INDEX(status, created_at) |
| `payment_webhook_events` | id, provider, event_id VARCHAR(100), event_type VARCHAR(60), gateway_payment_id NULL, signature_valid BOOL, payload JSON, processing_status (`pending`/`processed`/`ignored`/`failed`)، error VARCHAR(500) NULL, received_at, processed_at NULL — UNIQUE(provider, event_id) (منع التكرار) |
| `refunds` | id, refund_number (UNIQUE `R-YYYYMMDD-#####`)، payment_id NULL, trip_id NULL, user_id (المستفيد)، amount, type (`full`/`partial`)، destination (`original_method`/`wallet`)، reason_code (`fare_dispute`/`trip_not_taken`/`duplicate_charge`/`service_issue`/`cancellation_fee_waived`/`goodwill`/`other`)، reason VARCHAR(500), status (`pending_approval`/`approved`/`processing`/`succeeded`/`failed`/`rejected`)، requested_by (admin user id)، approved_by NULL, approved_at NULL, rejected_by NULL, rejected_reason NULL, gateway_refund_id NULL, failure_message NULL, processed_at NULL, dispute_id NULL (F18)، created_at, updated_at — INDEX(status, created_at), INDEX(trip_id) |
| `payouts` | id, payout_number (UNIQUE `PO-YYYYMMDD-#####`)، driver_id (FK)، wallet_id, amount, iban_masked VARCHAR(34) (`SA03 **** **** 1234`)، iban_encrypted VARCHAR(500) (Data Protection)، account_holder_name, status (`requested`/`approved`/`paid`/`rejected`/`cancelled`)، batch_id NULL, idempotency_key UNIQUE, requested_at, approved_by NULL, approved_at NULL, paid_by NULL, paid_at NULL, bank_reference NULL, rejected_by NULL, rejected_reason NULL, cancelled_at NULL, created_at, updated_at — INDEX(driver_id, requested_at), INDEX(status) |
| `payout_batches` | id, batch_number (UNIQUE `PB-YYYYMMDD-##`)، status (`open`/`exported`/`paid`)، payouts_count, total_amount, export_file_id NULL (stored_files)، exported_by NULL, exported_at NULL, bank_reference NULL, paid_by NULL, paid_at NULL, created_by, created_at, updated_at |
| `settlement_batches` | id, batch_number (UNIQUE `SB-YYYYMMDD`)، city_id NULL, period_start DATETIME(6), period_end DATETIME(6) (نصف مفتوح `[start, end)`)، status (`generating`/`ready`/`finalized`/`failed`)، drivers_count, total_trips, total_gross_fares, total_earnings, total_commission, total_cash_collected, total_incentives, total_compensation, total_adjustments, total_net, error NULL, generated_by, generated_at, finalized_by NULL, finalized_at NULL, created_at, updated_at |
| `settlements` | id, batch_id (FK)، driver_id, trips_count, gross_fares, earnings, commission, cash_collected, incentives, cancellation_compensation, adjustments, fees (رسوم إلغاء على السائق), topups (شحن محفظة السائق لسداد الدين), payouts_in_period, net_amount, opening_balance, closing_balance, direction (`payable_to_driver`/`due_from_driver`/`zero`)، payout_id NULL, status (`open`/`finalized`)، created_at — UNIQUE(batch_id, driver_id) |
| `ledger_journals` | id, type (انظر §F11.3)، reference_type VARCHAR(40), reference_id CHAR(36) NULL, idempotency_key VARCHAR(100) UNIQUE NULL, description VARCHAR(255), created_by NULL, created_at |

تعديلات على جداول قائمة:
- `ledger_entries`: `transaction_id` يصبح NULL-able ويُضاف `journal_id CHAR(36) NULL` (FK). كل قيد يملك **واحداً فقط** منهما. مجموع المدين = مجموع الدائن لكل `transaction_id` ولكل `journal_id`.
- `wallet_transactions.type` يضاف إليه: `cash_collection`, `cancellation_compensation`, `payout_reversal`.
- `passengers`: `default_payment_method_id CHAR(36) NULL` (عندما `default_payment_method = card`).
- `trips`: `payment_method_id CHAR(36) NULL`, `fare_breakdown JSON NULL` (التفصيل النهائي بالشكل في §F11.6)، `discount_total DECIMAL(12,2) NOT NULL DEFAULT 0`.
- `Wallet.Post(...)` يأخذ معاملاً جديداً `allowOverdraft` (افتراضي `false`). مسموح `true` فقط للأنواع: `cash_collection` (محفظة السائق)، `cancellation_fee`، `trip_payment` (تحصيل متأخر بعد فشل البطاقة — §F11.4)، `adjustment`. الرصيد السالب = **دين** على صاحب المحفظة.

## F11.3 الدفتر المالي — الحسابات والحركات والقيود (مرجع موحّد)

### الحسابات (`ledger_entries.account`)

| الحساب | النوع | المعنى |
|---|---|---|
| `passenger_wallet:{walletId}` / `driver_wallet:{walletId}` | التزام | أرصدة المحافظ (الدائن يزيد الرصيد) — قائم |
| `platform_cash` | أصل | حساب المنصة البنكي/النقدي (خروج السحوبات، دخول دفعات الشركات) — قائم |
| `gateway_clearing` | أصل | أموال بطاقات لدى البوابة بانتظار تسويتها للبنك — قائم |
| `trip_revenue` | إيراد | أجرة الرحلات غير النقدية؛ يخرج منها نصيب السائق، والباقي عمولة المنصة — قائم |
| `cash_collected` | إيراد/مقاصة | رحلات النقد: نصيب السائق مقابل النقد الذي حصّله؛ الرصيد الدائن الصافي = عمولة النقد — قائم |
| `payouts_pending` | التزام | سحوبات طلبها السائق ولم تُحوَّل بعد — **جديد** |
| `refunds` | مقابل إيراد | المبالغ المستردة للركاب — **جديد** |
| `cancellation_fees` | إيراد | رسوم الإلغاء (F14)؛ يخرج منها تعويض السائق — **جديد** |
| `discount_promotion` | مصروف | خصومات أكواد العروض (F15) — **جديد** |
| `discount_favorite_driver` | مصروف | خصم السائق المفضل (F16) — **جديد** |
| `incentives` | مصروف | مكافآت حوافز السائقين (F15) — **جديد** |
| `adjustments` | مصروف/إيراد | تعديلات يدوية من الإدارة — **جديد** |
| `corporate_receivable:{corporateAccountId}` | أصل | مستحقات على الشركات (F19) — **جديد** |

### أنواع حركات المحافظ (`wallet_transactions.type`)
`topup`, `trip_payment`, `trip_earning`, `refund`, `payout`, `payout_reversal`, `adjustment`, `incentive`, `cancellation_fee`, `cancellation_compensation`, `cash_collection`.

### أنواع القيود بدون محفظة (`ledger_journals.type`)
`trip_card_capture`, `trip_discount`, `trip_corporate_charge`, `cancellation_fee_card`, `refund_card`, `payout_paid`, `corporate_invoice_payment`, `manual`.

### جدول القيود لكل حركة مالية

| الحدث | القيد (مدين ← دائن) | الأداة |
|---|---|---|
| شحن المحفظة (بطاقة/Apple Pay/sandbox) | `gateway_clearing` ← `passenger_wallet` (أو `driver_wallet`) | حركة `topup` |
| رحلة محفظة | `passenger_wallet` ← `trip_revenue` (الأجرة بعد الخصم) | حركة `trip_payment` |
| رحلة بطاقة (عند الـcapture) | `gateway_clearing` ← `trip_revenue` | قيد `trip_card_capture` |
| رحلة شركة | `corporate_receivable:{id}` ← `trip_revenue` | قيد `trip_corporate_charge` |
| نصيب السائق (محفظة/بطاقة/شركة) | `trip_revenue` ← `driver_wallet` | حركة `trip_earning` |
| رحلة نقدية | `cash_collected` ← `driver_wallet` (نصيب السائق) **ثم** `driver_wallet` ← `cash_collected` (كامل الأجرة النقدية، overdraft مسموح) | حركتا `trip_earning` + `cash_collection` |
| خصم (عرض / سائق مفضل) — لكل طرق الدفع | `discount_promotion` أو `discount_favorite_driver` ← `trip_revenue` (أو ← `cash_collected` للنقد) | قيد `trip_discount` |
| رسوم إلغاء من المحفظة | `passenger_wallet` ← `cancellation_fees` (overdraft مسموح) | حركة `cancellation_fee` |
| رسوم إلغاء من البطاقة | `gateway_clearing` ← `cancellation_fees` | قيد `cancellation_fee_card` |
| تعويض السائق عن الإلغاء | `cancellation_fees` ← `driver_wallet` | حركة `cancellation_compensation` |
| استرداد إلى البطاقة | `refunds` ← `gateway_clearing` | قيد `refund_card` |
| استرداد إلى المحفظة | `refunds` ← `passenger_wallet` | حركة `refund` |
| طلب سحب | `driver_wallet` ← `payouts_pending` | حركة `payout` |
| رفض/إلغاء سحب | `payouts_pending` ← `driver_wallet` | حركة `payout_reversal` |
| تحويل السحب للبنك | `payouts_pending` ← `platform_cash` | قيد `payout_paid` |
| مكافأة حافز | `incentives` ← `driver_wallet` | حركة `incentive` |
| تعديل يدوي | `adjustments` ↔ المحفظة حسب الاتجاه | حركة `adjustment` |
| سداد فاتورة شركة | `platform_cash` ← `corporate_receivable:{id}` | قيد `corporate_invoice_payment` |

ملاحظات:
- **نقد السائق**: بعد رحلة نقدية أجرتها 50 ونصيب السائق 40: +40 ثم −50 ⇒ محفظة السائق −10 = عمولة مستحقة على السائق. الرصيد السالب هو "دين النقد" (`cashDebt = max(0, −balance)`) ويُقاصّ تلقائياً من أرباح الرحلات غير النقدية والحوافز، ويظهر في التسويات.
- **الخصومات تتحملها المنصة**: نصيب السائق يُحسب قبل الخصم (معادلة F10)، والخصم يُقيَّد مصروفاً حتى يبقى `trip_revenue` معبّراً عن الأجرة الإجمالية.
- **مفاتيح Idempotency للحركات الآلية**: `trip:{tripId}:payment`, `trip:{tripId}:earning`, `trip:{tripId}:cash`, `trip:{tripId}:discount:{source}`, `trip:{tripId}:capture`, `cancel:{tripId}:fee`, `cancel:{tripId}:compensation`, `refund:{refundId}`, `payout:{payoutId}`, `payout:{payoutId}:reversal`, `payout:{payoutId}:paid`, `incentive:{progressId}`.
- حد دين النقد: `Payouts:MaxCashDebt` (افتراضي 500). عند تجاوزه: `PUT /driver/status {isOnline:true}` → `403 cash_debt_limit_exceeded` مع `details { cashDebt, limit }`، والسائق مستبعد من المطابقة (شرط أهلية إضافي لـF9). السائق يسدد بشحن محفظة السائق بالبطاقة (`POST /wallet/topups?kind=driver`).

## F11.4 القواعد والخوارزميات

### حالات الدفعة
```
initiated ──(3DS)──► initiated(action_url) ──► authorized ──► captured ──► partially_refunded ──► refunded
    │                         │                    │                          
    └──────► failed ◄─────────┘                    └──► voided
```
الانتقالات أحادية الاتجاه وidempotent: وصول Webhook بحالة أقدم أو مكررة لا يغيّر شيئاً (`processing_status = ignored`).

### رحلات البطاقة (تُعدّل F8)
1. **الطلب** `POST /passenger/trips` مع `paymentMethod: "card"`:
   - `paymentMethodId` مطلوب أو يُستخدم الافتراضي (`passengers.default_payment_method_id`). يجب أن تكون البطاقة `active` وغير منتهية وملكاً للراكب، وإلا `422 payment_method_expired` / `404 not_found`.
   - مبلغ التفويض = `ceil(estimatedFare × (1 + Payments:AuthBufferPercent/100))` (افتراضي 30%)؛ في `pricingMode=offer` يُستخدم `offeredPrice`.
   - يُنفَّذ `AuthorizeAsync` **قبل** إدخال الرحلة:
     - `authorized` → تُنشأ الرحلة وتصبح `searching` كالمعتاد.
     - `initiated` مع `actionUrl` (3DS) → تُنشأ الرحلة بحالة `requested` (لا تدخل المطابقة) ويعيد الـAPI `201` مع `trip.payment.action`. عند اكتمال التحقق (Webhook أو صفحة العودة) → `searching`. إن لم يكتمل خلال `Payments:ActionTimeoutSeconds` (300) → الدفعة `failed` والرحلة `cancelled` بـ`cancelled_by=system` و`cancellation_reason=payment_failed`.
     - فشل فوري → `422 payment_failed` مع `details { failureCode, failureMessage }` ولا تُنشأ رحلة.
   - `Payments:SandboxSkipAuthorization=true` أو `Payments:CardAuthorizeOnRequest=false` → لا تفويض؛ التحصيل عند الإكمال بـ`PurchaseAsync`.
2. **الإلغاء**: رسوم الإلغاء (F14) تُحصَّل بـ`CaptureAsync(fee)` من التفويض ويُحرَّر الباقي؛ بلا رسوم → `VoidAsync`. `no_drivers` → `VoidAsync`.
3. **الإكمال** `POST /driver/trips/{id}/complete`:
   - تُحسب الأجرة النهائية `F`. استدعاء البوابة يتم قبل معاملة قاعدة البيانات بمهلة `Payments:CaptureTimeoutSeconds` (10):
     - `F ≤ authorized_amount` → `CaptureAsync(F)`.
     - `F > authorized_amount` → `VoidAsync` ثم `PurchaseAsync(F)` بنفس الرمز (عملية يبدؤها التاجر).
   - نجاح → قيد `trip_card_capture` + `trip_earning` + خصومات، وحدث رحلة `payment_recorded`.
   - **رفض نهائي** (`IsTransient=false`) → سلوك F8: `trips.payment_method = cash`، حدث `payment_fallback_cash` بـ`reason = card_capture_failed`، إشعار `payment.failed` للراكب، واستجابة السائق تحمل `collectCashAmount = F`. تُطبق قيود النقد.
   - **نتيجة غير معروفة** (مهلة/خطأ شبكة) → تكتمل الرحلة كبطاقة، والدفعة تبقى `authorized` مع `metadata.capturePending=true`. `PaymentCaptureRetryJob` يعيد المحاولة (§F11.8). إن فشلت نهائياً → حركة `trip_payment` على محفظة الراكب بـ`allowOverdraft=true` (دين على الراكب) + إشعار `payment.failed`.
   - نصيب السائق يُضاف عند الإكمال في كل الحالات (المنصة تتحمل مخاطرة التحصيل).
4. **الدين على الراكب**: إن كان رصيد محفظة الراكب سالباً و`Payments:BlockOnOutstandingBalance=true` (افتراضي) → `POST /passenger/trips` يعيد `422 outstanding_balance` مع `details { amount }` حتى يشحن المحفظة. الشحن يسدد الدين تلقائياً (الرصيد يعود موجباً).
5. **المحفظة**: بدون تغيير عن F8 (رصيد غير كافٍ → تحويل إلى نقد).
6. Apple Pay غير مدعوم لدفع الرحلات في v1 (للشحن فقط).

### الشحن بالبطاقة
- `POST /wallet/topups` مع `method: "card"` (+`paymentMethodId`) أو `"apple_pay"` (+`applePayToken`). تُنشأ `payments` بـ`purpose=topup`, `capture_mode=auto`، ثم `PurchaseAsync`.
- `captured` → حركة `topup` (مفتاحها `Idempotency-Key` الطلب) في نفس معاملة تحديث الدفعة. 3DS → `202` مع `action`، والمحفظة تُضاف عند وصول الحالة `captured` (Webhook/العودة). فشل → `422 payment_failed`.
- الحدود `Payments:MinTopup` (10) و`Payments:MaxTopup` (5000) قائمة، ويضاف `Payments:MaxTopupPerDay` (10000) لكل مستخدم → `422 validation_failed { amount: "daily_limit" }`.

### حفظ البطاقات
- العميل يحصل على `token` من SDK المزوّد (أو رمز sandbox) ثم `POST /passenger/payment-methods`.
- الخادم يستدعي `VerifyCardTokenAsync` (تفويض 1 ر.س ثم إلغاؤه، أو تحقق المزوّد) → `active`؛ إن تطلب 3DS → `pending_verification` مع `action`؛ بعد العودة يصبح `active` أو `failed`.
- البطاقة المنتهية (`expiry` < الشهر الحالي) تظهر `isExpired: true` ولا يمكن استخدامها. الحذف منطقي (`removed`) ويُستدعى `DeleteCardTokenAsync`؛ ممنوع إن كانت مرتبطة برحلة نشطة (`409 payment_method_in_use`).
- الحد: `Payments:MaxCardsPerUser` (5).

### الاستردادات
- تنشئها الإدارة (`payments.refund`) أو تسوية نزاع (F18). المبلغ ≤ `captured_amount − refunded_amount` للدفعة، أو ≤ أجرة الرحلة المدفوعة للمحفظة/النقد، وإلا `422 refund_exceeds_amount` مع `details { refundable }`.
- `destination`: `original_method` (بطاقة فقط) أو `wallet` (الافتراضي لرحلات النقد والمحفظة).
- الموافقة بمبدأ **الأربع عيون**: المبلغ ≥ `Payments:RefundAutoApproveLimit` (50) يتطلب موافقة مستخدم آخر يملك `payments.refund_approve` (`409 four_eyes_required` إن كان نفس المنشئ)؛ أقل من الحد → يُعتمد تلقائياً عند الإنشاء.
- التنفيذ: `approved → processing` (استدعاء `RefundAsync` خارج المعاملة) → `succeeded` مع قيد `refund_card` أو حركة `refund`، وتحديث `payments.refunded_amount` والحالة (`partially_refunded`/`refunded`)، وإشعار `payment.refunded`. فشل → `failed` مع `failure_message` ويمكن إعادة المحاولة (`POST /admin/refunds/{id}/retry`).

### السحوبات (Payouts)
- `availableForPayout = balance − Σ(payouts requested/approved غير المقيدة)` — عملياً الرصيد لأن الطلب يخصم فوراً. الشروط: `amount ≥ Payouts:MinAmount` (100) وإلا `422 payout_below_minimum { minAmount }`؛ `amount ≤ balance` وإلا `422 insufficient_balance`؛ IBAN سعودي صالح في `drivers.iban` وإلا `422 iban_missing`؛ لا يوجد سحب `requested` آخر وإلا `409 payout_pending_exists`؛ السائق غير موقوف.
- الطلب يخصم المحفظة فوراً (حركة `payout` ← `payouts_pending`) ويأخذ لقطة IBAN.
- الإدارة: `approve` (`requested→approved`)، `reject {reason}` (→ `rejected` + `payout_reversal`)، `mark-paid {bankReference}` (`approved→paid` + قيد `payout_paid`). السائق يستطيع `cancel` ما دام `requested` (→ `cancelled` + `payout_reversal`).
- دفعات التحويل: الإدارة تجمع السحوبات `approved` في `payout_batches`، تصدّر ملف CSV للبنك (أعمدة: `payout_number, beneficiary_name, iban, amount, currency, reference`)، ثم `mark-paid` للدفعة كاملة يحوّل كل سحوباتها إلى `paid`.
- إشعار `payout.status` عند كل تغيير (`data.status`).
- `Payouts:AutoApprove=false` (افتراضي). عند `true` تُعتمد الطلبات ≤ `Payouts:AutoApproveLimit` تلقائياً.

### التسويات (Settlements)
- دفعة تسوية لكل فترة (افتراضياً أسبوعية: الأحد 00:00 إلى الأحد التالي بتوقيت الرياض، `Settlements:PeriodDays` = 7)، تُولَّد يدوياً أو بالمهمة الأسبوعية. لا تتداخل الفترات لنفس المدينة (`409 settlement_period_overlap`).
- لكل سائق له حركات في الفترة (من `wallet_transactions` لمحفظة السائق ومن `trips`):

```
trips_count        = عدد رحلاته المكتملة (completed_at في الفترة)
gross_fares        = Σ (trips.final_fare + trips.discount_total)
earnings           = Σ trip_earning
commission         = gross_fares − earnings
cash_collected     = Σ cash_collection
incentives         = Σ incentive
cancellation_compensation = Σ cancellation_compensation
adjustments        = Σ adjustment (موقّع: credit موجب، debit سالب)
fees               = Σ cancellation_fee (على محفظة السائق — F14)
topups             = Σ topup (محفظة السائق)
payouts_in_period  = Σ payout − Σ payout_reversal
net_amount         = earnings + incentives + cancellation_compensation + adjustments − cash_collected
opening_balance    = balance_after لآخر حركة قبل period_start (أو 0)
closing_balance    = balance_after لآخر حركة قبل period_end
direction          = closing_balance > 0 ? payable_to_driver : closing_balance < 0 ? due_from_driver : zero
```
- ثابت يُختبر: `closing_balance = opening_balance + net_amount + topups − fees − payouts_in_period`.
- `finalize` يقفل الدفعة (لا إعادة توليد). إن `Settlements:AutoCreatePayouts=true` يُنشأ سحب `approved` لكل سائق `payable_to_driver` بمبلغ `closing_balance` إن ≥ `Payouts:MinAmount` وله IBAN.
- التسوية بيان محاسبي ولا تنشئ حركات مالية بنفسها (المقاصة تحدث لحظياً في المحفظة). إشعار `settlement.ready` للسائقين عند الـfinalize.

## F11.5 الـAPI — المدفوعات

### عام (مصادق)
- GET `/payments/config` →
```json
{ "provider": "sandbox", "publishableKey": "pk_test_…", "currency": "SAR",
  "supportedBrands": ["mada", "visa", "mastercard"],
  "applePay": { "enabled": false, "merchantId": null },
  "sandboxTokens": ["tok_sandbox_mada", "tok_sandbox_visa", "tok_sandbox_3ds", "tok_sandbox_declined"] }
```
(`sandboxTokens` فقط عند `provider=sandbox`.)
- GET `/payments/{id}` (مالك الدفعة) → `Payment`:
```json
{ "id": "uuid", "purpose": "trip|topup|cancellation_fee", "status": "initiated", "method": "card", "amount": 45.50,
  "authorizedAmount": null, "capturedAmount": null, "refundedAmount": 0, "currency": "SAR",
  "card": { "brand": "mada", "last4": "4201" } | null,
  "action": { "type": "redirect", "url": "https://…", "expiresAt": "…" } | null,
  "failureCode": null, "failureMessage": null, "tripId": null, "createdAt": "…", "capturedAt": null }
```
- `returnUrl` الافتراضي للتطبيق: `ata://payments/return`. العميل **لا يثق** بحالة العودة ويستطلع `GET /payments/{id}` (كل 2 ث حتى 60 ث) أو ينتظر `PaymentUpdated` عبر SignalR.

### Webhooks وصفحات العودة (بدون مصادقة)
- POST `/payments/webhooks/{provider}` → `200 {}` دائماً بعد الحفظ (حتى المكرر)؛ `401 webhook_signature_invalid` عند فشل التوقيع؛ `404 not_found` لمزوّد غير معروف. المعالجة: إدراج `payment_webhook_events` (تكرار `(provider, event_id)` → `200` بلا معالجة)، ثم مطابقة `gateway_payment_id` وتطبيق الانتقال وتنفيذ الأثر المالي (شحن، بدء المطابقة، …) في معاملة واحدة.
- GET `/payments/return/{provider}?id=…` → يجلب حالة الدفعة من البوابة (`FetchAsync`)، يطبقها، ثم `302` إلى `{payments.return_url}?paymentId={id}&status={status}`.
- GET/POST `/payments/sandbox/challenge/{paymentId}` (sandbox فقط، انظر §F11.1).

### الراكب `/passenger`
- GET `/passenger/payment-methods` → `[PaymentMethod]`
```json
{ "id": "uuid", "type": "card", "brand": "mada", "last4": "4201", "expiryMonth": 8, "expiryYear": 2029, "holderName": "SARA A",
  "status": "active", "isDefault": true, "isExpired": false, "createdAt": "…" }
```
- POST `/passenger/payment-methods` `{ "token": "tok_sandbox_mada", "setDefault": true, "returnUrl": "ata://payments/return" }` → `201 PaymentMethod` أو `202 { "paymentMethod": {…status:"pending_verification"}, "action": { "type": "redirect", "url": "…" } }`. الأخطاء: `422 payment_failed` (رفض التحقق)، `409 conflict` (نفس البطاقة محفوظة — `fingerprint`)، `422 validation_failed { token: "limit" }` عند تجاوز `MaxCardsPerUser`.
- POST `/passenger/payment-methods/{id}/default` → `200 PaymentMethod` (يضبط أيضاً `passengers.default_payment_method = card`).
- DELETE `/passenger/payment-methods/{id}` → `204`؛ `409 payment_method_in_use`.
- GET `/passenger/trips/{id}/receipt` → `Receipt` (§F11.6)؛ `409 conflict` إن لم تكن الرحلة `completed` أو `cancelled` برسوم.
- `POST /passenger/trips` يقبل حقلاً جديداً `paymentMethodId` (UUID). و`Trip` يضاف إليه:
```json
"payment": { "id": "uuid", "status": "authorized", "method": "card", "brand": "mada", "last4": "4201",
             "authorizedAmount": 59.00, "capturedAmount": null,
             "action": { "type": "redirect", "url": "…", "expiresAt": "…" } | null } | null,
"collectCashAmount": null,
"discountTotal": 0
```
(`collectCashAmount` يظهر للسائق فقط: أجرة النقد المطلوب تحصيلها بعد الإكمال، بما فيها حالة `card_capture_failed`.) و`Offer` يضاف إليه `paymentMethod` (`cash|wallet|card|corporate`).

### المحفظة `/wallet` (توسعة F5)
- GET `/wallet` — `paymentMethods` تشمل البطاقات: `{ "type": "card", "id": "uuid", "label": "مدى •••• 4201", "brand": "mada", "last4": "4201", "isDefault": false }`. ولمحفظة السائق يضاف `cashDebt` (رقم ≥ 0).
- POST `/wallet/topups` (Idempotency-Key مطلوب)
```json
{ "amount": 100, "method": "sandbox" | "card" | "apple_pay", "paymentMethodId": "uuid?", "applePayToken": "string?", "returnUrl": "ata://payments/return" }
```
→ `201 { "transactionId": "uuid", "balance": 225.00, "paymentId": "uuid|null", "status": "captured" }` أو `202 { "paymentId": "uuid", "status": "initiated", "action": { "type": "redirect", "url": "…" } }`. الأخطاء: `422 payment_failed`, `422 payment_method_expired`, `422 validation_failed`. تكرار نفس المفتاح يعيد نفس النتيجة.

### السائق `/driver`
- GET `/driver/earnings/statement?from=2026-09-01&to=2026-09-28` (تواريخ محلية، حد أقصى 92 يوماً) →
```json
{ "from": "2026-09-01", "to": "2026-09-28",
  "totals": { "trips": 84, "grossFares": 3120.00, "commission": 624.00, "earnings": 2496.00, "cashCollected": 1400.00,
              "incentives": 150.00, "cancellationCompensation": 12.00, "adjustments": 0, "payouts": 800.00, "net": 1258.00 },
  "days": [ { "date": "2026-09-28", "trips": 6, "earnings": 180.00, "cashCollected": 90.00, "incentives": 0, "onlineHours": 7.5 } ] }
```
- GET `/driver/trips/{id}/earnings` → `{ "tripId", "fare": 50.00, "discountTotal": 5.00, "grossFare": 55.00, "commission": 11.00, "commissionPercent": 20.0, "tierCommissionDiscountPercent": 0, "driverEarnings": 44.00, "paymentMethod": "cash", "cashCollected": 50.00 }`
- GET `/driver/payouts/summary` → `{ "balance": 640.00, "cashDebt": 0, "availableForPayout": 640.00, "minPayoutAmount": 100, "pendingPayout": null | Payout, "ibanMasked": "SA03 **** **** 1234" | null, "canRequest": true, "reason": null | "iban_missing" | "payout_pending_exists" | "cash_debt_outstanding" | "below_minimum" }`
- POST `/driver/payouts` (Idempotency-Key) `{ "amount": 500 }` → `201 Payout`
```json
{ "id": "uuid", "payoutNumber": "PO-20260928-00012", "amount": 500.00, "ibanMasked": "SA03 **** **** 1234", "status": "requested",
  "requestedAt": "…", "approvedAt": null, "paidAt": null, "rejectedReason": null, "bankReference": null }
```
  الأخطاء: `payout_below_minimum`, `insufficient_balance`, `payout_pending_exists`, `iban_missing`, `account_suspended`.
- GET `/driver/payouts?page=` → صفحة `Payout`. POST `/driver/payouts/{id}/cancel` → `200 Payout` (`409 conflict` إن لم يكن `requested`).
- GET `/driver/settlements?page=` → صفحة `{ id, batchNumber, periodStart, periodEnd, tripsCount, earnings, cashCollected, netAmount, closingBalance, direction, status }`؛ GET `/driver/settlements/{id}` → نفس الحقول كاملة (كل أعمدة `settlements`).

### الإدارة `/admin` (الصلاحيات من F20)

| المسار | الصلاحية |
|---|---|
| GET `/admin/payments?status=&purpose=&provider=&method=&from=&to=&search=&page=` → `{ id, purpose, status, method, provider, amount, capturedAmount, refundedAmount, userName, userPhone, tripNumber, gatewayPaymentId, createdAt }` | `payments.view` |
| GET `/admin/payments/{id}` → الدفعة كاملة + `webhookEvents[]` + `refunds[]` + `ledger[]` (القيود المرتبطة) | `payments.view` |
| POST `/admin/payments/{id}/refunds` `{ amount, reasonCode, reason, destination }` → `201 Refund` | `payments.refund` |
| POST `/admin/trips/{id}/refunds` `{ amount, reasonCode, reason }` (رحلات النقد/المحفظة → `destination = wallet`) → `201 Refund` | `payments.refund` |
| GET `/admin/refunds?status=&from=&to=&page=` | `payments.view` |
| POST `/admin/refunds/{id}/approve` · `/reject {reason}` · `/retry` | `payments.refund_approve` |
| GET `/admin/payouts?status=&driverId=&from=&to=&page=` → Payout + `driverName`, `driverPhone`, `batchNumber` | `payments.view` |
| POST `/admin/payouts/{id}/approve` · `/reject {reason}` · `/mark-paid {bankReference, paidAt?}` | `payouts.approve` |
| POST `/admin/payout-batches` `{ payoutIds?: [], allApproved?: true }` → `201 Batch`؛ GET `/admin/payout-batches?page=`؛ GET `/admin/payout-batches/{id}`؛ GET `/admin/payout-batches/{id}/export?format=csv`؛ POST `/admin/payout-batches/{id}/mark-paid {bankReference}` | `payouts.approve` |
| POST `/admin/settlement-batches` `{ periodStart, periodEnd, cityId? }` → `202 SettlementBatch (generating)` | `settlements.manage` |
| GET `/admin/settlement-batches?page=`؛ GET `/admin/settlement-batches/{id}`؛ GET `/admin/settlement-batches/{id}/settlements?direction=&search=&page=`؛ GET `/admin/settlements/{id}` | `settlements.manage` |
| GET `/admin/settlement-batches/{id}/export?format=csv` (UTF-8 BOM؛ أعمدة: `driver_name, phone, trips, gross_fares, earnings, commission, cash_collected, incentives, compensation, adjustments, fees, topups, payouts, net, opening_balance, closing_balance, direction`) | `settlements.manage` |
| POST `/admin/settlement-batches/{id}/finalize` · `/regenerate` (قبل الـfinalize فقط) | `settlements.manage` |
| GET `/admin/wallets?kind=&search=&negativeOnly=&page=` → `{ id, userId, userName, phone, kind, balance, status }`؛ GET `/admin/wallets/{id}` (+ آخر الحركات) | `payments.view` |
| POST `/admin/wallets/{id}/adjustments` `{ direction: "credit|debit", amount, reason }` → `201 WalletTransaction` | `wallets.adjust` |
| POST `/admin/wallets/{id}/freeze` · `/unfreeze {reason}` | `wallets.adjust` |
| GET `/admin/ledger/balances?from=&to=` → `[ { account, debit, credit, balance } ]` (الحسابات العامة؛ حسابات المحافظ مجمّعة كـ`passenger_wallets`/`driver_wallets`) | `payments.view` |

كل إجراء كتابة → `audit_logs` (`refund.create`, `refund.approve`, `refund.reject`, `payout.approve`, `payout.reject`, `payout.mark_paid`, `payout_batch.create`, `payout_batch.export`, `payout_batch.mark_paid`, `settlement_batch.generate`, `settlement_batch.finalize`, `wallet.adjust`, `wallet.freeze`, `wallet.unfreeze`).

### أكواد الأخطاء الجديدة (ErrorCatalog)

| الكود | HTTP | ar |
|---|---|---|
| `payment_failed` | 422 | تعذّر إتمام الدفع |
| `payment_method_expired` | 422 | البطاقة منتهية الصلاحية |
| `payment_method_in_use` | 409 | البطاقة مرتبطة برحلة جارية |
| `payment_provider_unavailable` | 503 | خدمة الدفع غير متاحة حالياً |
| `webhook_signature_invalid` | 401 | توقيع غير صالح |
| `refund_exceeds_amount` | 422 | مبلغ الاسترداد يتجاوز المبلغ المدفوع |
| `four_eyes_required` | 409 | يجب أن يعتمد العملية مستخدم آخر |
| `payout_below_minimum` | 422 | المبلغ أقل من الحد الأدنى للسحب |
| `payout_pending_exists` | 409 | لديك طلب سحب قيد المعالجة |
| `iban_missing` | 422 | أضف رقم الآيبان أولاً |
| `cash_debt_limit_exceeded` | 403 | تجاوزت مستحقات النقد الحد المسموح، سدّدها للاتصال |
| `outstanding_balance` | 422 | يوجد مبلغ مستحق على حسابك، اشحن المحفظة للمتابعة |
| `settlement_period_overlap` | 409 | فترة التسوية متداخلة مع دفعة سابقة |

## F11.6 كائن الإيصال `Receipt` وتفصيل الأجرة

`trips.fare_breakdown` يُحفظ عند الإكمال (والإلغاء برسوم) بشكل `FareBreakdownDto` من F10 مع حقل إضافي `discounts`:
```json
{ "baseFare": 8.00, "distanceFare": 21.60, "timeFare": 9.00, "waitingFare": 1.00, "minFareApplied": false,
  "timeMultiplier": 1.00, "timeMultiplierLabel": null, "demandMultiplier": 1.20, "bookingFee": 2.00, "serviceFee": 1.50,
  "discount": 5.00,
  "discounts": [ { "source": "promotion", "reference": "ATA10", "label": "خصم ATA10", "amount": 5.00 } ] }
```
`discounts[].source` ∈ `promotion` | `favorite_driver` (F15/F16)؛ `discount` = مجموعها = `trips.discount_total`.

GET `/passenger/trips/{id}/receipt` (وللإدارة GET `/admin/trips/{id}/receipt`):
```json
{ "tripId": "uuid", "tripNumber": "T-20260928-00042", "status": "completed", "issuedAt": "…", "currency": "SAR",
  "passengerName": "سارة", "driverName": "محمد", "vehicle": "تويوتا كامري · أ ب ج 2841", "rideCategory": "اقتصادي",
  "pickup": { "name", "address", "lat", "lng" }, "dropoff": { … }, "stops": [ … ],
  "startedAt": "…", "completedAt": "…", "distanceMeters": 14200, "durationSeconds": 1380, "waitingSeconds": 120,
  "lines": [
    { "code": "base_fare", "label": "الأجرة الأساسية", "amount": 8.00 },
    { "code": "distance_fare", "label": "المسافة (14.2 كم)", "amount": 21.60 },
    { "code": "time_fare", "label": "الوقت (23 د)", "amount": 9.00 },
    { "code": "waiting_fare", "label": "الانتظار", "amount": 1.00 },
    { "code": "min_fare_adjustment", "label": "فرق الحد الأدنى", "amount": 0 },
    { "code": "time_multiplier", "label": "تعرفة الوقت", "amount": 0 },
    { "code": "demand_multiplier", "label": "الطلب المرتفع ×1.2", "amount": 7.92 },
    { "code": "booking_fee", "label": "رسوم الحجز", "amount": 2.00 },
    { "code": "service_fee", "label": "رسوم الخدمة", "amount": 1.50 },
    { "code": "discount", "label": "خصم ATA10", "amount": -5.00, "source": "promotion", "reference": "ATA10" },
    { "code": "cancellation_fee", "label": "رسوم الإلغاء", "amount": 0 },
    { "code": "rounding", "label": "التقريب", "amount": -0.02 } ],
  "discounts": [ { "source": "promotion", "reference": "ATA10", "label": "خصم ATA10", "amount": 5.00 } ],
  "subtotal": 51.02, "discountTotal": 5.00, "total": 46.00, "vatRate": 15, "vatIncluded": 6.00,
  "payment": { "method": "card", "brand": "mada", "last4": "4201", "status": "captured", "paidAmount": 46.00, "fallbackToCash": false },
  "refunds": [ { "id": "uuid", "amount": 10.00, "status": "succeeded", "destination": "wallet", "createdAt": "…" } ],
  "netPaid": 36.00 }
```
قواعد: الأسطر ذات القيمة صفر تُحذف عدا `base_fare`. `vatIncluded = round(total × 15/115, 2)` (الأسعار شاملة الضريبة). رحلة ملغاة برسوم: سطر `cancellation_fee` فقط و`total` = الرسوم.

## F11.7 SignalR (`/hubs/trips`)
- `PaymentUpdated({ paymentId, purpose, status, tripId, failureCode })` → للمستخدم مالك الدفعة عند كل تغيير حالة.
- `TripUpdated` يحمل كائن `payment` الجديد.
- للإدارة (مجموعة `admins`): `PayoutRequested({ payoutId, driverName, amount })`.

## F11.8 المهام الخلفية والإعدادات

كل مهمة `BackgroundService` تحصل على قفل `lock:job:{name}` عبر `IDistributedLock` (تنفيذ في الذاكرة الآن، Redis في F21) وتُعطَّل في الاختبارات بمفتاح `Enabled`.

| المهمة | الجدولة | العمل |
|---|---|---|
| `PaymentActionExpiryJob` | كل 30 ث | دفعات `initiated` تجاوزت `action_expires_at` → `failed` (+ إلغاء رحلتها `payment_failed` أو ترك الشحن فاشلاً) |
| `PaymentCaptureRetryJob` | كل دقيقة | دفعات `authorized` مع `metadata.capturePending` → إعادة `Capture`/`Purchase` حتى `Payments:CaptureMaxAttempts` (5) ثم الدين على المحفظة |
| `AuthorizationReconcileJob` | كل 10 د | دفعات رحلات منتهية (`cancelled`/`no_drivers`) ما زالت `authorized` → `Void` |
| `PaymentWebhookRetryJob` | كل دقيقة | أحداث `failed`/`pending` أقدم من دقيقة → إعادة المعالجة (حتى 10 مرات) |
| `RefundProcessorJob` | كل 30 ث | استردادات `approved` → التنفيذ |
| `SettlementWeeklyJob` | الأحد 01:00 (الرياض) | توليد دفعة الأسبوع المنتهي لكل مدينة إن `Settlements:AutoGenerate=true` |

| المفتاح | الافتراضي |
|---|---|
| `Payments:Provider` | `sandbox` |
| `Payments:SandboxEnabled` | `true` (تطوير) — قائم |
| `Payments:SandboxSkipAuthorization` | `false` |
| `Payments:CardAuthorizeOnRequest` | `true` |
| `Payments:AuthBufferPercent` | 30 |
| `Payments:ActionTimeoutSeconds` | 300 |
| `Payments:GatewayTimeoutSeconds` / `Payments:CaptureTimeoutSeconds` | 15 / 10 |
| `Payments:CaptureMaxAttempts` | 5 |
| `Payments:MinTopup` / `Payments:MaxTopup` / `Payments:MaxTopupPerDay` | 10 / 5000 / 10000 |
| `Payments:MaxCardsPerUser` | 5 |
| `Payments:RefundAutoApproveLimit` | 50 |
| `Payments:BlockOnOutstandingBalance` | `true` |
| `Payments:PublicBaseUrl` | `https://api.ata.sa` (لـ`callback_url`) |
| `Payments:Moyasar:*` | انظر §F11.1 |
| `Payouts:MinAmount` / `Payouts:MaxCashDebt` | 100 / 500 |
| `Payouts:AutoApprove` / `Payouts:AutoApproveLimit` | `false` / 1000 |
| `Settlements:PeriodDays` / `Settlements:AutoGenerate` / `Settlements:AutoCreatePayouts` | 7 / `true` / `false` |

## F11.9 Flutter

**feature `payments`** (جديد):
- `domain/entities`: `PaymentMethod`, `PaymentConfig`, `Payment`, `PaymentAction`, `Receipt`, `ReceiptLine`.
- `domain/usecases`: `GetPaymentConfig`, `GetPaymentMethods`, `AddPaymentMethod`, `SetDefaultPaymentMethod`, `RemovePaymentMethod`, `GetPayment`, `WatchPayment` (استطلاع + `PaymentUpdated`)، `GetTripReceipt`.
- `data/`: `PaymentsRemoteDataSource`، `CardTokenizer` (واجهة: `SandboxCardTokenizer` يعرض قائمة رموز sandbox، و`MoyasarCardTokenizer` placeholder يغلّف SDK المزوّد).
- `presentation/cubit`: `PaymentMethodsCubit` (القائمة + الافتراضي + الحذف)، `AddCardCubit` (الترميز → الإرسال → إجراء 3DS → الحالة النهائية)، `PaymentActionCubit` (يفتح `action.url` في WebView داخلي `PaymentWebViewPage`، يلتقط التوجيه إلى `ata://payments/return`، ثم يستطلع `GET /payments/{id}`)، `ReceiptCubit`.
- `presentation/pages`: `PaymentMethodsPage` (`/wallet/payment-methods`)، `AddCardPage` (`/wallet/payment-methods/add`)، `PaymentWebViewPage` (`/payments/action`)، `ReceiptPage` (`/rides/:tripId/receipt`).
- التكامل: `HomeCubit` يعرض البطاقات المحفوظة في اختيار طريقة الدفع ويرسل `paymentMethodId`؛ `ActiveTripCubit` عند `trip.payment.action != null` يوجّه إلى `/payments/action`؛ شاشة نهاية الرحلة تعرض زر "الإيصال".
- **feature `wallet`** (توسعة): `TopUpCubit` يدعم `method: card|apple_pay|sandbox` واختيار البطاقة وحالة `202` (إجراء 3DS)، ويعرض الدين إن كان الرصيد سالباً.

**feature `driver_wallet`** (جديد، للسائق):
- entities: `EarningsStatement`, `TripEarnings`, `PayoutSummary`, `Payout`, `Settlement`.
- usecases: `GetEarningsStatement`, `GetPayoutSummary`, `RequestPayout`, `CancelPayout`, `GetPayouts`, `GetSettlements`, `GetSettlement`, `GetTripEarnings`.
- cubits: `EarningsStatementCubit` (نطاق التاريخ: اليوم/الأسبوع/الشهر/مخصص)، `PayoutRequestCubit` (المبلغ، التحقق من الحد الأدنى، الإرسال بـ`Idempotency-Key`)، `PayoutsCubit`، `SettlementsCubit`.
- pages: `DriverEarningsPage` (`/driver/earnings`)، `DriverPayoutsPage` (`/driver/payouts`)، `PayoutRequestPage` (`/driver/payouts/request`)، `DriverSettlementsPage` (`/driver/settlements`)، `SettlementDetailPage` (`/driver/settlements/:id`). زر "تحويل الأرباح" في نظرة السائق العامة يفتح `/driver/payouts/request`. بطاقة تحذير عند `cashDebt > 0` مع زر "سداد" (شحن محفظة السائق).
- `OnlineStatusCubit` يعالج `cash_debt_limit_exceeded` برسالة وزر سداد.

## F11.10 لوحة الإدارة

| المسار | الصفحة |
|---|---|
| `/payments` | جدول الدفعات بفلاتر (الحالة، الغرض، الطريقة، التاريخ، بحث بالجوال/رقم الرحلة) |
| `/payments/:id` | التفاصيل: الحالة والمبالغ، الأحداث من البوابة، القيود، زر "استرداد" (نموذج: المبلغ، السبب، الوجهة) |
| `/refunds` | قائمة الاستردادات مع تبويب "بانتظار الموافقة" وأزرار اعتماد/رفض (الأربع عيون) |
| `/payouts` | طلبات السحب: تبويبات (مطلوبة/معتمدة/مدفوعة/مرفوضة)، اعتماد/رفض، تحديد متعدد → "إنشاء دفعة تحويل" |
| `/payout-batches` و`/payout-batches/:id` | الدفعات، تصدير CSV، تأكيد الدفع برقم مرجع البنك |
| `/settlements` و`/settlements/:batchId` | الدفعات والتوليد اليدوي (فترة + مدينة)، جدول السائقين مع فلتر الاتجاه، تصدير CSV، Finalize |
| `/wallets` و`/wallets/:id` | المحافظ (فلتر الأرصدة السالبة = ديون النقد)، الحركات، تعديل يدوي، تجميد |
| `/ledger` | أرصدة حسابات الدفتر لفترة |

وفي تفاصيل الرحلة (`/trips/:id`) قسم "الدفع" (الدفعة، الإيصال، زر استرداد).

---

# F13 — الإشعارات (OneSignal + SMS + القوالب + الحملات)

## F13.1 التجريدات

```csharp
public interface IPushSender   // ATA.Infrastructure.Push
{
    Task<PushSendResult> SendAsync(PushMessage message, CancellationToken ct);
}

public sealed record PushMessage(
    IReadOnlyList<Guid> ExternalUserIds,                 // ≤ Notifications:PushBatchSize (2000)
    IReadOnlyDictionary<string, string> Headings,        // { "ar": "…", "en": "…" } — "en" إلزامي لدى OneSignal
    IReadOnlyDictionary<string, string> Contents,
    IReadOnlyDictionary<string, object?> Data,           // eventCode, notificationId, deepLink, entityId …
    string Category,                                     // trips|offers|safety|wallet|promotions|system → android_channel_id
    string Priority,                                     // "high" | "normal"
    int? TtlSeconds, string? CollapseId, IReadOnlyList<PushButton>? Buttons, string IdempotencyKey);

public sealed record PushButton(string Id, string TextAr, string TextEn);

public sealed record PushSendResult(bool Success, string? ProviderMessageId, int Recipients,
    IReadOnlyList<Guid> InvalidExternalUserIds, string? ErrorCode, string? ErrorMessage, bool IsTransient);
```

- `OneSignalPushSender`: `POST https://api.onesignal.com/notifications` بترويسة `Authorization: Key {OneSignal:RestApiKey}` وجسم:
```json
{ "app_id": "{OneSignal:AppId}", "target_channel": "push",
  "include_aliases": { "external_id": ["<userId>"] },
  "headings": { "en": "…", "ar": "…" }, "contents": { "en": "…", "ar": "…" },
  "data": { "eventCode": "trip.driver_arrived", "notificationId": "uuid", "deepLink": "ata://trip/uuid", "tripId": "uuid" },
  "android_channel_id": "{OneSignal:AndroidChannels:trips}", "priority": 10, "ttl": 600,
  "collapse_id": "trip-uuid", "buttons": [ { "id": "ok", "text": "أنا بخير" } ],
  "idempotency_key": "<deliveryId>" }
```
  الأخطاء: 5xx/429/مهلة → `IsTransient=true`؛ `errors.invalid_aliases` أو "not subscribed" → `InvalidExternalUserIds` (تصبح الإرسالية `skipped` بسبب `no_subscription`).
- `LoggingPushSender` في التطوير والاختبارات (`OneSignal:Enabled=false`)؛ يسجل الرسالة ويعيد نجاحاً بمعرّف `log-{guid}`.
- `ISmsSender` (قائم) يتغير توقيعه إلى `Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken ct)` حيث `SmsSendResult(bool Success, string? ProviderMessageId, string? ErrorCode, string? ErrorMessage, bool IsTransient)`. التنفيذات: `LoggingSmsSender` (قائم)، `UnifonicSmsSender` (نمط Unifonic: `POST {BaseUrl}/rest/SMS/messages` بـ`AppSid`, `SenderID`, `Recipient`, `Body`)، `TaqnyatSmsSender` (نمط Taqnyat: `POST {BaseUrl}/v1/messages` بـ`Bearer` token و`recipients[]`, `body`, `sender`). الاختيار `Sms:Provider` = `logging|unifonic|taqnyat`. `Sms:Sandbox=true` يجبر `LoggingSmsSender` مهما كان المزوّد. المحوّلان placeholders باختبارات عقد.
- `INotificationDispatcher` نقطة الدخول الوحيدة لكل الوحدات (يحل محل `NotificationService.Add` الذي يبقى داخلياً):
```csharp
Task DispatchAsync(NotificationRequest request, CancellationToken ct);
public sealed record NotificationRequest(string EventCode, Guid RecipientUserId, IReadOnlyDictionary<string, string?> Placeholders,
    string? EntityType = null, Guid? EntityId = null, IReadOnlyDictionary<string, object?>? ExtraData = null,
    string? RecipientPhoneOverride = null /* SMS لجهة موثوقة غير مسجلة */, Guid? CampaignId = null);
```

## F13.2 كتالوج الأحداث (مرجع موحّد لكل الميزات)

الأعمدة: الكود (= `notifications.type` = `notification_templates.code`)، المستلم (P راكب، D سائق، O عمليات، C جهة موثوقة، A مسؤول شركة، G ضيف رحلة شركة — SMS برقم غير مسجل)، الفئة (تحدد التفضيل وقناة أندرويد)، حرج (يتجاوز التفضيلات)، القنوات الافتراضية المفعّلة، العناصر النائبة، الرابط العميق.

| الكود | إلى | الفئة | حرج | القنوات | العناصر النائبة | الرابط |
|---|---|---|---|---|---|---|
| `trip.driver_assigned` | P | trips | ✔ | inapp, push | `{driverName}` `{vehicle}` `{plateNumber}` `{etaMinutes}` | `ata://trip/{tripId}` |
| `trip.driver_arrived` | P | trips | ✔ | inapp, push | `{driverName}` `{plateNumber}` `{freeWaitingMinutes}` | `ata://trip/{tripId}` |
| `trip.started` | P | trips | | push | `{dropoffName}` | `ata://trip/{tripId}` |
| `trip.completed` | P | trips | | inapp, push | `{fare}` `{tripNumber}` | `ata://rides/{tripId}/receipt` |
| `trip.cancelled` | P/D | trips | ✔ | inapp, push | `{tripNumber}` `{cancelledBy}` `{fee}` | `ata://rides/{tripId}` |
| `trip.no_drivers` | P | trips | ✔ | inapp, push | `{categoryName}` | `ata://home` |
| `trip.favorite_fallback` | P | trips | | push | `{driverName}` | `ata://trip/{tripId}` |
| `trip.message` | P/D | trips | | push | `{senderName}` `{preview}` | `ata://trip/{tripId}/chat` |
| `trip.payment_action_required` | P | trips | ✔ | push | `{amount}` | `ata://trip/{tripId}` |
| `offer.received` | D | offers* | ✔ | push (TTL = مهلة العرض، collapse = offerId) | `{pickupName}` `{driverNet}` `{etaMinutes}` | `ata://driver/offer` |
| `rating.reminder` | P/D | trips | | inapp, push | `{driverName}`/`{passengerName}` | `ata://rate/{tripId}` |
| `scheduled.booked` | P | trips | | inapp, push | `{scheduledAt}` `{pickupName}` | `ata://scheduled/{tripId}` |
| `scheduled.reminder` | P/D | trips | | inapp, push | `{scheduledAt}` `{minutesBefore}` `{pickupName}` | `ata://scheduled/{tripId}` |
| `scheduled.driver_reserved` | P | trips | | inapp, push | `{driverName}` `{scheduledAt}` | `ata://scheduled/{tripId}` |
| `scheduled.confirm_request` | D | trips | ✔ | inapp, push | `{scheduledAt}` `{pickupName}` `{deadlineMinutes}` | `ata://driver/scheduled/{tripId}` |
| `scheduled.reservation_released` | P/D | trips | | inapp, push | `{scheduledAt}` `{reason}` | `ata://scheduled/{tripId}` / `ata://driver/scheduled` |
| `scheduled.favorite_request` | D | trips | | inapp, push | `{passengerName}` `{scheduledAt}` | `ata://driver/scheduled/{tripId}` |
| `scheduled.rematched` | P | trips | | inapp, push | `{scheduledAt}` | `ata://scheduled/{tripId}` |
| `payment.succeeded` | P | wallet | | inapp | `{amount}` `{purpose}` | `ata://wallet` |
| `payment.failed` | P | wallet | ✔ | inapp, push | `{amount}` `{reason}` | `ata://wallet` |
| `payment.refunded` | P | wallet | | inapp, push | `{amount}` `{tripNumber}` | `ata://wallet/transactions` |
| `wallet.topup` | P/D | wallet | | inapp, push | `{amount}` `{balance}` | `ata://wallet` |
| `cancellation.fee_charged` | P | wallet | | inapp, push | `{fee}` `{tripNumber}` | `ata://rides/{tripId}` |
| `cancellation.compensation` | D | wallet | | inapp | `{amount}` `{tripNumber}` | `ata://driver/earnings` |
| `payout.status` | D | wallet | | inapp, push | `{amount}` `{status}` `{payoutNumber}` | `ata://driver/payouts` |
| `settlement.ready` | D | wallet | | inapp | `{periodLabel}` `{netAmount}` | `ata://driver/settlements/{settlementId}` |
| `incentive.new` | D | promotions | | inapp, push | `{incentiveName}` `{reward}` | `ata://driver/incentives/{incentiveId}` |
| `incentive.achieved` | D | wallet | | inapp, push | `{incentiveName}` `{reward}` | `ata://driver/incentives/{incentiveId}` |
| `driver.application.under_review` | D | system | ✔ | inapp, push | `{applicationNumber}` | `ata://driver/pending` |
| `driver.application.approved` | D | system | ✔ | inapp, push, sms | `{applicationNumber}` | `ata://driver` |
| `driver.application.rejected` | D | system | ✔ | inapp, push | `{reason}` | `ata://driver/pending` |
| `driver.suspended` / `driver.reinstated` | D | system | ✔ | inapp, push | `{reason}` | `ata://driver` |
| `driver.tier_changed` | D | system | | inapp, push | `{tierName}` | `ata://driver/tier` |
| `document.expiring` | D | system | ✔ | inapp, push (+sms عند 1 يوم) | `{documentName}` `{daysLeft}` `{expiresAt}` | `ata://driver/documents` |
| `document.expired` | D | system | ✔ | inapp, push, sms | `{documentName}` | `ata://driver/documents` |
| `reliability.warning` | P/D | system | ✔ | inapp, push | `{level}` `{cancellationRate}` | `ata://reliability` |
| `reliability.restricted` | P/D | system | ✔ | inapp, push | `{level}` `{restrictedUntil}` | `ata://reliability` |
| `support.reply` | P/D | system | | inapp, push | `{ticketNumber}` `{preview}` | `ata://support/tickets/{ticketId}` |
| `support.status` | P/D | system | | inapp | `{ticketNumber}` `{status}` | `ata://support/tickets/{ticketId}` |
| `safety.check` | P | safety | ✔ | inapp, push (أزرار `ok` / `help`) | `{alertType}` | `ata://safety/check/{alertId}` |
| `safety.alert` | O | safety | ✔ | push (+sms لخط العمليات) | `{caseNumber}` `{type}` `{priority}` `{userName}` | (لوحة الإدارة) `/safety/cases/{caseId}` |
| `safety.sos_contact` | C | safety | ✔ | sms | `{userName}` `{shareUrl}` `{mapsUrl}` | — |
| `safety.trip_shared` | C | safety | ✔ | sms | `{userName}` `{shareUrl}` | — |
| `safety.case_update` | P/D | safety | | inapp, push | `{caseNumber}` `{status}` | `ata://safety/cases/{caseId}` |
| `lost_item.reported` | D | trips | | inapp, push | `{tripNumber}` `{itemCategory}` | `ata://driver/lost-items/{reportId}` |
| `lost_item.update` | P | trips | | inapp, push | `{status}` | `ata://support/tickets/{ticketId}` |
| `promo.new` | P | promotions | | inapp, push | `{code}` `{title}` | `ata://promotions` |
| `corporate.invitation` | P | system | | inapp, push, sms | `{companyName}` | `ata://corporate/invitations` |
| `corporate.guest_trip` | G | trips | ✔ | sms | `{companyName}` `{pickupName}` `{pickupTime}` `{shareUrl}` `{pin}` | — |
| `corporate.invoice_issued` | A | system | | inapp, sms | `{invoiceNumber}` `{total}` | (بوابة الشركات) `/business/invoices/{invoiceId}` |
| `privacy.export_ready` | P/D | system | | inapp, push | `{expiresAt}` | `ata://account/privacy` |
| `account.deletion_scheduled` | P/D | system | ✔ | inapp, sms | `{scheduledFor}` | `ata://account` |
| `campaign.broadcast` | حسب الجمهور | حسب الحملة | | حسب الحملة | `{title}` `{body}` | حسب الحملة |

\* `offers` هنا قناة أندرويد خاصة بعروض الرحلات للسائق (صوت مميز)، وليست تفضيل "العروض الترويجية".

**ربط الفئة بالتفضيل** (`notification_preferences`): `trips → trips`، `wallet → wallet`، `safety → safety`، `promotions → offers`، `offers` (عروض السائق) و`system` → **لا تُعطَّل** (إلزامية). القنوات: إيقاف تفضيل يمنع **push وsms** فقط؛ صف `inapp` يُنشأ دائماً (عدا `offer.received` الذي لا يُنشئ صف وارد). الأحداث الحرجة تتجاوز التفضيل دائماً.

**توافق الأنواع القديمة**: الصفوف الموجودة في `notifications.type` بالشكل القديم تبقى، والعميل يطبّعها: `driver_application_approved → driver.application.approved`، `driver_application_rejected → driver.application.rejected`، `driver_application_under_review → driver.application.under_review`، `driver_suspended → driver.suspended`، `driver_reinstated → driver.reinstated`، `trip_driver_assigned → trip.driver_assigned`، `trip_driver_arrived → trip.driver_arrived`، `trip_completed → trip.completed`، `trip_cancelled → trip.cancelled`، `trip_no_drivers → trip.no_drivers`. `NotificationTypes` في الخلفية تُستبدل ثوابتها بالأكواد الجديدة.

## F13.3 الجداول

| جدول | الأعمدة |
|---|---|
| `notification_templates` | id, code VARCHAR(60), channel (`push`/`sms`/`inapp`)، title_ar VARCHAR(120) NULL (لا عنوان للـsms)، title_en NULL, body_ar VARCHAR(1000), body_en VARCHAR(1000), is_active BOOL, updated_by NULL, created_at, updated_at — UNIQUE(code, channel) |
| `notification_deliveries` | id, notification_id NULL (FK notifications)، user_id NULL, phone_number NULL (SMS لجهة غير مسجلة)، event_code, channel (`push`/`sms`)، campaign_id NULL, status (`queued`/`sent`/`failed`/`skipped`)، skipped_reason NULL (`preference_off`/`no_subscription`/`template_inactive`/`no_phone`/`user_inactive`)، provider (`onesignal`/`logging`/`unifonic`/`taqnyat`)، provider_message_id NULL, error_code NULL, error_message VARCHAR(500) NULL, attempts TINYINT, next_attempt_at NULL, sent_at NULL, opened_at NULL, payload JSON (الرسالة المُرسلة)، created_at, updated_at — INDEX(status, next_attempt_at), INDEX(user_id, created_at), INDEX(campaign_id) |
| `notification_campaigns` | id, name, category (`promotions`/`system`/`trips`/`wallet`)، channels JSON (`["inapp","push"]`؛ `sms` يتطلب صلاحية `notifications.sms_broadcast`)، audience JSON (§F13.5)، title_ar, title_en, body_ar, body_en, deep_link VARCHAR(255) NULL, status (`draft`/`scheduled`/`sending`/`sent`/`cancelled`/`failed`)، scheduled_at NULL, started_at NULL, completed_at NULL, target_count, inapp_created, push_sent, push_failed, push_skipped, sms_sent, sms_failed, opened_count, created_by, updated_by, created_at, updated_at |
| `document_expiry_notices` | id, driver_document_id, offset_days (30/7/1/0)، sent_at — UNIQUE(driver_document_id, offset_days) (منع التكرار؛ 0 = انتهى) |
| `admin_accounts` (+) | `on_duty BOOL DEFAULT false` (مستلمو `safety.alert`) |

`notifications` يضاف إليها: `category VARCHAR(20)`, `campaign_id CHAR(36) NULL`.

## F13.4 خط الإرسال

1. الوحدة تستدعي `DispatchAsync` **داخل** معاملتها التجارية.
2. المُرسِل يقرأ القوالب النشطة للكود (كاش في الذاكرة يُبطَل عند التعديل؛ عند غياب صف القالب يُستخدم نص افتراضي في الكود `NotificationDefaults`).
3. `inapp` نشط → صف `notifications` (`title_*`/`body_*` من قالب `inapp`، `data` = `{ eventCode, deepLink, entityType, entityId, …ExtraData }`، `category`).
4. `push` نشط → يُحسب التفضيل (§F13.2)؛ صف `notification_deliveries` بحالة `queued` أو `skipped`.
5. `sms` نشط → فقط إن كان الحدث حرجاً أو صريحاً في الكتالوج؛ الهاتف من `users.phone_number` أو `RecipientPhoneOverride`.
6. بعد `SaveChanges` تُرسل معرفات الإرساليات إلى `Channel<Guid>` داخلي يقرأه `NotificationDeliveryWorker` فوراً (زمن إرسال `offer.received` < 1 ث)، ويستطلع الـworker أيضاً كل `Notifications:WorkerPollSeconds` الصفوف `queued` أو `failed` التي حان `next_attempt_at`.
7. العرض: استبدال `{placeholder}` بقيم `Placeholders` (المفقود → فارغ + تحذير في السجل). المبالغ بصيغة `46.00 ر.س` / `SAR 46.00`، الأوقات بتوقيت الرياض `HH:mm dd/MM`.
8. إعادة المحاولة: خطأ عابر → `failed` و`next_attempt_at = now + 30s × 2^(attempts−1)` حتى `Notifications:MaxAttempts` (5)؛ خطأ دائم → `failed` نهائي.
9. المستخدم `suspended`/`deleted` → `skipped (user_inactive)` عدا أحداث `system` المتعلقة بالحساب نفسه.

إشعارات العمليات (`safety.alert`): المستلمون = مستخدمو الإدارة `on_duty = true` ولديهم صلاحية `safety.manage` (لوحة الإدارة تسجّل OneSignal Web Push بـ`login(userId)`)، إضافة إلى SMS لأرقام `Safety:OpsHotlinePhones`.

## F13.5 الحملات (Broadcast)

`audience` JSON:
```json
{ "roles": ["passenger"], "cityIds": ["uuid"], "languages": ["ar"], "genders": ["female"],
  "driverTiers": ["gold", "platinum"], "lastActiveWithinDays": 30, "hasCompletedTrip": true,
  "userIds": ["uuid"] }
```
كل المفاتيح اختيارية وتُجمع بـAND (`userIds` وحده يتجاهل البقية). مدينة الراكب = مدينة آخر رحلة، ومدينة السائق = `drivers.city_id`.
- التنفيذ (`CampaignSenderJob` كل 30 ث): حملة `scheduled` حان وقتها → `sending` → حلّ الجمهور بصفحات من 2000 مستخدم → لكل صفحة: صفوف `notifications` (إن `inapp`) + إرسالية push واحدة بعدة External IDs (نفس الـpayload) تُسجَّل كصف `notification_deliveries` لكل مستخدم مع نفس `provider_message_id` → تحديث الإحصاءات → `sent`.
- احترام التفضيل: فئة `promotions` تستثني من أوقف `offers`. الإلغاء ممكن في `draft`/`scheduled`، وفي `sending` يوقف الصفحات المتبقية.
- حد المعدل: `Notifications:CampaignPushPerMinute` (60 طلب OneSignal/دقيقة).
- تتبع الفتح: `POST /notifications/{id}/opened` من التطبيق عند النقر يحدّث `read_at` و`notification_deliveries.opened_at` و`opened_count`.

## F13.6 الـAPI

### المستخدم (مصادق) — توسعة §7 من `05`
- GET `/notifications?page=&category=` → عناصر `{ id, type, category, title, body, data, readAt, createdAt }` + `unreadCount` (`type` = كود الحدث، `data.deepLink` دائماً موجود للأنواع الجديدة).
- GET `/notifications/unread-count` → `{ "unreadCount": 3 }`
- POST `/notifications/read` قائم. POST `/notifications/{id}/opened` → `204`.
- PUT `/me/devices` قائم (`pushToken` = OneSignal Subscription ID للتشخيص).

### الإدارة

| المسار | الصلاحية |
|---|---|
| GET `/admin/notification-events` → `[ { code, category, isCritical, recipients, allowedChannels, placeholders, deepLink } ]` (الكتالوج من الكود) | `notifications.view` |
| GET `/admin/notification-templates?code=&channel=&isActive=` → `[ { id, code, channel, titleAr, titleEn, bodyAr, bodyEn, isActive, updatedAt, updatedByName } ]` | `notifications.view` |
| POST `/admin/notification-templates` `{ code, channel, titleAr?, titleEn?, bodyAr, bodyEn, isActive }` → `201` (كود معروف فقط؛ قناة ضمن `allowedChannels`) | `notifications.manage` |
| PUT `/admin/notification-templates/{id}` `{ titleAr, titleEn, bodyAr, bodyEn, isActive }` → `200` | `notifications.manage` |
| POST `/admin/notification-templates/{id}/preview` `{ placeholders: { … }, language: "ar" }` → `{ title, body, length, smsSegments }` | `notifications.view` |
| POST `/admin/notification-templates/{id}/test` `{ userId }` → `202` (يُرسل فعلياً لمستخدم واحد، مع علامة `test` في `data`) | `notifications.manage` |
| GET/POST `/admin/notification-campaigns`، GET/PUT/DELETE `/admin/notification-campaigns/{id}` (التعديل والحذف في `draft` فقط، وإلا `409 campaign_not_editable`) | `notifications.manage` |
| POST `/admin/notification-campaigns/{id}/schedule` `{ scheduledAt }` · `/send-now` · `/cancel` | `notifications.manage` |
| POST `/admin/notification-campaigns/audience-preview` `{ audience }` → `{ count, sample: [ { userId, name, phoneMasked } ] }` | `notifications.manage` |
| GET `/admin/notification-deliveries?userId=&eventCode=&channel=&status=&campaignId=&from=&to=&page=` → `{ id, eventCode, channel, status, skippedReason, userName, phoneMasked, provider, providerMessageId, errorCode, errorMessage, attempts, sentAt, openedAt, createdAt }` | `notifications.view` |
| POST `/admin/notification-deliveries/{id}/retry` → `202` | `notifications.manage` |
| GET/PUT `/admin/me/duty` `{ onDuty: true }` | (أي إداري لديه `safety.manage`) |

أكواد: `unknown_event_code` (422)، `campaign_not_editable` (409)، `template_placeholder_invalid` (422، `details.unknownPlaceholders` — عنصر نائب غير معرّف للحدث).
Audit: `notification_template.create|update`, `notification_campaign.create|update|schedule|send|cancel`, `notification_delivery.retry`.

### SignalR
- `NotificationCreated({ id, type, category, title, body, data, createdAt })` للمستخدم (تحديث الجرس والعدّاد فوراً).

## F13.7 مخطط الروابط العميقة `ata://` (مرجع موحّد)

| الرابط | مسار go_router | ملاحظة |
|---|---|---|
| `ata://home` | `/home` | |
| `ata://trip/{tripId}` | `/trip` إن كانت نشطة، وإلا `/rides/{tripId}` | |
| `ata://trip/{tripId}/chat` | `/trip/chat` (راكب) أو `/driver/trip/chat` (سائق) | F12 |
| `ata://rides/{tripId}` | `/rides/{tripId}` | |
| `ata://rides/{tripId}/receipt` | `/rides/{tripId}/receipt` | F11 |
| `ata://rate/{tripId}` | `/rate/{tripId}` | F15 |
| `ata://scheduled/{tripId}` | `/scheduled/{tripId}` | F17 |
| `ata://wallet` · `ata://wallet/transactions` | `/wallet` · `/wallet/transactions` | |
| `ata://promotions` | `/promotions` | F15 |
| `ata://reliability` | `/account/reliability` (راكب) أو `/driver/reliability` (سائق) | F14 |
| `ata://safety/check/{alertId}` | `/safety/check/{alertId}` | F12 |
| `ata://safety/cases/{caseId}` | `/safety/cases/{caseId}` | F12 |
| `ata://support/tickets/{ticketId}` | `/support/tickets/{ticketId}` | F18 |
| `ata://corporate/invitations` | `/account/corporate` | F19 |
| `ata://account` · `ata://account/privacy` | `/account` · `/account/privacy` | F21 |
| `ata://driver` · `ata://driver/pending` | `/driver` · `/driver/pending` | |
| `ata://driver/offer` · `ata://driver/trip` | `/driver/offer` · `/driver/trip` | |
| `ata://driver/documents` | `/driver?tab=documents` | |
| `ata://driver/earnings` · `ata://driver/payouts` · `ata://driver/settlements/{id}` | `/driver/earnings` · `/driver/payouts` · `/driver/settlements/{id}` | F11 |
| `ata://driver/tier` · `ata://driver/incentives/{id}` | `/driver/tier` · `/driver/incentives/{id}` | F15 |
| `ata://driver/scheduled` · `ata://driver/scheduled/{tripId}` | `/driver/scheduled` · `/driver/scheduled/{tripId}` | F17 |
| `ata://driver/lost-items/{id}` | `/driver/lost-items/{id}` | F12 |
| `ata://notifications` | يفتح ورقة الإشعارات | |
| `ata://payments/return?paymentId=…` | يُلتقط داخل `PaymentWebViewPage` | F11 |

رابط غير معروف أو دور غير مطابق → `/home` أو `/driver` حسب الدور، مع تسجيل تحذير. قواعد إعادة التوجيه في الراوتر (الرحلة النشطة، العرض) لها الأولوية على الرابط.

## F13.8 المهام الخلفية والإعدادات

| المهمة | الجدولة | العمل |
|---|---|---|
| `NotificationDeliveryWorker` | فوري + كل 5 ث | إرسال `queued` وإعادة `failed` المستحقة |
| `CampaignSenderJob` | كل 30 ث | الحملات المجدولة |
| `DocumentExpiryScanJob` | يومياً 06:00 الرياض | لكل `driver_documents` بحالة `verified` و`expires_at`: عند بقاء 30/7/1 يوم (بتاريخ الرياض) → `document.expiring` مرة واحدة (`document_expiry_notices`)؛ عند `expires_at < today` → الحالة `expired` + `document.expired` + إن كان النوع `is_required` والسائق متصل وبلا رحلة → `is_online=false` مع سجل `driver_status_logs` + `audit_logs` (`document.expire`, actor system) |
| `NotificationDeliveryCleanupJob` | يومياً | ضمن سياسة الاحتفاظ (F21) |

| المفتاح | الافتراضي |
|---|---|
| `OneSignal:Enabled` | `false` تطوير، `true` إنتاج |
| `OneSignal:AppId`, `OneSignal:RestApiKey` (سر) | — (قائم في `03`) |
| `OneSignal:ApiBaseUrl` | `https://api.onesignal.com` |
| `OneSignal:AndroidChannels:{trips,offers,safety,wallet,promotions,system}` | معرّفات القنوات من لوحة OneSignal |
| `Notifications:WorkerPollSeconds` / `Notifications:MaxAttempts` / `Notifications:PushBatchSize` | 5 / 5 / 2000 |
| `Notifications:CampaignPushPerMinute` | 60 |
| `Notifications:DocumentExpiryOffsetsDays` | `[30, 7, 1]` |
| `Notifications:DocumentScanHourLocal` | 6 |
| `Sms:Provider` / `Sms:Sandbox` / `Sms:SenderName` | `logging` / `true` / `ATA` |
| `Sms:Unifonic:BaseUrl`, `Sms:Unifonic:AppSid` (سر) | — |
| `Sms:Taqnyat:BaseUrl`, `Sms:Taqnyat:BearerToken` (سر) | — |
| `Safety:OpsHotlinePhones` | `[]` |

## F13.9 Flutter

**`core/push/`** (جديد، بنية تحتية لا feature):
- `PushService` (واجهة) + `OneSignalPushService` (`onesignal_flutter`) + `NoopPushService` (عند غياب `ONESIGNAL_APP_ID` أو في الاختبارات).
- التهيئة في `bootstrap`: `OneSignal.initialize(appId)`. طلب الإذن `OneSignal.Notifications.requestPermission(true)` بعد أول دخول ناجح (ليس عند التشغيل الأول).
- الربط بالجلسة (listener على `SessionCubit`، لا منطق في الواجهات): عند الدخول `OneSignal.login(userId)` ثم `OneSignal.User.addTags({ 'role': 'passenger|driver', 'lang': 'ar|en', 'city': '<cityCode>' })` و`OneSignal.User.setLanguage(lang)`، ثم `PUT /me/devices` مع `pushToken = OneSignal.User.pushSubscription.id`. عند تغيير اللغة (`LocaleCubit`) تُحدَّث `lang` و`setLanguage`. عند الخروج `OneSignal.logout()`.
- `OneSignal.Notifications.addClickListener`: يقرأ `additionalData.deepLink` و`notificationId` و`result.actionId` (أزرار `safety.check`) → `DeepLinkCubit.open(link, actionId)` + `POST /notifications/{id}/opened`.
- `addForegroundWillDisplayListener`: للأحداث التي يعرضها التطبيق بنفسه (`offer.received`، `trip.*` أثناء وجود الشاشة المعنية، `safety.check`) → `event.preventDefault()` وتحديث الـCubit المعني؛ غيرها يُعرض كالمعتاد.
- `--dart-define=ONESIGNAL_APP_ID=…` (قائم في `03`).

**feature `notifications`** (توسعة):
- entities: `NotificationItem` + `category`, `deepLink`؛ `DeepLink` (قيمة مُحلَّلة: `route`, `params`).
- usecases: `GetNotifications` (قائم + فلتر الفئة)، `MarkNotificationsRead` (قائم)، `GetUnreadCount`، `MarkNotificationOpened`، `ParseDeepLink` (جدول §F13.7 + توافق الأنواع القديمة).
- cubits: `NotificationsCubit` (قائم + تحديث لحظي من `NotificationCreated` + فتح الرابط عند النقر على عنصر)، `DeepLinkCubit` (app-wide: يستقبل روابط من الإشعارات، ينتظر جاهزية الجلسة، ثم يطلب من الراوتر `go(route)`؛ يُختبر بـ`bloc_test`).
- `account/notifications` (تفضيلات، قائم) يوضح أن السلامة وتنبيهات الرحلة الحرجة تصل دائماً.

## F13.10 لوحة الإدارة

| المسار | الصفحة |
|---|---|
| `/notifications/templates` | جدول الأحداث (من `/admin/notification-events`) مع قنوات كل حدث؛ محرر القالب (عربي/إنجليزي جنباً إلى جنب، إدراج العناصر النائبة بالنقر، عدّاد أحرف وأجزاء SMS، معاينة، إرسال تجريبي، تفعيل/إيقاف) |
| `/notifications/campaigns` | الحملات: قائمة بالحالة والإحصاءات؛ نموذج (الاسم، الفئة، القنوات، الجمهور مع "معاينة العدد"، النص ثنائي اللغة، الرابط العميق، الجدولة)؛ صفحة تفاصيل بإحصاءات الإرسال والفتح |
| `/notifications/deliveries` | سجل الإرسال بفلاتر (المستخدم، الحدث، القناة، الحالة، الحملة، التاريخ)، عرض الـpayload والخطأ، زر إعادة المحاولة |

والشريط العلوي: مفتاح "مناوب" (`/admin/me/duty`) لمن يملك `safety.manage`، وتسجيل OneSignal Web SDK (`VITE_ONESIGNAL_APP_ID`) مع `login(userId)` لاستقبال `safety.alert`.

---

## البيانات الأولية (Seed)

- قوالب لكل الأحداث في §F13.2 بالقنوات الافتراضية (عربي/إنجليزي). أمثلة:
  - `trip.driver_arrived` push: `وصل الكابتن` / `{driverName} بانتظارك · {plateNumber}. الانتظار مجاني لمدة {freeWaitingMinutes} دقائق.`
  - `document.expiring` push: `مستند قارب على الانتهاء` / `ينتهي {documentName} خلال {daysLeft} يوم. حدّثه لتستمر في استقبال الرحلات.`
  - `safety.sos_contact` sms: `تنبيه طوارئ من {userName} عبر ATA. الموقع: {mapsUrl} تتبع الرحلة: {shareUrl}`
- لا بيانات أولية للمدفوعات عدا: `Payments:Provider=sandbox` في `appsettings.Development.json`.

## سيناريوهات الاختبار (الخلفية — xUnit)

المدفوعات:
1. إضافة بطاقة بـ`tok_sandbox_visa` → `active`، وبـ`tok_sandbox_3ds` → `202` ثم التحدي `approve` → `active`، و`decline` → `failed`. نفس البصمة مرتين → `409`.
2. رحلة بطاقة: التفويض بـ`estimatedFare × 1.3`؛ الإكمال بأجرة أقل → `captured` بالأجرة، قيد `trip_card_capture` متوازن + `trip_earning`.
3. أجرة نهائية > التفويض → Void + Purchase.
4. `tok_sandbox_capture_fail` → الرحلة تكتمل `cash`، حدث `payment_fallback_cash (card_capture_failed)`، `collectCashAmount` للسائق، إشعار `payment.failed`.
5. `tok_sandbox_timeout` عند الإكمال → `capturePending`؛ المهمة تعيد المحاولة؛ بعد الحد الأقصى → حركة `trip_payment` overdraft ورصيد سالب، ثم `POST /passenger/trips` → `422 outstanding_balance`، والشحن يعيد الرصيد موجباً ويسمح بالطلب.
6. 3DS عند طلب الرحلة → الرحلة `requested` لا تدخل المطابقة؛ انتهاء المهلة → `cancelled (payment_failed)` والدفعة `failed`.
7. إلغاء برسوم على رحلة بطاقة → Capture جزئي بقيمة الرسوم؛ بدون رسوم → Void.
8. Webhook بتوقيع خاطئ → `401` وصف محفوظ `signature_valid=false`؛ نفس `event_id` مرتين → معالجة واحدة؛ حالة أقدم بعد أحدث → `ignored`.
9. شحن بالبطاقة: `201` فوري، و3DS → `202` ثم Webhook `captured` → حركة `topup` مرة واحدة حتى مع تكرار الـWebhook؛ نفس `Idempotency-Key` يعيد نفس النتيجة.
10. رحلة نقدية 50/40 → محفظة السائق −10، القيود متوازنة؛ تجاوز `MaxCashDebt` → `403 cash_debt_limit_exceeded` عند الاتصال واستبعاد من المطابقة.
11. خصم عرض على رحلة محفظة → الراكب يدفع بعد الخصم، السائق يأخذ نصيبه قبل الخصم، قيد `trip_discount` على `discount_promotion`، و`trip_revenue` صافيه = العمولة على الإجمالي.
12. الاسترداد: جزئي ثم كامل → `partially_refunded` ثم `refunded`؛ تجاوز المتبقي → `422 refund_exceeds_amount`؛ فوق حد الموافقة من نفس المنشئ → `409 four_eyes_required`؛ رحلة نقدية → وجهة المحفظة وحركة `refund`.
13. السحب: أقل من الحد → `422`؛ بدون IBAN → `422 iban_missing`؛ طلبان → `409`؛ الرفض والإلغاء يعيدان الرصيد (`payout_reversal`)؛ `mark-paid` ينشئ قيد `payout_paid`؛ دفعة التحويل تصدّر CSV صحيحاً.
14. التسوية: الثابت `closing = opening + net + topups − fees − payouts` لكل سائق؛ فترة متداخلة → `409`؛ `finalize` يمنع إعادة التوليد؛ `AutoCreatePayouts` ينشئ سحوبات للموجبين فقط.
15. الإيصال: الأسطر ومجموعها = `total`، ضريبة 15/115، الخصومات بمصدرها.
16. كل قيد (حركة أو journal) متوازن — اختبار شامل يجمع `ledger_entries` بعد كل سيناريو.

الإشعارات:
17. حدث غير حرج مع تفضيل مُطفأ → صف `inapp` + إرسالية `skipped (preference_off)`؛ حدث حرج → `queued` رغم التفضيل.
18. قالب غير نشط → لا قناة؛ غياب القالب → النص الافتراضي.
19. `OneSignalPushSender` مع `HttpMessageHandler` مزيّف: جسم الطلب مطابق (`include_aliases.external_id`, `headings.en`, `idempotency_key`)؛ 500 → إعادة محاولة بتراجع؛ `invalid_aliases` → `skipped (no_subscription)`.
20. `DocumentExpiryScanJob` بساعة مزيّفة: إشعار واحد عند 30/7/1 يوم فقط حتى مع تشغيلين، والانتهاء يحوّل الحالة ويفصل السائق.
21. الحملة: حل الجمهور (دور + مدينة + لغة) وعدم إرسال فئة `promotions` لمن أوقف `offers`، وتقسيم الدفعات بحجم 2000، وتحديث الإحصاءات، والإلغاء أثناء الإرسال.
22. `POST /admin/notification-templates` بكود مجهول → `422 unknown_event_code`؛ عنصر نائب غير معرّف → `422 template_placeholder_invalid`.
23. SMS: `Sms:Sandbox=true` لا يستدعي أي HTTP.

## قرارات تحتاج تأكيد مالك المنتج

1. دين النقد على السائق يُمثَّل برصيد سالب في محفظة السائق (حد 500 ر.س يمنع الاتصال) بدل جدول ديون منفصل.
2. جدول `ledger_journals` جديد للقيود التي لا تمس محفظة (البطاقة، الخصم، الشركات، تحويل السحوبات).
3. المنصة تتحمل كل الخصومات، ونصيب السائق يُحسب قبل الخصم.
4. الأسعار شاملة ضريبة القيمة المضافة 15%، والإيصال يعرض الضريبة المضمّنة.
5. Apple Pay للشحن فقط في v1.
6. فشل التحصيل النهائي بعد رحلة البطاقة يصبح ديناً على محفظة الراكب يمنع الطلب التالي حتى الشحن.
7. الاسترداد ≥ 50 ر.س يتطلب موافقة شخص ثانٍ.
8. إضافة الحالتين `voided` (للدفعة) و`cancelled` (للسحب) إلى القوائم المطلوبة.
9. التسوية أسبوعية وبيانية فقط (المقاصة لحظية في المحفظة).
