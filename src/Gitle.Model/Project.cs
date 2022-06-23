namespace Gitle.Model
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Enum;
    using Helpers;
    using Interfaces.Model;

    [DebuggerDisplay("{Name}")]
    public class Project : ModelBase, IDocumentContainer, ISlugger
    {
        public Project()
        {
            Users = new List<UserProject>();
            Labels = new List<Label>();
            Documents = new List<Document>();
        }

        public virtual int Number { get; set; }

        public virtual string Name { get; set; }

        public virtual string Slug { get; set; }

        public virtual string Repository { get; set; }
        public virtual int MilestoneId { get; set; }
        public virtual string MilestoneName { get; set; }
        public virtual decimal HourPrice { get; set; }
        public virtual int FreckleId { get; set; }
        public virtual string FreckleName { get; set; }
        public virtual string Information { get; set; }
        public virtual string Comments { get; set; }

        public virtual double BudgetMinutes { get; set; }
        public virtual double BudgetHours => BudgetMinutes / 60.0;
        public virtual string BudgetTime => $"{Math.Floor(BudgetHours)}:{BudgetMinutes - (Math.Floor(BudgetHours)*60):00}";

        public virtual bool Unbillable { get; set; }

        public virtual bool Closed { get; set; }

        public virtual ProjectType Type { get; set; }
        public virtual string TypeString => Type.GetDescription();

        public virtual bool TicketRequiredForBooking { get; set; }

        public virtual bool IsMuted { get; set; } //Prevents all action notification emails from being sent to users

        //public virtual Customer Customer { get; set; }
        public virtual Application Application { get; set; }

        public virtual IList<UserProject> Users { get; set; }
        public virtual IList<Label> Labels { get; set; }
        public virtual IList<Issue> Issues { get; set; }
        public virtual IList<Invoice> Invoices { get; set; }
        public virtual IList<Document> Documents { get; set; }
        public virtual IList<Booking> Bookings { get; set; }

        public virtual int NotifiedUsers => Users.Count(u => u.Notifications);

        public virtual int NewIssueNumber => (Issues.Any() ? Issues.Max(x => x.Number) : 0) + 1;

        public virtual double BillableMinutes => Bookings?.Where(x => x.IsActive && !x.Unbillable).Sum(x => x.Minutes) ?? 0;
        public virtual double BillableHours => Bookings?.Where(x => x.IsActive && !x.Unbillable).Sum(x => x.Hours) ?? 0;
        public virtual double UnBillableMinutes => Bookings?.Where(x => x.IsActive && x.Unbillable).Sum(x => x.Minutes) ?? 0;
        public virtual double UnBillableHours => Bookings?.Where(x => x.IsActive && x.Unbillable).Sum(x => x.Hours) ?? 0;
        public virtual double TotalHours => Bookings?.Where(x => x.IsActive).Sum(x => x.Hours) ?? 0;
        public virtual double RemainingHours => BudgetHours - TotalHours;

        public virtual string CompleteName => $"{Name} ({Application?.Name}, {Application?.Customer?.Name})";

        public virtual IReadOnlyList<Issue> OpenIssues => Issues?.Where(x => !x.IsArchived && !x.IsAdministrative && x.IsOpen || x.IsUrgent).ToList() ?? new List<Issue>();
        public virtual IReadOnlyList<Issue> UrgentIssues => Issues?.Where(x => !x.IsArchived && !x.IsAdministrative && x.SetUrgent).ToList() ?? new List<Issue>();
        public virtual IReadOnlyList<Issue> DoneIssues => Issues?.Where(x => !x.IsArchived && !x.IsAdministrative && x.IsDone).ToList() ?? new List<Issue>();
        public virtual IReadOnlyList<Issue> HoldIssues => Issues?.Where(x => !x.IsArchived && !x.IsAdministrative && x.IsOnHold).ToList() ?? new List<Issue>();
        public virtual IReadOnlyList<Issue> ClosedIssues => Issues?.Where(x => !x.IsArchived && !x.IsAdministrative && x.IsClosed && !x.IsFullyInvoiced).ToList() ?? new List<Issue>();
        public virtual IReadOnlyList<Issue> TotalIssues => Issues?.Where(x => !x.IsArchived && !x.IsAdministrative && (!x.IsClosed || !x.IsFullyInvoiced)).ToList() ?? new List<Issue>();

        public virtual double OpenIssuesPercentageOf(int totalIssues) => PercentageOf(OpenIssues.Count, totalIssues);
        public virtual double DoneIssuesPercentageOf(int totalIssues) => PercentageOf(DoneIssues.Count, totalIssues);
        public virtual double HoldIssuesPercentageOf(int totalIssues) => PercentageOf(HoldIssues.Count, totalIssues);
        public virtual double ClosedIssuesPercentageOf(int totalIssues) => PercentageOf(ClosedIssues.Count, totalIssues);
        public virtual double UrgentIssuesPercentageOf(int totalIssues) => PercentageOf(UrgentIssues.Count, totalIssues);

        public virtual double OpenIssuesPercentage => OpenIssuesPercentageOf(TotalIssues.Count);
        public virtual double DoneIssuesPercentage => DoneIssuesPercentageOf(TotalIssues.Count);
        public virtual double HoldIssuesPercentage => HoldIssuesPercentageOf(TotalIssues.Count);
        public virtual double UrgentIssuesPercentage => UrgentIssuesPercentageOf(TotalIssues.Count);
        public virtual double ClosedIssuesPercentage => ClosedIssuesPercentageOf(TotalIssues.Count);

        public virtual double TotalEstimateAllTickets()
        {
            return Issues.Where(x => !x.IsArchived).Sum(x => x.TotalHours);
        }

        public virtual double TotalEstimateAllOpenTickets()
        {
            return Issues.Where(x => !x.IsArchived && !x.IsDone && !x.IsClosed).Sum(x => x.TotalHours);
        }

        public virtual double Progress()
        {
            //return BillableHours / TotalEstimateAllTickets() * 100;
            var estimateTotalHours = TotalEstimateAllTickets();
            if (estimateTotalHours > 0)
                return Issues.Where(x => x.IsDone || x.IsClosed).Sum(x => x.TotalHours) / estimateTotalHours * 100;
            if (BudgetHours > 0)
                return TotalHours / BudgetHours * 100;
            return TotalHours > 0 ? 100 : 0;
        }

        public virtual double PercentageOverBudget()
        {
            var estimateTotalHours = TotalEstimateAllTickets();
            if (estimateTotalHours > 0)
                return (TotalHours + TotalEstimateAllOpenTickets()) / estimateTotalHours * 100 - 100;
            if (BudgetHours > 0)
                return TotalHours / BudgetHours * 100 - 100;
            return TotalHours > 0 ? 100 : 0;
        }

        public virtual bool HasEstimate => TotalEstimateAllTickets() != 0 || BudgetHours != 0;

        public virtual double SumMaxOfEstimateAndBooking()
        {
            if (Type != ProjectType.Service) return 0.0;
            var issueMax = Issues.Where(i => i.IsActive).Sum(i => i.MaxOfBookingAndTotalHours());
            var noIssueBookingsMax = Bookings.Where(b => b.Issue == null && b.IsActive && !b.Unbillable).Sum(b => b.Hours);

            return issueMax + noIssueBookingsMax;
        }

        public virtual double TotalDefinitiveHours()
        {
            return Invoices.Where(i => i.State == InvoiceState.Definitive && i.IsActive).Sum(i => i.TotalBillableHours);
        }

        public virtual double ToInvoice()
        {
            if (Type == ProjectType.Service)
            {
                return SumMaxOfEstimateAndBooking() - TotalDefinitiveHours();
            }
            if (Type == ProjectType.Initial)
            {
                return Math.Max(BudgetHours, BillableHours) - TotalDefinitiveHours();
            }
            return 0.0;
        }

        private static double PercentageOf(int numerator, int denominator) => denominator == 0 ? 0 : numerator / (denominator * 1.0) * 100;

        public virtual UserProject GetUserProject(User user)
        {
            return Users.FirstOrDefault(x => x.User == user) ?? new UserProject();
        }
    }
}