using System.ComponentModel.DataAnnotations;
using ESDEMO.Api.Contracts.Examples;

namespace ESDEMO.Tests;

public sealed class ValidateTextRequestTests
{
    [Fact]
    public void Empty_text_is_invalid()
    {
        var request = new ValidateTextRequest { Text = string.Empty };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.NotEmpty(validationResults);
    }

    [Fact]
    public void Non_empty_text_is_valid()
    {
        var request = new ValidateTextRequest { Text = "ESDEMO" };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }
}
