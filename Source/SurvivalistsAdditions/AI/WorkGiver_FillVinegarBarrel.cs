using System;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class WorkGiver_FillVinegarBarrel : WorkGiver_Scanner {

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
      Building_VinegarBarrel vinegarBarrel = t as Building_VinegarBarrel;
      if (vinegarBarrel == null || vinegarBarrel.Fermented || vinegarBarrel.SpaceLeftForJuice <= 0) {
        return false;
      }
      float ambientTemperature = vinegarBarrel.AmbientTemperature;
      CompProperties_TemperatureRuinable compProperties = vinegarBarrel.def.GetCompProperties<CompProperties_TemperatureRuinable>();
      if (ambientTemperature < compProperties.minSafeTemperature + 2f || ambientTemperature > compProperties.maxSafeTemperature - 2f) {
        JobFailReason.Is(Static.TemperatureTrans);
        return false;
      }
      if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced)) {
        return false;
      }
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) {
        return false;
      }
      if (FindIngredient(pawn) == null) {
        JobFailReason.Is(Static.NoJuice);
        return false;
      }
      return !t.IsBurning();
    }


    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
      Building_VinegarBarrel barrel = (Building_VinegarBarrel)t;
      Thing t2 = FindIngredient(pawn);
      return new Job(SrvDefOf.SRV_FillVinegarBarrel, t, t2) {
        count = barrel.SpaceLeftForJuice
      };
    }


    private Thing FindIngredient(Pawn pawn) {
      Predicate<Thing> validator = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x);
      return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(SrvDefOf.SRV_VinegarJuice), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator);
    }
  }
}
