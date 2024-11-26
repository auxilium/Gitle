namespace Gitle.Web.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Web;
  using Castle.MonoRail.Framework;
  using Gitle.Model.Helpers;
  using Gitle.ViewModel;
  using Model;
  using Model.Enum;
  using NHibernate;

  public class DashboardController : SecureController
  {
    private ISessionFactory sessionFactory;

    public DashboardController(ISessionFactory sessionFactory) : base(sessionFactory)
    {
      this.sessionFactory = sessionFactory;
    }
    public void Index()
    {
      var initialProjects = session.Query<Project>().Where(x => x.IsActive && x.Type == ProjectType.Initial && x.Issues.Any()).OrderBy(x => x.Name).ToList();
      var initialOpenProjects = initialProjects.Where(p => !p.Closed);
      var initialProjectsClosed = initialProjects.Where(p => p.Closed);

      List<Project> serviceProjects = session.Query<Project>()
       .Where(x => x.IsActive && !x.Closed && x.Type == ProjectType.Service && x.Issues.Any())
       .Where(x => CurrentUser.IsAdmin || x.Users.Any(u => u.User == CurrentUser))
      .OrderBy(x => x.Name)
      .ToList();

      List<Project> internalProjects = session.Query<Project>()
     .Where(x => x.IsActive && !x.Closed && x.Type == ProjectType.Internal && x.Issues.Any())
     .Where(x => CurrentUser.IsAdmin || x.Users.Any(u => u.User == CurrentUser))
    .OrderBy(x => x.Name)
    .ToList();

      List<Project> administrationProjects = session.Query<Project>()
     .Where(x => x.IsActive && !x.Closed && x.Type == ProjectType.Administration && x.Issues.Any())
     .Where(x => CurrentUser.IsAdmin || x.Users.Any(u => u.User == CurrentUser))
    .OrderBy(x => x.Name)
    .ToList();

      var serviceOpenProjects = serviceProjects.Where(sp => !sp.Closed);
      var serviceClosedProjects = serviceProjects.Where(sp => sp.Closed);

      PropertyBag.Add("allTypes", EnumHelper.ToDictionary(typeof(ProjectType)).Where(x => x.Key > 0));
 
      PropertyBag.Add("initialProjects", initialOpenProjects);
      PropertyBag.Add("initialProjectsOptions", initialProjects.Where(ip => !ip.Closed).OrderBy(x => x.Name).Select(p => p.Name));
      PropertyBag.Add("internalProjectsOptions", internalProjects.Where(ip => !ip.Closed).OrderBy(x => x.Name).Select(p => p.Name));
      PropertyBag.Add("administrationProjectsOptions", administrationProjects.Where(ip => !ip.Closed).OrderBy(x => x.Name).Select(p => p.Name));
      PropertyBag.Add("serviceOpenProjectsOptions", serviceOpenProjects.Where(ip => !ip.Closed).OrderBy(x => x.Name).Select(p => p.Name));

      PropertyBag.Add("initialProjectsClosed", initialProjectsClosed);
      PropertyBag.Add("serviceProjects", serviceOpenProjects);
      PropertyBag.Add("internalProjects", internalProjects);
      PropertyBag.Add("administrationProjects", administrationProjects);
      PropertyBag.Add("serviceProjectsClosed", serviceClosedProjects);
      PropertyBag.Add("serviceProjectsMaxOpenIssues", serviceProjects.Any() ? serviceProjects.Max(project => project.OpenIssues.Count) : 0);
    }

    [return: JSONReturnBinder]
    public object List(int start, int length, int draw, string orderColumn, string orderDir, string search, ProjectType type, string projectName)
    {      
      var issues = session.Query<Issue>().Where(x => x.IsActive);

      if (!CurrentUser.IsDanielle)
      {
        issues = issues.Where(i => i.Project.Name == projectName).Where(x => x.Project.Type != ProjectType.Administration);
      }

      if (!CurrentUser.IsAdmin)
      {
        issues = issues.Where(i => i.Project.Users.Any(x => x.User == CurrentUser));
      }

      var recordsTotal = issues.Count();

      if (!string.IsNullOrEmpty(orderColumn))
      {
        issues = issues.OrderByProperty(orderColumn, orderDir != "asc");
      }   

      if (type != ProjectType.Unknown)
      {
        issues = issues.Where(i => i.Project.Name == projectName).Where(x => x.Project.Type == type);
      }

      if (!string.IsNullOrEmpty(search))
      {
        issues = issues.Where(i => i.Project.Name == projectName).Where(x => x.Number.ToString() == search || x.Name.Contains(search) ||
                                       x.Project.Application.Name.Contains(search) ||
                                       x.Project.Application.Customer.Name.Contains(search));
      }

      if (projectName != null) 
      {
        issues = issues.Where(p => p.Project.Name == projectName);
      }

      var recordsFiltered = issues.Count();

      if (length > 0)
      {
        issues = issues.Skip(start).Take(length);
      }

      var data = issues.Where(p => p.Project.Name == projectName).Select(p => new IssueListViewModel(p)).ToList();

      var result = new { draw, start, length, recordsTotal, recordsFiltered, data };

      return result;
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