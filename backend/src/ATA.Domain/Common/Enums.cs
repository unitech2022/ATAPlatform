namespace ATA.Domain.Common;

public enum Language { Ar, En }

public enum Gender { Unknown, Male, Female }

public enum UserStatus { Active, Suspended, Deleted }

public enum Role { Passenger, Driver, Admin, Operations, CorporateAdmin }

public enum DevicePlatform { Android, Ios, Web }

/// <summary><c>corporate</c> (F19) is a trip payment method only: it can never be <c>passengers.default_payment_method</c>.</summary>
public enum PaymentMethodKind { Cash, Wallet, Card, Corporate }
