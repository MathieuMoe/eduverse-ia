namespace EduVerse.Core.Institutions;

/// <param name="MonthlyBase">Price per month including <paramref name="IncludedStudents"/> students.</param>
/// <param name="ExtraStudent">Price per student per month above the included ones.</param>
public sealed record Plan(string Key, decimal MonthlyBase, int IncludedStudents, decimal ExtraStudent);

public sealed record Bill(Plan Plan, int Students, int ActiveStudents, decimal Total, decimal? PerActiveStudent, int FreeSeats);

/// <summary>Indicative prices of the business plan ("academy from 79 €/month"); the bill is computed in code.</summary>
public static class Plans
{
    public static readonly Plan Academy = new("academy", 79m, 50, 1.20m);

    public static Bill Compute(Plan plan, int students, int activeStudents)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var extra = Math.Max(0, students - plan.IncludedStudents);
        var total = plan.MonthlyBase + extra * plan.ExtraStudent;
        return new Bill(plan, students, activeStudents, total, activeStudents == 0 ? null : Math.Round(total / activeStudents, 2), Math.Max(0, plan.IncludedStudents - students));
    }
}
