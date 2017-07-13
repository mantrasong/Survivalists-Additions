using System;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class WorkGiver_FillCharcoalPit : WorkGiver_Scanner {

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
      Building_CharcoalPit pit = t as Building_CharcoalPit;
      if (pit == null || pit.Charred || pit.SpaceLeftForWood <= 0) {
        return false;
      }
      if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced)) {
        return false;
      }
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) {
        return false;
      }
      if (FindIngredient(pawn) == null) {
        JobFailReason.Is(Static.NoWood);
        return false;
      }
      return !t.IsBurning();
    }


    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
      Building_CharcoalPit pit = (Building_CharcoalPit)t;
      Thing t2 = FindIngredient(pawn);
      return new Job(SrvDefOf.SRV_FillCharcoalPit, t, t2) {
        count = pit.SpaceLeftForWood
      };
    }


    private Thing FindIngredient(Pawn pawn) {
      Predicate<Thing> validator = ( (Thing x) => 
        x.def != null && x.def == ThingDefOf.WoodLog && !x.IsForbidden(pawn) && pawn.CanReserve(x)
      );
      return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator);
    }
  }
}
