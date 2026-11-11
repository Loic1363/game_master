using System.Collections.Immutable;
using Nodal.Foundation.Schedules;
using Xunit;

namespace Nodal.Foundation.Tests.Schedules;

public class PressScheduleTextTests
{
    [Theory]
    [InlineData("released")]
    [InlineData("Released")]
    [InlineData("  released  ")]
    public void ReleasedIsOpenForTheWholeRun(string text)
    {
        var schedule = Parsed(text);

        Assert.True(schedule.IsReleased);
        Assert.False(schedule.IsClosedAt(0));
        Assert.False(schedule.IsClosedAt(1));
        Assert.Empty(schedule.Presses);
    }

    [Theory]
    [InlineData("pressed")]
    [InlineData("PRESSED")]
    public void PressedIsClosedForTheWholeRun(string text)
    {
        var schedule = Parsed(text);

        Assert.True(schedule.IsPressedThroughout);
        Assert.True(schedule.IsClosedAt(0));
        Assert.True(schedule.IsClosedAt(1e6));
    }

    [Fact]
    public void IntervalsAreClosedFromTheirStartUpToTheirEnd()
    {
        var schedule = Parsed("1m to 3m, 5m to 6m");

        Assert.Equal(2, schedule.Presses.Length);
        Assert.Equal(new PressInterval(1e-3, 3e-3), schedule.Presses[0]);
        Assert.Equal(new PressInterval(5e-3, 6e-3), schedule.Presses[1]);

        Assert.False(schedule.IsClosedAt(0.9e-3));
        Assert.True(schedule.IsClosedAt(1e-3));
        Assert.True(schedule.IsClosedAt(2e-3));
        Assert.False(schedule.IsClosedAt(3e-3));
        Assert.False(schedule.IsClosedAt(4e-3));
        Assert.True(schedule.IsClosedAt(5e-3));
        Assert.False(schedule.IsClosedAt(6e-3));
    }

    [Theory]
    [InlineData("1m to 3m")]
    [InlineData("1ms to 3ms")]
    [InlineData("0.001 to 0.003")]
    [InlineData("1m TO 3m")]
    [InlineData(" 1m  to  3m ")]
    public void TimesUseTheQuantityNotationWithOptionalUnits(string text) =>
        Assert.Equal(
            [new PressInterval(1e-3, 3e-3)], Parsed(text).Presses);

    [Fact]
    public void ASingleInstantIsAnInstantOfTheSchedule() =>
        Assert.Equal([1e-3, 3e-3, 5e-3, 6e-3], Parsed("1m to 3m, 5m to 6m").Instants);

    [Fact]
    public void TouchingPressesAreAllowed() =>
        Assert.Equal(2, Parsed("1m to 2m, 2m to 3m").Presses.Length);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("open")]
    [InlineData("1m")]
    [InlineData("1m to")]
    [InlineData("1m to 3m to 5m")]
    [InlineData("1m to3m")]
    [InlineData("1m to 3V")]
    [InlineData("pressed, 1m to 3m")]
    public void UnreadableTextNamesTheThreeAllowedForms(string text) =>
        Assert.Equal(
            "Unrecognized press schedule. Use released, pressed, or intervals such as "
            + "1m to 3m, 5m to 6m.",
            ProblemOf(text));

    [Fact]
    public void APressMustEndAfterItStarts() =>
        Assert.Equal(
            "Each press must end after it starts: 3m to 1m.", ProblemOf("3m to 1m"));

    [Fact]
    public void APressOfZeroLengthIsRefusedWithItsBounds() =>
        Assert.Equal(
            "Each press must end after it starts: 1m to 1m.", ProblemOf("1m to 1m"));

    [Theory]
    [InlineData("5m to 6m, 1m to 3m")]
    [InlineData("1m to 4m, 2m to 6m")]
    public void PressesMustBeOrderedAndDisjoint(string text) =>
        Assert.Equal(
            "Presses must be in increasing time order and must not overlap.", ProblemOf(text));

    [Fact]
    public void APressCannotStartBeforeZero() =>
        Assert.Equal("A press cannot start before 0 s.", ProblemOf("-1m to 3m"));

    [Fact]
    public void AThousandPressesAreAllowed() =>
        Assert.Equal(PressSchedule.MaxPresses, Parsed(Presses(PressSchedule.MaxPresses)).Presses.Length);

    [Fact]
    public void MoreThanAThousandPressesAreRefused() =>
        Assert.Equal(
            "A schedule has at most 1,000 presses.",
            ProblemOf(Presses(PressSchedule.MaxPresses + 1)));

    [Theory]
    [InlineData("released")]
    [InlineData("pressed")]
    [InlineData("1m to 3m")]
    [InlineData("1m to 3m, 5m to 6m")]
    [InlineData("500µ to 1.5m")]
    public void TextSurvivesParsingAndFormatting(string text) =>
        Assert.Equal(text, PressScheduleText.Format(Parsed(text)));

    [Fact]
    public void AScheduleFormatsItselfAsItsText() =>
        Assert.Equal("1m to 3m", Parsed("1m to 3m").ToString());

    [Fact]
    public void AnOpenEndedPressAwayFromZeroIsNotAScheduleTheTextCanExpress()
    {
        var schedule = new PressSchedule(
            [new PressInterval(1e-3, null)]);

        Assert.False(schedule.IsValid);
        Assert.Equal(PressScheduleError.Unrecognized, schedule.Validate().Error);
    }

    [Fact]
    public void AnOpenEndedPressMustBeTheOnlyPress()
    {
        var schedule = new PressSchedule(
            [PressInterval.FromStart, new PressInterval(1e-3, 2e-3)]);

        Assert.False(schedule.IsValid);
        Assert.Equal(PressScheduleError.OutOfOrder, schedule.Validate().Error);
    }

    [Fact]
    public void ADefaultArrayOfPressesReadsAsReleased() =>
        Assert.True(new PressSchedule(default(ImmutableArray<PressInterval>)).IsReleased);

    private static PressSchedule Parsed(string text)
    {
        Assert.True(
            PressScheduleText.TryParse(text, out var schedule, out var fault),
            $"'{text}' was refused: {PressScheduleErrorText.Of(fault)}");

        return schedule!;
    }

    private static string ProblemOf(string text)
    {
        Assert.False(
            PressScheduleText.TryParse(text, out _, out var fault), $"'{text}' was accepted");

        return PressScheduleErrorText.Of(fault);
    }

    private static string Presses(int count) =>
        string.Join(", ", Enumerable.Range(1, count).Select(index => $"{index * 2}m to {(index * 2) + 1}m"));
}
