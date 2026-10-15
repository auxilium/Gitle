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
    public DashboardController(ISessionFactory sessionFactory) : base(sessionFactory)
    {
    }
    public void Index()
    {
      // Each tab requests its own scalar data after it becomes visible.
    }

    public void Section(string section, bool closed = false)
    {
      CancelLayout();
      if (section == "issues")
      {
        AddProjectOptions();
        RenderView("_issues");
        return;
      }
      if (section != "initial" && section != "progress" && section != "overview")
      {
        Response.StatusCode = 400;
        CancelView();
        return;
      }
      if (section == "initial" && !CurrentUser.IsAdmin)
      {
        Response.StatusCode = 403;
        CancelView();
        return;
      }
      var type = section == "initial" ? ProjectType.Initial : ProjectType.Service;
      // The overview's scale includes closed service projects, as on the original dashboard.
      var projects = LoadDashboardProjects(type, closed, section == "overview");
      if (section == "overview")
      {
        PropertyBag.Add("serviceProjects", projects.Where(p => !p.Closed).ToList());
        PropertyBag.Add("serviceProjectsMaxOpenIssues", LoadMaxOpenIssues());
        RenderView("_overview");
      }
      else
      {
        PropertyBag.Add("projects", projects);
        RenderView("_progress");
      }
    }

    private void AddProjectOptions()
    {
      PropertyBag.Add("allTypes", EnumHelper.ToDictionary(typeof(ProjectType)).Where(x => x.Key > 0));
      var options = session.Query<Project>()
        .Where(p => p.IsActive && !p.Closed && p.Issues.Any())
        .Where(p => p.Type == ProjectType.Initial || CurrentUser.IsAdmin || p.Users.Any(u => u.User == CurrentUser))
        .OrderBy(p => p.Name).Select(p => new { p.Name, p.Type }).ToList();
      PropertyBag.Add("initialProjectsOptions", options.Where(p => p.Type == ProjectType.Initial).Select(p => p.Name).ToList());
      PropertyBag.Add("internalProjectsOptions", options.Where(p => p.Type == ProjectType.Internal).Select(p => p.Name).ToList());
      PropertyBag.Add("administrationProjectsOptions", options.Where(p => p.Type == ProjectType.Administration).Select(p => p.Name).ToList());
      PropertyBag.Add("serviceOpenProjectsOptions", options.Where(p => p.Type == ProjectType.Service).Select(p => p.Name).ToList());
    }

    private int LoadMaxOpenIssues()
    {
      // Preserve the original scale without loading closed projects or their financial summaries.
      const string sql = @"
select coalesce(max(counts.OpenCount), 0) as MaxOpenIssues from (
  select p.Id, sum(case when (coalesce(latest.IssueState, :unknown) = :open and i.Administrative = 0)
    or latest.IssueState = :urgent or i.Urgent = 1 then 1 else 0 end) as OpenCount
  from [Project] p join [Issue] i on i.Project_id = p.Id
  outer apply (select top (1) cs.IssueState from [ChangeState] cs
    where cs.Issue_id = i.Id order by cs.CreatedAt desc, cs.Id desc) latest
  where p.IsActive = 1 and p.Type = :type and (:unrestricted = 1 or exists (
    select 1 from [UserProject] up where up.Project_id = p.Id and up.User_id = :userId))
  group by p.Id
) counts";
      return Convert.ToInt32(session.CreateSQLQuery(sql)
        .SetString("type", ProjectType.Service.ToString()).SetBoolean("unrestricted", CurrentUser.IsAdmin)
        .SetInt64("userId", CurrentUser.Id).SetString("unknown", IssueState.Unknown.ToString())
        .SetString("open", IssueState.Open.ToString()).SetString("urgent", IssueState.Urgent.ToString())
        .UniqueResult());
    }

    private IList<DashboardProjectViewModel> LoadDashboardProjects(ProjectType type, bool closed, bool overview)
    {
      // Aggregate independent collections separately to avoid multiplying bookings and invoice lines.
      // Issue.IsActive and ChangeState.IsActive are deliberately not filters: the model did not filter them.
      var sql = @"
with SelectedProjects as (
  select p.Id, p.Name, p.Slug, p.Closed, p.BudgetMinutes
  from [Project] p
  where p.IsActive = 1 and p.Type = :type
    and p.Closed = :closed
    and (:unrestricted = 1 or exists (
      select 1 from [UserProject] up where up.Project_id = p.Id and up.User_id = :userId))
    and exists (select 1 from [Issue] i where i.Project_id = p.Id)
), IssueStates as (
  select i.Id, i.Project_id, i.Administrative, i.Urgent, i.Hours * i.Devvers as Estimate,
    coalesce(latest.IssueState, :unknown) as State
  from [Issue] i join SelectedProjects p on p.Id = i.Project_id
  outer apply (select top (1) cs.IssueState from [ChangeState] cs
    where cs.Issue_id = i.Id order by cs.CreatedAt desc, cs.Id desc) latest
), IssueFacts as (
  select i.*, {0} as HasUninvoicedBooking
  from IssueStates i
), IssueTotals as (
  select Project_id,
    sum(case when State <> :archived then Estimate else 0 end) as Estimated,
    sum(case when State not in (:archived, :done, :issueClosed) then Estimate else 0 end) as RemainingEstimate,
    sum(case when State in (:done, :issueClosed) then Estimate else 0 end) as CompletedEstimate,
    {1}
  from IssueFacts group by Project_id
), BookingTotals as (
  select b.Project_id, sum(b.Minutes) / 60.0 as Hours
  from [Booking] b join SelectedProjects p on p.Id = b.Project_id
  where b.IsActive = 1 group by b.Project_id
), InvoiceTotals as (
  select inv.Project_id, sum(il.Hours) as Hours
  from [Invoice] inv join SelectedProjects p on p.Id = inv.Project_id
  join [InvoiceLine] il on il.Invoice_id = inv.Id
  where inv.IsActive = 1 and inv.State = :definitive and il.[Null] = 0
  group by inv.Project_id
)
select p.Id, p.Name, p.Slug, p.Closed, p.BudgetMinutes / 60.0 as BudgetHours,
  coalesce(b.Hours, 0) as TotalHours, coalesce(inv.Hours, 0) as InvoicedHours,
  i.Estimated, i.RemainingEstimate, i.CompletedEstimate,
  i.OpenCount, i.DoneCount, i.HoldCount, i.ClosedCount, i.UrgentCount, i.TotalCount
from SelectedProjects p join IssueTotals i on i.Project_id = p.Id
left join BookingTotals b on b.Project_id = p.Id
left join InvoiceTotals inv on inv.Project_id = p.Id
order by p.Name";
      const string uninvoiced = @"case when exists (
    select 1 from [Booking] b
    where b.Issue_id = i.Id and b.IsActive = 1 and b.Unbillable = 0 and b.Minutes > 0
      and not exists (select 1 from [InvoiceLineBooking] ilb
        join [InvoiceLine] il on il.Id = ilb.InvoiceLine_id
        join [Invoice] inv on inv.Id = il.Invoice_id
        where ilb.Booking_id = b.Id and inv.State = :definitive)
  ) then 1 else 0 end";
      const string counts = @"
    sum(case when (State = :open and Administrative = 0) or State = :urgent or Urgent = 1 then 1 else 0 end) as OpenCount,
    sum(case when State = :done and Administrative = 0 then 1 else 0 end) as DoneCount,
    sum(case when State = :hold and Administrative = 0 then 1 else 0 end) as HoldCount,
    sum(case when State = :issueClosed and Administrative = 0 and HasUninvoicedBooking = 1 then 1 else 0 end) as ClosedCount,
    sum(case when State = :urgent and Administrative = 0 then 1 else 0 end) as UrgentCount,
    sum(case when State <> :archived and Administrative = 0
      and (State <> :issueClosed or HasUninvoicedBooking = 1) then 1 else 0 end) as TotalCount
