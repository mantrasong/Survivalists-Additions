using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_FillVinegarBarrel : JobDriver {

    private const TargetIndex BarrelInd = TargetIndex.A;
    private const TargetIndex JuiceInd = TargetIndex.B;
    private const int Duration = 200;

    protected Building_VinegarBarrel Barrel {
      get {
        return (Building_VinegarBarrel)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Juice {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }

    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify barrel and ingredient validity
      this.FailOn(() => Barrel.SpaceLeftForJuice <= 0);
      this.FailOnDestroyedNullOrForbidden(BarrelInd);
      this.FailOnDestroyedNullOrForbidden(JuiceInd);

      // Reserve resources
      // Creating the toil before yielding allows for CheckForGetOpportunityDuplicate
      Toil ingToil = Toils_Reserve.Reserve(JuiceInd);
      yield return ingToil;

      // Reserve barrel
      yield return Toils_Reserve.Reserve(BarrelInd);

      // Go to the juice
      yield return Toils_Goto.GotoThing(JuiceInd, PathEndMode.ClosestTouch)
        .FailOnSomeonePhysicallyInteracting(JuiceInd)
        .FailOnDestroyedNullOrForbidden(JuiceInd);

      // Haul the juice
      yield return Toils_Haul.StartCarryThing(JuiceInd);
      yield return Toils_Haul.CheckForGetOpportunityDuplicate(ingToil, JuiceInd, TargetIndex.None);

      // Carry juice to the barrel
      yield return Toils_Haul.CarryHauledThingToCell(BarrelInd);

      // Add delay for adding juice to barrel
      yield return Toils_General.Wait(Duration).FailOnDestroyedNullOrForbidden(BarrelInd).WithProgressBarToilDelay(BarrelInd);

      // Use juice
      Toil add = new Toil();
      add.initAction = () => {
        int amountAccepted = Barrel.AddJuice(Juice);
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
