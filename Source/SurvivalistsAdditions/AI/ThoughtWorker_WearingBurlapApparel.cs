using System.Collections.Generic;

using RimWorld;
using Verse;

namespace SurvivalistsAdditions {

  public class ThoughtWorker_WearingBurlapApparel : ThoughtWorker {

    protected override ThoughtState CurrentStateInternal(Pawn p) {
      string text = null;
      int num = 0;
      List<Apparel> wornApparel = p.apparel.WornApparel;
      for (int i = 0; i < wornApparel.Count; i++) {
        if (wornApparel[i].Stuff == SrvDefOf.SRV_Burlap && wornApparel[i].def.apparel.layers.Contains(ApparelLayer.OnSkin)) {
          if (text == null) {
            text = wornApparel[i].def.label;
          }
          num++;
          break;
        }
      }
      if (num == 0) {
        return ThoughtState.Inactive;
      }
      return ThoughtState.ActiveAtStage(1, text);
    }
  }
}
