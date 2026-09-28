# F12 السلامة + F14 محرك الإلغاء والموثوقية

الطبقات: MySQL → API (وحدتا `Safety` و`Cancellation` + تعديلات على `Trips` و`Matching`) → Flutter (features `safety`, `trip_chat` + توسعة `trip`) → الموقع (صفحة التتبع العامة `/t/{token}`) → لوحة الإدارة (حالات السلامة، التنبيهات، المفقودات، قواعد الإلغاء، الأسباب، العتبات، مراجعة الأعذار، ملفات الموثوقية).

يعتمد على: `06` (دورة الرحلة)، `07` (المطابقة/التسعير)، `08` (الدفتر المالي §F11.3، كتالوج الإشعارات §F13.2، الروابط العميقة §F13.7). الصلاحيات من F20 (`12`). الاصطلاحات كما في `04`/`05`.

---

# F12 — السلامة

## F12.1 الجداول

| جدول | الأعمدة |
|---|---|
| `trip_shares` | id, trip_id (FK)، token VARCHAR(32) UNIQUE (128-bit عشوائي base64url، 22 حرفاً)، created_by_user_id, trusted_contact_id NULL, channel (`link`/`sms`/`auto`)، expires_at NULL (يُضبط عند انتهاء الرحلة = وقت الانتهاء + `Safety:ShareExpiryMinutesAfterEnd`)، revoked_at NULL, view_count INT, last_viewed_at NULL, created_at — INDEX(trip_id) |
| `trusted_contacts` | id, user_id (FK)، name VARCHAR(80), phone_number (E.164)، relationship VARCHAR(40) NULL, auto_share BOOL, notify_on_sos BOOL DEFAULT true, created_at, updated_at — UNIQUE(user_id, phone_number) — الحد 5 لكل مستخدم (يحل محل `emergency_contacts` المذكور في `04`) |
| `safety_cases` | id, case_number (UNIQUE `SC-YYYYMMDD-####`)، type (`sos`/`unexpected_stop`/`route_deviation`/`trip_overrun`/`safety_report`)، source (`rider_sos`/`driver_sos`/`alert`/`report`/`support`/`admin`)، priority (`critical`/`high`/`medium`/`low`)، status (`open`/`in_progress`/`escalated`/`resolved`)، trip_id NULL, reporter_user_id NULL, reporter_role (`passenger`/`driver`/`system`/`admin`)، subject_user_id NULL (الطرف المُبلَّغ عنه)، report_category NULL (§F12.6)، description VARCHAR(2000) NULL, lat NULL, lng NULL, last_lat NULL, last_lng NULL, last_location_at NULL, contacts_notified TINYINT, assigned_to_user_id NULL, assigned_at NULL, escalated_to VARCHAR(40) NULL (`police`/`ambulance`/`civil_defense`/`management`/`other`)، resolution_code NULL (`false_alarm`/`resolved_contacted`/`escalated_authorities`/`action_taken_driver`/`action_taken_passenger`/`no_action`/`other`)، resolution VARCHAR(2000) NULL, reporter_cancelled_at NULL, support_ticket_id NULL (F18)، opened_at, first_response_at NULL, resolved_at NULL, created_at, updated_at — INDEX(status, priority, opened_at), INDEX(trip_id) |
| `safety_case_notes` | id, case_id (FK)، author_user_id NULL (NULL = النظام)، kind (`note`/`status_change`/`assignment`/`contact_attempt`/`system`)، body VARCHAR(2000), is_internal BOOL DEFAULT true, created_at — INDEX(case_id, created_at) |
| `safety_case_attachments` | id, case_id, file_id (stored_files)، uploaded_by, created_at |
| `safety_alerts` | id, trip_id (FK)، type (`unexpected_stop`/`route_deviation`/`trip_overrun`)، status (`pending_rider`/`resolved_ok`/`escalated`/`no_response`/`dismissed`)، detected_at, lat, lng, metrics JSON (`{ stoppedSeconds, deviationMeters, deviationSeconds, elapsedSeconds, estimatedSeconds }`)، prompted_at NULL, respond_by NULL, responded_at NULL, response NULL (`ok`/`need_help`)، safety_case_id NULL, dismissed_by NULL, created_at — INDEX(trip_id, type, status) |
| `trip_messages` | id, trip_id (FK)، sender_user_id NULL, sender_role (`passenger`/`driver`/`system`)، kind (`text`/`quick_reply`/`system`)، body VARCHAR(500), quick_reply_code VARCHAR(40) NULL, read_at NULL, created_at — INDEX(trip_id, created_at) — الاحتفاظ `Retention:TripMessagesDays` (180) |
| `lost_item_reports` | id, report_number (UNIQUE `LI-YYYYMMDD-####`)، trip_id (FK)، reporter_user_id (الراكب)، driver_id, item_category (`phone`/`wallet`/`bag`/`keys`/`documents`/`other`)، description VARCHAR(1000), contact_phone NULL (افتراضياً جوال الراكب)، status (`open`/`driver_contacted`/`found`/`returned`/`not_found`/`closed`)، driver_response NULL (`found`/`not_found`)، driver_note VARCHAR(500) NULL, driver_responded_at NULL, support_ticket_id NULL (F18)، closed_by NULL, closed_at NULL, created_at, updated_at — INDEX(driver_id, status) |

تعديلات:
- `trips`: `planned_route JSON NULL` (مصفوفة `[[lat,lng],…]` تُحسب عند الإنشاء كقطع مستقيمة الالتقاط → المحطات → الوجهة، أو من مزوّد الخرائط لاحقاً)، `planned_route_source` (`straight`/`maps`).
- `admin_accounts.on_duty` (معرّف في `08` §F13.3).

## F12.2 مشاركة الرحلة والتتبع العام

- الإنشاء: راكب الرحلة فقط، والحالة بين `driver_assigned` و`in_trip` (وإلا `409 conflict`). حد `Safety:MaxSharesPerTrip` (10).
- `channel=sms` مع `contactIds` → رابط مستقل لكل جهة + SMS بحدث `safety.trip_shared` (`{shareUrl}` = `{Safety:ShareBaseUrl}/t/{token}`، افتراضياً `https://ata.sa/t/{token}`).
- **المشاركة التلقائية**: عند انتقال الرحلة إلى `driver_assigned`، لكل جهة `auto_share = true` للراكب → `channel=auto` + SMS (إن `Safety:AutoShareSmsEnabled=true`).
- الانتهاء: عند `completed`/`cancelled`/`no_drivers` تُضبط `expires_at` لكل روابط الرحلة. الرابط الملغى أو المنتهي → `410 share_expired`. غير موجود → `404 share_not_found`.
- الصفحة العامة لا تكشف: رقم الجوال، اسم العائلة، PIN، الأجرة، طريقة الدفع.
- حد المعدل: `Safety:PublicShareRatePerMinute` (30 طلب/دقيقة لكل IP) → `429 rate_limited`. كل قراءة تزيد `view_count` (مرة كل 60 ث لكل IP على الأكثر).

## F12.3 الطوارئ (SOS)

`POST /safety/sos` من الراكب أو السائق:
1. الدور: إن كان المستخدم طرفاً في `tripId` فالدور دوره في الرحلة؛ وإلا حقل `role` أو الدور الوحيد في الـJWT.
2. إن وُجدت حالة `sos` مفتوحة لنفس المستخدم ونفس الرحلة خلال `Safety:SosDedupMinutes` (10) → تُعاد نفسها (`200`) مع ملاحظة `system` "ضغط متكرر".
3. إنشاء `safety_cases` بـ`type=sos`, `priority=critical`, `status=open`, `source=rider_sos|driver_sos`، الموقع، و`safety_case_notes` نظامية.
4. الإشعار: SignalR `SafetyCaseOpened` لمجموعة `admins` + حدث `safety.alert` لكل مناوب لديه `safety.manage` (push) + SMS لـ`Safety:OpsHotlinePhones`.
5. إن `notifyTrustedContacts=true`: لكل جهة `notify_on_sos = true` → رابط مشاركة (إن وُجدت رحلة) + SMS `safety.sos_contact` (`{mapsUrl}` = `https://maps.google.com/?q={lat},{lng}`). يُحدَّث `contacts_notified`.
6. الاستجابة تتضمن `emergencyNumber` = `Safety:EmergencyNumber` (`911`) ليعرض التطبيق زر الاتصال المباشر.
7. أثناء الحالة المفتوحة يرسل التطبيق الموقع كل `Safety:SosLocationIntervalSeconds` (10) إلى `/safety/sos/{caseId}/location` (يحدّث `last_lat/lng` ويُبث `SafetyCaseUpdated`).
8. `cancel` من المُبلِّغ ("ضغطت بالخطأ") يسجل `reporter_cancelled_at` وملاحظة، ويخفض الأولوية إلى `high`؛ الحالة تبقى للعمليات لإغلاقها بعد التواصل.

