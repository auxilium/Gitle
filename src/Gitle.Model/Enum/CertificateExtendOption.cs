using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gitle.Model.Enum
{
  public enum CertificateExtendOption
  {
    [Description("Automatisch")]
    Automatisch = 1,
    [Description("Handmatig")]
    Handmatig = 2,
  }
}
