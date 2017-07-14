using UnityEngine;
using Verse;

namespace SurvivalistsAdditions {

  public sealed class SrvMod : Mod {


    public SrvMod(ModContentPack content) : base(content) {
      GetSettings<SrvSettings>();
    }


    public override string SettingsCategory() {
      return Static.ModName;
    }


    public override void DoSettingsWindowContents(Rect rect) {

      Listing_Standard list = new Listing_Standard();

      list.ColumnWidth = rect.width;
      list.Begin(rect);
      list.Gap();
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_VinegarBarrel_MaxCapacity".Translate(SrvSettings.VinegarBarrel_MaxCapacity));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.VinegarBarrel_MaxCapacity, 1, 75, 5);
      }
      list.Gap();
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_VinegarBarrel_FermentDays".Translate(SrvSettings.VinegarBarrel_FermentDays));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.VinegarBarrel_FermentDays, 1, 30);
      }
      list.Gap(25);
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_CheeseBarrel_MaxCapacity".Translate(SrvSettings.CheeseBarrel_MaxCapacity));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.CheeseBarrel_MaxCapacity, 1, 75, 5);
      }
      list.Gap();
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_CheeseBarrel_AgingDays".Translate(SrvSettings.CheeseBarrel_AgingDays));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.CheeseBarrel_AgingDays, 1, 30);
      }
      list.Gap(25);
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_Smoker_MaxCapacity".Translate(SrvSettings.Smoker_MaxCapacity));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.Smoker_MaxCapacity, 10, 75, 5);
      }
      list.Gap();
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_Smoker_SmokeHours".Translate(SrvSettings.Smoker_SmokeHours));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.Smoker_SmokeHours, 4, 72, 4);
      }
      list.Gap();
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_Smoker_TendHours".Translate(SrvSettings.Smoker_TendHours));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.Smoker_TendHours, 1, 4);
      }
      list.Gap(25);
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_CharcoalPit_MaxCapacity".Translate(SrvSettings.CharcoalPit_MaxCapacity));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.CharcoalPit_MaxCapacity, 5, 75, 5);
      }
      list.Gap();
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_CharcoalPit_BurnHours".Translate(SrvSettings.CharcoalPit_BurnHours));
        SettingsWidgets.IntSliderWithButtons(rightRect, ref SrvSettings.CharcoalPit_BurnHours, 1, 48, 4);
      }
      list.Gap();
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_CharcoalPit_CharcoalPerWoodLog".Translate(SrvSettings.CharcoalPit_CharcoalPerWoodLog));
        SettingsWidgets.FloatSliderWithButtons(rightRect, ref SrvSettings.CharcoalPit_CharcoalPerWoodLog, 0.5f, 5f, 0.5f, floatRound: 0.5f);
      }
      list.Gap(25);
      {
        Rect fullRect = list.GetRect(Text.LineHeight);
        Rect leftRect = fullRect.LeftHalf().RightPartPixels(325).Rounded();
        Rect rightRect = fullRect.RightHalf().Rounded();

        Widgets.Label(leftRect, "SRV_Label_GenStep_PlantDensity".Translate(SrvSettings.GenStep_PlantDensity));
        SettingsWidgets.FloatSliderWithButtons(rightRect, ref SrvSettings.GenStep_PlantDensity, 0f, 5f, 0.5f, floatRound: 0.5f);
      }
      list.End();
    }
  }
}