## F12.4 التواصل المُقنَّع

- **المحادثة داخل التطبيق** (`trip_messages`): متاحة للطرفين من `driver_assigned` حتى `in_trip`؛ بعد انتهاء الرحلة تصبح للقراءة فقط (`409 chat_closed` عند الإرسال). النص ≤ 500 حرف؛ إن `Safety:ChatMaskPhoneNumbers=true` تُستبدل أي سلسلة ≥ 8 أرقام بـ`••••`. كل رسالة → SignalR `TripMessage` للطرف الآخر + push `trip.message` (غير حرج؛ لا يُرسل إن كان المستقبل متصلاً بالـHub وشاشة المحادثة مفتوحة — العميل يرسل `POST …/messages/read`).
- **الردود السريعة** (كتالوج في الكود):

| الدور | الكود | ar | en |
|---|---|---|---|
| driver | `on_my_way` | أنا في الطريق إليك | I'm on my way |
| driver | `arrived` | وصلت إلى نقطة الالتقاط | I've arrived at the pickup |
| driver | `cant_find_you` | لا أستطيع إيجادك، أين أنت؟ | I can't find you, where are you? |
| driver | `traffic_delay` | تأخرت قليلاً بسبب الزحام | Running a bit late due to traffic |
| passenger | `coming_now` | قادم الآن | Coming now |
| passenger | `wait_please` | انتظرني دقيقتين من فضلك | Please wait two minutes |
| passenger | `at_pickup` | أنا في نقطة الالتقاط | I'm at the pickup point |
| passenger | `where_are_you` | أين أنت؟ | Where are you? |

- **المكالمة المُقنَّعة**: تجريد `ICallMaskingProvider` (`NoneCallMaskingProvider` افتراضياً؛ مزوّد رقم وسيط لاحقاً عبر `CallMasking:Provider`). `POST …/call` يعيد `{ "mode": "proxy", "proxyNumber": "+9668…", "pin": "1234", "expiresAt": "…" }` أو `{ "mode": "unavailable" }` → التطبيق يخفي زر الاتصال ويعرض المحادثة. **لا يُعاد رقم الطرف الآخر الحقيقي أبداً** (الحقل `phoneMasked` في `Trip` يبقى للعرض فقط، والتطبيق يتوقف عن استخدام `tel:` برقم حقيقي).

## F12.5 كشف الحالات الشاذة

`SafetyMonitorJob` كل `Safety:MonitorIntervalSeconds` (30) على الرحلات `in_trip`، باستخدام `driver_locations` (السرعة/الموقع الحالي) و`driver_location_history`:

| النوع | الشرط | الإعدادات (افتراضي) |
|---|---|---|
| `unexpected_stop` | السرعة < `Safety:StopSpeedMps` (1.5) بشكل متواصل لأكثر من `Safety:StopMinutes` (5) دقائق، والموقع أبعد من `Safety:StopIgnoreRadiusMeters` (300) عن الالتقاط والوجهة وكل المحطات | 1.5 م/ث، 5 د، 300 م |
| `route_deviation` | أقل مسافة من الموقع إلى `planned_route` (قطع مستقيمة، إسقاط equirectangular) > `Safety:DeviationMeters` (2000 لـ`straight`، و`Safety:DeviationMetersMaps` 500 لـ`maps`) لمدة متواصلة > `Safety:DeviationSeconds` (120) | 2000/500 م، 120 ث |
| `trip_overrun` | زمن الرحلة منذ `started_at` > `estimated_duration_s × Safety:OverrunFactor` (2.0) **و** الزيادة > `Safety:OverrunMinMinutes` (15) | ×2.0، 15 د |

- الاستمرارية تُحسب من `driver_location_history` (نقاط آخر نافذة)؛ موقع أقدم من `Matching:LocationMaxAgeSeconds` يعني عدم كفاية البيانات (لا تنبيه).
- منع التكرار: تنبيه واحد مفتوح لكل `(trip, type)`، وبعد الإغلاق مهلة `Safety:AlertCooldownMinutes` (10) قبل تنبيه جديد من نفس النوع.
- الدورة:
  1. إنشاء `safety_alerts` بحالة `pending_rider`، `respond_by = now + Safety:CheckResponseSeconds` (120).
  2. سؤال الراكب "هل أنت بخير؟": push `safety.check` (حرج، أزرار `ok` "أنا بخير" و`help` "أحتاج مساعدة") + SignalR `SafetyCheck`.
  3. `ok` → `resolved_ok`. `need_help` → `escalated` + `safety_cases` (النوع = نوع التنبيه، `priority=high`، `source=alert`) + إشعار العمليات كما في SOS.
  4. بلا رد حتى `respond_by` → `no_response` + حالة `priority=high` + إشعار العمليات.
  5. SignalR `SafetyAlertRaised` للإدارة عند كل تنبيه (حتى قبل التصعيد).

## F12.6 البلاغات والمفقودات

- **بلاغ سلامة غير طارئ** `POST /safety/reports`: خلال `Safety:ReportWindowDays` (7) من الرحلة. `category` ∈ `unsafe_driving`, `harassment`, `vehicle_mismatch`, `driver_mismatch`, `passenger_misconduct`, `other`. الأولوية: `harassment` → `high`، غير ذلك `medium`. يُنشئ `safety_cases` (`type=safety_report`, `subject_user_id` = الطرف الآخر).
- **المفقودات** `POST /passenger/trips/{id}/lost-items`: رحلة `completed` خلال `Safety:LostItemWindowDays` (7) وإلا `422 lost_item_window_closed`. ينشئ التقرير + تذكرة دعم `type=lost_item` مرتبطة (F18، `11`) + إشعار `lost_item.reported` للسائق. رد السائق (`found`/`not_found`) → الحالة `found`/`not_found` + إشعار `lost_item.update` للراكب + رسالة نظامية في التذكرة. العمليات تكمل: `driver_contacted` → `returned` → `closed`.

## F12.7 الـAPI

### عام (بدون مصادقة) — للموقع
- GET `/public/trip-shares/{token}` →
```json
{ "status": "in_trip", "passengerFirstName": "سارة",
  "driver": { "firstName": "محمد", "ratingAvg": 4.92, "photoUrl": "/api/v1/public/trip-shares/{token}/driver-photo" },
  "vehicle": { "make": "Toyota", "model": "Camry", "color": "أبيض", "plateNumber": "أ ب ج 2841" },
  "rideCategory": { "code": "economy", "name": "اقتصادي" },
  "pickup": { "name": "…", "lat": 24.7136, "lng": 46.6753 }, "dropoff": { … }, "stops": [ … ],
  "driverLocation": { "lat": 24.72, "lng": 46.68, "heading": 90, "updatedAt": "…" } | null,
  "route": { "planned": [[24.7136, 46.6753], …], "travelled": [[…], …] },
  "etaSeconds": 540, "etaTarget": "pickup" | "dropoff" | null,
  "timeline": { "assignedAt": "…", "arrivedAt": null, "startedAt": "…", "completedAt": null, "cancelledAt": null },
  "expiresAt": null, "refreshSeconds": 10 }
```
  `status` هو حالة الرحلة كما في F8 (`pin_verified` يُعرض كـ`waiting`). `route.travelled` من `driver_location_history` منذ `started_at` مخفَّضة إلى ≤ 300 نقطة. الأخطاء: `404 share_not_found`, `410 share_expired`, `429 rate_limited`.
- GET `/public/trip-shares/{token}/driver-photo` → صورة السائق (نفس صلاحية الرابط) أو `404`.

