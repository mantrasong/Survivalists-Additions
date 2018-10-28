using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {
  [StaticConstructorOnStartup]
  public class Building_Smoker : Building, IItemProcessor {
    #region Fields
    private const float MinIdealTemperature = 7f;
    private const float FoodAddingPct = 0.045f;
    private readonly int MaxCapacity = SrvSettings.Smoker_MaxCapacity;
    private readonly int BaseSmokeDuration = SrvSettings.Smoker_SmokeTicks;
    private readonly int TicksUntilTending = SrvSettings.Smoker_TendTicks;

    private int ticksSinceTending = 0;
    private int rottingTicks = 0;
    private int smokeTimer;
    private float progressInt;
    private Material barFilledCachedMat;
    private CompSmoker smokerComp;
    private CompRefuelable fuelComp;
    private List<ThingCountExposable> foodSources;
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

    public ThingRequest InputRequest {
      get { return ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree); }
    }

    public bool Finished {
      get {
        return !Empty && Progress >= 1f;
      }
    }

    public bool TemperatureAcceptable {
      get { return true; }
    }

    public bool CanAddFood {
      get {
        return Progress < FoodAddingPct;
      }
    }    

    public int SpaceLeftForItem {
      get {
        if (Finished || !CanAddFood) {
          return 0;
        }
        return MaxCapacity - FoodCount;
      }
    }

    public bool NeedsTending {
      get {
        return ticksSinceTending >= TicksUntilTending;
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

    private int FoodCount {
      get {
        if (foodSources == null || foodSources.Count <= 0) {
          return 0;
        }
        int num = 0;
        for (int i = 0; i < foodSources.Count; i++) {
          num += foodSources[i].count;
        }

        if (num > MaxCapacity) {
          Log.Error($"Survivalist's Additions:: Smoker at {Position} has more than {MaxCapacity} food stored inside. Current count: {num}");
        }

        return num;
      }
    }

    public float RotProgressPct {
      get {
        return (float)rottingTicks / GenDate.TicksPerDay;
      }
    }

    public bool Empty {
      get {
        return FoodCount <= 0;
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

    public int EstimatedTicksLeft {
      get {
        return Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTickAtCurrentTemp), 0);
      }
    }
    #endregion Properties


    #region MethodGroup_Root
    public override void Tick() {
      base.Tick();

      if (!Empty && !Finished) {
        if (fuelComp.HasFuel) {
          // The smoker has stored food and fuel
          Progress = Mathf.Min(Progress + ProgressPerTickAtCurrentTemp, 1f);
          fuelComp.Notify_UsedThisTick();
          // Throw smoke manually
          if (this.IsHashIntervalTick(smokeTimer)) {
            smokeTimer = Rand.RangeInclusive(120, 360);
            smokerComp.ThrowSmokeSingle();
          }
        }
        else {
          // Simulate rotting from the food being left uncooked
          Progress = Mathf.Max(Progress - ProgressPerTickAtCurrentTemp, 0.0f);
          rottingTicks++;
        }
        ticksSinceTending++;
      }
      else {
        ticksSinceTending = 0;
      }

      if (this.IsHashIntervalTick(250)) {
        // Rot the food if it has been left uncooked for too long
        // This prevents players from using the smoker to store food indefinitely until needed
        if (rottingTicks >= GenDate.TicksPerDay) {
          Reset();
          Messages.Message("MessageRottedAwayInStorage".Translate(Static.Food), this, MessageTypeDefOf.NegativeEvent);
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
      Scribe_Collections.Look(ref foodSources, "foodSources", LookMode.Deep);
    }


    public override void SpawnSetup(Map map, bool respawningAfterLoad) {
      base.SpawnSetup(map, respawningAfterLoad);
      smokerComp = GetComp<CompSmoker>();
      fuelComp = GetComp<CompRefuelable>();
      smokeTimer = Rand.RangeInclusive(120, 360);

      // Create the list for food sources
      if (foodSources == null) {
        foodSources = new List<ThingCountExposable>();
      }
    }
    #endregion MethodGroup_SaveLoad


    #region MethodGroup_Smoking
    // Tending implies rotating the food, removing rotten bits, adjusting the coals, etc.
    public void Tend() {
      ticksSinceTending = 0;

      // Remove some of the rotting bits of the food
      while (RotProgressPct >= 0.05f) {
        int i = Rand.Range(0, foodSources.Count);
        int amountToTrim = (int)(FoodCount * 0.05);
        int trimmedAmount = amountToTrim * 1000;

        if (amountToTrim > 0) {
          ThingCountExposable rottingFood = foodSources.RandomElement();
          int trimmedCount = rottingFood.count - amountToTrim;
          if (trimmedCount <= 0) {
            trimmedAmount = rottingFood.count * 1000;
            foodSources.Remove(rottingFood);
          }
          else {
            foodSources.Find((ThingCountExposable c) => c == rottingFood).count = trimmedCount;
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


    public Predicate<Thing> ItemValidator(Pawn pawn) {
      return ((Thing item) =>
        !item.IsForbidden(pawn) && pawn.CanReserve(item) &&
        item.def.IsNutritionGivingIngestible && ((item.def.ingestible.foodType & FoodTypeFlags.Meat) != FoodTypeFlags.None || item.def == SrvDefOf.SRV_Cheese) && 
        item.TryGetComp<CompRottable>() != null && item.TryGetComp<CompRottable>().Stage == RotStage.Fresh && item.def != SrvDefOf.SRV_SmokedMeat
      );
    }


    public int AddItem(Thing food) {
      int countAccepted = 0;

      // Make sure the food isn't rotten - there's no point in preserving food that has already rotted
      if (food.TryGetComp<CompRottable>() != null && food.TryGetComp<CompRottable>().Stage > RotStage.Fresh) {
        return countAccepted;
      }
      
      // Determine how much food to add
      if (food.stackCount <= SpaceLeftForItem) {
        countAccepted = food.stackCount;
      }
      else {
        countAccepted = SpaceLeftForItem;
      }

      // Adjust the rotting ticks based on the food added
      float t = (float)countAccepted / (FoodCount + countAccepted);
      int newItemRottingTicks = (int)(((ThingWithComps)food).GetComp<CompRottable>().RotProgressPct * GenDate.TicksPerDay);
      rottingTicks = (int)(Mathf.Lerp(rottingTicks, newItemRottingTicks, t));

      // Add food, then return the amount to the JobDriver
      AddItem(countAccepted, food.def);
      return countAccepted;
    }


    public void AddItem(int count, ThingDef sourceDef) {
      // Additional integrity checks
      if (Finished) {
        Log.Warning("Survivalist's Additions:: Tried to add food to a full smoker. Colonists should take the smoked food out first.");
        return;
      }
      int num = Mathf.Min(count, MaxCapacity - FoodCount);
      if (num <= 0) {
        return;
      }

      // If this type of food is already in the smoker, add this stack to it
      if (foodSources.Find((ThingCountExposable c) => c.thingDef == sourceDef) != null) {
        foodSources.Find((ThingCountExposable c) => c.thingDef == sourceDef).count += count;
      }
      // otherwise, add a new stack of food
      else {
        foodSources.Add(new ThingCountExposable(sourceDef, count));
      }

      // Adjust the progress to account for the new food
      Progress = GenMath.WeightedAverage(0f, num, Progress, FoodCount);
    }


    public Thing TakeOutProduct() {
      // Integrity check for the food being ready
      if (!Finished) {
        Log.Warning("Survivalist's Additions:: Tried to get smoked food but it's not yet smoked.");
        return null;
      }

      // Create the food
      Thing smokedFood;
      ThingCountExposable selectedFood = foodSources.RandomElement();
      if (selectedFood.thingDef == SrvDefOf.SRV_Cheese) {
        smokedFood = ThingMaker.MakeThing(SrvDefOf.SRV_SmokedCheese, null);
      }
      else {
        smokedFood = ThingMaker.MakeThing(SrvDefOf.SRV_SmokedMeat, null);
        smokedFood.TryGetComp<CompIngredients>().RegisterIngredient(selectedFood.thingDef);
      }

      smokedFood.stackCount = selectedFood.count;

      // Remove this food from the list, resetting if this is the last one
      foodSources.Remove(selectedFood);
      if (foodSources.Count <= 0) {
        Reset();
      }

      return smokedFood;
    }
    #endregion MethodGroup_Smoking


    #region MethodGroup_Signalling
    public void Reset() {
      Progress = 0f;
      foodSources.Clear();
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
          fillPercent = FoodCount / (float)MaxCapacity,
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
        if (Finished) {
          stringBuilder.AppendLine("SRV_ContainsSmokedFood".Translate(FoodCount, MaxCapacity));
          stringBuilder.AppendLine("SRV_Smoked".Translate());
        }
        else {
          
          stringBuilder.AppendLine("SRV_ContainsFood".Translate(FoodCount,MaxCapacity));
          if (NeedsTending) {
            stringBuilder.AppendLine("SRV_InspectSmokerTending".Translate());
          }
          stringBuilder.Append("FermentationProgress".Translate(Progress.ToStringPercent(),
            EstimatedTicksLeft.ToStringTicksToPeriod()));
          stringBuilder.Append(" ~ ");
          stringBuilder.AppendLine("SRV_Rot".Translate(RotProgressPct.ToStringPercent()));
          if (CurrentTempProgressSpeedFactor != 1f) {
            stringBuilder.AppendLine("SRV_SmokerOutOfIdealTemperature".Translate( CurrentTempProgressSpeedFactor.ToStringPercent()));
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
