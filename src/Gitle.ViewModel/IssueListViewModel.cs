using Gitle.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitle.ViewModel
{
  public class IssueListViewModel
  {
    public IssueListViewModel(Issue issue)
    {
      Application = issue.Project?.Application?.Name;
      Id = issue.Id;
      Name = issue.Name;
      Devvers = issue.Devvers;
      EstimatePublic = issue.EstimatePublic;
      EstimateString = issue.EstimateString;
      Number = issue.Number;
      Hours = issue.Hours;
      HourPrice = issue.Project.HourPrice;
      Urgent = issue.Urgent;
      TotalHours = issue.TotalHoursString;
      Cost = issue.CostString(issue.Project.HourPrice);
      BillableBookingHours = issue.BillableBookingHours();
      BillableBookingHoursString = issue.BillableBookingHoursString();
      UnbillableBookingHoursString = issue.UnbillableBookingHoursString();
      TotalBillableHoursInvoicedString = issue.TotalBillableHoursInvoicedString;
      TotalUnbillableHoursInvoicedString = issue.TotalUnbillableHoursInvoicedString;
      Closed = issue.IsClosed;
      Customer = issue.Project?.Application?.Customer.Name;
      Progress = issue.Progress();
      ProjectSlug = issue.Project.Slug;
    }

    public long Id { get; }
    public string Application { get; }
    public bool Closed { get; }
    public string Customer { get; }
    public int Devvers { get; }
    public bool EstimatePublic { get; }
    public string EstimateString { get; }
    public int Number { get; }
    public string Name { get; }
    public double Hours { get; }
    public decimal HourPrice { get; }
    public bool Urgent { get; }
    public string TotalHours { get; }
    public string Cost { get; }
    public string BillableBookingHoursString { get; }
    public string UnbillableBookingHoursString { get; }
    public double BillableBookingHours { get; }
    public string TotalBillableHoursInvoicedString { get; }
    public string TotalUnbillableHoursInvoicedString { get; }
    public double Progress { get; }
    public string ProjectSlug { get; }
  }
}
