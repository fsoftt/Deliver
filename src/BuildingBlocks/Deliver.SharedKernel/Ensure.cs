namespace Deliver.SharedKernel;

/// <summary>Tiny guard helpers for value object construction.</summary>
public static class Ensure
{
    public static string NotEmpty(string? value, string name, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidValueException($"{name} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new InvalidValueException($"{name} must be at most {maxLength} characters.");

        return trimmed;
    }

    public static decimal Positive(decimal value, string name) =>
        value > 0 ? value : throw new InvalidValueException($"{name} must be greater than zero.");

    public static int Positive(int value, string name) =>
        value > 0 ? value : throw new InvalidValueException($"{name} must be greater than zero.");

    public static Guid NotEmpty(Guid value, string name) =>
        value != Guid.Empty ? value : throw new InvalidValueException($"{name} is required.");
}
