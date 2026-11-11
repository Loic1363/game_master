using Xunit;

namespace Nodal.Simulation.Tests.Reference;

public class ReferenceFolderTests
{
    public static TheoryData<string> Folders()
    {
        var names = new TheoryData<string>();

        foreach (var folder in ReferenceCircuits.Folders())
        {
            names.Add(folder);
        }

        return names;
    }

    [Theory]
    [MemberData(nameof(Folders))]
    public void EveryFolderCarriesASchematicADeckAndAProbePlan(string name)
    {
        foreach (var fileName in new[]
        {
            ReferenceCircuits.SchematicFileName,
            ReferenceCircuits.DeckFileName,
            ReferenceCircuits.ProbesFileName,
        })
        {
            Assert.True(
                ReferenceCircuits.Carries(name, fileName),
                $"{name} carries no {fileName}.");
        }
    }

    [Theory]
    [MemberData(nameof(Folders))]
    public void EveryFolderCarriesAtLeastOneExpectation(string name)
    {
        Assert.True(
            ReferenceCircuits.Carries(name, ReferenceCircuits.ExpectationFileName)
                || ReferenceCircuits.Carries(
                    name, ReferenceCircuits.TransientExpectationFileName),
            $"{name} carries no expectation, so nothing compares it to ngspice.");
    }

    [Theory]
    [MemberData(nameof(Folders))]
    public void OnlyACircuitWithoutAnOperatingPointSkipsTheOperatingPointExpectation(string name)
    {
        if (ReferenceCircuits.Carries(name, ReferenceCircuits.ExpectationFileName))
        {
            return;
        }

        var solution = ReferenceCircuits.Solve(name);

        Assert.False(
            solution.IsSolved,
            $"{name} has an operating point, so it must carry "
            + $"{ReferenceCircuits.ExpectationFileName}.");
    }
}