### الراكب والسائق `/safety` (مصادق، `Policies.Authenticated`)
- GET `/safety/trusted-contacts` → `[ { "id", "name", "phoneNumber", "relationship", "autoShare", "notifyOnSos", "createdAt" } ]`
- POST `/safety/trusted-contacts` `{ "name", "phoneNumber", "relationship"?, "autoShare": false, "notifyOnSos": true }` → `201`؛ `422 trusted_contacts_limit`، `409 trusted_contact_exists`، `400 phone_invalid`، رقم المستخدم نفسه → `422 validation_failed { phoneNumber: "self" }`.
- PUT `/safety/trusted-contacts/{id}` (نفس الحقول) → `200`؛ DELETE → `204`.
- POST `/safety/trips/{tripId}/shares` `{ "channel": "link" | "sms", "contactIds": ["uuid"]? }` → `201 { "shares": [ { "id", "url": "https://ata.sa/t/Xy…", "channel", "trustedContactId", "expiresAt": null } ] }`
- GET `/safety/trips/{tripId}/shares` → `[ { "id", "url", "channel", "trustedContactName", "viewCount", "expiresAt", "revokedAt", "createdAt" } ]`؛ DELETE `/safety/shares/{id}` → `204`.
- POST `/safety/sos` `{ "tripId"?: "uuid", "role"?: "passenger|driver", "lat": 24.7, "lng": 46.6, "accuracy"?: 12, "note"?: "…", "notifyTrustedContacts": true }` → `201` (أو `200` عند التكرار)
```json
{ "caseId": "uuid", "caseNumber": "SC-20260928-0007", "status": "open", "emergencyNumber": "911", "contactsNotified": 2 }
```
- POST `/safety/sos/{caseId}/location` `{ "lat", "lng", "accuracy"? }` → `204` (المُبلِّغ فقط، الحالة غير `resolved`).
- POST `/safety/sos/{caseId}/cancel` `{ "reason": "accidental" | "resolved" }` → `200 SafetyCaseSummary`.
- POST `/safety/reports` `{ "tripId", "category", "description", "fileIds"?: [] }` → `201 SafetyCaseSummary`؛ `422 validation_failed` خارج النافذة (`{ tripId: "window_closed" }`).
- GET `/safety/cases?page=` → صفحة `SafetyCaseSummary` `{ "id", "caseNumber", "type", "status", "priority", "tripId", "tripNumber", "openedAt", "resolvedAt", "publicNotes": [ { "body", "createdAt" } ] }` (فقط ما أنشأه المستخدم؛ الملاحظات غير الداخلية).
- GET `/safety/cases/{id}` → نفس الشكل.
- GET `/safety/alerts/pending` → `SafetyAlert | null` `{ "id", "tripId", "type", "status", "detectedAt", "respondBy" }`
- POST `/safety/alerts/{id}/respond` `{ "response": "ok" | "need_help", "lat"?, "lng"? }` → `200 SafetyAlert`؛ `409 conflict` إن لم يكن `pending_rider`.

### المحادثة والاتصال (داخل مجموعات الرحلة القائمة)
- GET `/passenger/trips/{id}/messages?after={messageId}` و`/driver/trips/{id}/messages?after=` → `[ TripMessage ]`
```json
{ "id": "uuid", "tripId": "uuid", "senderRole": "driver", "kind": "quick_reply", "body": "وصلت إلى نقطة الالتقاط", "quickReplyCode": "arrived", "isMine": false, "readAt": null, "createdAt": "…" }
```
- POST `/passenger/trips/{id}/messages` و`/driver/trips/{id}/messages` `{ "body"?: "…", "quickReplyCode"?: "arrived" }` (أحدهما) → `201 TripMessage`؛ `409 chat_closed`.
- POST `…/messages/read` `{ "upToId": "uuid" }` → `204`.
- POST `/passenger/trips/{id}/call` و`/driver/trips/{id}/call` → `200 { "mode": "proxy" | "unavailable", "proxyNumber"?, "pin"?, "expiresAt"? }`.
- GET `/catalog/chat-quick-replies?role=driver|passenger` → `[ { "code", "text" } ]`.

### المفقودات
- POST `/passenger/trips/{id}/lost-items` `{ "itemCategory": "phone", "description": "…", "contactPhone"?: "05…" }` → `201 LostItemReport`
```json
{ "id", "reportNumber": "LI-20260928-0003", "tripId", "tripNumber", "itemCategory", "description", "status": "open",
  "driverResponse": null, "supportTicketId": "uuid", "createdAt" }
```
- GET `/passenger/lost-items?page=` → صفحة `LostItemReport`.
- GET `/driver/lost-items?status=open&page=` → صفحة `{ id, reportNumber, tripNumber, itemCategory, description, status, createdAt }` (بدون جوال الراكب).
- POST `/driver/lost-items/{id}/respond` `{ "found": true, "note"?: "…" }` → `200`؛ `409 conflict` إن سبق الرد.

### الإدارة (`safety.manage`)
| المسار | الوصف |
|---|---|
| GET `/admin/safety/summary` | `{ "open": { "critical": 1, "high": 3, "medium": 5, "low": 0 }, "unassigned": 2, "avgFirstResponseSeconds": 95, "pendingAlerts": 1, "onDutyAgents": 3 }` |
| GET `/admin/safety/cases?status=&priority=&type=&assignedTo=me|unassigned|{userId}&from=&to=&search=&page=` | `{ id, caseNumber, type, source, priority, status, tripNumber, reporterName, reporterRole, assignedToName, openedAt, firstResponseAt, ageSeconds }` |
| GET `/admin/safety/cases/{id}` | الحالة كاملة + `trip` (ملخص + الأطراف بالجوال الكامل) + `liveLocation` + `alerts[]` + `notes[]` + `attachments[]` + `sharesCount` + `trustedContactsNotified` |
| POST `/admin/safety/cases/{id}/assign` `{ "userId": "uuid" | null }` (null = نفسي) | يضبط `first_response_at` إن كان فارغاً؛ `in_progress` |
| POST `/admin/safety/cases/{id}/status` `{ "status": "in_progress|escalated", "escalatedTo"?, "note"? }` | |
| POST `/admin/safety/cases/{id}/notes` `{ "body", "kind": "note|contact_attempt", "isInternal": true }` | ملاحظة غير داخلية → `safety.case_update` للمُبلِّغ |
| POST `/admin/safety/cases/{id}/resolve` `{ "resolutionCode", "resolution" }` | `resolved` + إشعار المُبلِّغ |
| POST `/admin/safety/cases` `{ "tripId"?, "type": "safety_report", "priority", "description", "subjectUserId"? }` | إنشاء يدوي (مكالمة هاتفية) |
| GET `/admin/safety/alerts?status=&type=&from=&to=&page=`؛ POST `/admin/safety/alerts/{id}/dismiss` `{ "note" }` | |
| GET `/admin/trips/{id}/messages` | قراءة المحادثة (تُسجَّل القراءة في `audit_logs` كـ`trip_messages.view`) |
| GET `/admin/lost-items?status=&page=`؛ PATCH `/admin/lost-items/{id}` `{ "status", "note"? }` | صلاحية `safety.manage` أو `support.manage` |
| GET `/admin/users/{userId}/trusted-contacts` | للقراءة أثناء حالة مفتوحة فقط، مُسجَّل في audit |

أول إجراء إداري على الحالة (تعيين، ملاحظة، تغيير حالة) يضبط `first_response_at`. Audit: `safety_case.assign|status|note|resolve|create`, `safety_alert.dismiss`, `lost_item.update`.

### أكواد الأخطاء (F12)
| الكود | HTTP | ar |
|---|---|---|
| `share_not_found` | 404 | رابط التتبع غير موجود |
| `share_expired` | 410 | انتهت صلاحية رابط التتبع |
| `trusted_contacts_limit` | 422 | الحد الأقصى 5 جهات موثوقة |
| `trusted_contact_exists` | 409 | الجهة مضافة مسبقاً |
| `chat_closed` | 409 | المحادثة مغلقة لهذه الرحلة |
| `lost_item_window_closed` | 422 | انتهت مدة الإبلاغ عن المفقودات |

