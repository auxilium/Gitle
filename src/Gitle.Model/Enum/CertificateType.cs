using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitle.Model.Enum
{
  public enum CertificateType
  {
    [Description("Publiek")]
    Publiek = 1,
    [Description("Privaat")]
    Privaat = 2,
  }
}
