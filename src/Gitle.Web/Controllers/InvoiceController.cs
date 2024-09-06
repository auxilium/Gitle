using Gitle.Model;
using Gitle.Model.Enum;
using Gitle.Model.Interfaces.Service;
using Gitle.Web.Helpers;
using NHibernate;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Gitle.ViewModel;

namespace Gitle.Web.Controllers
{
  using System.Configuration;  
  using System.Text;
  using Castle.MonoRail.Framework;
  using Model.Helpers;
  public class InvoiceController : SecureController
  {
    private readonly IPdfExportService pdfExportService;

    public InvoiceController(ISessionFactory sessionFactory, IPdfExportService pdfExportService) : base(sessionFactory)
    {
      this.pdfExportService = pdfExportService;
    }

    [Danielle]
    public void Index()
    {
      var invoices = session.Query<Invoice>().Where(x => x.IsActive).OrderByDescending(x => x.Number).ToList().OrderBy(x => (int)(x.State));
      PropertyBag.Add("invoices", invoices);

      var today = DateTime.Today.AddMonths(-1);
      PropertyBag.Add("startOfMonth", today.StartOfMonth());
      PropertyBag.Add("endOfMonth", today.EndOfMonth());
      PropertyBag.Add("allCustomers", session.Query<Customer>().Where(x => x.IsActive).OrderBy(x => x.Name));

      PropertyBag.Add("allApplications",
        session.Query<Application>().Where(x => x.IsActive).OrderBy(x => x.Name));

      PropertyBag.Add("allTypes", EnumHelper.ToDictionary(typeof(ProjectType)).Where(x => x.Key > 0));
    }

    [return: JSONReturnBinder]
    public object List(long customer, long application, ProjectType type)
    {
      var invoices = session.Query<Invoice>().Where(x => x.IsActive);

      var recordsTotal = invoices.Count();

      if (customer > 0)
      {
        invoices = invoices.Where(x => x.Project.Application.Customer.Id == customer);
      }

      if (application > 0)
      {
        invoices = invoices.Where(x => x.Project.Application.Id == application);
      }

      if (type != ProjectType.Unknown)
      {
        invoices = invoices.Where(x => x.Project.Type == type);
      }

      if (invoices.Count() > 0)
      {
        var data = invoices.OrderByDescending(i => i.Number).Select(x => new InvoiceListViewModel(x));
        return new { data };
      }
      else
        return null;
    }

    [Danielle]
    public void Create(long projectId, DateTime startDate, DateTime endDate, bool oldBookings)
    {
      var application = session.Query<Application>().Where(x => x.IsActive && x.Projects.Where(p => p.Id == projectId).Any()).FirstOrDefault();
      var project = session.Query<Project>().FirstOrDefault(x => x.IsActive && x.Id == projectId);
      var bookings = session.Query<Booking>().Where(x => x.IsActive && x.Project == project);

      if (oldBookings)
      {
        bookings = bookings.Where(x => x.Date <= endDate && x.InvoiceLines.Count(il => il.Invoice.State == InvoiceState.Definitive) == 0);
      }
      else
      {
        bookings = bookings.Where(x => x.Date <= endDate && x.Date >= startDate && x.InvoiceLines.Count(il => il.Invoice.State == InvoiceState.Definitive) == 0);
      }

      var invoice = new Invoice(application, project, startDate, endDate, bookings.ToList());

      PropertyBag.Add("invoice", invoice);
      PropertyBag.Add("project", project);
      PropertyBag.Add("editInvoice", false);
    }

    [Danielle]
    public void Copy(string projectSlug, long invoiceId)
    {
      var project = session.SlugOrDefault<Project>(projectSlug);
      var invoice = session.Query<Invoice>().FirstOrDefault(i => i.Id == invoiceId && i.Project == project);

      PropertyBag.Add("invoice", invoice);
      PropertyBag.Add("project", project);

      RenderView("create");
    }

    [Danielle]
    public void Edit(string projectSlug, long invoiceId)
    {
      var project = session.SlugOrDefault<Project>(projectSlug);
      var invoice = session.Query<Invoice>().FirstOrDefault(i => i.Id == invoiceId && i.Project == project);

      for (var i = invoice.Corrections.Count(); i < 10; i++)
      {
        invoice.Corrections.Add(new Correction());
      }
      PropertyBag.Add("editInvoice", true);
      PropertyBag.Add("invoice", invoice);
      PropertyBag.Add("project", project);

      RenderView("create");
    }

