import type { Lang } from '../i18n'
import { SUPPORT_EMAIL, SUPPORT_PHONE } from '../lib/catalog'

// Public legal texts (PDPL privacy notice + terms of use). Both languages share
// the same section ids so the table of contents and anchors stay in sync.
// Wording is subject to legal review (docs/12 §F21.12).

export const PRIVACY_POLICY_VERSION = '2026-09'
export const TERMS_VERSION = '2026-09'
export const LEGAL_UPDATED_AT = '2026-09-01'

export interface LegalSection {
  id: string
  heading: string
  paragraphs: string[]
  bullets?: string[]
}

export interface LegalDocument {
  title: string
  intro: string
  sections: LegalSection[]
}

export const privacyPolicy: Record<Lang, LegalDocument> = {
  ar: {
    title: 'سياسة الخصوصية',
    intro:
      'توضح هذه السياسة كيف تجمع منصة ATA بياناتك الشخصية وتستخدمها وتحميها، وحقوقك بموجب نظام حماية البيانات الشخصية في المملكة العربية السعودية ولوائحه.',
    sections: [
      {
        id: 'data',
        heading: 'البيانات التي نجمعها',
        paragraphs: ['نجمع الحد الأدنى من البيانات اللازمة لتقديم الخدمة:'],
        bullets: [
          'بيانات الحساب: رقم الجوال، الاسم، اللغة المفضلة، والجنس (اختياري).',
          'بيانات الرحلات: نقاط الالتقاط والوصول والمحطات، الأوقات، الأجرة، والتقييمات.',
          'بيانات الموقع: موقع الكابتن أثناء الاتصال والرحلة، وموقع الراكب عند الطلب أو عند استخدام زر الطوارئ.',
          'بيانات الدفع: نحتفظ برموز البطاقات فقط لدى مزوّد دفع مرخّص، ولا نخزّن أرقام البطاقات.',
          'بيانات الكباتن: الهوية الوطنية، الرخصة، بيانات المركبة، المستندات، والآيبان لتحويل الأرباح.',
          'بيانات الدعم والسلامة: التذاكر والرسائل والبلاغات وجهات الاتصال الموثوقة التي تضيفها.',
        ],
      },
      {
        id: 'purposes',
        heading: 'أغراض المعالجة',
        paragraphs: [
          'نستخدم بياناتك لتنفيذ الرحلات ومطابقة الركاب بالكباتن، وحساب الأجرة وتحصيلها، والتحقق من أهلية الكباتن، وتقديم الدعم، وحماية سلامة المستخدمين، والوفاء بالالتزامات النظامية أمام الجهات المختصة، وتحسين الخدمة بإحصاءات مجمّعة. لا نرسل عروضاً تسويقية إلا بموافقتك، ويمكنك سحبها في أي وقت من إعدادات الإشعارات.',
        ],
      },
      {
        id: 'sharing',
        heading: 'مشاركة البيانات',
        paragraphs: [
          'نشارك مع الكابتن أو الراكب ما يلزم لإتمام الرحلة فقط (الاسم الأول، التقييم، بيانات المركبة، ونقاط الرحلة)، ولا نكشف رقم جوالك الحقيقي؛ فالتواصل يتم عبر المحادثة داخل التطبيق أو رقم وسيط.',
          'روابط مشاركة الرحلة العامة لا تعرض رقم الجوال أو اسم العائلة أو رمز الرحلة أو الأجرة أو طريقة الدفع، وتنتهي صلاحيتها تلقائياً بعد انتهاء الرحلة.',
          'نستعين بمزوّدي خدمات (الرسائل النصية، الإشعارات، الدفع، الاستضافة) ملزمين تعاقدياً بحماية البيانات. عند نقل بيانات خارج المملكة نقتصر على الحد الأدنى (مثل معرّف مجهول للإشعارات) ووفق الضوابط النظامية.',
        ],
      },
      {
        id: 'retention',
        heading: 'مدة الاحتفاظ',
        paragraphs: [
          'نحتفظ بالبيانات طوال مدة الحاجة إليها للأغراض أعلاه. تُحذف رسائل المحادثة وسجل المواقع التفصيلي بعد مدد محددة، ويُحتفظ بسجلات الرحلات والسجلات المالية للمدة التي تفرضها الأنظمة، مرتبطة بمعرّف مُجهَّل بعد حذف الحساب.',
        ],
      },
      {
        id: 'rights',
        heading: 'حقوقك',
        paragraphs: ['يحق لك بموجب نظام حماية البيانات الشخصية:'],
        bullets: [
          'الاطلاع على بياناتك والحصول على نسخة منها.',
          'تصحيح بياناتك أو تحديثها من صفحة الملف الشخصي.',
          'طلب حذف حسابك وبياناتك.',
          'سحب موافقتك على الرسائل التسويقية في أي وقت.',
        ],
      },
      {
        id: 'export',
        heading: 'تنزيل نسخة من بياناتك',
        paragraphs: [
          'من تطبيق ATA: حسابي ← الخصوصية والأمان ← تنزيل بياناتي. نجهّز ملفاً مضغوطاً (ZIP) يحتوي ملفك الشخصي ورحلاتك ومدفوعاتك وحركات المحفظة وتقييماتك وإشعاراتك وتذاكر الدعم وبلاغات السلامة وجهات الاتصال الموثوقة والأماكن المحفوظة، ونرسل لك إشعاراً عند جاهزيته.',
          'يمكن تقديم طلب واحد كل 24 ساعة، ويبقى رابط التنزيل متاحاً لك وحدك لمدة 7 أيام.',
        ],
      },
      {
        id: 'deletion',
        heading: 'حذف الحساب',
        paragraphs: [
          'من تطبيق ATA: حسابي ← الخصوصية والأمان ← حذف الحساب. يُجدول الحذف بعد مهلة سماح مدتها 30 يوماً، وتسجيل الدخول خلالها يلغي الطلب تلقائياً.',
          'قد يتأخر التنفيذ إلى حين تسوية ما يلي: رحلة نشطة، رصيد مستحق أو رصيد متبقٍ في المحفظة، سحب قيد المعالجة، نزاع أو حالة سلامة مفتوحة، أو كونك المسؤول الوحيد لحساب شركة.',
          'عند التنفيذ نُجهّل هويتك (الجوال والاسم) ونحذف جهات الاتصال والأماكن المحفوظة ووسائل الدفع والأجهزة والإشعارات، ونمحو محتوى رسائلك وتعليقاتك، ونحتفظ بسجلات الرحلات والسجلات المالية مجهّلة وفق المتطلبات النظامية.',
        ],
      },
      {
        id: 'security',
        heading: 'حماية البيانات',
        paragraphs: [
          'نستخدم التشفير أثناء النقل والتخزين، وصلاحيات وصول بالحد الأدنى مع التحقق متعدد العوامل لفريق الإدارة، وسجلات تدقيق لكل الإجراءات الحساسة. في حال وقوع تسرب يؤثر على بياناتك نُبلغ الجهة المختصة خلال 72 ساعة ونُخطرك وفق النظام.',
        ],
      },
      {
        id: 'contact',
        heading: 'التواصل معنا',
        paragraphs: [
          `لأي استفسار أو طلب يتعلق ببياناتك الشخصية راسلنا على ${SUPPORT_EMAIL} أو اتصل على ${SUPPORT_PHONE}. يحق لك أيضاً تقديم شكوى إلى الهيئة السعودية للبيانات والذكاء الاصطناعي (سدايا).`,
        ],
      },
    ],
  },
  en: {
    title: 'Privacy Policy',
    intro:
      'This policy explains how ATA collects, uses and protects your personal data, and your rights under the Saudi Personal Data Protection Law (PDPL) and its regulations.',
    sections: [
      {
        id: 'data',
        heading: 'Data we collect',
        paragraphs: ['We collect the minimum data needed to provide the service:'],
        bullets: [
          'Account data: phone number, name, preferred language and gender (optional).',
          'Trip data: pickup, destination and stops, times, fares and ratings.',
          'Location data: the captain’s location while online and on a trip, and the rider’s location when requesting a ride or using the emergency button.',
          'Payment data: only card tokens held by a licensed payment provider; we never store card numbers.',
          'Captain data: national ID, licence, vehicle details, documents and IBAN for payouts.',
          'Support and safety data: tickets, messages, reports and the trusted contacts you add.',
        ],
      },
      {
        id: 'purposes',
        heading: 'Why we process it',
        paragraphs: [
          'We use your data to run trips and match riders with captains, calculate and collect fares, verify captain eligibility, provide support, protect user safety, meet our legal obligations to the competent authorities, and improve the service with aggregated statistics. We only send marketing offers with your consent, which you can withdraw at any time in notification settings.',
        ],
      },
      {
        id: 'sharing',
        heading: 'Sharing',
        paragraphs: [
          'We share with the captain or rider only what is needed to complete the trip (first name, rating, vehicle details and trip points). Your real phone number is never revealed; contact happens through in-app chat or a proxy number.',
          'Public trip-share links never show phone numbers, family names, the trip PIN, the fare or the payment method, and they expire automatically after the trip ends.',
          'We rely on service providers (SMS, push notifications, payments, hosting) that are contractually bound to protect your data. When data leaves the Kingdom we limit it to the minimum (such as an anonymous notification identifier) and follow the regulatory controls.',
        ],
      },
      {
        id: 'retention',
        heading: 'Retention',
        paragraphs: [
          'We keep data only as long as needed for the purposes above. Chat messages and detailed location history are deleted after set periods; trip and financial records are kept for the period required by law, linked to an anonymised identifier after account deletion.',
        ],
      },
      {
        id: 'rights',
        heading: 'Your rights',
        paragraphs: ['Under the PDPL you have the right to:'],
        bullets: [
          'Access your data and obtain a copy of it.',
          'Correct or update your data from your profile.',
          'Request deletion of your account and data.',
          'Withdraw consent to marketing messages at any time.',
        ],
      },
      {
        id: 'export',
        heading: 'Download a copy of your data',
        paragraphs: [
          'In the ATA app: Account → Privacy & security → Download my data. We prepare a ZIP file with your profile, trips, payments, wallet transactions, ratings, notifications, support tickets, safety reports, trusted contacts and saved places, and notify you when it is ready.',
          'You can make one request every 24 hours, and the download stays available to you alone for 7 days.',
        ],
      },
      {
        id: 'deletion',
        heading: 'Deleting your account',
        paragraphs: [
          'In the ATA app: Account → Privacy & security → Delete account. Deletion is scheduled after a 30-day grace period, and signing in during that period cancels the request automatically.',
          'Deletion may be held until the following are settled: an active trip, an outstanding or remaining wallet balance, a payout in progress, an open dispute or safety case, or being the only admin of a company account.',
          'When it runs we anonymise your identity (phone and name), delete your contacts, saved places, payment methods, devices and notifications, erase the content of your messages and comments, and keep trip and financial records anonymised as required by law.',
        ],
      },
      {
        id: 'security',
        heading: 'Security',
        paragraphs: [
          'We use encryption in transit and at rest, least-privilege access with multi-factor authentication for our operations team, and audit logs for every sensitive action. If a breach affects your data we notify the competent authority within 72 hours and inform you as required by law.',
        ],
      },
      {
        id: 'contact',
        heading: 'Contact us',
        paragraphs: [
          `For any question or request about your personal data, email ${SUPPORT_EMAIL} or call ${SUPPORT_PHONE}. You may also lodge a complaint with the Saudi Data & AI Authority (SDAIA).`,
        ],
      },
    ],
  },
}

