using System;
using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace SurvivalistsAdditions
{

    public class JobDriver_FillProcessor : JobDriver
    {

        private const TargetIndex ProcessorInd = TargetIndex.A;
        private const TargetIndex ItemInd = TargetIndex.B;

        protected IItemProcessor Processor
        {
            get
            {
                return (IItemProcessor)job.GetTarget(TargetIndex.A).Thing;
            }
        }

        protected Thing Item
        {
            get
            {
                return job.GetTarget(TargetIndex.B).Thing;
            }
        }


        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve((Thing)Processor, job) && pawn.Reserve((Thing)Processor, job);
        }


        protected override IEnumerable<Toil> MakeNewToils()
        {

            // Verify processor and item validity
            this.FailOn(() => Processor.SpaceLeftForItem <= 0);
            this.FailOnDespawnedNullOrForbidden(ProcessorInd);
            this.FailOn(() => (Item.TryGetComp<CompRottable>() != null && Item.TryGetComp<CompRottable>().Stage != RotStage.Fresh));
            AddEndCondition(() => (Processor.SpaceLeftForItem > 0) ? JobCondition.Ongoing : JobCondition.Succeeded);

            // Reserve resources
            yield return Toils_General.DoAtomic(delegate
            {
                job.count = Processor.SpaceLeftForItem;
            });
            Toil reserveItem = Toils_Reserve.Reserve(ItemInd);
            yield return reserveItem;

            // Haul and add items
            yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(ItemInd)
                .FailOnSomeonePhysicallyInteracting(ItemInd);
            yield return Toils_Haul.StartCarryThing(ItemInd, false, true, false)
                .FailOnDestroyedNullOrForbidden(ItemInd);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveItem, ItemInd, TargetIndex.None, true, null);
            yield return Toils_Goto.GotoThing(ProcessorInd, PathEndMode.Touch);
            yield return Toils_General.Wait(Static.GenericWaitDuration)
                .FailOnDestroyedNullOrForbidden(ItemInd)
                .FailOnDestroyedNullOrForbidden(ProcessorInd)
                .FailOnCannotTouch(ProcessorInd, PathEndMode.Touch)
                .WithProgressBarToilDelay(ProcessorInd);

            // Use the item
            yield return new Toil()
            {
                initAction = () =>
                {
                    int amountAccepted = Processor.AddItem(Item);
                    if (amountAccepted <= 0)
                    {
                        EndJobWith(JobCondition.Incompletable);
                    }
                    if (amountAccepted >= pawn.carryTracker.CarriedThing.stackCount)
                    {
                        pawn.carryTracker.CarriedThing.Destroy();
                    }
                    else
                    {
                        pawn.carryTracker.CarriedThing.stackCount -= amountAccepted;
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
