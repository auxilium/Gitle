using System.Globalization;

namespace Gitle.Web.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Text;
  using Helpers;
  using Model.Helpers;
  using QueryParsers;
  using Gitle.Model;
  using NHibernate;
  using System.Linq;
  using Gitle.Model.James;
  using Model.Interfaces.Service;
  using NHibernate.Linq;

  public class ReportController : SecureController
  {
    protected IJamesRegistrationService JamesRegistrationService { get; }
    protected ISettingService SettingService { get; }

    public ReportController(IJamesRegistrationService jamesRegistrationService, ISessionFactory sessionFactory, ISettingService settingService) : base(sessionFactory)
    {
      SettingService = settingService;
      JamesRegistrationService = jamesRegistrationService;
    }

    [Admin]
    public void Index(string query)
    {
      var parser = new BookingQueryParser(session, query, CurrentUser);

      var reportPresets = session.Query<ReportPreset>().Where(x => x.User == CurrentUser && x.IsActive);
      var globalReportPresets = session.Query<ReportPreset>().Where(x => x.User == null && x.IsActive);

      PropertyBag.Add("result", parser);

      PropertyBag.Add("reportPresets", reportPresets);
      PropertyBag.Add("globalReportPresets", globalReportPresets);

      PropertyBag.Add("allUsers", session.Query<User>().Where(x => x.CanBookHours && x.IsActive).OrderBy(x => x.FullName).ToList());

      var allCustomers = session.Query<Customer>().Where(x => x.IsActive);
      var allClosedCustomers = session.Query<Customer>().Where(x => !x.IsActive);
      var allApplications = session.Query<Application>().Where(x => x.IsActive);
      var allProjects = session.Query<Project>().Where(x => x.IsActive && !x.Closed);
      var allClosedAndOpenProjects = session.Query<Project>().Where(x => x.IsActive && x.Closed || !x.Closed);   
      if (parser.Applications.Count > 0)
      {
        allProjects = allProjects.Where(x => parser.Applications.Contains(x.Application));
      }
      if (parser.Customers.Count > 0)
      {
        allApplications = allApplications.Where(x => parser.Customers.Contains(x.Customer));
        allProjects = allProjects.Where(x => parser.Customers.Contains(x.Application.Customer));
        allClosedAndOpenProjects = allClosedAndOpenProjects.Where(x => parser.Customers.Contains(x.Application.Customer));      
      }
      PropertyBag.Add("allCustomers", allCustomers.OrderBy(x => x.Name).ToList());
      PropertyBag.Add("allClosedCustomers", allClosedCustomers.OrderBy(x => x.Name).ToList());
      PropertyBag.Add("allApplications", allApplications.OrderBy(x => x.Name).ToList());
      PropertyBag.Add("allProjects", allProjects.OrderBy(x => x.Name).ToList());
      PropertyBag.Add("allClosedAndOpenProjects", allClosedAndOpenProjects.OrderBy(x => x.Name).ToList());
    
      PropertyBag.Add("selectedProjects", parser.Projects);

      var presetDates = new List<dynamic>();
      var today = DateTime.Today;

      presetDates.Add(new DatePreset { startDate = today.StartOfWeek(), endDate = today.EndOfWeek(), title = "Deze week" });

      for (int i = 1; i <= 8; i++)
      {
        today = today.AddDays(-7);
        presetDates.Add(new DatePreset { startDate = today.StartOfWeek(), endDate = today.EndOfWeek(), title = $"Week {today.WeekNr()}" });
      }

      PropertyBag.Add("presetDates", presetDates);

      var setting = SettingService.Load();
      PropertyBag.Add("setting", setting);
    }

    [Danielle]
    public void Block(string query)
    {
      var parser = new BookingQueryParser(session, query, CurrentUser);

      var setting = SettingService.Load();
      setting.ClosedForBookingsBefore = parser.EndDate.Date;

      session.SaveOrUpdate(setting);
      session.Flush();

      RedirectToReferrer();
    }

    [Danielle]
    public void Unblock()
    {
      var setting = SettingService.Load();
      setting.ClosedForBookingsBefore = null;
      session.SaveOrUpdate(setting);
      session.Flush();
      RedirectToReferrer();
    }

    [Admin]
    public void ExportCsv(string query)
    {
      var parser = new BookingQueryParser(session, $"{query} take:all", CurrentUser);
      var bookings = parser.AllBookings();

      var csv = CsvHelper.BookingsCsv(bookings);
      CancelView();

      Response.ClearContent();
      Response.Clear();

      var filename = $"bookings_{parser.StartDate:yyyyMMdd_hhmm}_{parser.EndDate:yyyyMMdd_hhmm}.csv";

      Response.AppendHeader("content-disposition", $"attachment; filename={filename}");
      Response.ContentType = "application/csv";

      var byteArray = Encoding.Default.GetBytes(csv);
      var stream = new MemoryStream(byteArray);
      Response.BinaryWrite(stream);
    }

    [Admin]
    public void ExportWeeks(string query)
    {
      var parser = new BookingQueryParser(session, $"{query} take:all", CurrentUser);

      var year = parser.StartDate.Year;

      var employees = session.Query<User>().Where(x => x.IsActive && x.JamesEmployeeId > 0).ToList();
      var exportWeeks = new List<ExportWeeksGitleVsJames>();

      var cultureInfo = new CultureInfo("nl-NL");
      var calendar = cultureInfo.Calendar;

      var startOfYear = new DateTime(year, 1, 1).StartOfWeek();
      var endOfYear = new DateTime(year, 12, 31).EndOfWeek();

      if (calendar.GetWeekOfYear(startOfYear.AddDays(3), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) > 1)
        startOfYear = startOfYear.AddDays(7);

      if (calendar.GetWeekOfYear(endOfYear.StartOfWeek().AddDays(3), CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday) == 1)
        endOfYear = endOfYear.AddDays(-7);

      foreach (var employee in employees)
      {
        var bookings = session.Query<Booking>().Where(x => x.IsActive && x.Date >= startOfYear && x.Date <= endOfYear && x.User.Id == employee.Id).ToList();
        var exportWeek = new ExportWeeksGitleVsJames { JamesEmployeeId = employee.JamesEmployeeId, NameOfEmployee = employee.FullName };

        for (int i = 0; i < 53; i++)
        {
          exportWeek.Weeks[i].MinutesGitle = bookings.Where(x => x.Date.WeekNr() == i + 1).Sum(x => x.Minutes);

          exportWeek.Weeks[i].MinutesJames = JamesRegistrationService.GetTotalMinutesForEmployee(employee.JamesEmployeeId, year, i + 1);
        }

        exportWeeks.Add(exportWeek);
      }

      var csv = CsvHelper.ExportWeeks(exportWeeks, employees);
      CancelView();

      Response.ClearContent();
      Response.Clear();

      var filename = $"gitle_vs_james_{DateTime.Today:yyyyMMdd_hhmm}.csv";

      Response.AppendHeader("content-disposition", $"attachment; filename={filename}");
      Response.ContentType = "application/csv";

      var byteArray = Encoding.Default.GetBytes(csv);
      var stream = new MemoryStream(byteArray);
      Response.BinaryWrite(stream);
    }
  }

  public class DatePreset
  {
    public string title;
    public DateTime endDate;
    public DateTime startDate;
  }
}