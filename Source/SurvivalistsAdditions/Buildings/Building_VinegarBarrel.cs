using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace SurvivalistsAdditions {
  [StaticConstructorOnStartup]
  public class Building_VinegarBarrel : Building, IItemProcessor {

    private const float MinIdealTemperature = 7f;
    private readonly int MaxCapacity = SrvSettings.VinegarBarrel_MaxCapacity;
    private readonly int BaseFermentationDuration = SrvSettings.VinegarBarrel_FermentTicks;

    private int itemCount;
    private float progressInt;
    private Material barFilledCachedMat;
    private CompTemperatureRuinable temperatureComp;

    public float Progress {
      get { return progressInt; }
      set {
        if (value == progressInt) {
          return;
        }
        progressInt = value;
        barFilledCachedMat = null;
      }
    }

    private Material BarFilledMat {
      get {
        if (barFilledCachedMat == null) {
          barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(Static.BarZeroProgressColor_Generic, Static.BarFullColor_Generic, Progress), false);
        }
        return barFilledCachedMat;
      }
    }

    public ThingRequest InputRequest {
      get { return ThingRequest.ForDef(SrvDefOf.SRV_VinegarJuice); }
    }

    public int SpaceLeftForItem {
      get {
        if (Finished) {
          return 0;
        }
        return MaxCapacity - itemCount;
      }
    }

    public bool Empty {
      get {
        return itemCount <= 0;
      }
    }

    public bool Finished {
      get {
        return !Empty && Progress >= 1f;
      }
    }

    public bool TemperatureAcceptable {
      get {
        if (AmbientTemperature < temperatureComp.Props.minSafeTemperature + 2f || AmbientTemperature > temperatureComp.Props.maxSafeTemperature - 2f) {
          return false;
        }
        return true;
      }
    }

    private float CurrentTempProgressSpeedFactor {
      get {
        CompProperties_TemperatureRuinable compProperties = def.GetCompProperties<CompProperties_TemperatureRuinable>();
        float ambientTemperature = AmbientTemperature;
        if (ambientTemperature < compProperties.minSafeTemperature) {
          return 0.1f;
        }
        if (ambientTemperature < MinIdealTemperature) {
          return GenMath.LerpDouble(compProperties.minSafeTemperature, MinIdealTemperature, 0.1f, 1f, ambientTemperature);
        }
        return 1f;
      }
    }

    private float ProgressPerTickAtCurrentTemp {
      get {
        return (1f / BaseFermentationDuration) * CurrentTempProgressSpeedFactor;
      }
    }

    public int EstimatedTicksLeft {
      get {
        return Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTickAtCurrentTemp), 0);
      }
    }


    public override void Draw() {
      base.Draw();
      if (!Empty) {
        Vector3 drawPos = DrawPos;
        drawPos.y += 0.0483870953f;
        drawPos.z += 0.25f;
        GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest {
          center = drawPos,
          size = Static.BarSize_Generic,
          fillPercent = itemCount / (float)MaxCapacity,
          filledMat = BarFilledMat,
          unfilledMat = Static.BarUnfilledMat_Generic,
          margin = 0.1f,
          rotation = Rot4.North
        });
      }
    }


    public override void ExposeData() {
      base.ExposeData();
      Scribe_Values.Look(ref itemCount, "itemCount", 0, false);
      Scribe_Values.Look(ref progressInt, "progress", 0f, false);
    }


    public override void SpawnSetup(Map map, bool respawningAfterLoad) {
      base.SpawnSetup(map, respawningAfterLoad);
      temperatureComp = GetComp<CompTemperatureRuinable>();
    }


    public override void TickRare() {
      base.TickRare();

      if (!Empty) {
        Progress = Mathf.Min(Progress + 250f * ProgressPerTickAtCurrentTemp, 1f);
      }
    }


    public Predicate<Thing> ItemValidator(Pawn pawn) {
      return null;
    }


    public int AddItem(Thing item) {
      int count = 0;

      if (item.stackCount <= SpaceLeftForItem) {
        count = item.stackCount;
      }
      else {
        count = SpaceLeftForItem;
      }
      AddItem(count);
      return count;
    }


    public void AddItem(int count) {
      GetComp<CompTemperatureRuinable>().Reset();
      if (Finished) {
        Log.Warning("Survivalist's Additions:: Tried to add juice to a vinegar barrel full of vinegar. Colonists should take the vinegar first.");
        return;
      }
      int num = Mathf.Min(count, MaxCapacity - itemCount);
      if (num <= 0) {
        return;
      }
      Progress = GenMath.WeightedAverage(0f, num, Progress, itemCount);
      itemCount += num;
    }


    protected override void ReceiveCompSignal(string signal) {
      if (signal == "RuinedByTemperature") {
        Reset();
      }
    }


    public void Reset() {
      itemCount = 0;
      Progress = 0f;
    }


    public Thing TakeOutProduct() {
      if (!Finished) {
        Log.Warning("Survivalist's Additions:: Tried to get vinegar but it's not yet fermented.");
        return null;
      }
      Thing thing = ThingMaker.MakeThing(SrvDefOf.SRV_Vinegar, null);
      thing.stackCount = itemCount;
      Reset();
      return thing;
    }


    public override IEnumerable<Gizmo> GetGizmos() {

      // Add button for finishing the fermenting
      Command_Action DevFinish = new Command_Action() {
        defaultLabel = "Debug: Finish",
        activateSound = SoundDefOf.Click,
        action = () => { Progress = 1f; },
      };

      if (Prefs.DevMode && !Empty) {
        yield return DevFinish;
      }

      foreach (Command c in base.GetGizmos()) {
        yield return c;
      }
    }


    public override string GetInspectString() {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(base.GetInspectString());
      if (stringBuilder.Length != 0) {
        stringBuilder.AppendLine();
      }
      
      if (!Empty && !temperatureComp.Ruined) {
        if (Finished) {
          stringBuilder.AppendLine("SRV_ContainsVinegar".Translate(new object[]
          {
            itemCount,
            MaxCapacity
          }));
        }
        else {
          stringBuilder.AppendLine("SRV_ContainsJuice".Translate(new object[]
          {
            itemCount,
            MaxCapacity
          }));
        }
      }
      if (!Empty) {
        if (Finished) {
          stringBuilder.AppendLine("Fermented".Translate());
        }
        else {
          stringBuilder.AppendLine("FermentationProgress".Translate(new object[]
          {
            Progress.ToStringPercent(),
            EstimatedTicksLeft.ToStringTicksToPeriod(true, false, true)
          }));
          if (CurrentTempProgressSpeedFactor != 1f) {
            stringBuilder.AppendLine("FermentationBarrelOutOfIdealTemperature".Translate(new object[]
            {
              CurrentTempProgressSpeedFactor.ToStringPercent()
            }));
          }
        }
      }
      stringBuilder.AppendLine("Temperature".Translate() + ": " + AmbientTemperature.ToStringTemperature("F0"));
      stringBuilder.AppendLine(string.Concat(new string[]
      {
        "IdealFermentingTemperature".Translate(),
        ": ",
        MinIdealTemperature.ToStringTemperature("F0"),
        " ~ ",
        temperatureComp.Props.maxSafeTemperature.ToStringTemperature("F0")
      }));
      return stringBuilder.ToString().TrimEndNewlines();
    }
  }
}
