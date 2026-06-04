using Shouldly;
using Xunit;

namespace Solhigson.Utilities.Tests;

public class HelperFunctionsTests
{
    // --- IsValidPhoneNumber ---

    [Theory]
    [InlineData("08031234567")]
    [InlineData("+2348031234567")]
    [InlineData("0803 123 4567")]
    [InlineData("+234 803 123 456")]
    public void IsValidPhoneNumber_ValidNumber_ReturnsTrue(string phoneNumber)
    {
        HelperFunctions.IsValidPhoneNumber(phoneNumber).ShouldBeTrue();
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("123")]
    [InlineData("test@example.com")]
    public void IsValidPhoneNumber_InvalidNumber_ReturnsFalse(string phoneNumber)
    {
        HelperFunctions.IsValidPhoneNumber(phoneNumber).ShouldBeFalse();
    }
}
