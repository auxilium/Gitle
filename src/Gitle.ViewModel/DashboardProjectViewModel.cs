namespace Gitle.ViewModel
{
  // Scalar dashboard data only: rendering this model never loads NHibernate collections.
  public class DashboardProjectViewModel
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public bool Closed { get; set; }
    public double BudgetHours { get; set; }
    public double TotalHours { get; set; }
    public double InvoicedHours { get; set; }
    public double EstimatedHours { get; set; }
    public double RemainingEstimatedHours { get; set; }
    public double CompletedEstimatedHours { get; set; }
    public int OpenIssueCount { get; set; }
    public int DoneIssueCount { get; set; }
    public int HoldIssueCount { get; set; }
    public int ClosedIssueCount { get; set; }
    public int UrgentIssueCount { get; set; }
    public int TotalIssueCount { get; set; }
    public double RemainingHours => BudgetHours - TotalHours;
    public bool HasEstimate => EstimatedHours != 0 || BudgetHours != 0;
    public double TotalDefinitiveHours() => InvoicedHours;
    public double TotalEstimateAllTickets() => EstimatedHours;
    public double TotalEstimateAllOpenTickets() => RemainingEstimatedHours;
    public double BilledHoursBudgetHours() => BudgetHours - InvoicedHours;
    public double BilledHoursTotalHours() => TotalHours - InvoicedHours;
    public double Progress() => EstimatedHours > 0 ? CompletedEstimatedHours / EstimatedHours * 100
      : BudgetHours > 0 ? TotalHours / BudgetHours * 100 : TotalHours > 0 ? 100 : 0;
    // Only the visual width is capped; tooltips and budget calculations keep the actual percentage.
    public string ProgressWidth
    {
      get
      {
        var progress = Progress();
        var width = double.IsNaN(progress) ? 0 : System.Math.Max(0, System.Math.Min(100, progress));
        return width.ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
      }
    }
    public double ProgressTotalHours() => TotalHours / BudgetHours * 100;
    public double ProgressTotalDefinitiveHours() => BudgetHours / InvoicedHours * 100;
    public double PercentageOverBudget() => EstimatedHours > 0
      ? (TotalHours + RemainingEstimatedHours) / EstimatedHours * 100 - 100
      : BudgetHours > 0 ? TotalHours / BudgetHours * 100 - 100 : TotalHours > 0 ? 100 : 0;
    public double OpenIssuesPercentageOf(int total) => Percentage(OpenIssueCount, total);
    public double OpenIssuesPercentage => Percentage(OpenIssueCount, TotalIssueCount);
    public double DoneIssuesPercentage => Percentage(DoneIssueCount, TotalIssueCount);
    public double HoldIssuesPercentage => Percentage(HoldIssueCount, TotalIssueCount);
    public double ClosedIssuesPercentage => Percentage(ClosedIssueCount, TotalIssueCount);
    public double UrgentIssuesPercentage => Percentage(UrgentIssueCount, TotalIssueCount);
    private static double Percentage(int count, int total) => total == 0 ? 0 : count * 100.0 / total;
  }
}