## F12.8 SignalR (`/hubs/trips`)
- للطرفين: `TripMessage(TripMessage)`، `TripMessagesRead({ tripId, upToId })`.
- للراكب: `SafetyCheck({ alertId, tripId, type, respondBy })`.
- لمجموعة `admins`: `SafetyCaseOpened(caseSummary)`، `SafetyCaseUpdated(caseSummary)` (يتضمن `lastLat/lastLng`)، `SafetyAlertRaised(alert)`.

## F12.9 المهام والإعدادات

| المهمة | الجدولة | العمل |
|---|---|---|
| `SafetyMonitorJob` | كل 30 ث | الكشف §F12.5 |
| `SafetyCheckTimeoutJob` | كل 15 ث | تنبيهات `pending_rider` تجاوزت `respond_by` → `no_response` + حالة |
| `TripShareExpiryJob` | كل دقيقة | ضبط `expires_at` لروابط الرحلات المنتهية (احتياطي إن فات الحدث) |

| المفتاح | الافتراضي |
|---|---|
| `Safety:ShareBaseUrl` / `Safety:ShareExpiryMinutesAfterEnd` / `Safety:MaxSharesPerTrip` | `https://ata.sa` / 30 / 10 |
| `Safety:AutoShareSmsEnabled` / `Safety:PublicShareRatePerMinute` | `true` / 30 |
| `Safety:EmergencyNumber` / `Safety:SosDedupMinutes` / `Safety:SosLocationIntervalSeconds` | `911` / 10 / 10 |
| `Safety:OpsHotlinePhones` | `[]` |
| `Safety:ChatMaskPhoneNumbers` | `true` |
| `Safety:MonitorIntervalSeconds` | 30 |
| `Safety:StopSpeedMps` / `Safety:StopMinutes` / `Safety:StopIgnoreRadiusMeters` | 1.5 / 5 / 300 |
| `Safety:DeviationMeters` / `Safety:DeviationMetersMaps` / `Safety:DeviationSeconds` | 2000 / 500 / 120 |
| `Safety:OverrunFactor` / `Safety:OverrunMinMinutes` | 2.0 / 15 |
| `Safety:AlertCooldownMinutes` / `Safety:CheckResponseSeconds` | 10 / 120 |
| `Safety:ReportWindowDays` / `Safety:LostItemWindowDays` | 7 / 7 |
| `CallMasking:Provider` | `none` |

---

# F14 — محرك الإلغاء والموثوقية

## F14.1 مراحل الإلغاء

| المرحلة `stage` | حالة الرحلة | نقطة الإسناد (`anchor`) |
|---|---|---|
| `before_accept` | `requested`, `searching` | `requested_at` |
| `after_accept` | `driver_assigned` | `assigned_at` |
| `en_route` | `driver_en_route` | `assigned_at` |
| `arrived` | `waiting`/`pin_verified` و`now − arrived_at ≤ الانتظار المجاني` | `arrived_at` |
| `waiting` | `waiting`/`pin_verified` بعد انتهاء الانتظار المجاني | `arrived_at` |
| `no_show` | عبر نقطة "لم يحضر الراكب" فقط | `arrived_at` |
| `scheduled` | `scheduled` (F17) — تقيّمه `scheduled_ride_rules` في `11`، لا `cancellation_rules` | `scheduled_at` |

الانتظار المجاني = `pricing_rules.free_waiting_minutes` للرحلة (أو `Trips:FreeWaitingMinutes`). `in_trip` لا يُلغى (قائم).

## F14.2 الجداول

| جدول | الأعمدة |
|---|---|
| `cancellation_reasons` | id, code VARCHAR(60) UNIQUE, actor (`passenger`/`driver`/`system`)، name_ar, name_en, stages JSON NULL (المراحل التي يظهر فيها؛ null = كلها)، is_excusable BOOL (يحتاج مراجعة العمليات ليُعفى)، is_emergency BOOL (يُعامل كقابل للإعذار + ينشئ حالة سلامة `medium`)، requires_note BOOL, is_selectable BOOL (false لأسباب النظام مثل `passenger_no_show`)، sort_order, is_active, created_at, updated_at |
| `cancellation_rules` | id, name, actor (`passenger`/`driver`)، stage (`before_accept`/`after_accept`/`en_route`/`arrived`/`waiting`/`no_show`)، booking_type NULL (`now`/`scheduled`)، ride_category_id NULL, zone_id NULL (منطقة الالتقاط)، free_window_seconds INT DEFAULT 0, fee_type (`none`/`fixed`/`percent`/`pricing_rule`)، fee_amount DECIMAL(12,2) NULL, fee_percent DECIMAL(5,2) NULL (من `estimated_fare`)، min_fee NULL, max_fee NULL, driver_compensation_percent DECIMAL(5,2) DEFAULT 0, penalty_points INT DEFAULT 0, priority INT DEFAULT 0, is_active, created_at, updated_at — INDEX(actor, stage, is_active) |
| `cancellation_events` | id, trip_id (FK UNIQUE)، actor (`passenger`/`driver`/`system`/`admin`)، user_id NULL, at_fault (`passenger`/`driver`/`none`)، stage (يشمل `scheduled`)، booking_type, reason_id NULL, reason_code VARCHAR(60), note VARCHAR(500) NULL, rule_id NULL, seconds_since_accept INT NULL, seconds_since_arrival INT NULL, estimated_fare, fee_amount (المحسوب)، fee_charged (المحصّل فعلاً)، fee_status (`none`/`charged`/`pending_review`/`waived`/`failed`/`refunded`)، fee_method NULL (`wallet`/`card`/`corporate`)، compensation_amount, penalty_points, counts_toward_rate BOOL, excuse_status (`not_applicable`/`pending`/`approved`/`rejected`)، reviewed_by NULL, reviewed_at NULL, review_note NULL, created_at — INDEX(user_id, created_at), INDEX(excuse_status) |
| `reliability_profiles` | id, user_id, role (`passenger`/`driver`)، window_days, trips_requested, offers_received (سائق)، offers_accepted (سائق)، trips_accepted, trips_completed, cancellations_at_fault, no_show_count, cancellation_rate DECIMAL(5,4), acceptance_rate DECIMAL(5,4) NULL, reliability_rate DECIMAL(5,4) (= completed / accepted)، penalty_points INT, restriction_level (`none`/`warning`/`matching_deprioritized`/`incentives_reduced`/`temporarily_restricted`/`suspended`)، restricted_until NULL, level_changed_at NULL, last_computed_at, created_at, updated_at — UNIQUE(user_id, role), INDEX(role, restriction_level) |
| `reliability_thresholds` | id, role, level (نفس القيم عدا `none`)، min_penalty_points INT NULL, min_cancellation_rate DECIMAL(5,4) NULL, min_trips_for_rate INT DEFAULT 10, restriction_hours INT NULL (لـ`temporarily_restricted`)، deprioritize_factor DECIMAL(4,2) NULL (مضاعف درجة المطابقة، مثل 0.70)، incentive_reduction_percent DECIMAL(5,2) NULL، sort_order (الأعلى = الأشد)، is_active, created_at, updated_at — UNIQUE(role, level) |
| `reliability_adjustments` | id, user_id, role, action (`add_points`/`remove_points`/`set_level`/`clear_restriction`)، points INT NULL (موقّع)، level NULL, until NULL, reason VARCHAR(500), created_by, created_at |

`trips.cancellation_reason` يبقى ويحمل `reason_code`. `pricing_rules.cancellation_fee` (F10) يُستخدم عبر `fee_type = pricing_rule`.

## F14.3 القواعد

### اختيار القاعدة
المرشحون: `is_active` و`actor` و`stage` مطابقان، و`booking_type`/`ride_category_id`/`zone_id` إما مطابقة أو NULL. الترتيب: عدد الحقول المطابقة غير الفارغة (الأكثر تحديداً أولاً) ثم `priority` تنازلياً ثم `created_at` الأحدث. لا قاعدة → رسوم 0، نقاط 0.

