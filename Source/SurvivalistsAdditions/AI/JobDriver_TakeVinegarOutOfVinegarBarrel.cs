using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_TakeVinegarOutOfVinegarBarrel : JobDriver {

    private const TargetIndex BarrelInd = TargetIndex.A;
    private const TargetIndex VinegarToHaulInd = TargetIndex.B;
    private const TargetIndex StorageCellInd = TargetIndex.C;

    protected Building_VinegarBarrel Barrel {
      get {
        return (Building_VinegarBarrel)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Vinegar {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }


    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify barrel validity
      this.FailOn(() => !Barrel.Fermented);
      this.FailOnDestroyedNullOrForbidden(BarrelInd);

      // Reserve barrel
      yield return Toils_Reserve.Reserve(BarrelInd);

      // Go to the barrel
      yield return Toils_Goto.GotoThing(BarrelInd, PathEndMode.ClosestTouch);

      // Add delay for collecting vinegar from barrel
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(BarrelInd).WithProgressBarToilDelay(BarrelInd);

      // Collect vinegar
      Toil collect = new Toil();
      collect.initAction = () => {
        Thing vinegar = Barrel.TakeOutProduct();
        GenPlace.TryPlaceThing(vinegar, pawn.Position, Map, ThingPlaceMode.Near);
        StoragePriority storagePriority = HaulAIUtility.StoragePriorityAtFor(vinegar.Position, vinegar);
        IntVec3 c;

        // Try to find a suitable storage spot for the alcohol
        if (StoreUtility.TryFindBestBetterStoreCellFor(vinegar, pawn, Map, storagePriority, pawn.Faction, out c)) {
          CurJob.SetTarget(TargetIndex.B, vinegar);
          CurJob.count = vinegar.stackCount;
          CurJob.SetTarget(TargetIndex.C, c);
        }
        // If there is no spot to store the vinegar, end this job
        else {
          EndJobWith(JobCondition.Incompletable);
        }
      };
      collect.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return collect;

      // Reserve the vinegar
      yield return Toils_Reserve.Reserve(VinegarToHaulInd);

      // Reserve the storage cell
      yield return Toils_Reserve.Reserve(StorageCellInd);

      // Go to the vinegar
      yield return Toils_Goto.GotoThing(VinegarToHaulInd, PathEndMode.ClosestTouch);

      // Pick up the vinegar
      yield return Toils_Haul.StartCarryThing(VinegarToHaulInd);

      // Carry the vinegar to the storage cell, then place it down
      Toil carry = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
      yield return carry;
      yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carry, true);

      // End the current job
      yield break;
    }
  }
}