export const termsOfUse: Record<Lang, LegalDocument> = {
  ar: {
    title: 'الشروط والأحكام',
    intro:
      'باستخدامك تطبيق ATA أو موقعه أو بوابة الشركات فإنك توافق على هذه الشروط. يُرجى قراءتها بعناية.',
    sections: [
      {
        id: 'service',
        heading: 'الخدمة',
        paragraphs: [
          'ATA منصة تقنية تربط الركاب بكباتن مستقلين مرخّصين لتقديم خدمة نقل الركاب عبر التطبيقات داخل المملكة العربية السعودية، وتعمل وفق متطلبات الهيئة العامة للنقل.',
        ],
      },
      {
        id: 'accounts',
        heading: 'الحساب',
        paragraphs: [
          'يتم الدخول برقم جوال سعودي ورمز تحقق لمرة واحدة. أنت مسؤول عن صحة بياناتك وعن أي استخدام لحسابك، ويجب ألا يقل عمرك عن 18 عاماً لإنشاء حساب.',
        ],
      },
      {
        id: 'fares',
        heading: 'الأجرة والدفع',
        paragraphs: [
          'تُعرض أجرة تقديرية شاملة الضريبة قبل تأكيد الطلب، وتُحسب الأجرة النهائية بناءً على المسافة والوقت الفعليين وفق قواعد التسعير المعلنة، ويصلك إيصال بعد كل رحلة.',
          'يمكن الدفع نقداً أو بالمحفظة أو بالبطاقة عبر مزوّد دفع مرخّص، أو عبر حساب الشركة لموظفي الشركات المشتركة.',
        ],
      },
      {
        id: 'cancellation',
        heading: 'الإلغاء',
        paragraphs: [
          'يمكنك الإلغاء مجاناً خلال المهلة الموضحة في التطبيق، وقد تُفرض رسوم إلغاء بعد ذلك أو عند عدم الحضور، وتُعرض الرسوم المتوقعة قبل تأكيد الإلغاء. يمكن تقديم عذر للمراجعة في الحالات الطارئة.',
        ],
      },
      {
        id: 'conduct',
        heading: 'السلوك والسلامة',
        paragraphs: [
          'يلتزم الركاب والكباتن بالاحترام المتبادل والأنظمة المرورية. يُحظر التحرش أو الإساءة أو استخدام الخدمة لأغراض غير نظامية، ويحق لنا تقييد أو إيقاف أي حساب يخالف ذلك بعد المراجعة.',
          'تتوفر أدوات السلامة داخل التطبيق: مشاركة الرحلة، زر الطوارئ، وجهات الاتصال الموثوقة، والتواصل المُقنَّع.',
        ],
      },
      {
        id: 'captains',
        heading: 'الكباتن',
        paragraphs: [
          'يخضع الكباتن للتحقق من المستندات والأهلية قبل التفعيل، ويلتزمون بصلاحية الرخصة والتأمين والفحص الدوري للمركبة. تُحوَّل الأرباح إلى الآيبان المسجل وفق جدول التحويل المعلن.',
        ],
      },
      {
        id: 'corporate',
        heading: 'حسابات الشركات',
        paragraphs: [
          'تخضع رحلات الشركات لسياسات الشركة وميزانياتها، وتُصدر فاتورة ضريبية شهرية للشركة. مسؤول الشركة مسؤول عن دقة بيانات الموظفين والضيوف الذين يحجز لهم.',
        ],
      },
      {
        id: 'liability',
        heading: 'المسؤولية',
        paragraphs: [
          'نبذل عناية معقولة لضمان توفر الخدمة وجودتها، ولا نتحمل المسؤولية عن الأضرار غير المباشرة أو الناتجة عن ظروف خارجة عن إرادتنا، وذلك في الحدود التي يسمح بها النظام.',
        ],
      },
      {
        id: 'law',
        heading: 'النظام الحاكم',
        paragraphs: [
          'تخضع هذه الشروط لأنظمة المملكة العربية السعودية، وتختص الجهات القضائية فيها بالنظر في أي نزاع. قد نحدّث الشروط ونُخطرك بالتغييرات الجوهرية داخل التطبيق.',
        ],
      },
      {
        id: 'contact',
        heading: 'التواصل',
        paragraphs: [`للاستفسارات: ${SUPPORT_EMAIL} · ${SUPPORT_PHONE}`],
      },
    ],
  },
  en: {
    title: 'Terms of Use',
    intro: 'By using the ATA app, website or business portal you agree to these terms. Please read them carefully.',
    sections: [
      {
        id: 'service',
        heading: 'The service',
        paragraphs: [
          'ATA is a technology platform that connects riders with independent, licensed captains for app-based passenger transport within the Kingdom of Saudi Arabia, operating under the requirements of the Transport General Authority.',
        ],
      },
      {
        id: 'accounts',
        heading: 'Your account',
        paragraphs: [
          'You sign in with a Saudi mobile number and a one-time code. You are responsible for the accuracy of your details and for any use of your account, and you must be at least 18 years old to create one.',
        ],
      },
      {
        id: 'fares',
        heading: 'Fares and payment',
        paragraphs: [
          'An estimated fare including VAT is shown before you confirm a request. The final fare is based on the actual distance and time under the published pricing rules, and you receive a receipt after every trip.',
          'You can pay in cash, by wallet, by card through a licensed payment provider, or through a company account if your employer is subscribed.',
        ],
      },
      {
        id: 'cancellation',
        heading: 'Cancellation',
        paragraphs: [
          'You can cancel for free within the window shown in the app; a cancellation fee may apply afterwards or for no-shows, and the expected fee is shown before you confirm. You can submit an excuse for review in emergencies.',
        ],
      },
      {
        id: 'conduct',
        heading: 'Conduct and safety',
        paragraphs: [
          'Riders and captains must treat each other with respect and follow traffic regulations. Harassment, abuse or unlawful use of the service is prohibited, and we may restrict or suspend any account that breaks these rules after review.',
          'Safety tools are available in the app: trip sharing, the emergency button, trusted contacts and masked communication.',
        ],
      },
      {
        id: 'captains',
        heading: 'Captains',
        paragraphs: [
          'Captains are verified for documents and eligibility before activation and must keep their licence, insurance and periodic vehicle inspection valid. Earnings are paid to the registered IBAN on the published payout schedule.',
        ],
      },
      {
        id: 'corporate',
        heading: 'Company accounts',
        paragraphs: [
          'Company trips follow the company’s policies and budgets, and a monthly tax invoice is issued to the company. The company admin is responsible for the accuracy of employee and guest details they book for.',
        ],
      },
      {
        id: 'liability',
        heading: 'Liability',
        paragraphs: [
          'We take reasonable care to keep the service available and of good quality, and are not liable for indirect damages or events beyond our control, to the extent permitted by law.',
        ],
      },
      {
        id: 'law',
        heading: 'Governing law',
        paragraphs: [
          'These terms are governed by the laws of the Kingdom of Saudi Arabia, whose courts have jurisdiction over any dispute. We may update the terms and will notify you of material changes in the app.',
        ],
      },
      {
        id: 'contact',
        heading: 'Contact',
        paragraphs: [`Questions: ${SUPPORT_EMAIL} · ${SUPPORT_PHONE}`],
      },
    ],
  },
}
