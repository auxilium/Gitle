using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertificatesExpireEmailNotificationService
{
  using Castle.Core.Logging;
  using Gitle.Model;
  using Gitle.Model.Interfaces.Service;
  using NHibernate;
  using System;
  using System.Threading;
  using System.Threading.Tasks;

  public class DailyCertificateCheck
  {    
    private ISession _session;
    private readonly ILogger _logger;
    private IEmailService _emailService;

    public DailyCertificateCheck(ISessionFactory sessionFactory, IEmailService emailService, ILogger logger)
    {
      _session = sessionFactory.GetCurrentSession();
      _emailService = emailService;
      _logger = logger;
    }

    public void Run()
    {
      _logger.Debug("DailyCertificateCheck started");

      RunDailyTask();

      _logger.Debug("DailyCertificateCheck done");
    }

    private void RunDailyTask()
    {
      _logger.Debug($"Task executed at: {DateTime.UtcNow}");

      var items = _session.Query<CertificateInfo>().Where(x => x.IsActive).ToList();

      foreach (var item in items)
      {
        var expiryDate = item.ExpiryDate;
        if (CheckExpiresThisMonth(expiryDate))
        {
          _emailService.SendCertificateReminderNotification(item);
        }
      }
    }

    private bool CheckExpiresThisMonth(DateTime expiryDate)
    {
      DateTime now = DateTime.Now;
      return expiryDate.Year == now.Year && expiryDate.Month <= now.Month;
    }
  }
}
