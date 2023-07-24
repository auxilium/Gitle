namespace Gitle.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Castle.MonoRail.Framework;
    using Helpers;
    using Model;
    using Model.Helpers;
    using NHibernate;
    using NHibernate.Linq;

    public class PlanningController : SecureController
    {
        public PlanningController(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        public void Index()
        {
            var employees = session.Query<User>().Where(x => x.IsActive && x.IsAdmin && x.Color != null);
            PropertyBag.Add("employees", employees);
            PropertyBag.Add("comments", session.Query<PlanningComment>().Where(x => x.IsActive));
        }

        [return: JSONReturnBinder]
        public List<Event> Events(DateTime start, DateTime end)
        {
            return GetEvents(start, end)
                .Select(x => new Event
                {
                    id = x.Id,
                    resourceId = x.Resource,
                    start = x.Start.ToString("yyyy-MM-dd HH:mm"),
                    end = x.End.ToString("yyyy-MM-dd HH:mm"),
                    title = x.User?.FullName ?? x.Text,
                    color = x.User != null ? $"#{x.User?.Color}" : "#ccc",
                    userId = x.User?.Id.ToString()
                }).ToList();
        }

        [return: JSONReturnBinder]
        public List<Resource> Projects(DateTime start, DateTime end)
        {
            var events = GetEvents(start, end);

            var resourceIds = events.Select(x => x.Resource).Distinct().ToList();

            var issueIds = resourceIds.Where(x => x.StartsWith("i")).Select(x => long.Parse(x.Substring(1))).ToList();
            var issues = session.Query<Issue>().Where(x => issueIds.Contains(x.Id)).ToList();

            var projectIds = resourceIds.Where(x => x.StartsWith("p")).Select(x => long.Parse(x.Substring(1))).ToList();
            // Add all projectIds that dont have events but have issues who have events
            projectIds.AddRange(issues.Select(x => x.Project.Id).Except(projectIds));
            var projects = session.Query<Project>().Where(x => projectIds.Contains(x.Id));

            var resources = new List<Resource>();

            foreach (var project in projects)
            {
                var issuesForProject = issues.Where(x => x.Project.Id == project.Id);
                resources.Add(new Resource(project, issuesForProject));
            }

            resources.Insert(0, new Resource {title = "Algemeen", id = "general"});
            resources.Insert(1, new Resource {title = "Vakantie", id = "vacation"});

            return resources;
        }

        private List<PlanningItem> GetEvents(DateTime start, DateTime end)
        {
            return session.Query<PlanningItem>()
                .Where(x => x.End >= start && x.Start <= end && x.IsActive).ToList();
        }

        [return: JSONReturnBinder]
        public Resource GetResource(long projectId, long[] issueIds)
        {
            var issues = session.Query<Issue>().Where(x => issueIds.Contains(x.Id));
            var project = session.Get<Project>(projectId);

            return new Resource(project, issues);
        }

        public void UpdateEvent(long eventId, long userId, string resource, string text, DateTime start, DateTime end)
        {
            var planningItem = session.Get<PlanningItem>(eventId);

            if (planningItem == null)
            {
                planningItem = new PlanningItem
                {
                    User = session.Get<User>(userId),
                    Resource = resource,
                    Start = start,
                    End = end,
                    Text = text
                };
            }
            else
            {
                planningItem.User = session.Get<User>(userId);
                planningItem.Resource = resource;
                planningItem.Start = start;
                planningItem.End = end;
                planningItem.Text = text;
            }

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(planningItem);
                tx.Commit();
            }

            RenderText(planningItem.Id.ToString());
        }

        public void DeleteEvent(long eventId)
        {
            var planningItem = session.Get<PlanningItem>(eventId);
            planningItem.Deactivate();

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(planningItem);
                tx.Commit();
            }

            RenderText("ok");
        }

        [Admin]
        public void Comment(string slug, string comment)
        {
            var item = session.SlugOrDefault<PlanningComment>(slug);

            item.Comment = comment;
            item.User = CurrentUser;

            using (var tx = session.BeginTransaction())
            {
                session.SaveOrUpdate(item);
                tx.Commit();
            }

            RenderText(comment);
        }
    }

    public class Event
    {
        public long id;
        public string resourceId;
        public string userId;
        public string start;
        public string end;
        public string title;
        public string color;
        public string borderColor = "#666";
    }

    public class Resource
    {
        public Resource()
        {
        }

        public Resource(Project project, IEnumerable<Issue> issues)
        {
            id = $"p{project.Id}";
            title = project.Name;
            children = issues.Select(i => new Resource()
            {
                id = $"i{i.Id}",
                title = $"#{i.Id} - {i.Name}",
                parentId = $"p{project.Id}"
            }).ToList();
        }

        public string id;
        public string parentId;
        public string title;
        public IList<Resource> children = new List<Resource>();
    }
}