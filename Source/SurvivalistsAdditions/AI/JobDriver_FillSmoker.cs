using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_FillSmoker : JobDriver {

    private const TargetIndex SmokerInd = TargetIndex.A;
    private const TargetIndex MeatInd = TargetIndex.B;

    protected Building_Smoker Smoker {
      get {
        return (Building_Smoker)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Meat {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }

    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify smoker and meat validity
      this.FailOnDestroyedNullOrForbidden(SmokerInd);
      this.FailOnDestroyedNullOrForbidden(MeatInd);
      this.FailOn(() => Smoker.SpaceLeftForMeat <= 0);
      this.FailOn(() => Meat.TryGetComp<CompRottable>() == null || Meat.TryGetComp<CompRottable>().Stage != RotStage.Fresh);

      // Reserve resources
      // Creating the toil before yielding allows for CheckForGetOpportunityDuplicate
      Toil ingToil = Toils_Reserve.Reserve(MeatInd);
      yield return ingToil;

      // Reserve the smoker
      yield return Toils_Reserve.Reserve(SmokerInd);

      // Go to the meat
      yield return Toils_Goto.GotoThing(MeatInd, PathEndMode.ClosestTouch)
        .FailOnSomeonePhysicallyInteracting(MeatInd)
        .FailOnDestroyedNullOrForbidden(MeatInd);

      // Haul the meat
      yield return Toils_Haul.StartCarryThing(MeatInd);
      yield return Toils_Haul.CheckForGetOpportunityDuplicate(ingToil, MeatInd, TargetIndex.None);

      // Carry meat to the smoker
      yield return Toils_Haul.CarryHauledThingToCell(SmokerInd);

      // Add delay for adding meat to smoker
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(SmokerInd).WithProgressBarToilDelay(SmokerInd);

      // Use meat
      Toil add = new Toil();
      add.initAction = () => {
        int amountAccepted = Smoker.AddMeat(Meat);
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
