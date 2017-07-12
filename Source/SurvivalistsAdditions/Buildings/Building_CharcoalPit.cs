using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace SurvivalistsAdditions {

  internal class Building_CharcoalPit : Building {

    // Burning ticks to reach. Starts at 20000 (8 hours)
    private int baseBurnTicks = 20000;

    private float progressInt;
    public float Progress {
      get { return progressInt; }
      set {
        if (value == progressInt) {
          return;
        }
        progressInt = value;
      }
    }

    private float ProgressPerTick {
      get {
        return 1f / baseBurnTicks;
      }
    }

    private int EstimatedTicksLeft {
      get {
        return Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTick), 0);
      }
    }


    public override void ExposeData() {
      base.ExposeData();
      Scribe_Values.Look(ref progressInt, "SRV_CharcoalPit_progressInt", 0);
    }


    public override void TickRare() {
      base.TickRare();

      // increase the progress
      Progress += (250f * ProgressPerTick);

      // Spawn charcoal
      if (Progress >= 1f) {
        PlaceProduct(Map);
      }

      // Occasionally throw smoke motes
      if (Rand.Bool && Position.ShouldSpawnMotesAt(Map) && !Map.moteCounter.SaturatedLowPriority) {
        MoteMaker.ThrowSmoke(Position.ToVector3Shifted(), Map, 1f);
      }
    }


    private void PlaceProduct(Map map) {
      Thing placedProduct = ThingMaker.MakeThing(SrvDefOf.SRV_Charcoal);
      placedProduct.stackCount = 75;
      Destroy();
      GenPlace.TryPlaceThing(placedProduct, Position, map, ThingPlaceMode.Direct);
    }


    public override string GetInspectString() {
      StringBuilder stringBuilder = new StringBuilder();

      // Display the burning progress
      stringBuilder.AppendLine("FermentationProgress".Translate(new object[]{
        Progress.ToStringPercent(),
        EstimatedTicksLeft.ToStringTicksToPeriod(true, false, true)
      }));

      return stringBuilder.ToString().TrimEndNewlines();
    }
  }
}
