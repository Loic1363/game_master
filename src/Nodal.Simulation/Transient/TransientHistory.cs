namespace Nodal.Simulation.Transient;

internal sealed class TransientHistory
{
    public const int Generations = 3;

    private readonly double[][] _charge = new double[Generations][];
    private readonly double[][] _flow = new double[Generations][];
    private readonly double[] _steps = new double[Generations];

    public TransientHistory(double[] charge, double[] flow, double step)
    {
        Real = 1;

        for (var generation = 0; generation < Generations; generation++)
        {
            _charge[generation] = (double[])charge.Clone();
            _flow[generation] = (double[])flow.Clone();
            _steps[generation] = step;
        }
    }

    public int Real { get; private set; }

    public double Charge(int generation, int element) => _charge[generation][element];

    public double Flow(int generation, int element) => _flow[generation][element];

    public double Step(int generation) => _steps[generation];

    public void Push(double[] charge, double[] flow, double step)
    {
        for (var generation = Generations - 1; generation > 0; generation--)
        {
            _charge[generation] = _charge[generation - 1];
            _flow[generation] = _flow[generation - 1];
            _steps[generation] = _steps[generation - 1];
        }

        _charge[0] = (double[])charge.Clone();
        _flow[0] = (double[])flow.Clone();
        _steps[0] = step;
        Real = Math.Min(Generations, Real + 1);
    }
}