### الحساب
```
elapsed = now − anchor(stage)
if elapsed < rule.free_window_seconds:  fee = 0, points = 0
else:
  fee = none → 0 | fixed → fee_amount | percent → estimated_fare × fee_percent / 100 | pricing_rule → pricing_rules.cancellation_fee
  fee = clamp(fee, min_fee, max_fee); fee = min(fee, estimated_fare); fee = round(fee, 2)
  points = rule.penalty_points
compensation = (actor = passenger و يوجد سائق) ? round(fee_charged × driver_compensation_percent / 100, 2) : 0
```
قيود التحقق عند حفظ القاعدة (`422 validation_failed`): `actor=passenger` و`stage=before_accept` ⇒ `fee_type=none` (**لا رسوم على الراكب قبل قبول السائق**)؛ `fee_type=fixed` يتطلب `fee_amount`، و`percent` يتطلب `fee_percent` (0–100)؛ `stage=no_show` لـ`actor=passenger` فقط.

### من المخطئ ومن يُحتسب عليه
| الحالة | `at_fault` | `counts_toward_rate` |
|---|---|---|
| إلغاء الراكب `before_accept` | `none` | false |
| إلغاء الراكب داخل النافذة المجانية | `none` | false |
| إلغاء الراكب بعد النافذة | `passenger` | true |
| إلغاء السائق بعد القبول (أي مرحلة) | `driver` | true (حتى داخل النافذة المجانية؛ النافذة تلغي النقاط والرسوم فقط) |
| عدم حضور الراكب | `passenger` | true (+ `no_show_count`) |
| النظام (`no_drivers`, `payment_failed`, `scheduled_driver_unavailable`) | `none` | false |
| الإدارة | حسب اختيار المسؤول (`atFault`)، افتراضي `none` | حسب `atFault` |

سبب `is_excusable` أو `is_emergency`: `fee_status = pending_review`، `excuse_status = pending`، لا رسوم ولا نقاط حتى المراجعة. الاعتماد → `waived`، `at_fault = none`، `counts_toward_rate = false`. الرفض → تُحصَّل الرسوم الآن (نفس خوارزمية التحصيل) وتُطبق النقاط والاحتساب. `is_emergency` ينشئ أيضاً `safety_cases` (`type=safety_report`, `priority=medium`, `source=report`).

### تحصيل الرسوم (عبر F11)
1. رحلة `card` بتفويض قائم → `CaptureAsync(fee)` (`fee_method=card`، قيد `cancellation_fee_card`).
2. رحلة `corporate` → سطر `cancellation_fee` في فاتورة الشركة (`fee_method=corporate`، قيد `trip_corporate_charge` بمرجع الإلغاء).
3. غير ذلك (نقد/محفظة/فشل البطاقة) → حركة `cancellation_fee` على محفظة الراكب بـ`allowOverdraft=true` (`fee_method=wallet`)؛ الرصيد السالب يمنع الطلب التالي (`outstanding_balance`، `08` §F11.4).
4. التعويض → حركة `cancellation_compensation` لمحفظة السائق من `cancellation_fees`.
5. رسوم على السائق (إن ضُبطت في قاعدة `actor=driver`) → حركة `cancellation_fee` على محفظة السائق (overdraft).
6. إشعارات: `cancellation.fee_charged` للراكب، `cancellation.compensation` للسائق، و`trip.cancelled` للطرف الآخر (قائم).
7. `fee_charged` يُعاد كاملاً باسترداد F11 (`reason_code = cancellation_fee_waived`) → `fee_status = refunded`.

### عدم حضور الراكب (No-show)
`POST /driver/trips/{id}/no-show`: الحالة `waiting`، و`now − arrived_at ≥ Cancellation:NoShowWaitMinutes` (5، ويجب ≥ الانتظار المجاني) وإلا `422 no_show_too_early { secondsRemaining }`، والسائق ضمن `Trips:ArrivalRadiusMeters` من الالتقاط (وإلا حدث تحذير `arrival_distance_warning` فقط). النتيجة: إلغاء بـ`cancelled_by = driver`، `reason_code = passenger_no_show`، `stage = no_show`، `at_fault = passenger`، رسوم حسب قاعدة `no_show` + تعويض السائق، حدث رحلة `passenger_no_show`.

### ملفات الموثوقية
- التحديث: تزايدي عند كل `cancellation_events`، وإكمال رحلة، وقبول/رفض عرض؛ وكامل ليلياً (`ReliabilityRecalcJob`).
- النافذة: آخر `Reliability:WindowDays` (30) يوماً؛ النقاط: مجموع `penalty_points` للأحداث غير المعفاة خلال `Reliability:PointsExpiryDays` (30) + مجموع تعديلات `add_points`/`remove_points` في نفس المدة (لا تقل عن 0).
- المعدلات:
  - السائق: `trips_accepted` = رحلات أُسندت إليه؛ `cancellation_rate = cancellations_at_fault / trips_accepted`؛ `acceptance_rate = offers_accepted / offers_received` (من `trip_offers`)؛ `reliability_rate = trips_completed / trips_accepted`.
  - الراكب: `trips_accepted` = رحلاته التي أُسند لها سائق؛ `cancellation_rate = cancellations_at_fault / trips_accepted`؛ `reliability_rate = trips_completed / trips_accepted`.
  - عند `trips_accepted = 0`: المعدلات 0 والموثوقية 1.
- المستوى: تجاوز العتبات النشطة للدور مرتبة بـ`sort_order` تنازلياً؛ أول عتبة يتحقق فيها `penalty_points ≥ min_penalty_points` **أو** (`trips_accepted ≥ min_trips_for_rate` **و** `cancellation_rate ≥ min_cancellation_rate`) تحدد المستوى. تعديل `set_level` ساري (`until` في المستقبل أو NULL) يتقدم على الحساب؛ `clear_restriction` يلغي أي تقييد ساري ويصفّر النقاط.
- `temporarily_restricted`: عند الدخول إليه `restricted_until = now + restriction_hours` ولا يُرفع قبل ذلك حتى لو انخفضت النقاط. `suspended`: بلا انتهاء حتى تتدخل العمليات (لا يغيّر `drivers.application_status`).
- تغيّر المستوى إلى `warning` أو أعلى → `reliability.warning`؛ إلى `temporarily_restricted`/`suspended` → `reliability.restricted`. + `audit_logs` (`reliability.level_change`, actor system).

| المستوى | أثره على السائق | أثره على الراكب |
|---|---|---|
| `none` | — | — |
| `warning` | إشعار فقط | إشعار فقط |
| `matching_deprioritized` | درجة F9 × `deprioritize_factor` | = `warning` |
| `incentives_reduced` | ما سبق + المكافآت × (1 − `incentive_reduction_percent`/100) | = `warning` |
| `temporarily_restricted` | مستبعد من المطابقة؛ `PUT /driver/status {isOnline:true}` → `403 account_restricted`؛ يُفصل إن كان متصلاً بلا رحلة | `POST /passenger/trips` → `403 account_restricted` |
| `suspended` | مثل السابق بلا انتهاء + تنبيه للعمليات | مثل السابق بلا انتهاء |

`details` لـ`account_restricted`: `{ "level": "temporarily_restricted", "restrictedUntil": "…" | null }`.

### نقاط الربط مع الميزات الأخرى
```csharp
public interface IReliabilityService
{
    Task<ReliabilitySnapshot> GetAsync(Guid userId, Role role, CancellationToken ct);
    decimal MatchingFactor(ReliabilitySnapshot s);      // 1.0 أو deprioritize_factor — يستهلكه ScoringMatcher (F9)
    bool IsRestricted(ReliabilitySnapshot s);           // شرط أهلية F9 + منع الطلب/الاتصال
    decimal IncentiveMultiplier(ReliabilitySnapshot s); // 1 − reduction% — يستهلكه F15 عند صرف الحوافز
}
```
- F9: `norm_cancellation = 1 − reliability_profiles.cancellation_rate` (سائق)، والدرجة النهائية × `MatchingFactor`. السائق المقيّد غير مؤهل (الشرط "غير محظور مؤقتاً" في `07`).
- F8: نقاط الإلغاء القائمة تستدعي `CancellationEngine.CancelAsync(trip, actor, reasonCode, note, userId)` بدلاً من `trip.Cancel` مباشرة. `reasonCode` يجب أن يطابق سبباً نشطاً وقابلاً للاختيار للفاعل والمرحلة، وإلا `422 cancellation_reason_invalid`. `requires_note` بلا ملاحظة → `422 validation_failed { note: "required" }`.
- قيم Flutter الحالية (`changed_mind`, `driver_late`, `wrong_pickup`, `other`) موجودة في البذور (`driver_late` مرادف مُبقى لـ`driver_too_far`/التأخر).

