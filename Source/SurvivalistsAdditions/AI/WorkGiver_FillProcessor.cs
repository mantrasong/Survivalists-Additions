using System;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class WorkGiver_FillProcessor : WorkGiver_Scanner {
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
      IItemProcessor workThing = t as IItemProcessor;
      if (workThing == null || workThing.Finished || workThing.SpaceLeftForItem <= 0) {
        return false;
      }
      if (!workThing.TemperatureAcceptable) {
        JobFailReason.Is(Static.TemperatureTrans);
        return false;
      }

      Building_Smoker smoker = t as Building_Smoker;
      if (smoker != null) {
        if (smoker.Finished) {
          return false;
        }
        if (!smoker.CanAddMeat) {
          JobFailReason.Is(Static.SmokerLocked);
          return false;
        }
      }
      
      if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced)) {
        return false;
      }
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) {
        return false;
      }
      if (FindIngredient(pawn, workThing) == null) {
        JobFailReason.Is(Static.NoIngredient);
        return false;
      }
      return !t.IsBurning();
    }


    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
      IItemProcessor workThing = t as IItemProcessor;
      Thing t2 = FindIngredient(pawn, workThing);
      return new Job(SrvDefOf.SRV_FillProcessor, t, t2) {
        count = workThing.SpaceLeftForItem
      };
    }


    private Thing FindIngredient(Pawn pawn, IItemProcessor workThing) {
      Predicate<Thing> validator;
      if (workThing.ItemValidator(pawn) != null) {
        validator = workThing.ItemValidator(pawn);
      }
      else {
        validator = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x);
      }
      return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, workThing.InputRequest, PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator);
    }
  }
}
