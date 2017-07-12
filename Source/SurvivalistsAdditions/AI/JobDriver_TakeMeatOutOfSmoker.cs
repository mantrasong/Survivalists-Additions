using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_TakeMeatOutOfSmoker : JobDriver {

    private const TargetIndex SmokerInd = TargetIndex.A;
    private const TargetIndex MeatToHaulInd = TargetIndex.B;
    private const TargetIndex StorageCellInd = TargetIndex.C;

    protected Building_Smoker Smoker {
      get {
        return (Building_Smoker)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing SmokedMeat {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }


    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify smoker validity
      this.FailOn(() => !Smoker.Smoked);
      this.FailOnDestroyedNullOrForbidden(SmokerInd);

      // Reserve the smoker
      yield return Toils_Reserve.Reserve(SmokerInd);

      // Go to the smoker
      yield return Toils_Goto.GotoThing(SmokerInd, PathEndMode.ClosestTouch);

      // Add delay for collecting meat from the smoker
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(SmokerInd).WithProgressBarToilDelay(SmokerInd);

      // Collect smoked meat
      Toil collect = new Toil();
      collect.initAction = () => {
        Thing smokedMeat = Smoker.TakeOutProduct();
        GenPlace.TryPlaceThing(smokedMeat, pawn.Position, Map, ThingPlaceMode.Near);
        StoragePriority storagePriority = HaulAIUtility.StoragePriorityAtFor(smokedMeat.Position, smokedMeat);
        IntVec3 c;

        // Try to find a suitable storage spot for the meat
        if (StoreUtility.TryFindBestBetterStoreCellFor(smokedMeat, pawn, Map, storagePriority, pawn.Faction, out c)) {
          CurJob.SetTarget(TargetIndex.B, smokedMeat);
          CurJob.count = smokedMeat.stackCount;
          CurJob.SetTarget(TargetIndex.C, c);
        }
        // If there is no spot to store the meat, end this job
        else {
          EndJobWith(JobCondition.Incompletable);
        }
      };
      collect.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return collect;

      // Reserve the meat
      yield return Toils_Reserve.Reserve(MeatToHaulInd);

      // Reserve the storage cell
      yield return Toils_Reserve.Reserve(StorageCellInd);

      // Go to the meat
      yield return Toils_Goto.GotoThing(MeatToHaulInd, PathEndMode.ClosestTouch);

      // Pick up the meat
      yield return Toils_Haul.StartCarryThing(MeatToHaulInd);

      // Carry the meat to the storage cell, then place it down
      Toil carry = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
      yield return carry;
      yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carry, true);

      // End the current job
      yield break;
    }
  }
}
