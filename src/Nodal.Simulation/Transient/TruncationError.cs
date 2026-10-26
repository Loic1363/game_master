namespace Nodal.Simulation.Transient;

internal static class TruncationError
{
    public static double Limit(
        TransientStorage storage,
        TransientHistory history,
        double[] charge,
        double[] flow,
        TransientIntegration integration,
        TransientOptions options)
    {
        var limit = double.PositiveInfinity;

        for (var element = 0; element < storage.Count; element++)
        {
            limit = Math.Min(limit, Of(history, charge, flow, element, integration, options));
        }

        return limit;
    }

    private static double Of(
        TransientHistory history,
        double[] charge,
        double[] flow,
        int element,
        TransientIntegration integration,
        TransientOptions options)
    {
        var order = integration.Order;
        var differences = new double[order + 2];
        var spans = new double[order + 1];

        for (var index = order + 1; index >= 0; index--)
        {
            differences[index] = ChargeAt(history, charge, index, element);
        }

        for (var index = 0; index <= order; index++)
        {
            spans[index] = StepAt(history, integration.Step, index);
        }

        var remaining = order;

        while (true)
        {
            for (var index = 0; index <= remaining; index++)
            {
                differences[index] = (differences[index] - differences[index + 1]) / spans[index];
            }

            if (--remaining < 0)
            {
                break;
            }

            for (var index = 0; index <= remaining; index++)
            {
                spans[index] = spans[index + 1] + StepAt(history, integration.Step, index);
            }
        }

        var flowTolerance = options.AbsTol
            + (options.RelTol
                * Math.Max(
                    Math.Abs(FlowAt(history, flow, 0, element)),
                    Math.Abs(FlowAt(history, flow, 1, element))));

        var chargeTolerance = Math.Max(
            Math.Abs(ChargeAt(history, charge, 0, element)),
            Math.Abs(ChargeAt(history, charge, 1, element)));

        chargeTolerance = options.RelTol
            * Math.Max(chargeTolerance, options.ChgTol)
            / integration.Step;

        var tolerance = Math.Max(flowTolerance, chargeTolerance);

        var limit = options.TrTol * tolerance / Math.Max(
            options.AbsTol, Math.Abs(differences[0]));

        return integration.Order == TransientIntegration.Trapezoidal
            ? Math.Sqrt(limit)
            : limit;
    }

    private static double ChargeAt(
        TransientHistory history, double[] charge, int generation, int element) =>
        generation == 0 ? charge[element] : history.Charge(generation - 1, element);

    private static double FlowAt(
        TransientHistory history, double[] flow, int generation, int element) =>
        generation == 0 ? flow[element] : history.Flow(generation - 1, element);

    private static double StepAt(TransientHistory history, double step, int generation) =>
        generation == 0 ? step : history.Step(generation - 1);
}
