namespace Gitle.Model.Enum
{
  using System.ComponentModel;

  public enum InstallationType
  {
    [Description("Onbekend")]
    Unknown = 0,
    [Description("Live")]
    Live = 10,
    [Description("Live-DB")]
    Live_DB = 20,
    [Description("Acceptatie")]
    Acceptance = 30,
    [Description("Acceptence-DB")]
    Acceptence_DB = 40,
    [Description("Demo")]
    Demo = 50,
    [Description("Demo-DB")]
    Demo_DB = 60,
    [Description("Demo-Old")]
    Demo_Old = 70,
  }
}