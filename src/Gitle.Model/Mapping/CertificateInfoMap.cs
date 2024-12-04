using FluentNHibernate.Mapping;

namespace Gitle.Model.Mapping
{
  public class CertificateInfoMap : ModelBaseMap<CertificateInfo>
  {
    public CertificateInfoMap() 
    {
      Map(x => x.Name);
      Map(x => x.Description);
      Map(x => x.ExpiryDate);
      Map(x => x.InstallationDate);
      Map(x => x.ProvidedBy);
      Map(x => x.Organization);
      Map(x => x.Url);
      Map(x => x.ToApplyAt);
      Map(x => x.CertificateType);
      Map(x => x.ToRequestBy);
      Map(x => x.ExtendOption);
      Map(x => x.ExtendFrequency);
      Map(x => x.EncryptionScheme);
      Map(x => x.Slug);
      Map(x => x.Priority);
      References(x => x.Application).Column("Application_id").LazyLoad(Laziness.False);
      References(x => x.Installation).Column("Installation_id").LazyLoad(Laziness.False);
    }
  }
}
