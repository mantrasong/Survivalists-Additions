using System;
using System.Text;
using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;

namespace SurvivalistsAdditions {
  [StaticConstructorOnStartup]
  public class Building_CharcoalPit : Building, IItemProcessor {

    private readonly int MaxCapacity = SrvSettings.CharcoalPit_MaxCapacity;
    private readonly int BaseBurnDuration = SrvSettings.CharcoalPit_BurnTicks;
    private readonly float CharcoalPerWoodLog = SrvSettings.CharcoalPit_CharcoalPerWoodLog;

    private int woodCount;
    private float progressInt;
    private Material barFilledCachedMat;

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

    public ThingRequest InputRequest {
      get { return ThingRequest.ForDef(ThingDefOf.WoodLog); }
    }

    public bool TemperatureAcceptable {
      get { return true; }
    }

    private Material BarFilledMat {
      get {
        if (barFilledCachedMat == null) {
          barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(Static.BarZeroProgressColor_Generic, Static.BarFullColor_Generic, Progress), false);
        }
        return barFilledCachedMat;
      }
    }

    public int SpaceLeftForItem {
      get {
        if (Finished) {
          return 0;
        }
        return MaxCapacity - woodCount;
      }
    }

    public bool Empty {
      get {
        return woodCount <= 0;
      }
    }

    public bool Finished {
      get {
        return !Empty && Progress >= 1f;
      }
    }

    private float ProgressPerTick {
      get {
        return 1f / BaseBurnDuration;
      }
    }

    public int EstimatedTicksLeft {
      get {
        return Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTick), 0);
      }
    }

    public override Graphic Graphic {
      get {
        if (Empty) {
          return base.Graphic;
        }
        return Static.Graphic_CharcoalPitFilled;
      }
    }


    public override void Draw() {
      base.Draw();
      if (!Empty) {
        Vector3 drawPos = DrawPos;
        drawPos.y = 6.5625f;
        drawPos.z += 0.25f;
        GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest {
          center = drawPos,
          size = Static.BarSize_Generic,
          fillPercent = woodCount / (float)MaxCapacity,
          filledMat = BarFilledMat,
          unfilledMat = Static.BarUnfilledMat_Generic,
          margin = 0.1f,
          rotation = Rot4.North
        });
      }
    }


    public override void ExposeData() {
      base.ExposeData();
      Scribe_Values.Look(ref woodCount, "woodCount", 0, false);
      Scribe_Values.Look(ref progressInt, "progress", 0f, false);
    }


    public override void TickRare() {
      base.TickRare();

      // increase the progress
      if (!Empty) {
        Progress = Mathf.Min(Progress + (250f * ProgressPerTick), 1f);
        // Occasionally throw smoke motes
        if (!Finished && Rand.Bool && Position.ShouldSpawnMotesAt(Map) && !Map.moteCounter.SaturatedLowPriority) {
          MoteMaker.ThrowSmoke(Position.ToVector3Shifted(), Map, 1f);
        }
      }
    }


    public Predicate<Thing> ItemValidator(Pawn pawn) {
      return null;
    }


    public int AddItem(Thing wood) {
      int count = 0;

      if (wood.stackCount <= SpaceLeftForItem) {
        count = wood.stackCount;
      }
      else {
        count = SpaceLeftForItem;
      }
      AddItem(count);
      return count;
    }


    public void AddItem(int count) {
      bool needsUpdate = false;
      if (Empty) {
        needsUpdate = true;
      }

      if (Finished) {
        Log.Warning("Survivalist's Additions:: Tried to add wood to a charcoal pit full of charcoal. Colonists should take the charcoal first.");
        return;
      }
      int num = Mathf.Min(count, MaxCapacity - woodCount);
      if (num <= 0) {
        return;
      }
      Progress = GenMath.WeightedAverage(0f, num, Progress, woodCount);
      woodCount += num;
      if (needsUpdate) {
        Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things);
      }
    }


    public void Reset() {
      woodCount = 0;
      Progress = 0f;
      Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things);
    }


    public Thing TakeOutProduct() {
      if (!Finished) {
        Log.Warning("Survivalist's Additions:: Tried to get charcoal but it's not yet charred.");
        return null;
      }
      Thing thing = ThingMaker.MakeThing(SrvDefOf.SRV_Charcoal, null);
      thing.stackCount = (int)(woodCount * CharcoalPerWoodLog);
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
      if (!Empty) {
        if (Finished) {
          stringBuilder.AppendLine("SRV_ContainsCharcoal".Translate(new object[]
          {
            (int)(woodCount * CharcoalPerWoodLog),
            (int)(MaxCapacity * CharcoalPerWoodLog)
          }));
          stringBuilder.AppendLine("SRV_Charred".Translate());
        }
        else {

          stringBuilder.AppendLine("SRV_ContainsWood".Translate(new object[]
          {
            woodCount,
            MaxCapacity
          }));
          stringBuilder.Append("FermentationProgress".Translate(new object[]
          {
            Progress.ToStringPercent(),
            EstimatedTicksLeft.ToStringTicksToPeriod(true, false, true)
          }));
        }
      };
      return stringBuilder.ToString().TrimEndNewlines();
    }
  }
}
