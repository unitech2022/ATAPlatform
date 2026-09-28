using ATA.Domain.Common;

namespace ATA.Tests.Unit;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("0512345678", "+966512345678")]
    [InlineData("512345678", "+966512345678")]
    [InlineData("+966512345678", "+966512345678")]
    [InlineData("966512345678", "+966512345678")]
    [InlineData("00966512345678", "+966512345678")]
    [InlineData("+966 51 234 5678", "+966512345678")]
    [InlineData(" 05-1234-5678 ", "+966512345678")]
    public void Normalizes_accepted_formats_to_e164(string input, string expected)
    {
        Assert.True(PhoneNumber.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0412345678")]
    [InlineData("05123456")]
    [InlineData("051234567890")]
    [InlineData("+971512345678")]
    [InlineData("05x2345678")]
    [InlineData("++966512345678")]
    public void Rejects_invalid_numbers(string input)
    {
        Assert.False(PhoneNumber.TryNormalize(input, out _));
        var ex = Assert.Throws<DomainException>(() => PhoneNumber.Normalize(input));
        Assert.Equal(ErrorCodes.PhoneInvalid, ex.Code);
    }
}
