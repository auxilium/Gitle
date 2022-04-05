namespace Gitle.Web.Controllers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web;
    using Gitle.Model.Helpers;
    using Model;
    using Model.Enum;
    using NHibernate;
    using NHibernate.Linq;

    public class DashboardController : SecureController
    {
        public DashboardController(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }
        public void Index()
        {
            var initialProjects = session.Query<Project>().Where(x => x.IsActive && !x.Closed && x.Type == ProjectType.Initial && x.Issues.Count > 0).OrderBy(x => x.Name).ToList();

            var serviceProjects = session.Query<Project>().Where(x => x.IsActive && !x.Closed && x.Type == ProjectType.Service && x.Issues.Count > 0).OrderBy(x => x.Name).ToList();

            PropertyBag.Add("initialProjects", initialProjects);
            PropertyBag.Add("serviceProjects", serviceProjects);
            PropertyBag.Add("serviceProjectsMaxOpenIssues", serviceProjects.Any() ? serviceProjects.Max(project => project.OpenIssues.Count) : 0);
        }

        public void ExportOpenTicketsByProject()
        {
            var openIssues = session.Query<Issue>().Where(i => (i.Project.Type == ProjectType.Initial || i.Project.Type == ProjectType.Service) && !i.Project.Closed).ToList().Where(i => i.ChangeStates.OrderByDescending(x => x.CreatedAt).First().IssueState == IssueState.Open).ToList();

            var csv = CsvHelper.OpenIssuesCsv(openIssues);
            CancelView();

            Response.ClearContent();
            Response.Clear();

            var filename = $"open_issues_{DateTime.Today:yyyyMMdd_hhmm}.csv";

            Response.AppendHeader("content-disposition", $"attachment; filename={HttpUtility.UrlEncode(filename)}");
            Response.ContentType = "application/csv";

            var byteArray = Encoding.Default.GetBytes(csv);
            var stream = new MemoryStream(byteArray);
            Response.BinaryWrite(stream);
        }
    }
}