namespace Gitle.Web.QueryParsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text.RegularExpressions;
    using FluentNHibernate.Utils;
    using Model;
    using Model.Helpers;
    using NHibernate;
    using NHibernate.Linq;

    public class BookingQueryParser
    {
        private const string queryRegex = @"[a-zA-Z0-9_]+:(([a-zA-Z0-9-_,.]+)|('[a-zA-Z0-9-_,. ]+'))";

        public IList<User> Users { get; set; } = new List<User>();
        public IList<Project> Projects { get; set; } = new List<Project>();
        public IList<Application> Applications { get; set; } = new List<Application>();
        public IList<Customer> Customers { get; set; } = new List<Customer>();
        public IList<string> Labels { get; set; } = new List<string>();
        public IList<Issue> Issues { get; set; } = new List<Issue>();
        public bool Dump { get; set; }
        public bool? Billable { get; set; } = null;

        public List<BookingGroup> GroupedBookings { get; set; }

        public List<Booking> AllBookings()
        {
            return GroupedBookings.SelectMany(x => x.Bookings).ToList();
        }

        public string GroupedBy { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now.StartOfMonth();
        public DateTime EndDate { get; set; } = DateTime.Now.EndOfMonth();

        public int MaxResults { get; set; } = 100;

        public int ResultCount { get; set; }
        public int TotalResultCount { get; set; }
        public int OmmitedResults => TotalResultCount - ResultCount;

        public Project SelectedProject;

        public string Query { get; set; }

        public Dictionary<string, string> AllGroupbys = new Dictionary<string, string>()
                            {
                                {"user", "Medewerker"},
                                {"project", "Project"},
                                {"application", "Applicatie"},
                                {"customer", "Klant"},
                                {"day", "Dag"},
                                {"week", "Week"}
                            };


        public BookingQueryParser(ISession session, string query, User currentUser)
        {
            Query = query ?? string.Empty;

            var matches = Regex.Matches(Query, queryRegex);

            IList<string> userStrings = new List<string>();
            IList<string> projects = new List<string>();
            IList<string> applications = new List<string>();
            IList<string> customers = new List<string>();
            IList<long> issues = new List<long>();
            string period = null;
            bool nullIssues = false;
            var searchQuery = Query;
            var take = MaxResults;
            var all = false;

            foreach (Match match in matches)
            {
                var parts = match.Value.Split(':');
                var value = parts[1].Replace("'", "");
                searchQuery = searchQuery.Replace(match.Value, "").Trim();
                switch (parts[0])
                {
                    case "user":
                        userStrings.Add(value == "me" ? currentUser.Name : value);
                        break;
                    case "project":
                        projects.Add(value);
                        break;
                    case "application":
                        applications.Add(value);
                        break;
                    case "customer":
                        customers.Add(value);
                        break;
                    case "issue":
                        long issueNumber;
                        if (long.TryParse(value, out issueNumber))
                        {
                            issues.Add(issueNumber);
                        }
                        else
                        {
                            if (value == "null")
                                nullIssues = true;
                        }
                        break;
                    case "label":
                        Labels.Add(value);
                        break;
                    case "groupby":
                        GroupedBy = value;
                        break;
                    case "take":
                        if (value == "all")
                            all = true;
                        else
                            take = int.Parse(value);
                        break;
                    case "start":
                        DateTime startDate;
                        if(DateTime.TryParse(value, out startDate))
                            StartDate = startDate;
                        break;
                    case "end":
                        DateTime endDate;
                        if(DateTime.TryParse(value, out endDate))
                            EndDate = endDate;
                        break;
                    case "period":
                        period = value;
                        break;
                    case "billable":
                        if (value == "yes")
                            Billable = true;
                        if (value == "no")
                            Billable = false;
                        break;
                }
            }

            if (!string.IsNullOrEmpty(period))
            {
                switch (period)
                {
                    case "thisweek":
                        StartDate = DateTime.Today.StartOfWeek();
                        EndDate = DateTime.Today.EndOfWeek();
                        break;
                    case "lastweek":
                        StartDate = DateTime.Today.AddDays(-7).StartOfWeek();
                        EndDate = DateTime.Today.AddDays(-7).EndOfWeek();
                        break;
                    case "thismonth":
                        StartDate = DateTime.Today.StartOfMonth();
                        EndDate = DateTime.Today.EndOfMonth();
                        break;
                    case "lastmonth":
                        StartDate = DateTime.Today.AddMonths(-1).StartOfMonth();
                        EndDate = DateTime.Today.AddMonths(-1).EndOfMonth();
                        break;
                    case "always":
                        StartDate = new DateTime(2010, 1, 1);
                        EndDate = DateTime.Today.AddYears(1);
                        break;
                }
            }

            var bookings = session.Query<Booking>().Where(x => x.IsActive && x.Date >= StartDate && x.Date <= EndDate);

            if (Billable != null)
            {
                bookings = bookings.Where(x => x.Unbillable == !Billable);
            }

            if (userStrings.Count > 0)
            {
                bookings = bookings.Where(x => x.User != null && (userStrings.Contains(x.User.Name) || userStrings.Contains(x.User.FullName)));
                foreach (var userString in userStrings)
                {
                    Users.Add(session.Query<User>().FirstOrDefault(x => x.Name == userString || x.FullName == userString));
                }
            }

            if (projects.Count > 0)
            {
                bookings = bookings.Where(x => x.Project != null && projects.Contains(x.Project.Slug));
                foreach (var project in projects)
                {
                    Projects.Add(session.SlugOrDefault<Project>(project));
                }
            }

            if (applications.Count > 0)
            {
                bookings = bookings.Where(x => x.Project != null && x.Project.Application != null && applications.Contains(x.Project.Application.Slug));
                foreach (var application in applications)
                {
                    Applications.Add(session.SlugOrDefault<Application>(application));
                }
            }

            if (customers.Count > 0)
            {
                bookings = bookings.Where(x => x.Project != null && x.Project.Application != null && x.Project.Application.Customer != null && customers.Contains(x.Project.Application.Customer.Slug));
                foreach (var customer in customers)
                {
                    Customers.Add(session.SlugOrDefault<Customer>(customer));
                }
            }

            Dump = nullIssues;

            Expression<Func<Booking, bool>> issueExpression = null;
            if (issues.Count > 0 && Projects.Count == 1) //Er mag maar één Project selecteerd zijn, anders wordt het heel onoverzichtelijk met dubbele Issue Numbers
            {
                SelectedProject = Projects.Single(); //Het is er altijd maar één
                foreach (var issue in issues)
                {
                    Issues.Add(session.Query<Issue>().FirstOrDefault(x => x.Id == issue && x.Project == SelectedProject));
                }
                issueExpression = x => issues.Contains(x.Issue.Id);
            }

            if (nullIssues)
            {
                Expression<Func<Booking, bool>> nullIssueExpression = x => x.Issue == null;
                if (issueExpression != null)
                {
                    var binaryExpression = Expression.OrElse(nullIssueExpression.Body, issueExpression.Body); //Zowel boekingen zonder Issue als boekingen met gekozen Issue(s) zoeken, hierbij wordt dezelfde (booking =>) als parameter meegegeven aan beide expressies
                    var boolExpression = Expression.Lambda<Func<Booking, bool>>(binaryExpression, issueExpression.Parameters[0]);
                    bookings = bookings.Where(boolExpression);
                }
                else
                {
                    bookings = bookings.Where(nullIssueExpression); //Geen Issue(s) geselecteerd: haal alle boekingen op zonder Issue, van alle projecten
                }
            }
            else if(issueExpression != null)
            {
                bookings = bookings.Where(issueExpression); //Boekingen zonder Issue niet weergeven, wel filteren op gekozen Issue(s)
            }

            if (Labels.Count > 0)
            {
                bookings = bookings.Where(x => x.Issue.Labels.Any(l => Labels.Contains(l.Name)));
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                bookings = bookings.Where(x => x.Comment.Contains(searchQuery));
            }

            bookings = bookings.OrderByDescending(x => x.Date).ToList().AsQueryable();

            TotalResultCount = bookings.Count();

            if (!all)
            {
                bookings = bookings.Take(take);
            }

            ResultCount = TotalResultCount > take && !all ? take : TotalResultCount;

            switch (GroupedBy)
            {
                case "project":
                    GroupedBookings = bookings.OrderBy(x => x.Project.Name).ToList().GroupBy(x => x.Project).Select(x => new BookingGroup(x.Key.Name, x.ToList())).ToList();
                    break;
                case "user":
                    GroupedBookings = bookings.OrderBy(x => x.User.FullName).ToList().GroupBy(x => x.User).Select(x => new BookingGroup(x.Key.FullName, x.ToList())).ToList();
                    break;
                case "application":
                    GroupedBookings = bookings.Where(x => x.Project.Application != null).OrderBy(x => x.Project.Application.Name).ToList().GroupBy(x => x.Project.Application).Select(x => new BookingGroup(x.Key.Name, x.ToList())).ToList();
                    break;
                case "customer":
                    GroupedBookings = bookings.Where(x => x.Project.Application != null && x.Project.Application.Customer != null).OrderBy(x => x.Project.Application.Customer.Name).ToList().GroupBy(x => x.Project.Application.Customer).Select(x => new BookingGroup(x.Key.Name, x.ToList())).ToList();
                    break;
                case "day":
                    GroupedBookings = bookings.GroupBy(x => x.Date).Select(x => new BookingGroup(x.Key.ToShortDateString(), x.ToList())).ToList();
                    break;
                case "week":
                    GroupedBookings = bookings.GroupBy(x => x.Date.WeekNr()).Select(x => new BookingGroup($"week {x.Key}", x.ToList())).ToList();
                    break;
                default:
                    GroupedBookings = new List<BookingGroup> { new BookingGroup("Alle uren", bookings.ToList()) };
                    break;
            }
        }

        public class BookingGroup
        {
            public BookingGroup(string title, List<Booking> bookings)
            {
                Bookings = bookings;
                Title = title;
            }

            public string Title { get; set; }
            public List<Booking> Bookings { get; set; }

            public double Minutes
            {
                get { return Bookings.Sum(x => x.Minutes); }
            }
            public virtual double Hours => Minutes / 60.0;

            public virtual string Time => $"{Math.Floor(Hours)}:{Minutes - (Math.Floor(Hours)*60):00}";
        }
    }
}