using System;

using RimWorld;
using Verse;

namespace SurvivalistsAdditions {

  public class GenStep_Plants : GenStep {

    public override void Generate(Map map) {
      // Adjust the amount of plants to spawn
      int timesToSpawn = (int)((map.Size.x / 25) * map.Biome.plantDensity * ((map.TileInfo.VisibleRivers.NullOrEmpty()) ? 1f : 2f) * SrvSettings.GenStep_PlantDensity);

      // Validate water cells
      Predicate<IntVec3> validWater = ((IntVec3 c) => (
        c.GetEdifice(map) == null && !c.Roofed(map) && map.thingGrid.ThingsListAtFast(c).NullOrEmpty() &&
        (c.GetTerrain(map) == TerrainDefOf.WaterShallow || c.GetTerrain(map) == TerrainDefOf.WaterMovingShallow || c.GetTerrain(map) == SrvDefOf.Marsh)
      ));

      // Validate water and shore cells
      Predicate<IntVec3> waterAndShoreValidator = ((IntVec3 c) => (
        c.GetEdifice(map) == null && !c.Roofed(map) && map.thingGrid.ThingsListAtFast(c).NullOrEmpty() &&
        (c.GetTerrain(map) == TerrainDefOf.WaterShallow || c.GetTerrain(map) == TerrainDefOf.WaterMovingShallow ||
        c.GetTerrain(map).fertility > 0 || c.GetTerrain(map) == SrvDefOf.Mud || c.GetTerrain(map) == SrvDefOf.Marsh)
      ));

      // Validate shore cells
      Predicate<IntVec3> shoreValidator = ((IntVec3 c) => (
        c.GetEdifice(map) == null && !c.Roofed(map) && map.thingGrid.ThingsListAtFast(c).NullOrEmpty() &&
        (c.GetTerrain(map).fertility > 0 || c.GetTerrain(map) == SrvDefOf.Mud)
      ));

      // Spawn Hayreed
      for (int t = 0; t < timesToSpawn; t++) {
        IntVec3 intVec;
        CellFinderLoose.TryFindRandomNotEdgeCellWith(10, validWater, map, out intVec);
        if (intVec == null || intVec == IntVec3.Invalid) {
          continue;
        }
        ClusterAround(intVec, 5.8f, map, shoreValidator, SrvDefOf.SRV_Hayreed);
      }

      // Spawn Jute
      for (int t = 0; t < timesToSpawn; t++) {
        IntVec3 intVec;
        CellFinderLoose.TryFindRandomNotEdgeCellWith(10, validWater, map, out intVec);
        if (intVec == null || intVec == IntVec3.Invalid) {
          continue;
        }
        ClusterAround(intVec, 3.6f, map, waterAndShoreValidator, SrvDefOf.SRV_WildJute);
      }
    }


    private void ClusterAround(IntVec3 intVec, float radius, Map map, Predicate<IntVec3> validator, ThingDef thingDef) {
      foreach (IntVec3 current in GenRadial.RadialCellsAround(intVec, radius, true)) {
        if (Rand.Chance(map.Biome.plantDensity) && validator(current)) {
          Plant plant = (Plant)ThingMaker.MakeThing(thingDef);
          plant.Growth = Rand.Range(0.07f, 1f);
          GenSpawn.Spawn(plant, current, map);
        }
      }
    }
  }
}
