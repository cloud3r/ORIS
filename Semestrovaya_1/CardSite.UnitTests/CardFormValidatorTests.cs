using CardSite.Services;
using NUnit.Framework;

namespace CardSite.UnitTests;

[TestFixture]
public class CardFormValidatorTests
{
    [Test]
    public void Validate_ValidData_ReturnsNoErrors()
    {
        var errors = CardFormValidator.Validate(
            "Son Goku",
            "FB11-001");

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Validate_EmptyName_ReturnsError()
    {
        var errors = CardFormValidator.Validate(
            "",
            "FB11-001");

        Assert.That(errors.ContainsKey("name"), Is.True);
    }

    [Test]
    public void Validate_InvalidName_ReturnsError()
    {
        var errors = CardFormValidator.Validate(
            "<script>alert(1)</script>",
            "FB11-001");

        Assert.That(errors.ContainsKey("name"), Is.True);
    }

    [Test]
    public void Validate_EmptyCardNumber_ReturnsError()
    {
        var errors = CardFormValidator.Validate(
            "Son Goku",
            "");

        Assert.That(errors.ContainsKey("cardNumber"), Is.True);
    }

    [Test]
    public void Validate_InvalidCardNumber_ReturnsError()
    {
        var errors = CardFormValidator.Validate(
            "Son Goku",
            "INVALID");

        Assert.That(errors.ContainsKey("cardNumber"), Is.True);
    }

    [Test]
    public void Validate_InvalidBothFields_ReturnsTwoErrors()
    {
        var errors = CardFormValidator.Validate(
            "",
            "");

        Assert.That(errors.Count, Is.EqualTo(2));
    }
}