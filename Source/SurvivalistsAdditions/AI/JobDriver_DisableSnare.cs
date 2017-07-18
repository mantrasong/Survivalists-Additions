using RimWorld;
using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace SurvivalistsAdditions {

	public class JobDriver_DisableSnare : JobDriver {

		protected override IEnumerable<Toil> MakeNewToils() {
			this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.Wait(100).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);

			yield return new Toil {
				initAction = delegate {
					Pawn actor = GetActor();
					Building_Snare snare = (Building_Snare)actor.CurJob.targetA.Thing;
					snare.Disable();
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
