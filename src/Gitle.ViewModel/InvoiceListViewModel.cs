using Gitle.Model;
using Gitle.Model.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitle.ViewModel
{
  public class InvoiceListViewModel
  {
    public InvoiceListViewModel(Invoice invoice)
    {
      Id = invoice.Id;
      Application = invoice.Project.Application?.Name;
      Customer = invoice.Project.Application?.Customer?.Name;
      Type = invoice.Project.TypeString;
      Number = invoice.Number;
      StateString = invoice.StateString;
      Title = invoice.Title;
      CreatedAt = invoice.CreatedAt;
      Project = invoice.Project;
      ProjectName = invoice.Project?.Name;
      Total = invoice.Total;
      TotalExclVat = invoice.TotalExclVat;
      IssueCount = invoice.IssueCount;
      IsConcept = invoice.IsConcept;
      IsArchived = invoice.IsArchived;
      Slug = invoice.Project.Slug;
    }

    public long Id { get; set; }
    public string Application { get; set; }
    public string Customer { get; set; }
    public string Type { get; set; }
    public string Number { get; set; }
    public string Title { get; set; }
    public string StateString { get; set; }
    public DateTime CreatedAt { get; set; }
    public Project Project { get; set; }
    public string ProjectName { get; set; }
    public decimal Total { get; set; }
    public decimal TotalExclVat { get; set; }
    public int IssueCount { get; set; }
    public bool IsConcept { get; set; }
    public bool IsArchived { get; set; }
    public string Slug { get; set; }
  }
}
