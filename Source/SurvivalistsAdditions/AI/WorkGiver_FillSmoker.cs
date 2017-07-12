using System;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class WorkGiver_FillSmoker : WorkGiver_Scanner {

    public override ThingRequest PotentialWorkThingRequest {
      get {
        return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
      }
    }

    public override PathEndMode PathEndMode {
      get {
        return PathEndMode.Touch;
      }
    }


    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
      Building_Smoker smoker = t as Building_Smoker;
      if (smoker == null || smoker.Smoked) {
        return false;
      }
      if (!smoker.CanAddMeat) {
        JobFailReason.Is(Static.SmokerLocked);
        return false;
      }
      if (smoker.SpaceLeftForMeat <= 0 || t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced)) {
        return false;
      }
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) {
        return false;
      }
      if (FindIngredient(pawn, smoker) == null) {
        JobFailReason.Is(Static.NoMeat);
        return false;
      }
      return !t.IsBurning();
    }


    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
      Building_Smoker building_Smoker = (Building_Smoker)t;
      Thing t2 = FindIngredient(pawn, building_Smoker);
      return new Job(SrvDefOf.SRV_FillSmoker, t, t2) {
        count = building_Smoker.SpaceLeftForMeat
      };
    }


    private Thing FindIngredient(Pawn pawn, Building_Smoker smoker) {
      Predicate<Thing> validator = ((Thing meat) => 
        !meat.IsForbidden(pawn) && pawn.CanReserve(meat) &&
        meat.def.IsNutritionGivingIngestible && (meat.def.ingestible.foodType & FoodTypeFlags.Meat) != FoodTypeFlags.None &&
        meat.TryGetComp<CompRottable>() != null && meat.TryGetComp<CompRottable>().Stage == RotStage.Fresh && meat.def != SrvDefOf.SRV_SmokedMeat
      );
      return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator);
    }
  }
}
