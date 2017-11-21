using System.Collections.Generic;

using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_TendToSmoker : JobDriver {

    private const TargetIndex SmokerInd = TargetIndex.A;

    protected Building_Smoker Smoker {
      get {
        return (Building_Smoker)job.GetTarget(TargetIndex.A).Thing;
      }
    }


		public override bool TryMakePreToilReservations() {
			return pawn.Reserve(Smoker, job);
		}


		protected override IEnumerable<Toil> MakeNewToils() {

      // Verify smoker and meat validity
      this.FailOnDespawnedNullOrForbidden(SmokerInd);
      this.FailOn(() => !Smoker.NeedsTending);

      // Go to the smoker
      yield return Toils_Goto.GotoThing(SmokerInd, PathEndMode.ClosestTouch)
				.FailOnSomeonePhysicallyInteracting(SmokerInd);

      // Add delay for tending to the smoker
      yield return Toils_General.Wait(Static.GenericWaitDuration)
				.FailOnDestroyedNullOrForbidden(SmokerInd)
				.WithProgressBarToilDelay(SmokerInd);

			// Tend to the smoker
			yield return new Toil() {
				initAction = () => {
					Smoker.Tend();
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
    }
  }
}
