using UnityEngine;
using Verse;

namespace SurvivalistsAdditions {

  public enum SmokeStyle {
    None,
    Single,
    Triple
  }



  public class CompProperties_Smoker : CompProperties {

    public SmokeStyle smokeStyle = SmokeStyle.None;
    public int frequencyMin = 60;
    public int frequencyMax = 60;
    public float size = 1f;
    public Vector3 offset = new Vector3(0.5f, 0, 0.5f);
    public bool produceSmokeOnlyWhenUsed = false;


    public CompProperties_Smoker() {
      compClass = typeof(CompSmoker);
    }
  }
}
