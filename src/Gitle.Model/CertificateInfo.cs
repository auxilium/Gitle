using Gitle.Model.Enum;
using Gitle.Model.Interfaces.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitle.Model
{
  public class CertificateInfo : ModelBase, ISlugger
  {
    public CertificateInfo()
    {
    }

    public CertificateInfo(DateTime installationDate, DateTime expiryDate)
    {
      InstallationDate = installationDate;
      ExpiryDate = expiryDate;
    }

    public virtual string Description { get; set; }
    public virtual string Name { get; set; } // gitle.auxilium.nl
    public virtual string Url { get; set; } // url
    public virtual CertificateType CertificateType { get; set; } // publiek / privaat /
    public virtual string ExtendFrequency { get; set; }
    public virtual CertificateExtendOption ExtendOption { get; set; }
    public virtual string ToRequestBy { get; set; }
    public virtual string ToApplyAt { get; set; }
    public virtual string EncryptionScheme { get; set; } //bv SHA-256 With RSA Encryption
    public virtual string Organization { get; set; } // Let's Encrypt
    public virtual string ProvidedBy { get; set; } // R10
    public virtual DateTime InstallationDate { get; set; }
    public virtual DateTime ExpiryDate { get; set; } // Verloopt op:
    public virtual Application Application { get; set; } // Gitle
    public virtual Installation Installation { get; } // omgeving
    public virtual string Slug { get; set; }
    public virtual bool Priority { get; set; }
    public virtual InstallationType InstallationType { get; set; }
  }
}