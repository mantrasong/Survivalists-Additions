using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
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
        return (IItemProcessor)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Item {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }


    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify processor validity
      this.FailOn(() => !Processor.Finished);
      this.FailOnDestroyedNullOrForbidden(ProcessorInd);

      // Reserve the processor
      yield return Toils_Reserve.Reserve(ProcessorInd);

      // Go to the processor
      yield return Toils_Goto.GotoThing(ProcessorInd, PathEndMode.ClosestTouch);

      // Add delay for collecting items from the processor
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(ProcessorInd).WithProgressBarToilDelay(ProcessorInd);

      // Collect items
      Toil collect = new Toil();
      collect.initAction = () => {
        Thing item = Processor.TakeOutProduct();
        GenPlace.TryPlaceThing(item, pawn.Position, Map, ThingPlaceMode.Near);
        StoragePriority storagePriority = HaulAIUtility.StoragePriorityAtFor(item.Position, item);
        IntVec3 c;

        // Try to find a suitable storage spot for the item
        if (StoreUtility.TryFindBestBetterStoreCellFor(item, pawn, Map, storagePriority, pawn.Faction, out c)) {
          CurJob.SetTarget(TargetIndex.B, item);
          CurJob.count = item.stackCount;
          CurJob.SetTarget(TargetIndex.C, c);
        }
        // If there is no spot to store the item, end this job
        else {
          EndJobWith(JobCondition.Incompletable);
        }
      };
      collect.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return collect;

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

      // End the current job
      yield break;
    }
  }
}