    [Danielle]
    public void Save(string projectSlug, long invoiceId)
    {
      var project = session.SlugOrDefault<Project>(projectSlug);
      var application = session.Query<Application>().Where(x => x.IsActive && x.Projects.Where(p => p.Id == project.Id).Any()).FirstOrDefault();
      var invoice = session.Query<Invoice>().FirstOrDefault(i => i.Id == invoiceId && i.Project == project);
      if (invoiceId > 0)
      {
        BindObjectInstance(invoice, "invoice");
      }
      else
      {
        invoice = BindObject<Invoice>("invoice");
      }

      var lines = BindObject<InvoiceLine[]>("lines");
      var bindObject = BindObject<Correction[]>("corrections");
      var corrections = bindObject.Where(x => x.Price != 0.0M).ToArray();

      invoice.CreatedBy = CurrentUser;
      invoice.CreatedAt = DateTime.Now;
      invoice.State = InvoiceState.Concept;
      invoice.Application = application;

      foreach (var line in invoice.Lines.ToList())
      {
        invoice.RemoveLine(line);
      }
      foreach (var line in lines)
      {
        invoice.AddLine(line);
      }

      foreach (var correction in invoice.Corrections.ToList())
      {
        invoice.RemoveCorrection(correction);
      }
      foreach (var correction in corrections)
      {
        invoice.AddCorrection(correction);
      }

      using (var tx = session.BeginTransaction())
      {

        session.SaveOrUpdate(invoice);
        tx.Commit();
      }

      RedirectToAction("index");
    }

    [Danielle]
    public void Definitive(string projectSlug, long invoiceId)
    {
      ChangeState(projectSlug, invoiceId, InvoiceState.Definitive);
    }

    [Danielle]
    public void Archive(string projectSlug, long invoiceId)
    {
      ChangeState(projectSlug, invoiceId, InvoiceState.Archived);
    }

    [Danielle]
    public void Delete(string projectSlug, long invoiceId)
    {
      var project = session.SlugOrDefault<Project>(projectSlug);
      var invoice = session.Query<Invoice>().First(i => i.Id == invoiceId && i.Project == project);

      invoice.Deactivate();

      using (var tx = session.BeginTransaction())
      {
        session.SaveOrUpdate(invoice);
        tx.Commit();
      }
      ChangeState(projectSlug, invoiceId, InvoiceState.Archived);
    }

    [Danielle]
    public void ArchiveIssues(string projectSlug, long invoiceId)
    {
      var project = session.SlugOrDefault<Project>(projectSlug);
      var invoice = session.Query<Invoice>().FirstOrDefault(i => i.Id == invoiceId && i.Project == project);

      using (var tx = session.BeginTransaction())
      {
        foreach (var issue in invoice.Issues)
        {
          issue.Archive(CurrentUser);
          session.SaveOrUpdate(issue);
        }
        tx.Commit();
      }

      RedirectToReferrer();
    }

    [Danielle]
    public void Download(string projectSlug, long invoiceId)
    {
      CancelView();

      var project = session.SlugOrDefault<Project>(projectSlug);
      var invoice = session.Query<Invoice>().FirstOrDefault(i => i.Id == invoiceId && i.Project == project);

      string invoiceTemplate = ConfigurationManager.AppSettings["invoice_template"];
      var pdf = pdfExportService.ConvertHtmlToPdf(invoiceTemplate, new Dictionary<string, object>{
                {"invoice", invoice},

            });

      var filename = string.Format("{0} - {1} - {2:yyyyMMddHHmm}.pdf", invoice.Number, invoice.Title, DateTime.Now);

      var pdfMemory = new MemoryStream(pdf);

      Context.Response.ContentType = "application/pdf";

      var buffer = new byte[pdfMemory.Length];
      pdfMemory.Position = 0;
      pdfMemory.Read(buffer, 0, pdf.Length);

      Context.Response.OutputStream.Write(buffer, 0, buffer.Length);

      Response.BinaryWrite(pdf);
      Response.AppendHeader("content-disposition", string.Format("filename={0}", filename));
    }

    [Danielle]
    public void Export()
    {
      var projects = session.Query<Project>().Where(x => x.IsActive).OrderBy(x => x.Application.Customer.Name).ToList();

      var csv = CsvHelper.InvoiceCsv(projects);
      CancelView();

      Response.ClearContent();
      Response.Clear();

      var filename = $"invoices_{DateTime.Now:yyyyMMdd_hhmm}.csv";

      Response.AppendHeader("content-disposition", $"attachment; filename={filename}");
      Response.ContentType = "application/csv";

      var byteArray = Encoding.Default.GetBytes(csv);
      var stream = new MemoryStream(byteArray);
      Response.BinaryWrite(stream);
    }

    private void ChangeState(string projectSlug, long invoiceId, InvoiceState state)
    {
      var project = session.SlugOrDefault<Project>(projectSlug);
      var invoice = session.Query<Invoice>().FirstOrDefault(i => i.Id == invoiceId && i.Project == project);

      invoice.State = state;

      using (var tx = session.BeginTransaction())
      {
        session.SaveOrUpdate(invoice);
        tx.Commit();
      }

      RedirectToReferrer();
    }
  }
}