";
      sql = string.Format(sql, overview ? uninvoiced : "0", overview ? counts :
        "0 as OpenCount, 0 as DoneCount, 0 as HoldCount, 0 as ClosedCount, 0 as UrgentCount, 0 as TotalCount");
      var query = session.CreateSQLQuery(sql)
        .SetString("type", type.ToString()).SetBoolean("closed", overview ? false : closed)
        .SetBoolean("unrestricted", CurrentUser.IsAdmin).SetInt64("userId", CurrentUser.Id)
        .SetString("unknown", IssueState.Unknown.ToString())
        .SetString("done", IssueState.Done.ToString())
        .SetString("issueClosed", IssueState.Closed.ToString()).SetString("archived", IssueState.Archived.ToString())
        .SetString("definitive", InvoiceState.Definitive.ToString());
      if (overview)
        query.SetString("open", IssueState.Open.ToString()).SetString("hold", IssueState.Hold.ToString())
          .SetString("urgent", IssueState.Urgent.ToString());
      var rows = query.List<object[]>();
      return rows.Select(r => new DashboardProjectViewModel
      {
        Id = Convert.ToInt64(r[0]), Name = (string)r[1], Slug = (string)r[2], Closed = Convert.ToBoolean(r[3]),
        BudgetHours = Convert.ToDouble(r[4]), TotalHours = Convert.ToDouble(r[5]), InvoicedHours = Convert.ToDouble(r[6]),
        EstimatedHours = Convert.ToDouble(r[7]), RemainingEstimatedHours = Convert.ToDouble(r[8]), CompletedEstimatedHours = Convert.ToDouble(r[9]),
        OpenIssueCount = Convert.ToInt32(r[10]), DoneIssueCount = Convert.ToInt32(r[11]), HoldIssueCount = Convert.ToInt32(r[12]),
        ClosedIssueCount = Convert.ToInt32(r[13]), UrgentIssueCount = Convert.ToInt32(r[14]), TotalIssueCount = Convert.ToInt32(r[15])
      }).ToList();
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