## F14.4 الـAPI

### عام
- GET `/catalog/cancellation-reasons?actor=passenger|driver&stage=` → `[ { "code", "name", "requiresNote", "isExcusable", "isEmergency", "stages": ["after_accept", …] | null } ]` (القابلة للاختيار والنشطة فقط).

### الراكب
- POST `/passenger/trips/{id}/cancel/preview` `{ "reasonCode"?: "changed_mind" }` →
```json
{ "stage": "en_route", "bookingType": "now", "fee": 10.00, "isFree": false, "freeUntil": null,
  "requiresReview": false, "message": "سيتم خصم 10.00 ر.س رسوم إلغاء لأن الكابتن في الطريق إليك" }
```
  `freeUntil` = `anchor + free_window_seconds` إن كان في المستقبل. `requiresReview = true` لسبب قابل للإعذار (`fee` يُعرض كقيمة محتملة).
- POST `/passenger/trips/{id}/cancel` `{ "reasonCode": "changed_mind", "note"?: "…", "expectedFee"?: 10.00 }` → `Trip`. إن أُرسل `expectedFee` وكانت الرسوم الفعلية أعلى منه → `409 cancellation_fee_changed { fee }` ولا يُلغى. الأخطاء أيضاً: `cancellation_reason_invalid`, `validation_failed`, `conflict`.
- GET `/passenger/reliability` → `ReliabilitySummary`
```json
{ "role": "passenger", "level": "warning", "restrictedUntil": null, "windowDays": 30,
  "tripsAccepted": 22, "tripsCompleted": 18, "cancellationsAtFault": 4, "cancellationRate": 0.18, "reliabilityRate": 0.82,
  "noShowCount": 1, "penaltyPoints": 6,
  "nextLevel": { "level": "temporarily_restricted", "minPenaltyPoints": 12, "minCancellationRate": 0.35 } | null,
  "recentEvents": [ { "tripId", "tripNumber", "stage", "reasonName", "feeCharged", "penaltyPoints", "excuseStatus", "createdAt" } ] }
```

### السائق
- POST `/driver/trips/{id}/cancel/preview` `{ "reasonCode"? }` → `{ "stage", "fee": 0, "penaltyPoints": 3, "isFree": false, "freeUntil": null, "requiresReview": false, "message": "سيؤثر هذا الإلغاء على نسبة موثوقيتك (+3 نقاط)" }`
- POST `/driver/trips/{id}/cancel` `{ "reasonCode", "note"?, "expectedPenaltyPoints"? }` → `Trip` (نفس قواعد الراكب).
- POST `/driver/trips/{id}/no-show` `{ "lat", "lng" }` → `Trip`؛ `422 no_show_too_early`.
- GET `/driver/reliability` → `ReliabilitySummary` (+ `offersReceived`, `offersAccepted`, `acceptanceRate`، و`effects`: `{ "matchingFactor": 0.7, "incentiveMultiplier": 1.0 }`).

`Trip` يضاف إليه:
```json
"cancellation": { "stage": "en_route", "reasonCode": "changed_mind", "reasonName": "غيرت رأيي", "atFault": "passenger",
                  "fee": 10.00, "feeCharged": 10.00, "feeStatus": "charged", "excuseStatus": "not_applicable" } | null
```
(للسائق لا يظهر `fee` الراكب؛ يظهر `compensation`.)

### الإدارة
| المسار | الصلاحية |
|---|---|
| GET/POST `/admin/cancellation-reasons`، PUT/DELETE `/admin/cancellation-reasons/{id}` (الحقول: `code, actor, nameAr, nameEn, stages, isExcusable, isEmergency, requiresNote, isSelectable, sortOrder, isActive`؛ الحذف = تعطيل إن كان مستخدماً) | `cancellation.manage` |
| GET/POST `/admin/cancellation-rules`، PUT/DELETE `/admin/cancellation-rules/{id}` (الحقول: `name, actor, stage, bookingType, rideCategoryId, zoneId, freeWindowSeconds, feeType, feeAmount, feePercent, minFee, maxFee, driverCompensationPercent, penaltyPoints, priority, isActive`) | `cancellation.manage` |
| POST `/admin/cancellation-rules/simulate` `{ actor, stage, bookingType, rideCategoryId?, zoneId?, secondsSinceAnchor, estimatedFare }` → `{ ruleId, ruleName, fee, compensation, penaltyPoints, isFree }` | `cancellation.manage` |
| GET `/admin/reliability-thresholds?role=`، PUT `/admin/reliability-thresholds/{id}` (`minPenaltyPoints, minCancellationRate, minTripsForRate, restrictionHours, deprioritizeFactor, incentiveReductionPercent, sortOrder, isActive`) | `cancellation.manage` |
| GET `/admin/cancellations?actor=&stage=&atFault=&feeStatus=&excuseStatus=&from=&to=&search=&page=` → `{ id, tripId, tripNumber, actor, userName, atFault, stage, reasonCode, reasonName, feeAmount, feeCharged, feeStatus, compensationAmount, penaltyPoints, excuseStatus, createdAt }` | `trips.view` |
| GET `/admin/cancellations/excuses?status=pending&page=` (طابور الأعذار مع `ageHours` و`slaBreached` بعد `Cancellation:ExcuseReviewSlaHours`) | `cancellation.review` |
| POST `/admin/cancellations/{eventId}/review` `{ "decision": "approve" | "reject", "note" }` → الحدث المحدّث؛ `409 conflict` إن لم يكن `pending` | `cancellation.review` |
| GET `/admin/reliability-profiles?role=&level=&search=&page=` → `{ userId, name, phone, role, level, restrictedUntil, cancellationRate, reliabilityRate, penaltyPoints, noShowCount, tripsAccepted, lastComputedAt }` | `reliability.manage` |
| GET `/admin/reliability-profiles/{userId}?role=` → الملف + `events[]` (60 يوماً) + `adjustments[]` | `reliability.manage` |
| POST `/admin/reliability-profiles/{userId}/adjust` `{ role, action, points?, level?, until?, reason }` → الملف بعد إعادة الحساب | `reliability.manage` |
| GET `/admin/cancellations/stats?from=&to=&cityId=&zoneId=&rideCategoryId=` → KPIs §F14.7 | `reports.view` |
| POST `/admin/trips/{id}/cancel` (قائم) يقبل الآن `{ "reason", "atFault"?: "none|passenger|driver", "chargeFee"?: false }` | `trips.cancel` |

Audit: `cancellation_reason.create|update|delete`, `cancellation_rule.create|update|delete`, `reliability_threshold.update`, `cancellation.review`, `reliability.adjust`.

### أكواد الأخطاء (F14)
| الكود | HTTP | ar |
|---|---|---|
| `cancellation_reason_invalid` | 422 | سبب الإلغاء غير صالح |
| `cancellation_fee_changed` | 409 | تغيّرت رسوم الإلغاء، راجعها وأعد المحاولة |
| `no_show_too_early` | 422 | لم تنتهِ مدة الانتظار المطلوبة بعد |
| `account_restricted` | 403 | حسابك مقيّد مؤقتاً بسبب تكرار الإلغاء |

## F14.5 المهام والإعدادات

| المهمة | الجدولة | العمل |
|---|---|---|
| `ReliabilityRecalcJob` | يومياً 02:00 الرياض | إعادة حساب كل الملفات النشطة (لهم نشاط خلال النافذة أو تقييد ساري) + انتهاء `restricted_until` |
| `RestrictionExpiryJob` | كل 5 د | رفع التقييدات المنتهية وإشعار المستخدم |

| المفتاح | الافتراضي |
|---|---|
| `Cancellation:NoShowWaitMinutes` | 5 |
| `Cancellation:ExcuseReviewSlaHours` | 48 |
| `Reliability:WindowDays` / `Reliability:PointsExpiryDays` | 30 / 30 |

