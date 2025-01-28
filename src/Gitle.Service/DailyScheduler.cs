using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitle.Service
{
  using Gitle.Model;
  using Gitle.Model.Interfaces.Service;
  using NHibernate;
  using System;
  using System.Threading;
  using System.Threading.Tasks;

  public class DailyScheduler
  {
    private Timer _timer;
    private ISession _session;
    private IEmailService _emailService;

    public DailyScheduler(ISessionFactory sessionFactory, IEmailService emailService)
    {
      _session = sessionFactory.GetCurrentSession();
      _emailService = emailService;
    }

    public void StartScheduler()
    {
      ScheduleAction(RunDailyTask);
    }

    private void ScheduleAction(Action action)
    {
      DateTime now = DateTime.Now;
      //DateTime targetTime = DateTime.Today.AddHours(6); // 6:00 AM today
      DateTime targetTime = new DateTime(now.Year, now.Month, now.Day, 10, 47, 0);

      if (now > targetTime)
      {
        targetTime = targetTime.AddDays(1); // Schedule for 6:00 AM the next day
      }

      TimeSpan initialDelay = targetTime - now;
      _timer = new Timer(_ =>
      {
        action.Invoke();
        ScheduleAction(action); // Reschedule for the next day
      }, null, initialDelay, Timeout.InfiniteTimeSpan);
    }

    private void RunDailyTask()
    {
      Console.WriteLine($"Task executed at: {DateTime.Now}");

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
