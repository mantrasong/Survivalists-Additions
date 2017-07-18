using System;
using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SurvivalistsAdditions {

  public class Building_Snare : Building_TrapRearmable {

		private const int TicksToRemainDisabled = 5000;
		private Pawn affectedPawn;
		private int disabledTicks = 0;
		private bool disabled;

		public bool Disabled {
			get { return disabled; }
		}

		public int Difficulty {
			get {
				int num = Find.Storyteller.difficulty.difficulty;
				if (num <= 0) {
					num = 1;
				}
				return num;
			}
		}

		public float FailChance {
			get {
				return (SrvSettings.Snare_FailChance * Difficulty) / 100f;
			}
		}

		public float BreakChance {
			get {
				return (SrvSettings.Snare_BreakChance * Difficulty) / 100f;
			}
		}


		public override void Tick() {
			if (!Disabled) {
				if (affectedPawn != null) {
					if (affectedPawn.Position != Position || affectedPawn.Dead || affectedPawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) > 0.1f) {
						affectedPawn = null;
						if (Rand.Value < BreakChance) {
							Destroy(DestroyMode.Deconstruct);
							return;
						}
					}
				}

				if (Armed) {
					List<Thing> thingList = Position.GetThingList(Map);
					for (int i = 0; i < thingList.Count; i++) {
						Pawn pawn = thingList[i] as Pawn;
						if (pawn != null) {
							CheckSpring(pawn);
						}
					}
				}

				if (this.IsHashIntervalTick(1000) && !Armed && !Disabled && Spawned) {
					if (affectedPawn != null && affectedPawn.BodySize < 0.6f) {
						ApplyHediff(affectedPawn);
					}
				}
			}
			else {
				affectedPawn = null;
				if (disabledTicks < TicksToRemainDisabled) {
					disabledTicks++;
				}
				else {
					disabledTicks = 0;
					disabled = false;
				}
			}

			/// No need to tick the comps, since the snare doesn't have any comps
			/// No other override actions needed for A17
			/// CHECKME: Future alphas may add/require comps
		}


		protected override float SpringChance(Pawn p) {
			float num;
			if (Disabled) {
				return 0;
			}
			if (!IsValidAnimal(p)) {
				return 0;
			}
			if (KnowsOfSnare(p)) {
				num = 0.008f;
			}
			else {
				num = this.GetStatValue(StatDefOf.TrapSpringChance, true);
			}
			if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer) {
				num *= 0.5f;
			}
			return Mathf.Clamp01(num);
		}


		private bool IsValidAnimal(Pawn p) {
			if (!p.RaceProps.Animal) {
				return false;
			}
			if (p.BodySize > 1f) {
				return false;
			}
			if ((p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer) && 
					 p.RaceProps.TrainableIntelligence.intelligenceOrder >= TrainableIntelligenceDefOf.Intermediate.intelligenceOrder) {
				return false;
			}
			return true;
		}


		public bool KnowsOfSnare(Pawn p) {
			if ((p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer) &&
					 p.RaceProps.TrainableIntelligence.intelligenceOrder >= TrainableIntelligenceDefOf.Intermediate.intelligenceOrder) {
				return true;
			}
			if (p.Faction == null && p.RaceProps.Animal) {
				return false;
			}
			if (p.guest != null && p.guest.released) {
				return true;
			}
			Lord lord = p.GetLord();
			return p.guest != null && lord != null && lord.LordJob is LordJob_FormAndSendCaravan;
		}


		public override ushort PathFindCostFor(Pawn p) {
			if (!Armed) {
				return 0;
			}
			if (KnowsOfSnare(p)) {
				return 800;
			}
			return 0;
		}

		public override ushort PathWalkCostFor(Pawn p) {
			if (!Armed) {
				return 0;
			}
			if (KnowsOfSnare(p)) {
				return 30;
			}
			return 0;
		}


		public void Disable() {
			disabled = true;
			disabledTicks = 0;
			if (affectedPawn != null) {
				Hediff largeDiff = affectedPawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == SrvDefOf.SRV_SnaredLarge);
				if (largeDiff != null) {
					affectedPawn.health.RemoveHediff(largeDiff);
				}
				Hediff smallDiff = affectedPawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == SrvDefOf.SRV_SnaredSmall);
				if (smallDiff != null) {
					affectedPawn.health.RemoveHediff(smallDiff);
				}
			}
		}


		private void CheckSpring(Pawn p) {
			if (Rand.Value < SpringChance(p)) {
				Spring(p);
			}
		}


		protected override void SpringSub(Pawn p) {
			base.SpringSub(p);
			if (Rand.Value > FailChance && p != null) {
				affectedPawn = p;
				ApplyHediff(p);
      }
    }


		private void ApplyHediff(Pawn p) {

			if (p.BodySize >= 0.6f && p.BodySize <= 1f) {
				p.health.AddHediff(SrvDefOf.SRV_SnaredLarge);
			}
			else {
				if (!p.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredSmall)) {
					if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer) {
						Find.LetterStack.ReceiveLetter("SRV_LetterSnareTriggeredLabel".Translate(new object[]
						{
						p.NameStringShort.CapitalizeFirst()
						}), "SRV_LetterSnareTriggeredColony".Translate(new object[]
						{
						p.NameStringShort.CapitalizeFirst()
						}), LetterDefOf.BadNonUrgent, new TargetInfo(Position, Map, false), null);
					}
					else {
						Find.LetterStack.ReceiveLetter("SRV_LetterSnareTriggeredLabel".Translate(new object[]
						{
						p.NameStringShort.CapitalizeFirst()
						}), "SRV_LetterSnareTriggered".Translate(new object[]
						{
						p.NameStringShort
						}), LetterDefOf.Good, new TargetInfo(Position, Map, false), null);
					}
				}

				p.health.AddHediff(SrvDefOf.SRV_SnaredSmall);
			}
		}


		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
			if (!Armed && !Disabled) {
				Action disableSnare = delegate
				{
					if (selPawn.CanReserveAndReach(this, PathEndMode.ClosestTouch, Danger.Deadly, ignoreOtherReservations: true)) {
						Job job = new Job(SrvDefOf.SRV_DisableSnare, this);
						selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
					}
				};
				yield return new FloatMenuOption(Static.DisableSnare, disableSnare, MenuOptionPriority.RescueOrCapture);
			}
		}
	}
}
