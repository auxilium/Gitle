namespace Gitle.Web.Controllers
{
  using System;
  using System.Linq;
  using Castle.MonoRail.Framework;
  using FluentNHibernate.Conventions;
  using Gitle.Model.Enum;
  using Helpers;
  using Model;
  using Model.Helpers;
  using NHibernate;

  public class CertificateInfoController : SecureController
  {
    public CertificateInfoController(ISessionFactory sessionFactory) : base(sessionFactory)
    {
    }

    [Admin]
    public void Index()
    {
      var items = session.Query<CertificateInfo>().Where(x => x.IsActive).ToList();

      var certificatesToExpire = items.Where(c => c.ExtendOption == CertificateExtendOption.Handmatig && (CheckExpiresThisMonth(c.ExpiryDate) || c.ExpiryDate < DateTime.Today));

      foreach (var item in items)
      {
        item.ExpiryDate = item.ExpiryDate;
        item.InstallationDate = item.InstallationDate;
      }
      PropertyBag.Add("items", items);

      foreach (var item in certificatesToExpire)
      {
        item.ExpiryDate = item.ExpiryDate;
        item.InstallationDate = item.InstallationDate;
        item.Priority = CheckExpiresThisMonth(item.ExpiryDate) && item.ExtendOption == CertificateExtendOption.Handmatig || item.ExpiryDate < DateTime.Today;
      }
      PropertyBag.Add("certificatesToExpire", certificatesToExpire);
    }

    [Admin]
    public void Edit(long id)
    {
      var certificate = session.Query<CertificateInfo>().Where(c => c.Id == id).FirstOrDefault();

      var certificateTypes = EnumHelper.ToDictionary(typeof(CertificateType));
      var certificateTypesList = certificateTypes.Where(t => new[] { CertificateType.Publiek, CertificateType.Privaat }.Contains((CertificateType)t.Key)).ToList();

      var extendOptions = EnumHelper.ToDictionary(typeof(CertificateExtendOption));
      var options = extendOptions.Where(t => new[] { CertificateExtendOption.Handmatig, CertificateExtendOption.Automatisch }.Contains((CertificateExtendOption)t.Key)).ToList();

      var installationTypes = EnumHelper.ToDictionary(typeof(InstallationType));
      var installationTypesList = installationTypes.Where(t => new[] { InstallationType.Live, InstallationType.Acceptance, InstallationType.Demo }.Contains((InstallationType)t.Key)).ToList();

      PropertyBag.Add("applications", session.Query<Application>().Where(x => x.IsActive).OrderBy(x => x.Name));
      PropertyBag.Add("applicationId", certificate?.Application?.Id);
      PropertyBag.Add("servers", session.Query<Server>().Where(x => x.IsActive).OrderBy(x => x.Name));
      PropertyBag.Add("serverId", certificate?.Installation?.Server?.Id);
      PropertyBag.Add("certificationTypes", certificateTypesList);
      PropertyBag.Add("installationTypes", installationTypesList);
      PropertyBag.Add("options", options);

      if (certificate == null)
      {
        CertificateInfo certificateinfo = new CertificateInfo()
        {
          InstallationDate = DateTime.UtcNow,
          ExpiryDate = DateTime.UtcNow
        };

        PropertyBag.Add("item", certificateinfo);
      }
      else
        PropertyBag.Add("item", certificate);
      RenderView("edit");
    }

    [Admin]
    public void New()
    {
      CertificateInfo certificateinfo = new CertificateInfo()
      {
        InstallationDate = DateTime.UtcNow,
        ExpiryDate = DateTime.UtcNow,
      };

      PropertyBag.Add("item", certificateinfo);
      RenderView("edit");
    }

    [Admin]
    public void Save(long id, long applicationId)
    {
      var item = session.Query<CertificateInfo>().Where(c => c.Id == id).FirstOrDefault(); 
      var application = session.Get<Application>(applicationId);

      if (item != null)
      {
        BindObjectInstance(item, "item");

        item.Url = item.Url.Replace("https://", "").Replace("http://", "");
        item.Priority = CheckExpiresThisMonth(item.ExpiryDate) && item.ExtendOption == CertificateExtendOption.Handmatig;
      }
      else
      {
        item = BindObject<CertificateInfo>("item");
    
        item.Application = application;
        item.InstallationType = item.InstallationType;
        item.CertificateType = item.CertificateType;
        item.Description = item.Description;
        item.EncryptionScheme = item.EncryptionScheme;
        item.ExpiryDate = item.ExpiryDate.Date;
        item.ExtendOption = item.ExtendOption;
        item.ExtendFrequency = item.ExtendFrequency;
        item.InstallationDate = item.InstallationDate.Date;
        item.Name = item.Name;
        item.ToApplyAt = item.ToApplyAt;
        item.ToRequestBy = item.ToRequestBy;
        item.ProvidedBy = item.ProvidedBy;
        item.Slug = $"{application.Name}-{item.Id}".Slugify();
        item.Priority = CheckExpiresThisMonth(item.ExpiryDate) && item.ExtendOption == CertificateExtendOption.Handmatig;
      }

      using (var tx = session.BeginTransaction())
      {
        session.SaveOrUpdate(item);
        tx.Commit();
      }

      RedirectToUrl("/certificateinfo");
    }

    [Admin]
    public void Delete(long id)
    {
      var certificate = session.Query<CertificateInfo>().Where(c => c.Id == id).FirstOrDefault();
      certificate.Deactivate();
      using (var tx = session.BeginTransaction())
      {
        session.SaveOrUpdate(certificate);
        tx.Commit();
      }
      RedirectToReferrer();
    }

    [return: JSONReturnBinder]
    public object CheckInstallationName(string name, long installationId)
    {
      var validName = !session.Query<Installation>().Any(x => x.IsActive && x.Slug == name.Slugify() && x.Id != installationId);
      return new { success = validName, message = validName ? "" : "Er bestaat al een installatie met deze naam! Kies een ander naam!" };
    }

    private bool CheckExpiresThisMonth(DateTime expiryDate)
    {
      DateTime now = DateTime.Now;
      return expiryDate.Year == now.Year && expiryDate.Month == now.Month;
    }
  }
}
