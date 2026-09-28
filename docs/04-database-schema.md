# مخطط قاعدة البيانات — MySQL 8

اصطلاحات: أسماء الجداول والأعمدة `snake_case` بصيغة الجمع، المفتاح الأساسي `id CHAR(36)` (UUID v7)، أوقات `DATETIME(6)` بتوقيت UTC، `created_at`/`updated_at` في كل جدول، الحذف المنطقي `deleted_at` حيث يلزم، الترميز `utf8mb4` والترتيب `utf8mb4_0900_ai_ci`، المبالغ `DECIMAL(12,2)`، الإحداثيات `DECIMAL(10,7)`.

**رقم الجوال ليس مفتاحاً**؛ المفتاح `user_id` والجوال عمود فريد.

## الخطوة 1 — الجداول المنفذة

### identity
| جدول | الأعمدة الأساسية |
|---|---|
| `users` | id, phone_number (E.164, UNIQUE), phone_verified_at, full_name, language (`ar`/`en`), gender (`unknown`/`male`/`female`), status (`active`/`suspended`/`deleted`), terms_accepted_at, last_login_at, created_at, updated_at, deleted_at |
| `user_roles` | id, user_id (FK), role (`passenger`/`driver`/`admin`/`operations`/`corporate_admin`), granted_at — UNIQUE(user_id, role) |
| `otp_requests` | id, phone_number, purpose (`login`), code_hash, attempts, max_attempts, expires_at, consumed_at, ip_address, created_at — INDEX(phone_number, created_at) |
| `refresh_tokens` | id, user_id (FK), token_hash (UNIQUE), device_id, expires_at, revoked_at, replaced_by_id, created_by_ip, created_at |
| `user_devices` | id, user_id (FK), device_id, platform (`android`/`ios`/`web`), device_name, push_token (معرّف اشتراك OneSignal، اختياري للتشخيص فقط)، app_version, last_seen_at, created_at — UNIQUE(user_id, device_id) |
| `admin_accounts` | id, user_id (FK, UNIQUE), username (UNIQUE), password_hash, mfa_secret, mfa_enabled, permissions (JSON), is_active, last_login_at, created_at, updated_at |

### passengers
| جدول | الأعمدة |
|---|---|
| `passengers` | id, user_id (FK UNIQUE), rating_avg DECIMAL(3,2) default 5.00, rating_count, prefer_female_driver BOOL, default_payment_method (`cash`/`wallet`/`card`), created_at, updated_at |
| `saved_places` | id, passenger_id (FK), label (`home`/`work`/`other`), name, address, latitude, longitude, created_at |

### drivers
| جدول | الأعمدة |
|---|---|
| `drivers` | id, user_id (FK UNIQUE), application_number (`ATA-#####`, UNIQUE), application_status (`draft`/`submitted`/`under_review`/`approved`/`rejected`/`suspended`), rejection_reason, national_id, date_of_birth, city_id (FK), gender, iban, tier (`bronze`/`silver`/`gold`/`platinum`), rating_avg, rating_count, is_online BOOL, last_online_at, approved_at, approved_by (FK admin user), submitted_at, created_at, updated_at |
| `vehicles` | id, driver_id (FK), ride_category_id (FK), make, model, year SMALLINT, color, plate_number (UNIQUE), seats TINYINT, is_active, created_at, updated_at |
| `driver_documents` | id, driver_id (FK), vehicle_id (FK NULL), document_type_id (FK), file_id (FK), status (`pending`/`verified`/`rejected`/`expired`), expires_at DATE NULL, review_note, reviewed_by, reviewed_at, created_at, updated_at |
| `driver_status_logs` | id, driver_id, is_online, changed_at, latitude, longitude (يُستخدم في KPIs لساعات الاتصال) |

