using UnityEngine;
using Verse;

namespace SurvivalistsAdditions {
  [StaticConstructorOnStartup]
  public static class Static {

    public const int GenericWaitDuration = 200;

    public static string TemperatureTrans = "BadTemperature".Translate().ToLower();
    public static string NoJuice = "SRV_NoJuice".Translate();
    public static string NoMeat = "SRV_NoMeat".Translate();
    public static string SmokerLocked = "SRV_SmokerLocked".Translate();

    public static readonly Material BarUnfilledMat_Generic = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);
    public static readonly Vector2  BarSize_Generic = new Vector2(0.55f, 0.1f);
    public static readonly Color    BarZeroProgressColor_Generic = new Color(0.4f, 0.27f, 0.22f);
    public static readonly Color    BarZeroProgressColor_Smoker = new Color(0.9f, 0.2f, 0.2f);
    public static readonly Color    BarFullColor_Generic = new Color(0.9f, 0.85f, 0.2f);
    public static readonly Color    BarFullColor_Smoker = new Color(0.376f, 0.25f, 0.125f);
  }
}
