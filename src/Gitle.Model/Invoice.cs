using Gitle.Model.Enum;
using System;
using Gitle.Model.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Gitle.Model
{
    public class Invoice : ModelBase
    {
        public Invoice()
        {
            VAT = true;
            State = InvoiceState.Concept;
            Lines = new List<InvoiceLine>();
            Corrections = new List<Correction>();
        }

        public Invoice(Project project, DateTime startDate, DateTime endDate)
            : this()
        {
            Project = project;
            HourPrice = project.HourPrice;
            StartDate = startDate;
            EndDate = endDate;
            Title = $"{Project.Name} ({StartDate:dd-MM-yyyy} tot {EndDate:dd-MM-yyyy})";
        }

        public Invoice(Project project, DateTime startDate, DateTime endDate, IList<Booking> bookings)
            : this(project, startDate, endDate)
        {
            foreach (var booking in bookings.OrderBy(b => b.Date))
            {
                if(booking.Issue != null){
                    var invoiceLine = Lines.FirstOrDefault(il => il.Issue == booking.Issue);
                    if (invoiceLine == null)
                    {
                        invoiceLine = new InvoiceLine()
                        {
                            Description = $"#{booking.Issue.Id} - {booking.Issue.Name}",
                            Issue = booking.Issue,
                            Invoice = this
                        };
                        Lines.Add(invoiceLine);
                    }
                    invoiceLine.Hours += booking.Hours;
                    invoiceLine.Bookings.Add(booking);
                    if (booking.Unbillable)
                    {
                        invoiceLine.Null = true;
                    }
                }
                else
                {
                    if (!booking.InvoiceLines.Any(x => x.Invoice.IsDefinitive))
                    {
                        var invoiceLine = new InvoiceLine { Description = booking.Comment, Invoice = this, Hours = booking.Hours, Null = booking.Unbillable };
                        invoiceLine.Bookings.Add(booking);
                        Lines.Add(invoiceLine);
                    }
                }
            }
            for (var i = Corrections.Count(); i < 10; i++)
            {
                Corrections.Add(new Correction());
            }
        }

        public virtual string Number { get; set; }
        public virtual string Title { get; set; }
        public virtual decimal HourPrice { get; set; }
        public virtual DateTime CreatedAt { get; set; }
        public virtual bool VAT { get; set; }
        public virtual string Remarks { get; set; }
        public virtual InvoiceState State { get; set; }
        public virtual DateTime StartDate { get; set; }
        public virtual DateTime EndDate { get; set; }

        public virtual User CreatedBy { get; set; }
        public virtual Project Project { get; set; }
        public virtual IList<InvoiceLine> Lines { get; set; }
        public virtual IList<Booking> Bookings => Lines.SelectMany(l => l.Bookings).OrderBy(b => b.Date).ToList();
        public virtual IList<Correction> Corrections { get; set; }

        public virtual bool IsConcept => State == InvoiceState.Concept;
        public virtual bool IsDefinitive => State == InvoiceState.Definitive;
        public virtual bool IsArchived => State == InvoiceState.Archived;
        public virtual string StateString => State.GetDescription();

        public virtual decimal TotalExclVat => Lines.Sum(x => x.Price) + Corrections.Sum(x => x.Price);
        public virtual decimal TotalVat => TotalExclVat * (VAT ? 0.21M : 0);
        public virtual decimal Total => TotalExclVat + TotalVat;

        public virtual double TotalUnbillableHours => Lines.Where(x => x.Null).Sum(x => x.Hours);
        public virtual double TotalBillableHours => Lines.Where(x => !x.Null).Sum(x => x.Hours);
        public virtual double TotalHours => Lines.Sum(x => x.Hours);

        public virtual int IssueCount => Lines.Count(x => x.Issue != null);

        public virtual IList<InvoiceLine> ProjectLines => Lines.Where(x => x.Issue == null).ToList();
        public virtual IList<InvoiceLine> IssueLines => Lines.Where(x => x.Issue != null).OrderBy(l => l.Issue.Id).ToList();
        public virtual IList<Issue> Issues => Lines.Where(x => x.Issue != null).OrderBy(l => l.Issue.Id).Select(x => x.Issue).ToList();

        public virtual IList<InvoiceLine> UnbillableLines => Lines.Where(x => x.Null).OrderBy(l => l.Issue.Id).ToList();
        public virtual IList<InvoiceLine> BillableLines => Lines.Where(x => !x.Null).OrderBy(l => l.Issue.Id).ToList();

        public virtual void AddLine(InvoiceLine line)
        {
            line.Invoice = this;
            Lines.Add(line);
        }

        public virtual void RemoveLine(InvoiceLine line)
        {
            Lines.Remove(line);
        }

        public virtual void AddCorrection(Correction correction)
        {
            correction.Invoice = this;
            Corrections.Add(correction);
        }

        public virtual void RemoveCorrection(Correction correction)
        {
            Corrections.Remove(correction);
        }

    }
}