## F14.6 البذور (Seed)

أسباب الراكب: `changed_mind` (غيرت رأيي)، `driver_late` (الكابتن تأخر)، `driver_too_far` (الكابتن بعيد)، `wrong_pickup` (موقع الالتقاط خاطئ)، `found_other_ride` (وجدت وسيلة أخرى)، `driver_asked_to_cancel` (الكابتن طلب الإلغاء — `is_excusable`)، `driver_not_moving` (الكابتن لا يتحرك — `is_excusable`)، `safety_concern` (قلق على السلامة — `is_emergency`)، `other` (`requires_note`).
أسباب السائق: `passenger_not_responding`، `passenger_asked_to_cancel` (`is_excusable`)، `wrong_pickup_location`، `pickup_too_far`، `vehicle_issue` (`is_excusable`)، `safety_concern` (`is_emergency`)، `other` (`requires_note`)، `passenger_no_show` (`is_selectable=false`).
أسباب النظام (`actor=system`، غير قابلة للاختيار): `no_drivers`، `payment_failed`، `admin_cancelled`، `scheduled_driver_unavailable`.

قواعد (المدينة كلها، `booking_type` NULL):

| actor | stage | free_window_seconds | الرسوم | تعويض السائق | نقاط |
|---|---|---|---|---|---|
| passenger | before_accept | 0 | none | 0 | 0 |
| passenger | after_accept | 120 | fixed 5 | 50% | 1 |
| passenger | en_route | 120 | fixed 10 | 70% | 2 |
| passenger | arrived | 0 | pricing_rule | 80% | 2 |
| passenger | waiting | 0 | pricing_rule (min 10) | 80% | 3 |
| passenger | no_show | 0 | pricing_rule (min 10) | 80% | 4 |
| driver | after_accept | 60 | none | — | 2 |
| driver | en_route | 0 | none | — | 3 |
| driver | arrived | 0 | none | — | 4 |
| driver | waiting | 0 | none | — | 2 |

العتبات:

| role | level | نقاط ≥ | معدل ≥ (عينة ≥ 10) | إضافي |
|---|---|---|---|---|
| driver | warning | 4 | 0.10 | |
| driver | matching_deprioritized | 8 | 0.15 | factor 0.70 |
| driver | incentives_reduced | 12 | 0.20 | factor 0.60، reduction 50% |
| driver | temporarily_restricted | 18 | 0.30 | 24 ساعة |
| driver | suspended | 30 | 0.45 | |
| passenger | warning | 4 | 0.15 | |
| passenger | temporarily_restricted | 12 | 0.35 | 24 ساعة |
| passenger | suspended | 25 | 0.50 | |

## F14.7 مؤشرات الأداء
| KPI | التعريف |
|---|---|
| Passenger Cancellation Rate | إلغاءات الراكب `at_fault=passenger` ÷ رحلات أُسند لها سائق |
| Driver Cancellation Rate | إلغاءات السائق `at_fault=driver` ÷ رحلات أُسندت لسائقين |
| Cancellation Fee Revenue | Σ `fee_charged` − Σ المسترد منها (`fee_status=refunded`) |
| Repeat Cancellation Rate | المستخدمون الذين لديهم ≥ 2 إلغاء مخطئ في الفترة ÷ المستخدمون الذين لديهم ≥ 1 |
| Driver / Passenger Reliability Rate | Σ `trips_completed` ÷ Σ `trips_accepted` للدور خلال الفترة |
| Excuse Approval Rate | الأعذار المعتمدة ÷ المراجَعة |
| No-show Rate | إلغاءات `no_show` ÷ الرحلات التي وصل فيها السائق |
(الحساب اليومي في `report_snapshots` — `12` §F20.)

---

## Flutter

**feature `safety`** (توسعة الصفحة القائمة `/safety`):
- entities: `TrustedContact`, `TripShare`, `SafetyCaseSummary`, `SafetyAlert`, `SosResult`, `LostItemReport`.
- usecases: `GetTrustedContacts`, `AddTrustedContact`, `UpdateTrustedContact`, `DeleteTrustedContact`, `CreateTripShare`, `GetTripShares`, `RevokeTripShare`, `TriggerSos`, `SendSosLocation`, `CancelSos`, `SubmitSafetyReport`, `GetMySafetyCases`, `GetPendingSafetyAlert`, `RespondToSafetyAlert`, `ReportLostItem`, `GetMyLostItems`, `GetDriverLostItems`, `RespondToLostItem`.
- cubits: `TrustedContactsCubit` (القائمة، النموذج، الحد 5)، `TripShareCubit` (إنشاء الرابط ومشاركته عبر `share_plus`، الإرسال للجهات)، `SosCubit` (app-wide: ضغط مطوّل 2 ث للتأكيد → `TriggerSos` → بث الموقع كل 10 ث عبر `LocationStreamCubit`/geolocator حتى الإغلاق → زر اتصال `911`)، `SafetyCheckCubit` (app-wide: يستمع لـ`SafetyCheck` وروابط `ata://safety/check/{id}` وأزرار الإشعار `ok`/`help` → حوار "هل أنت بخير؟" بعدّاد حتى `respondBy`)، `SafetyReportCubit`، `SafetyCasesCubit`، `LostItemCubit` (راكب)، `DriverLostItemsCubit`.
- pages/routes: `/safety` (القائمة القائمة: مشاركة الرحلة، جهات موثوقة، بلاغاتي، الطوارئ)، `/safety/contacts`، `/safety/contacts/edit`، `/safety/report?tripId=`، `/safety/cases`، `/safety/cases/:id`، `/safety/check/:alertId` (حوار كامل الشاشة)، `/rides/:tripId/lost-item`، `/driver/lost-items`، `/driver/lost-items/:id`. زر SOS أحمر (`danger`) في ورقة الرحلة النشطة للراكب والسائق.

**feature `trip_chat`** (جديد):
- entities: `TripMessage`, `QuickReply`، usecases: `GetTripMessages`, `SendTripMessage`, `MarkMessagesRead`, `GetQuickReplies`, `RequestMaskedCall`, `WatchTripMessages` (Hub `TripMessage` + استطلاع احتياطي كل 5 ث).
- cubits: `TripChatCubit` (الرسائل، الإرسال المتفائل، العدّاد غير المقروء لشارة زر المحادثة)، `MaskedCallCubit` (`proxy` → `tel:` للرقم الوسيط؛ `unavailable` → فتح المحادثة).
- pages: `/trip/chat` (راكب)، `/driver/trip/chat` (سائق) — شريط ردود سريعة أفقي.

**feature `trip`** (توسعة F14):
- entities: `CancellationReason` (يحل محل الـenum الثابت `CancelReason`)، `CancelPreview`، `ReliabilitySummary`، `TripCancellation`.
- usecases: `GetCancellationReasons`, `PreviewCancellation`, `CancelTrip` (+`expectedFee`/`expectedPenaltyPoints`)، `MarkPassengerNoShow`, `GetReliability`.
- cubits: `CancelFlowCubit` (يجلب الأسباب للمرحلة → يطلب المعاينة عند اختيار السبب → يعرض الرسوم/النقاط والتحذير → التأكيد؛ يعالج `cancellation_fee_changed` بإعادة المعاينة)، `NoShowCubit` (عدّاد تنازلي حتى تفعيل زر "لم يحضر الراكب" في شاشة انتظار السائق)، `ReliabilityCubit`.
- `CancelReasonSheet` يُعاد بناؤه على `CancelFlowCubit`. صفحات: `/account/reliability` (راكب)، `/driver/reliability` (سائق). رسائل `account_restricted` في `TripRequestCubit` و`OnlineStatusCubit`.

## الموقع — صفحة التتبع العامة

