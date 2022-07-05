namespace Gitle.Web.QueryParsers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Model;
    using Model.Enum;
    using Model.Helpers;
    using NHibernate;

    public class IssueQueryParser
    {
        private const string queryRegex = @"[a-zA-Z0-9_]+:(([a-zA-Z0-9-_,.]+)|('[a-zA-Z0-9-_,.\(\)\+ ]+'))";

        public List<Issue> Items { get; set; }

        public int MaxResults { get; set; } = 100;

        public int ResultCount { get; set; }
        public int TotalResultCount { get; set; }
        public int OmmitedResults => TotalResultCount - ResultCount;

        public string Query { get; set; }

        public IList<string> SelectedLabels = new List<string>();
        public IList<string> AnySelectedLabels = new List<string>();
        public IList<string> NotSelectedLabels = new List<string>();
        public IList<IssueState> States = new List<IssueState>();
        public IList<IssueBookingState> BookingStates = new List<IssueBookingState>();
        public IDictionary<string, bool> SelectedSorts = new Dictionary<string, bool>();
        public IList<User> SelectedPickupbys = new List<User>();
        public bool PickupAny;
        public bool PickupNone;

        public Dictionary<string, string> AllSorts = new Dictionary<string, string>()
                            {
                                {"CreatedBy", "Created by"},
                                {"CreatedAt", "Created at"},
                                {"PickedUpBy", "Picked up by"},
                                {"PickedUpAt", "Picked up at"},
                                {"Number", "Number"},
                                {"Name", "Name"},
                                {"TotalHours", "Total hours"},
                                {"Comments.Count", "Comments count"},
                                {"IsAdministrative", "Administrative"}
                            };


        public IssueQueryParser(ISessionFactory sessionFactory, string query, Project project, User currentUser)
        {
            var session = sessionFactory.GetCurrentSession();
            Query = query ?? string.Empty;

            var matches = Regex.Matches(Query, queryRegex);


            IList<long> ids = new List<long>();
            IList<string> involveds = new List<string>();
            IList<string> openedbys = new List<string>();
            IList<string> closedbys = new List<string>();
            IList<string> pickupbys = new List<string>();
            IDictionary<string, bool> querySorts = new Dictionary<string, bool>();
            IDictionary<string, bool> linqSorts = new Dictionary<string, bool>();
            var searchQuery = Query;
            var take = MaxResults;
            var all = false;

            foreach (Match match in matches)
            {
                var parts = match.Value.Split(':');
                var value = parts[1].Replace("'", "");
                searchQuery = searchQuery.Replace(match.Value, "");
                switch (parts[0])
                {
                    case "label":
                        SelectedLabels.Add(value);
                        break;
                    case "anylabel":
                        AnySelectedLabels.Add(value);
                        break;
                    case "notlabel":
                        NotSelectedLabels.Add(value);
                        break;
                    case "state":
                        States.Add((IssueState)Enum.Parse(typeof(IssueState), value));
                        break;
                    case "bookingstate":
                        BookingStates.Add((IssueBookingState)Enum.Parse(typeof(IssueBookingState), value));
                        break;
                    case "id":
                        ids = value.Split(',').Select(long.Parse).ToList();
                        break;
                    case "involved":
                        involveds.Add(value == "me" ? currentUser.Name : value);
                        break;
                    case "opened":
                        openedbys.Add(value == "me" ? currentUser.Name : value);
                        break;
                    case "closed":
                        closedbys.Add(value == "me" ? currentUser.Name : value);
                        break;
                    case "pickup":
                        if (value == "any")
                        {
                            PickupAny = true;
                            break;
                        }
                        if (value == "none")
                        {
                            PickupNone = true;
                            break;
                        }
                        pickupbys.Add(value == "me" ? currentUser.Name : value);
                        break;
                    case "take":
                        if (value == "all")
                            all = true;
                        else
                            take = int.Parse(value);
                        break;
                    case "sort":
                        SelectedSorts.Add(value, false);
                        if (NHibernateMetadataHelper.IsMapped<Issue>(sessionFactory, value))
                            querySorts.Add(value, false);
                        else
                            linqSorts.Add(value, false);
                        break;
                    case "sortdesc":
                        SelectedSorts.Add(value, true);
                        if (NHibernateMetadataHelper.IsMapped<Issue>(sessionFactory, value))
                            querySorts.Add(value, true);
                        else
                            linqSorts.Add(value, true);
                        break;
                }
            }


            var itemsQuery =
                session.Query<Issue>().Where(x => x.Project == project && x.Labels.Count(l => l.IsActive && l.Project == project && SelectedLabels
                        .Contains(l.Name)) == SelectedLabels.Count && (AnySelectedLabels.Count == 0 || x.Labels
                        .Any(l => l.IsActive && l.Project == project && AnySelectedLabels.Contains(l.Name))) && !x.Labels.Any(l => l.IsActive && l.Project == project && NotSelectedLabels
                        .Contains(l.Name)));


            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var searchQueryParts = searchQuery.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x));
                foreach (var searchQueryPart in searchQueryParts)
                {
                    var trimmedSearchQueryPart = searchQueryPart.Trim();
                    itemsQuery = itemsQuery.Where(x => x.Name.Contains(trimmedSearchQueryPart) || x.Body.Contains(trimmedSearchQueryPart));
                }
            }

            if (ids.Any())
                itemsQuery = itemsQuery.Where(x => ids.Contains(x.Number));

            foreach (var involved in involveds)
            {
                itemsQuery =
                    itemsQuery.Where(
                        x =>
                        x.Comments.Any(a => a.User != null && (a.User.Name == involved || a.User.FullName == involved)) ||
                        x.ChangeStates.Any(a => a.User != null && (a.User.Name == involved || a.User.FullName == involved)) ||
                        x.Changes.Any(a => a.User != null && (a.User.Name == involved || a.User.FullName == involved)));
            }

            foreach (var openedby in openedbys)
            {
                itemsQuery = itemsQuery.Where(
                    x => x.ChangeStates.Any(
                        a => a.IssueState == IssueState.Open && a.User != null &&
                             (a.User.Name == openedby || a.User.FullName == openedby)));
            }

            foreach (var closedby in closedbys)
            {
                itemsQuery = itemsQuery.Where(
                    x => x.ChangeStates.Any(
                        a => a.IssueState == IssueState.Closed && a.User != null &&
                             (a.User.Name == closedby || a.User.FullName == closedby)));
            }

            foreach (var pickupby in pickupbys)
            {
                SelectedPickupbys.Add(session.Query<User>().FirstOrDefault(x => x.Name == pickupby || x.FullName == pickupby));
            }

            if (PickupAny)
            {
                itemsQuery = itemsQuery.Where(x => x.Pickups.Any());
            }

            if (PickupNone)
            {
                itemsQuery = itemsQuery.Where(x => x.Pickups.Count == 0);
            }

            if (pickupbys.Any())
            {
                itemsQuery = itemsQuery.Where(
                    x => x.Pickups.Any(
                        a => a.User != null && pickupbys.Contains(a.User.Name) || pickupbys.Contains(a.User.FullName)));
            }

            if (querySorts.Count > 0)
            {
                foreach (var sort in querySorts)
                {
                    itemsQuery = itemsQuery.OrderByProperty(sort.Key, sort.Value);
                }
            }

            var items = itemsQuery.ToList();

            if (linqSorts.Count > 0)
            {
                foreach (var sort in linqSorts)
                {
                    items = items.OrderByProperty(sort.Key, sort.Value).ToList();
                }
            }
            if (querySorts.Count == 0 && linqSorts.Count == 0)
            {
                items = items.OrderBy(x => x.State != IssueState.Urgent)
                    .ThenBy(x => x.State)
                    .ThenBy(x => x.Pickups.Count == 0)
                    .ThenBy(x => x.PrioOrder == 0)
                    .ThenBy(x => x.PrioOrder)
                    .ThenByDescending(x => x.ChangeStates.Max(cs => cs.CreatedAt))
                    .ToList();
            }

            if (States.Any())
            {
                items = items.Where(x => States.Contains(x.State) || x.Urgent).ToList();
            }


            if (BookingStates.Any())
            {
                var bookingStateItems = new List<Issue>();

                foreach (var bookingState in BookingStates)
                {
                    switch (bookingState)
                    {
                        case IssueBookingState.Any:
                            bookingStateItems.AddRange(items.Where(x => x.Bookings.Any(y => y.IsActive)));
                            break;
                        case IssueBookingState.None:
                            bookingStateItems.AddRange(items.Where(x => !x.Bookings.Any(y => y.IsActive)));
                            break;
                        default:
                            continue;
                    }
                }

                items = items.Where(x => bookingStateItems.Select(y => y.Id).Contains(x.Id)).ToList();
            }


            TotalResultCount = items.Count();

            if (!all)
            {
                items = items.Take(take).ToList();
            }

            ResultCount = TotalResultCount > take && !all ? take : TotalResultCount;

            Items = items.ToList();
        }
    }
}