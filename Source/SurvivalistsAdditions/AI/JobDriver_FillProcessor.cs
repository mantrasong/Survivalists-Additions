using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_FillProcessor : JobDriver {

    private const TargetIndex ProcessorInd = TargetIndex.A;
    private const TargetIndex ItemInd = TargetIndex.B;

    protected IItemProcessor Processor {
      get {
        return (IItemProcessor)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Item {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }

    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify processor and item validity
      this.FailOn(() => Processor.SpaceLeftForItem <= 0);
      this.FailOnDestroyedNullOrForbidden(ProcessorInd);
      this.FailOnDestroyedNullOrForbidden(ItemInd);
      this.FailOn(() => (Item.TryGetComp<CompRottable>() != null && Item.TryGetComp<CompRottable>().Stage != RotStage.Fresh));

      // Reserve resources
      // Creating the toil before yielding allows for CheckForGetOpportunityDuplicate
      Toil ingToil = Toils_Reserve.Reserve(ItemInd);
      yield return ingToil;

      // Reserve the processor
      yield return Toils_Reserve.Reserve(ProcessorInd);

      // Go to the item
      yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch)
        .FailOnSomeonePhysicallyInteracting(ItemInd)
        .FailOnDestroyedNullOrForbidden(ItemInd);

      // Haul the item
      yield return Toils_Haul.StartCarryThing(ItemInd);
      yield return Toils_Haul.CheckForGetOpportunityDuplicate(ingToil, ItemInd, TargetIndex.None);

      // Carry the item to the processor
      yield return Toils_Haul.CarryHauledThingToCell(ProcessorInd);

      // Add delay for adding items to the processor
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(ProcessorInd).WithProgressBarToilDelay(ProcessorInd);

      // Use the item
      Toil add = new Toil();
      add.initAction = () => {
        int amountAccepted = Processor.AddItem(Item);
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
