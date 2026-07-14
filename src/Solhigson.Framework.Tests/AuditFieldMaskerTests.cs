using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using Solhigson.Framework.AuditCapture;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Unit contract for the fail-closed masking decision (pin R1): masking is a pure OR of the fixed
/// default name-set, <c>[PersonalData]</c> reflection, and the additive consumer overlay — so the overlay
/// can only ever mask MORE, never un-mask a field the defaults or the attribute already protect.
/// </summary>
public class AuditFieldMaskerTests
{
    private static PropertyInfo Prop(string name) => typeof(MaskProbe).GetProperty(name)!;

    [Theory]
    [InlineData("Password")]
    [InlineData("PasswordHash")]
    [InlineData("ClientSecret")]
    [InlineData("AccessToken")]
    [InlineData("ApiKey")]
    [InlineData("Ssn")]
    [InlineData("CardPan")]
    [InlineData("Cvv")]
    [InlineData("Pin")]
    public void DefaultNamePatterns_MaskCaseInsensitively(string propertyName)
    {
        new AuditFieldMasker().ShouldMask(propertyName, propertyInfo: null).ShouldBeTrue();
    }

    [Fact]
    public void NonSensitiveName_WithNoAttribute_IsNotMasked()
    {
        new AuditFieldMasker().ShouldMask("Name", propertyInfo: null).ShouldBeFalse();
    }

    [Fact]
    public void PersonalDataAttribute_Masks_EvenWhenNameIsNotSensitive()
    {
        // "Email" matches no default name pattern; masking comes solely from [PersonalData].
        new AuditFieldMasker().ShouldMask("Email", Prop(nameof(MaskProbe.Email))).ShouldBeTrue();
    }

    [Fact]
    public void AdditiveOverlay_MasksAnExtraField()
    {
        var options = new AuditCaptureOptions();
        options.AdditionalSensitiveNamePatterns.Add("nickname");
        var masker = new AuditFieldMasker(options);

        masker.ShouldMask("Nickname", propertyInfo: null).ShouldBeTrue();
        masker.ShouldMask("Name", propertyInfo: null).ShouldBeFalse();
    }

    [Fact]
    public void Overlay_CannotUnmask_DefaultOrPersonalDataFields()
    {
        // The overlay is add-only; there is no input that removes a default or attribute match.
        var options = new AuditCaptureOptions();
        options.AdditionalSensitiveNamePatterns.Add("some-unrelated-pattern");
        var masker = new AuditFieldMasker(options);

        masker.ShouldMask("Password", propertyInfo: null).ShouldBeTrue();
        masker.ShouldMask("Email", Prop(nameof(MaskProbe.Email))).ShouldBeTrue();
    }

    [Fact]
    public void MaskMarker_IsTheFixedTripleAsterisk()
    {
        AuditFieldMasker.MaskMarker.ShouldBe("***");
    }

    [Fact]
    public void DefaultSensitiveNamePatterns_AreThePinnedSet()
    {
        AuditFieldMasker.DefaultSensitiveNamePatterns.ShouldBe(
            ["password", "secret", "token", "apikey", "ssn", "pan", "cvv", "pin"],
            ignoreOrder: false);
    }

    private sealed class MaskProbe
    {
        [PersonalData]
        public string? Email { get; set; }
    }
}