### catalog
| جدول | الأعمدة |
|---|---|
| `ride_categories` | id, code (`saver`/`economy`/`comfort`/`family`/`premium`/`airport`, UNIQUE), name_ar, name_en, description_ar, description_en, icon, seats, max_stops, sort_order, is_active, created_at, updated_at |
| `document_types` | id, code (`national_id`/`driving_license`/`vehicle_registration`/`insurance`/`profile_photo`), name_ar, name_en, applies_to (`driver`/`vehicle`), is_required, requires_expiry, sort_order, is_active |
| `cities` | id, code (`riyadh`), name_ar, name_en, country_code (`SA`), center_lat, center_lng, is_active |

### files
| جدول | الأعمدة |
|---|---|
| `stored_files` | id, owner_user_id, storage_key, original_name, content_type, size_bytes, sha256, created_at |

### wallet
| جدول | الأعمدة |
|---|---|
| `wallets` | id, user_id (FK), kind (`passenger`/`driver`), currency (`SAR`), balance DECIMAL(12,2) (قيمة مشتقة تُحدَّث فقط من الـLedger داخل نفس المعاملة), status, created_at, updated_at — UNIQUE(user_id, kind) |
| `wallet_transactions` | id, wallet_id (FK), type (`topup`/`trip_payment`/`trip_earning`/`refund`/`payout`/`adjustment`/`incentive`/`cancellation_fee`), direction (`credit`/`debit`), amount, balance_after, reference_type, reference_id, idempotency_key (UNIQUE NULL), description, created_at |
| `ledger_entries` | id, transaction_id (FK), account (`passenger_wallet:{id}`/`platform_cash`/`gateway_clearing`/`driver_wallet:{id}`…), debit, credit, created_at — كل معاملة لها قيدان متوازنان على الأقل |

### notifications
| جدول | الأعمدة |
|---|---|
| `notifications` | id, user_id (FK), type, title_ar, title_en, body_ar, body_en, data (JSON), read_at, created_at |
| `notification_preferences` | id, user_id (FK UNIQUE), trips BOOL, wallet BOOL, safety BOOL, offers BOOL, updated_at |

### admin
| جدول | الأعمدة |
|---|---|
| `audit_logs` | id, actor_user_id, actor_role, action, entity_type, entity_id, before_json, after_json, ip_address, created_at — INDEX(entity_type, entity_id), INDEX(actor_user_id, created_at) |

## الخطوات اللاحقة — جداول مخططة

- الخطوة 2: `trips`, `trip_stops`, `trip_offers`, `trip_events`, `driver_locations`, `zones`, `pricing_rules`, `demand_levels`, `demand_rules`.
- الخطوة 3: `payments`, `payment_methods` (tokens فقط)، `payouts`, `settlements`.
- الخطوة 4: `safety_cases`, `trip_shares`, `emergency_contacts`, `sms_logs`, `push_logs`.
- الخطوة 5: `ratings`, `promotions`, `promotion_redemptions`, `cancellation_rules`, `cancellation_reasons`, `cancellation_events`, `reliability_profiles`, `favorite_drivers`, `favorite_driver_discount_rules`, `driver_incentives`.
- الخطوة 6: `scheduled_ride_rules`, `scheduled_ride_reminders`, `airport_zones`.
- الخطوة 7: `roles`, `permissions`, `role_permissions`, `support_tickets`, `support_messages`, `report_snapshots`.
- الخطوة 8: `corporate_accounts`, `corporate_users`, `corporate_policies`, `corporate_invoices`.

## بيانات أولية (Seed)

- الفئات: saver (توفير)، economy (اقتصادي)، comfort (مريح)، family (عائلي، 6 مقاعد، حتى 2 محطة)، premium (ATA بلس)، airport (المطار).
- أنواع المستندات: الهوية الوطنية/الإقامة، رخصة القيادة، استمارة المركبة، التأمين، الصورة الشخصية.
- المدينة: الرياض.
- حساب إدارة تطويري: `admin` / `Admin@12345` (يُغيَّر في الإنتاج عبر متغيرات البيئة).
