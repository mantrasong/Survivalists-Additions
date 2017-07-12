using System.Collections.Generic;

using Verse.AI;

namespace SurvivalistsAdditions {

  public class JobDriver_TendToSmoker : JobDriver {

    private const TargetIndex SmokerInd = TargetIndex.A;

    protected Building_Smoker Smoker {
      get {
        return (Building_Smoker)CurJob.GetTarget(TargetIndex.A).Thing;
      }
    }


    protected override IEnumerable<Toil> MakeNewToils() {

      // Verify smoker and meat validity
      this.FailOnDestroyedNullOrForbidden(SmokerInd);
      this.FailOn(() => !Smoker.NeedsTending);

      // Reserve the smoker
      yield return Toils_Reserve.Reserve(SmokerInd);

      // Go to the smoker
      yield return Toils_Goto.GotoThing(SmokerInd, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(SmokerInd);

      // Add delay for tending to the smoker
      yield return Toils_General.Wait(Static.GenericWaitDuration).FailOnDestroyedNullOrForbidden(SmokerInd).WithProgressBarToilDelay(SmokerInd);

      // Tend to the smoker
      Toil tend = new Toil();
      tend.initAction = () => {
        Smoker.Tend();
      };
      tend.defaultCompleteMode = ToilCompleteMode.Instant;
      yield return tend;

      // End the current job
      yield break;
    }
  }
}
