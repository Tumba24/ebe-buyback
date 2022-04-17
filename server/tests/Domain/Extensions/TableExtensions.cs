using TechTalk.SpecFlow;

namespace EveBuyback.Domain.Specs;

public static class TableExtensions
{
    public static Dictionary<string, string> ToDictionary(this Table table)
    {
        if (table.RowCount != 1)
            throw new InvalidOperationException();

        return table.Header.ToDictionary(
            h => h,
            h => table.Rows[0][h],
            StringComparer.InvariantCultureIgnoreCase);
    }
}