

using System.ComponentModel;

namespace Gitle.Model.Enum
{
  public enum SubsidyProjectSection
  {
    [Description("digitaal ondertekenen - MF PP MVP")]
    OneOne = 11,
    [Description("digitaal ondertekenen, kluis, marktplaats etc BB MVPs")]
    OneTwo = 12,
    [Description("rekenmethodiek MVP")]
    OneThree = 13,
    [Description("web2 --> web3")]
    OneFour = 14,
    [Description("data entrypoint en toeristische verhuur data")]
    OneFive = 15,
    // --------------------------------------------------
    [Description("inkoopviewer")]
    TwoOne = 21,
    [Description("inkoopviewer - CO-portal mvp")]
    TwoTwo = 22,
    [Description("inkoopviewer - MVO component")]
    TwoThree = 23,
    [Description("inkoopviewer - outdoor experiment mvp")]
    TwoFour = 24,
    [Description("inkoopviewer - time specific mvp")]
    TwoFive = 25,
    [Description("waarderingstool")]
    TwoSix = 26,
    // --------------------------------------------------
    [Description("cross-blockchain mvp")]
    ThreeOne = 31,
    [Description("multi-layer mvp")]
    ThreeTwo = 32,
    [Description("pay-out mvp")]
    ThreeThree = 33,
    [Description("realtime-mint mvp")]
    ThreeFour = 34,
    [Description("rwa-mvp")]
    ThreeFive = 35,
    [Description("sponsored-transctions mvp")]
    ThreeSix = 36
    }
}
