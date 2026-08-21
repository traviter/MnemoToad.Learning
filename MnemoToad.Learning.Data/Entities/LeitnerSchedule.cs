namespace MnemoToad.Learning.Data.Entities;

/// <summary>How long a card in a given Leitner box waits before it's next due for review.</summary>
public class LeitnerSchedule
{
    /// <summary>The schedule row's id.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The Leitner box this row configures.</summary>
    public int BoxNumber { get; set; }

    /// <summary>How many hours after review a card in this box becomes due again.</summary>
    public int IntervalHours { get; set; }

    /// <summary>Jitter, in hours, applied around IntervalHours so due dates don't all clump together.</summary>
    public int VarianceHours { get; set; }
}
