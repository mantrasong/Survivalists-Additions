using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace SurvivalistsAdditions {
  [StaticConstructorOnStartup]
  public class Building_Smoker : Building {
    #region Fields
    public const int MaxCapacity = 60;
    public const float MinIdealTemperature = 7f;

    private const int BaseSmokeDuration = 60000;
    private const float MeatAddingPct = 0.045f;

    private int ticksSinceTending = 0;
    private int rottingTicks = 0;
    private int smokeTimer;
    private float progressInt;
    private Material barFilledCachedMat;
    private CompSmoker smokerComp;
    private CompRefuelable fuelComp;
    private List<ThingCountExposable> meatSources;
    #endregion Fields

    #region Properties
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

    public bool Smoked {
      get {
        return !Empty && Progress >= 1f;
      }
    }

    public bool CanAddMeat {
      get {
        return Progress < MeatAddingPct;
      }
    }

    public int SpaceLeftForMeat {
      get {
        if (Smoked || !CanAddMeat) {
          return 0;
        }
        return MaxCapacity - MeatCount;
      }
    }

    public bool NeedsTending {
      get {
        return ticksSinceTending >= 5000;
      }
    }

    private float TendedSpeedFactor {
      get {
        if (NeedsTending) {
          return 0.75f;
        }
        return 1f;
      }
    }

    private Material BarFilledMat {
      get {
        if (barFilledCachedMat == null) {
          barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(Static.BarZeroProgressColor_Smoker, Static.BarFullColor_Smoker, Progress), false);
        }
        return barFilledCachedMat;
      }
    }

    private int MeatCount {
      get {
        if (meatSources == null || meatSources.Count <= 0) {
          return 0;
        }
        int num = 0;
        for (int i = 0; i < meatSources.Count; i++) {
          num += meatSources[i].count;
        }

        if (num > 60) {
          Log.Error($"Survivalist's Additions:: Smoker at {Position} has more than 60 meat stored inside. Current count: {num}");
        }

        return num;
      }
    }

    public float RotProgressPct {
      get {
        return (float)rottingTicks / GenDate.TicksPerDay;
      }
    }

    private bool Empty {
      get {
        return MeatCount <= 0;
      }
    }

    private float CurrentTempProgressSpeedFactor {
      get {
        if (AmbientTemperature < MinIdealTemperature) {
          return GenMath.LerpDouble(-50f, MinIdealTemperature, 0.1f, 1f, AmbientTemperature);
        }
        return 1f;
      }
    }

    private float ProgressPerTickAtCurrentTemp {
      get {
        return (1f / BaseSmokeDuration) * CurrentTempProgressSpeedFactor * TendedSpeedFactor;
      }
    }

    private int EstimatedTicksLeft {
      get {
        return Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTickAtCurrentTemp), 0);
      }
    }
    #endregion Properties


    #region MethodGroup_Root
    public override void Tick() {
      base.Tick();

      if (!Empty && !Smoked) {
        if (fuelComp.HasFuel) {
          // The smoker has stored meat and fuel
          Progress = Mathf.Min(Progress + ProgressPerTickAtCurrentTemp, 1f);
          fuelComp.Notify_UsedThisTick();
          // Throw smoke manually
          if (this.IsHashIntervalTick(smokeTimer)) {
            smokeTimer = Rand.RangeInclusive(120, 360);
            smokerComp.ThrowSmokeSingle();
          }
        }
        else {
          // Simulate rotting from the meat being left uncooked
          Progress = Mathf.Max(Progress - ProgressPerTickAtCurrentTemp, 0.0f);
          rottingTicks++;
        }
        ticksSinceTending++;
      }
      else {
        ticksSinceTending = 0;
      }

      if (this.IsHashIntervalTick(250)) {
        // Rot the meat if it has been left uncooked for too long
        // This prevents players from using the smoker to store food indefinitely until needed
        if (rottingTicks >= GenDate.TicksPerDay) {
          Reset();
          Messages.Message("MessageRottedAwayInStorage".Translate("FoodTypeFlags_Meat".Translate()), this, MessageSound.Negative);
          LessonAutoActivator.TeachOpportunity(ConceptDefOf.SpoilageAndFreezers, OpportunityType.GoodToKnow);
        }
      }
    }


    #region MethodGroup_SaveLoad
    public override void ExposeData() {
      base.ExposeData();
      Scribe_Values.Look(ref ticksSinceTending, "ticksSinceTending", 0, false);
      Scribe_Values.Look(ref rottingTicks, "rottingTicks", 0, false);
      Scribe_Values.Look(ref progressInt, "progress", 0f, false);
      Scribe_Collections.Look(ref meatSources, "meatSources", LookMode.Deep);
    }


    public override void SpawnSetup(Map map, bool respawningAfterLoad) {
      base.SpawnSetup(map, respawningAfterLoad);
      smokerComp = GetComp<CompSmoker>();
      fuelComp = GetComp<CompRefuelable>();
      smokeTimer = Rand.RangeInclusive(120, 360);

      // Create the list for meat sources
      if (meatSources == null) {
        meatSources = new List<ThingCountExposable>();
      }
    }
    #endregion MethodGroup_SaveLoad


    #region MethodGroup_Smoking
    // Tending implies rotating the meat, removing rotten bits, adjusting the coals, etc.
    public void Tend() {
      ticksSinceTending = 0;

      // Remove some of the rotting bits of the meat
      while (RotProgressPct >= 0.05f) {
        int i = Rand.RangeInclusive(0, meatSources.Count);
        int amountToTrim = (int)(MeatCount * 0.05);
        int trimmedAmount = amountToTrim * 1000;

        if (amountToTrim > 0) {
          ThingCountExposable rottingMeat = meatSources.RandomElement();
          int trimmedCount = rottingMeat.count - amountToTrim;
          if (trimmedCount <= 0) {
            trimmedAmount = rottingMeat.count * 1000;
            meatSources.Remove(rottingMeat);
          }
          else {
            meatSources.Find((ThingCountExposable c) => c == rottingMeat).count = trimmedCount;
          }
        }

        if (trimmedAmount > 0) {
          rottingTicks -= trimmedAmount;
        }
        else {
          rottingTicks = 0;
        }
      }
    }


    public int AddMeat(Thing meat) {
      int countAccepted = 0;

      // Make sure the meat isn't rotten - there's no point in preserving meat that has already rotted
      if (meat.TryGetComp<CompRottable>() != null && meat.TryGetComp<CompRottable>().Stage > RotStage.Fresh) {
        return countAccepted;
      }
      
      // Determine how much meat to add
      if (meat.stackCount <= SpaceLeftForMeat) {
        countAccepted = meat.stackCount;
      }
      else {
        countAccepted = SpaceLeftForMeat;
      }

      // Adjust the rotting ticks based on the meat added
      float t = (float)countAccepted / (MaxCapacity + countAccepted);
      int newItemRottingTicks = (int)(((ThingWithComps)meat).GetComp<CompRottable>().RotProgressPct * GenDate.TicksPerDay);
      rottingTicks = (int)(Mathf.Lerp(rottingTicks, newItemRottingTicks, t));

      // Add meat, then return the amount to the JobDriver
      AddMeat(countAccepted, meat.def);
      return countAccepted;
    }


    public void AddMeat(int count, ThingDef sourceDef) {
      // Additional integrity checks
      if (Smoked) {
        Log.Warning("Survivalist's Additions:: Tried to add meat to a full smoker. Colonists should take the smoked meat out first.");
        return;
      }
      int num = Mathf.Min(count, MaxCapacity - MeatCount);
      if (num <= 0) {
        return;
      }

      // If this type of meat is already in the smoker, add this stack to it
      if (meatSources.Find((ThingCountExposable c) => c.thingDef == sourceDef) != null) {
        meatSources.Find((ThingCountExposable c) => c.thingDef == sourceDef).count += count;
      }
      // otherwise, add a new stack of meat
      else {
        meatSources.Add(new ThingCountExposable(sourceDef, count));
      }

      // Tend to the smoker while adding meat
      Tend();

      // Adjust the progress to account for the new meat
      Progress = GenMath.WeightedAverage(0f, num, Progress, MeatCount);
    }


    public Thing TakeOutProduct() {
      // Integrity check for the meat being ready
      if (!Smoked) {
        Log.Warning("Survivalist's Additions:: Tried to get smoked meat but it's not yet smoked.");
        return null;
      }

      // Create the meat
      Thing smokedMeat = ThingMaker.MakeThing(SrvDefOf.SRV_SmokedMeat, null);
      ThingCountExposable selectedMeat = meatSources.RandomElement();
      smokedMeat.stackCount = selectedMeat.count;
      smokedMeat.TryGetComp<CompIngredients>().RegisterIngredient(selectedMeat.thingDef);

      // Remove this meat from the list, resetting if this is the last one
      meatSources.Remove(selectedMeat);
      if (meatSources.Count <= 0) {
        Reset();
      } 
      
      return smokedMeat;
    }
    #endregion MethodGroup_Smoking


    #region MethodGroup_Signalling
    private void Reset() {
      Progress = 0f;
      meatSources.Clear();
      rottingTicks = 0;
    }
    #endregion MethodGroup_Signalling


    #region MethodGroup_Inspecting
    public override void Draw() {
      base.Draw();
      if (!Empty) {
        Vector3 drawPos = DrawPos;
        drawPos.y += 0.0483870953f;
        drawPos.z += 0.3f;
        GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest {
          center = drawPos,
          size = Static.BarSize_Generic,
          fillPercent = MeatCount / MaxCapacity,
          filledMat = BarFilledMat,
          unfilledMat = Static.BarUnfilledMat_Generic,
          margin = 0.1f,
          rotation = Rot4.North
        });
      }
    }


    public override IEnumerable<Gizmo> GetGizmos() {

      // Add button for finishing the smoking
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
      if (Empty) {
        stringBuilder.AppendLine();
      }
      else {
        stringBuilder.Append(" ~ ");
        if (Smoked) {
          stringBuilder.AppendLine("SRV_ContainsSmokedMeat".Translate(new object[]
          {
            MeatCount,
            MaxCapacity
          }));
          stringBuilder.AppendLine("SRV_Smoked".Translate());
        }
        else {
          
          stringBuilder.AppendLine("SRV_ContainsMeat".Translate(new object[]
          {
            MeatCount,
            MaxCapacity
          }));
          if (NeedsTending) {
            stringBuilder.AppendLine("SRV_InspectSmokerTending".Translate());
          }
          stringBuilder.Append("FermentationProgress".Translate(new object[]
          {
            Progress.ToStringPercent(),
            EstimatedTicksLeft.ToStringTicksToPeriod(true, false, true)
          }));
          stringBuilder.Append(" ~ ");
          stringBuilder.AppendLine("SRV_Rot".Translate(new object[]
          {
            RotProgressPct.ToStringPercent()
          }));
          if (CurrentTempProgressSpeedFactor != 1f) {
            stringBuilder.AppendLine("SRV_SmokerOutOfIdealTemperature".Translate(new object[]
            {
              CurrentTempProgressSpeedFactor.ToStringPercent()
            }));
          }
        }
      }
      stringBuilder.AppendLine("Temperature".Translate() + ": " + AmbientTemperature.ToStringTemperature("F0"));
      stringBuilder.AppendLine(string.Concat(new string[]
      {
        "SRV_IdealSmokingTemperature".Translate(),
        ": ",
        MinIdealTemperature.ToStringTemperature("F0"),
        " ~ ",
        100f.ToStringTemperature("F0")
      }));
      return stringBuilder.ToString().TrimEndNewlines();
    }
    #endregion MethodGroup_Inspecting
    #endregion MethodGroup_Root
  }
}