| المسار | الصفحة |
|---|---|
| `/t/:token` | صفحة عامة بلا تسجيل دخول ولا هيدر تنقل (شعار ATA فقط + مفتاح اللغة): خريطة Leaflet/OSM بالمسار المخطط (خط `brand` متقطع) والمسار المقطوع (خط `ink`) وعلامة السائق باتجاهه، بطاقة الحالة (بالعربية: "الكابتن في الطريق" / "وصل الكابتن" / "الرحلة جارية" / "انتهت الرحلة")، ETA، بطاقة السائق (الاسم الأول، الصورة، التقييم) والمركبة (الماركة، الموديل، اللون، اللوحة `dir=ltr`)، الالتقاط والوجهة. استطلاع `GET /public/trip-shares/{token}` كل `refreshSeconds`. `410` → رسالة "انتهت صلاحية رابط التتبع"، `404` → "الرابط غير صحيح". `<meta name="robots" content="noindex">`. |

يضاف `leaflet` إلى الموقع، ومكوّن `TrackingMap`، و`src/lib/publicShare.ts`.

## لوحة الإدارة

| المسار | الصفحة |
|---|---|
| `/safety` | مركز السلامة: بطاقات الملخص، طابور الحالات المفتوحة مرتّب بالأولوية ثم العمر مع مؤقت حيّ وتنبيه صوتي عند `SafetyCaseOpened` (critical)، فلاتر، مفتاح "مناوب" |
| `/safety/cases/:id` | تفاصيل الحالة: خريطة حيّة (آخر موقع + موقع السائق من `LiveSnapshot`)، بيانات الرحلة والأطراف (أزرار اتصال بالأرقام الكاملة)، التنبيهات، الجدول الزمني للملاحظات، الإجراءات (تعيين، تصعيد مع الجهة، ملاحظة/محاولة اتصال، حل برمز الحل)، المحادثة (قراءة مُسجَّلة) |
| `/safety/alerts` | التنبيهات الآلية بفلاتر النوع والحالة، إغلاق يدوي |
| `/lost-items` | تقارير المفقودات وتحديث الحالة |
| `/cancellation/reasons` | إدارة الأسباب (ثنائية اللغة، المراحل، الأعلام) |
| `/cancellation/rules` | جدول القواعد (فلاتر الفاعل/المرحلة/نوع الحجز) + نموذج + محاكي الرسوم |
| `/cancellation/excuses` | طابور مراجعة الأعذار مع مؤشر SLA وأزرار اعتماد/رفض بملاحظة |
| `/cancellation/events` | سجل الإلغاءات بالفلاتر + بطاقات KPIs من `/admin/cancellations/stats` |
| `/reliability` | ملفات الموثوقية (فلتر الدور/المستوى)، تفاصيل مع الأحداث والتعديلات، نموذج تعديل يدوي؛ تبويب "العتبات" لتحرير السلّم |

وفي `/trips/:id`: قسم "الإلغاء" (المرحلة، السبب، الرسوم، التعويض، العذر) وقسم "السلامة" (الروابط، التنبيهات، الحالات) وتبويب "المحادثة".

## سيناريوهات الاختبار (الخلفية)

السلامة:
1. إنشاء رابط مشاركة في `driver_assigned` → القراءة العامة لا تحتوي أي حقل هاتف/PIN/أجرة؛ في `searching` → `409`؛ بعد الإكمال + 30 د (ساعة مزيّفة) → `410`؛ الإلغاء اليدوي → `410`.
2. المشاركة التلقائية: جهة `auto_share` → رابط `auto` + SMS عند الإسناد.
3. SOS: إنشاء حالة `critical` + بث `SafetyCaseOpened` + `safety.alert` للمناوبين فقط + SMS للجهات `notify_on_sos`؛ الضغط المتكرر خلال 10 د يعيد نفس الحالة.
4. جهات موثوقة: السادسة → `422 trusted_contacts_limit`؛ التكرار → `409`.
5. المحادثة: الإرسال قبل الإسناد أو بعد الإكمال → `409 chat_closed`؛ غير الطرفين → `403`؛ إخفاء الأرقام ≥ 8 خانات؛ `TripMessage` يصل للطرف الآخر.
6. `SafetyMonitorJob` ببيانات مواقع مزيّفة: توقف 6 د بعيداً عن النقاط → تنبيه واحد؛ توقف قرب الوجهة → لا شيء؛ انحراف 2.5 كم لـ3 د → تنبيه؛ تجاوز ×2 + 15 د → تنبيه؛ عدم التكرار ضمن مهلة التبريد.
7. دورة "هل أنت بخير؟": `ok` → `resolved_ok`؛ `need_help` → حالة `high`؛ بلا رد → `no_response` + حالة.
8. المفقودات: بعد 8 أيام → `422`؛ الإنشاء يربط تذكرة دعم؛ رد السائق يحدّث الحالة ويشعر الراكب.
9. أول إجراء إداري يضبط `first_response_at` مرة واحدة؛ كل إجراء في `audit_logs`.

الإلغاء والموثوقية:
10. اختيار القاعدة الأكثر تحديداً (منطقة + فئة تتفوق على العامة؛ عند التساوي `priority`).
11. حفظ قاعدة راكب `before_accept` برسوم → `422`.
12. إلغاء الراكب في `searching` → رسوم 0، `counts_toward_rate=false`؛ في `after_accept` داخل 120 ث → مجاني؛ بعدها → 5 ر.س + تعويض 2.50 + نقطة، والقيود متوازنة (`cancellation_fees`).
13. `pricing_rule` يأخذ `pricing_rules.cancellation_fee` مقيّداً بـ`min_fee`؛ الرسوم لا تتجاوز الأجرة التقديرية.
14. رحلة بطاقة → Capture جزئي بالرسوم؛ رحلة نقد برصيد 0 → رصيد سالب ثم `outstanding_balance` عند الطلب التالي.
15. `expectedFee` أقل من الفعلية → `409 cancellation_fee_changed` ولا تغيير في الرحلة.
16. سبب قابل للإعذار → `pending_review` بلا رسوم/نقاط؛ الاعتماد → `waived`؛ الرفض → التحصيل والنقاط؛ `is_emergency` ينشئ حالة سلامة.
17. No-show قبل 5 د → `422 no_show_too_early` مع الثواني المتبقية؛ بعدها → إلغاء `no_show` برسوم وتعويض و`no_show_count`.
18. سبب غير موجود/لفاعل آخر → `422 cancellation_reason_invalid`؛ `other` بلا ملاحظة → `422 validation_failed`.
19. سلّم الموثوقية: تراكم النقاط ينقل السائق عبر المستويات؛ `matching_deprioritized` يخفض درجة المطابقة بالمعامل؛ `temporarily_restricted` يمنع الاتصال (`403 account_restricted`) ويستبعد من المطابقة ويُرفع تلقائياً بعد 24 س؛ الحد الأدنى للعينة يمنع التقييد بالمعدل لسائق بـ3 رحلات.
20. التعديل اليدوي `set_level` مع `until` يتقدم على الحساب ثم ينتهي؛ `clear_restriction` يرفع التقييد؛ كله في `audit_logs`.
21. `IncentiveMultiplier` = 0.5 عند `incentives_reduced`.
22. KPIs `/admin/cancellations/stats` مطابقة لبيانات مُعدّة يدوياً.

## قرارات تحتاج تأكيد مالك المنتج

1. الرابط العام يعرض الاسم الأول للراكب والسائق فقط، وصلاحيته حتى 30 دقيقة بعد انتهاء الرحلة.
2. رقم الطوارئ الافتراضي `911` (الموحد في المملكة) قابل للتعديل.
3. عتبة الانحراف 2000 م عند غياب مسار مزوّد الخرائط (قطع مستقيمة) و500 م مع المسار الحقيقي.
4. المكالمات المقنعة placeholder؛ بدون مزوّد تُخفى المكالمة ويُعتمد على المحادثة، ولا يُكشف الرقم الحقيقي لأي طرف.
5. تُحتسب إلغاءات السائق بعد القبول في معدله حتى داخل النافذة المجانية (بدون نقاط)، بينما لا تُحتسب إلغاءات الراكب داخلها.
6. الأعذار المعلّقة لا تُحسم تلقائياً؛ تبقى في الطابور مع مؤشر SLA (48 س).
7. مستوى `suspended` الآلي يمنع الاستخدام بلا انتهاء ولا يغيّر حالة اعتماد السائق؛ الرفع بيد العمليات.
8. رسوم الإلغاء غير المحصّلة على رحلات النقد تصبح رصيداً سالباً في محفظة الراكب يمنع الطلب التالي.
