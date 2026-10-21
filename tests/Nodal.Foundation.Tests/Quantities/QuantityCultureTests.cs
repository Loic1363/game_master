using System.Globalization;
using Nodal.Foundation.Quantities;
using Xunit;

namespace Nodal.Foundation.Tests.Quantities;

public class QuantityCultureTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("tr-TR")]
    [InlineData("ar-SA")]
    public void ParsingAndFormattingDoNotDependOnTheCurrentCulture(string cultureName)
    {
        var observed = RunUnderCulture(cultureName, () =>
        {
            Quantity.TryParse("4.7k", Unit.Ohm, out var fromDot, out _);
            Quantity.TryParse("4,7k", Unit.Ohm, out var fromComma, out _);

            return [
                fromDot.Value.ToString("R", CultureInfo.InvariantCulture),
                fromComma.Value.ToString("R", CultureInfo.InvariantCulture),
                new Quantity(4700, Unit.Ohm).ToString(),
                new Quantity(1e-7, Unit.Farad).ToString(),
            ];
        });

        Assert.Equal(["4700", "4700", "4.7 kΩ", "100 nF"], observed);
    }

    private static string[] RunUnderCulture(string cultureName, Func<string[]> action)
    {
        var results = Array.Empty<string>();

        var worker = new Thread(() =>
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            results = action();
        });

        worker.Start();
        worker.Join();

        return results;
    }
}
