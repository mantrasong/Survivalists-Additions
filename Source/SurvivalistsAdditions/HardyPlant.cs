using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace SurvivalistsAdditions {

	public class HardyPlant : Plant {

		public const float HardyMinGrowthTemperature = -10f;
		public const float HardyMinOptimalGrowthTemperature = 0f;
		public const float HardyMaxOptimalGrowthTemperature = 35f;
		public const float HardyMaxGrowthTemperature = 50f;
		public const float HardyMaxLeaflessTemperature = -12f;

		private const float HardyMinLeaflessTemperature = -18f;
		private string hardyCachedLabelMouseover;

		public override float GrowthRate {
			get {
				return GrowthRateFactor_Fertility * HardyGrowthRateFactor_Temperature * GrowthRateFactor_Light;
			}
		}

		public float HardyGrowthRateFactor_Temperature {
			get {
				float num;
				if (!GenTemperature.TryGetTemperatureForCell(Position, Map, out num)) {
					return 1f;
				}
				if (num < HardyMinOptimalGrowthTemperature) {
					return Mathf.InverseLerp(HardyMinGrowthTemperature, HardyMinOptimalGrowthTemperature, num);
				}
				if (num > HardyMaxOptimalGrowthTemperature) {
					return Mathf.InverseLerp(HardyMaxGrowthTemperature, HardyMaxOptimalGrowthTemperature, num);
				}
				return 1f;
			}
		}

		protected override float LeaflessTemperatureThresh {
			get {
				float num = 16f;
				return (float)this.HashOffset() * 0.01f % num - num + -2f;
			}
		}

		public override string LabelMouseover {
			get {
				if (hardyCachedLabelMouseover == null) {
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append(def.LabelCap);
					stringBuilder.Append(" (" + "PercentGrowth".Translate(new object[]
					{
						GrowthPercentString
					}));
					if (Dying) {
						stringBuilder.Append(", " + "DyingLower".Translate());
					}
					stringBuilder.Append(")");
					hardyCachedLabelMouseover = stringBuilder.ToString();
				}
				return hardyCachedLabelMouseover;
			}
		}


		public static bool GrowthSeasonNow(IntVec3 c, Map map) {
			Room roomOrAdjacent = c.GetRoomOrAdjacent(map, RegionType.Set_All);
			if (roomOrAdjacent == null) {
				return false;
			}
			if (roomOrAdjacent.UsesOutdoorTemperature) {
				return map.weatherManager.growthSeasonMemory.GrowthSeasonOutdoorsNow;
			}
			float temperature = c.GetTemperature(map);
			return temperature > HardyMinGrowthTemperature && temperature < HardyMaxGrowthTemperature;
		}


		public override void TickLong() {
			CheckTemperatureMakeLeafless();
			if (Destroyed) {
				return;
			}
			if (GrowthSeasonNow(Position, Map)) {
				if (!HasEnoughLightToGrow) {
					unlitTicks += 2000;
				}
				else {
					unlitTicks = 0;
				}
				float num = growthInt;
				bool flag = LifeStage == PlantLifeStage.Mature;
				growthInt += GrowthPerTick * 2000f;
				if (growthInt > 1f) {
					growthInt = 1f;
				}
				if (((!flag && LifeStage == PlantLifeStage.Mature) || (int)(num * 10f) != (int)(growthInt * 10f)) && CurrentlyCultivated()) {
					Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things);
				}
				if (def.plant.LimitedLifespan) {
					ageInt += 2000;
					if (Dying) {
						Map map = Map;
						bool isCrop = IsCrop;
						int amount = Mathf.CeilToInt(10f);
						TakeDamage(new DamageInfo(DamageDefOf.Rotting, amount, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
						if (Destroyed) {
							if (isCrop && def.plant.Harvestable && MessagesRepeatAvoider.MessageShowAllowed("MessagePlantDiedOfRot-" + def.defName, 240f)) {
								Messages.Message("MessagePlantDiedOfRot".Translate(new object[]
								{
									Label
								}).CapitalizeFirst(), new TargetInfo(Position, map, false), MessageSound.Negative);
							}
							return;
						}
					}
				}
				if (def.plant.reproduces && growthInt >= 0.6f && Rand.MTBEventOccurs(def.plant.reproduceMtbDays, 60000f, 2000f)) {
					if (!GenPlant.SnowAllowsPlanting(Position, Map)) {
						return;
					}
					GenPlantReproduction.TryReproduceFrom(Position, def, SeedTargFindMode.Reproduce, Map);
				}
			}
			hardyCachedLabelMouseover = null;
		}


		public override string GetInspectString() {
			StringBuilder stringBuilder = new StringBuilder();
			if (LifeStage == PlantLifeStage.Growing) {
				stringBuilder.AppendLine("PercentGrowth".Translate(new object[]
				{
					GrowthPercentString
				}));
				stringBuilder.AppendLine("GrowthRate".Translate() + ": " + GrowthRate.ToStringPercent());
				if (Resting) {
					stringBuilder.AppendLine("PlantResting".Translate());
				}
				if (!HasEnoughLightToGrow) {
					stringBuilder.AppendLine("PlantNeedsLightLevel".Translate() + ": " + this.def.plant.growMinGlow.ToStringPercent());
				}
				float growthRateFactor_Temperature = HardyGrowthRateFactor_Temperature;
				if (growthRateFactor_Temperature < 0.99f) {
					if (growthRateFactor_Temperature < 0.01f) {
						stringBuilder.AppendLine("OutOfIdealTemperatureRangeNotGrowing".Translate());
					}
					else {
						stringBuilder.AppendLine("OutOfIdealTemperatureRange".Translate(new object[]
						{
							Mathf.RoundToInt(growthRateFactor_Temperature * 100f).ToString()
						}));
					}
				}
			}
			else if (LifeStage == PlantLifeStage.Mature) {
				if (def.plant.Harvestable) {
					stringBuilder.AppendLine("ReadyToHarvest".Translate());
				}
				else {
					stringBuilder.AppendLine("Mature".Translate());
				}
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}
	}
}
