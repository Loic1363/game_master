using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Nodal.Content.Symbols;

public sealed class SymbolLibrary
{
    private readonly Dictionary<(string ComponentId, SymbolStyle Style), Symbol> _symbolByKey = [];

    public SymbolLibrary(IEnumerable<Symbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        Symbols =
        [
            .. symbols
                .OrderBy(symbol => symbol.ComponentId, StringComparer.Ordinal)
                .ThenBy(symbol => symbol.Style),
        ];

        foreach (var symbol in Symbols)
        {
            if (!_symbolByKey.TryAdd((symbol.ComponentId, symbol.Style), symbol))
            {
                throw new ArgumentException(
                    $"The symbol for component {symbol.ComponentId} in style {symbol.Style} appears twice.",
                    nameof(symbols));
            }
        }
    }

    public static SymbolLibrary Empty { get; } = new([]);

    public ImmutableArray<Symbol> Symbols { get; }

    public int Count => Symbols.Length;

    public bool TryGet(string componentId, SymbolStyle style, [NotNullWhen(true)] out Symbol? symbol) =>
        _symbolByKey.TryGetValue((componentId, style), out symbol);
}
