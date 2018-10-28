using System;
using System.Collections.Generic;

using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SurvivalistsAdditions
{

    public class Building_Snare : Building_TrapDamager
    {

        private const int TicksToRemainDisabled = 5000;
        private Pawn affectedPawn;
        private int disabledTicks = 0;
        private bool disabled;
        private bool rearmAfterCleared;

        public bool Disabled
        {
            get { return disabled; }
        }

        public int Difficulty
        {
            get
            {
                int num = Find.Storyteller.difficulty.difficulty;
                if (num <= 0)
                {
                    num = 1;
                }
                return num;
            }
        }

        public float FailChance
        {
            get
            {
                return (SrvSettings.Snare_FailChance * Difficulty) / 100f;
            }
        }

        public float BreakChance
        {
            get
            {
                return (SrvSettings.Snare_BreakChance * Difficulty) / 100f;
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref rearmAfterCleared, "rearmAfterCleared", false);
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            foreach (Pawn pawn in PawnsInCell())
            {
                if (!pawn.Dead && (pawn.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredLarge) || pawn.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredSmall)))
                {
                    affectedPawn = pawn;
                    break;
                }
            }
        }


        public override void Tick()
        {
            if (!Disabled)
            {
                if (affectedPawn != null)
                {
                    if (affectedPawn.Position != Position || affectedPawn.Dead || affectedPawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) > 0.1f)
                    {
                        affectedPawn = null;
                        if (Rand.Value < BreakChance)
                        {
                            Destroy(DestroyMode.Deconstruct);
                            return;
                        }
                    }
                }


                foreach (Pawn p in PawnsInCell())
                {
                    CheckSpring(p);
                }

                if (this.IsHashIntervalTick(500) && !Disabled && Spawned)
                {
                    if (affectedPawn != null && affectedPawn.BodySize < 0.6f)
                    {
                        // The animal is trying to get free, simulate the escape attempt
                        if (Rand.Value > (FailChance / 2f))
                        {
                            ApplyHediff(affectedPawn);
                        }
                        else
                        {
                            RemoveHediff();
                        }
                    }
                    //TODO this may require attention
                    if (rearmAfterCleared)
                    {
                        bool ableToRearm = true;
                        foreach (Pawn pawn in PawnsInCell())
                        {
                            if (!pawn.Dead && (pawn.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredLarge) || pawn.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredSmall)))
                            {
                                ableToRearm = false;
                                break;
                            }
                            ableToRearm = true;
                        }

                        if (ableToRearm)
                        {
                            Map map = base.Map;
                            GenConstruct.PlaceBlueprintForBuild(def, base.Position, map, base.Rotation, Faction.OfPlayer, base.Stuff);
                            rearmAfterCleared = false;
                        }
                    }
                }
            }
            else
            {
                affectedPawn = null;
                if (disabledTicks < TicksToRemainDisabled)
                {
                    disabledTicks++;
                }
                else
                {
                    disabledTicks = 0;
                    disabled = false;
                }
            }

            /// No need to tick the comps, since the snare doesn't have any comps
            /// No other override actions needed for A17
            /// CHECKME: Future alphas may add/require comps
        }


        private List<Pawn> PawnsInCell()
        {
            List<Pawn> list = new List<Pawn>();
            foreach (Thing t in Position.GetThingList(Map))
            {
                if (t is Pawn)
                {
                    list.Add(t as Pawn);
                }
            }
            return list;
        }


        protected override float SpringChance(Pawn p)
        {
            float num;
            if (Disabled)
            {
                return 0;
            }
            if (!IsValidAnimal(p))
            {
                return 0;
            }
            if (KnowsOfSnare(p))
            {
                num = 0.008f;
            }
            else
            {
                num = this.GetStatValue(StatDefOf.TrapSpringChance, true);
            }
            if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
            {
                num *= 0.5f;
            }
            return Mathf.Clamp01(num);
        }


        private bool IsValidAnimal(Pawn p)
        {
            if (!p.RaceProps.Animal)
            {
                return false;
            }
            if (p.BodySize > 1f)
            {
                return false;
            }
            if ((p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer) &&
                     p.RaceProps.trainability.intelligenceOrder >= TrainabilityDefOf.Intermediate.intelligenceOrder)
            {
                return false;
            }
            return true;
        }


        public bool KnowsOfSnare(Pawn p)
        {
            if ((p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer) &&
                     p.RaceProps.trainability.intelligenceOrder >= TrainabilityDefOf.Intermediate.intelligenceOrder)
            {
                return true;
            }
            if (p.Faction == null && p.RaceProps.Animal)
            {
                return false;
            }
            if (p.guest != null && p.guest.Released)
            {
                return true;
            }
            Lord lord = p.GetLord();
            return p.guest != null && lord != null && lord.LordJob is LordJob_FormAndSendCaravan;
        }


        public override ushort PathFindCostFor(Pawn p)
        {
            if (KnowsOfSnare(p))
            {
                return 800;
            }
            return 0;
        }

        public override ushort PathWalkCostFor(Pawn p)
        {
            if (KnowsOfSnare(p))
            {
                return 30;
            }
            return 0;
        }


        public void Disable()
        {
            disabled = true;
            disabledTicks = 0;
            RemoveHediff();
        }


        private void CheckSpring(Pawn p)
        {
            if (Rand.Value < SpringChance(p))
            {
                Spring(p);
            }
        }


        protected override void SpringSub(Pawn p)
        {
            base.SpringSub(p);
            if (p != null && Rand.Value > FailChance)
            {
                affectedPawn = p;
                ApplyHediff(p);
            }
            
            foreach (Pawn pawn in PawnsInCell())
            {
                if (!pawn.Dead && (pawn.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredLarge) || pawn.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredSmall)))
                {
                    Map.designationManager.RemoveAllDesignationsOn(this);
                    rearmAfterCleared = true;
                    break;
                }
            }
            
        }


        private void ApplyHediff(Pawn p)
        {
            if (p.BodySize > 1f)
            {
                return;
            }
            if (p.BodySize >= 0.6f && p.BodySize <= 1f)
            {
                p.health.AddHediff(SrvDefOf.SRV_SnaredLarge);
            }
            else
            {
                if (!p.health.hediffSet.HasHediff(SrvDefOf.SRV_SnaredSmall))
                {
                    NotifyPlayer(p);
                }

                p.health.AddHediff(SrvDefOf.SRV_SnaredSmall);
            }
        }


        private void RemoveHediff()
        {
            if (affectedPawn != null)
            {
                Hediff largeDiff = affectedPawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == SrvDefOf.SRV_SnaredLarge);
                if (largeDiff != null)
                {
                    affectedPawn.health.RemoveHediff(largeDiff);
                }
                Hediff smallDiff = affectedPawn.health.hediffSet.hediffs.Find((Hediff x) => x.def == SrvDefOf.SRV_SnaredSmall);
                if (smallDiff != null)
                {
                    affectedPawn.health.RemoveHediff(smallDiff);
                }
                // Try to enter either a berserk or panicking state as a result of being snared
                // Larger animals are more likely to go berserk, smaller animals are more likely to flee
                if (Rand.Chance(affectedPawn.BodySize))
                {
                    affectedPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, true);
                }
                else if (!Rand.Chance(affectedPawn.BodySize))
                {
                    affectedPawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee, null, true);
                }
            }
        }


        private void NotifyPlayer(Pawn p)
        {
            Map map = base.Map;
            // If the notification type isn't None, try to notify the player
            if (SrvSettings.Snare_NotificationType != NotificationType.None)
            {
                // Determine if this is a positive notification or not
                bool isPositive = !(p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer);
                // Check if this notification is allowed
                if ((isPositive && SrvSettings.Snare_AllowPositiveNotification) || (!isPositive && SrvSettings.Snare_AllowNegativeNotification))
                {

                    // If the notification type is SilentText
                    if (SrvSettings.Snare_NotificationType == NotificationType.SilentText)
                    {
                        Messages.Message("SRV_LetterSnareTriggered".Translate(p.LabelShort, p),
                            new TargetInfo(Position, Map),
                            MessageTypeDefOf.SilentInput
                        );
                    }
                    // If the notification type is TextWithSound
                    else if (SrvSettings.Snare_NotificationType == NotificationType.TextWithSound)
                    {
                        Messages.Message("SRV_LetterSnareTriggered".Translate(p.LabelShort, p),
                            new TargetInfo(Position, Map),
                            (isPositive) ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent
                        );
                    }
                    // If the notification type is Letter
                    else
                    {
                        Find.LetterStack.ReceiveLetter("SRV_LetterSnareTriggeredLabel".Translate(p.LabelShort, p),
                            "SRV_LetterSnareTriggered".Translate(p.LabelShort, p),
                            (isPositive) ? LetterDefOf.PositiveEvent : LetterDefOf.NegativeEvent,
                            new TargetInfo(Position, Map, false)
                        );
                    }
                }
            }
        }


        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            if (!Disabled)
            {
                Action disableSnare = delegate
                {
                    if (selPawn.CanReserveAndReach(this, PathEndMode.ClosestTouch, Danger.Deadly, ignoreOtherReservations: true))
                    {
                        Job job = new Job(SrvDefOf.SRV_DisableSnare, this);
                        selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                };
                yield return new FloatMenuOption(Static.DisableSnare, disableSnare, MenuOptionPriority.RescueOrCapture);
            }
        }
    }
}
