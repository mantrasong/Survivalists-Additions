using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_TakeItemOutOfProcessor : JobDriver {

    private const TargetIndex ProcessorInd = TargetIndex.A;
    private const TargetIndex ItemToHaulInd = TargetIndex.B;
    private const TargetIndex StorageCellInd = TargetIndex.C;

    protected IItemProcessor Processor {
      get {
        return (IItemProcessor)job.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Item {
      get {
        return job.GetTarget(TargetIndex.B).Thing;
      }
    }


		public override bool TryMakePreToilReservations() {
			return pawn.Reserve(job.GetTarget(TargetIndex.A).Thing, job);
		}


		protected override IEnumerable<Toil> MakeNewToils() {

      // Verify processor validity
      this.FailOnDespawnedNullOrForbidden(ProcessorInd);
      this.FailOn(() => !Processor.Finished);

      // Go to the processor
      yield return Toils_Goto.GotoThing(ProcessorInd, PathEndMode.ClosestTouch);

      // Add delay for collecting items from the processor
      yield return Toils_General.Wait(Static.GenericWaitDuration)
				.FailOnDestroyedNullOrForbidden(ProcessorInd)
				.WithProgressBarToilDelay(ProcessorInd);

			// Collect items
			yield return new Toil() {
				initAction = () => {
					Thing item = Processor.TakeOutProduct();
					GenPlace.TryPlaceThing(item, pawn.Position, Map, ThingPlaceMode.Near);
					StoragePriority storagePriority = HaulAIUtility.StoragePriorityAtFor(item.Position, item);

					// Try to find a suitable storage spot for the item
					if (StoreUtility.TryFindBestBetterStoreCellFor(item, pawn, Map, storagePriority, pawn.Faction, out IntVec3 c)) {
						job.SetTarget(TargetIndex.C, c);
						job.SetTarget(TargetIndex.B, item);
						job.count = item.stackCount;
					}
					// If there is no spot to store the item, end this job
					else {
						EndJobWith(JobCondition.Incompletable);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};

      // Reserve the item
      yield return Toils_Reserve.Reserve(ItemToHaulInd);

      // Reserve the storage cell
      yield return Toils_Reserve.Reserve(StorageCellInd);

      // Go to the item
      yield return Toils_Goto.GotoThing(ItemToHaulInd, PathEndMode.ClosestTouch);

      // Pick up the item
      yield return Toils_Haul.StartCarryThing(ItemToHaulInd);

      // Carry the item to the storage cell, then place it down
      Toil carry = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
      yield return carry;
      yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carry, true);
    }
  }
}
