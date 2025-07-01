using Castle.Windsor;
using NHibernate;
using NHibernate.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CertificatesExpireEmailNotificationService
{
  public class Program
  {
    private static IWindsorContainer _container;
    private static void Main(string[] args)
    {
      _container = new TaskProcessContainer();

      var sessionFactory = _container.Resolve<ISessionFactory>();
      var session = sessionFactory.OpenSession();
      ThreadLocalSessionContext.Bind(session);

      var service = _container.Resolve<DailyCertificateCheck>();
      service.Run();

      ThreadLocalSessionContext.Unbind(sessionFactory);

    }
  }
}
