using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_TakeCharcoalOutOfPit : JobDriver {

    private const TargetIndex PitInd = TargetIndex.A;
    private const TargetIndex CharcoalToHaulInd = TargetIndex.B;
    private const TargetIndex StorageCellInd = TargetIndex.C;

    protected Building_CharcoalPit Pit {
      get {
        return (Building_CharcoalPit)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Charcoal {
      get {
        return CurJob.GetTarget(TargetIndex.B).Thing;
      }
    }


    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify pit validity
      this.FailOn(() => !Pit.Charred);
      this.FailOnDestroyedNullOrForbidden(PitInd);

      // Reserve the pit
      yield return Toils_Reserve.Reserve(PitInd);

      // Go to the pit
      yield return Toils_Goto.GotoThing(PitInd, PathEndMode.ClosestTouch);

      // Add delay for collecting charcoal from the pit
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(PitInd).WithProgressBarToilDelay(PitInd);

      // Collect charcoal
      Toil collect = new Toil();
      collect.initAction = () => {
        Thing charcoal = Pit.TakeOutProduct();
        GenPlace.TryPlaceThing(charcoal, pawn.Position, Map, ThingPlaceMode.Near);
        StoragePriority storagePriority = HaulAIUtility.StoragePriorityAtFor(charcoal.Position, charcoal);
        IntVec3 c;

        // Try to find a suitable storage spot for the charcoal
        if (StoreUtility.TryFindBestBetterStoreCellFor(charcoal, pawn, Map, storagePriority, pawn.Faction, out c)) {
          CurJob.SetTarget(TargetIndex.B, charcoal);
          CurJob.count = charcoal.stackCount;
          CurJob.SetTarget(TargetIndex.C, c);
        }
        // If there is no spot to store the charcoal, end this job
        else {
          EndJobWith(JobCondition.Incompletable);
        }
      };
      collect.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return collect;

      // Reserve the charcoal
      yield return Toils_Reserve.Reserve(CharcoalToHaulInd);

      // Reserve the storage cell
      yield return Toils_Reserve.Reserve(StorageCellInd);

      // Go to the charcoal
      yield return Toils_Goto.GotoThing(CharcoalToHaulInd, PathEndMode.ClosestTouch);

      // Pick up the charcoal
      yield return Toils_Haul.StartCarryThing(CharcoalToHaulInd);

      // Carry the charcoal to the storage cell, then place it down
      Toil carry = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
      yield return carry;
      yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carry, true);

      // End the current job
      yield break;
    }
  }
}
