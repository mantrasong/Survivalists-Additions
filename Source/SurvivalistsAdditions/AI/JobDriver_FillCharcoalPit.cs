using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_FillCharcoalPit : JobDriver{

    private const TargetIndex PitInd = TargetIndex.A;
    private const TargetIndex WoodInd = TargetIndex.B;

    protected Building_CharcoalPit Pit {
      get {
        return (Building_CharcoalPit)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Wood {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }

    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify pit and wood validity
      this.FailOnDestroyedNullOrForbidden(PitInd);
      this.FailOnDestroyedNullOrForbidden(WoodInd);
      this.FailOn(() => Pit.SpaceLeftForWood <= 0);

      // Reserve resources
      // Creating the toil before yielding allows for CheckForGetOpportunityDuplicate
      Toil ingToil = Toils_Reserve.Reserve(WoodInd);
      yield return ingToil;

      // Reserve the pit
      yield return Toils_Reserve.Reserve(PitInd);

      // Go to the wood
      yield return Toils_Goto.GotoThing(WoodInd, PathEndMode.ClosestTouch)
        .FailOnSomeonePhysicallyInteracting(WoodInd)
        .FailOnDestroyedNullOrForbidden(WoodInd);

      // Haul the wood
      yield return Toils_Haul.StartCarryThing(WoodInd);
      yield return Toils_Haul.CheckForGetOpportunityDuplicate(ingToil, WoodInd, TargetIndex.None);

      // Carry wood to the pit
      yield return Toils_Haul.CarryHauledThingToCell(PitInd);

      // Add delay for adding wood to pit
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(PitInd).WithProgressBarToilDelay(PitInd);

      // Use wood
      Toil add = new Toil();
      add.initAction = () => {
        int amountAccepted = Pit.AddWood(Wood);
        if (amountAccepted <= 0) {
          EndJobWith(JobCondition.Incompletable);
        }
        if (amountAccepted >= pawn.carryTracker.CarriedThing.stackCount) {
          pawn.carryTracker.CarriedThing.Destroy();
        }
        else {
          pawn.carryTracker.CarriedThing.stackCount -= amountAccepted;
        }
      };
      add.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return add;

      // End the current job
      yield break;
    }
  }
}
