using Mlie;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ad2mod;

internal class Ad2Mod : Mod
{
    public static Ad2Settings Settings;
    private static string currentVersion;

    private readonly NumField defaultMaxTimeField = new(0, int.MaxValue);
    //private readonly NumField defaultThresholdField = new NumField();

    public Ad2Mod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<Ad2Settings>();
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    public override string SettingsCategory()
    {
        return "Bulk recipe generator";
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var x = inRect.x;
        var y = inRect.y;
        var lh = Text.LineHeight;
        y += lh;
        var rect = new Rect(x, y, 360, lh);
        Widgets.CheckboxLabeled(rect, "BRG.logging".Translate(), ref Settings.VerboseLogging);
        TooltipHandler.TipRegion(rect, "BRG.logginginfo".Translate());
        y += lh;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "BRG.global".Translate());
        y += Text.LineHeight + 2;
        Text.Font = GameFont.Small;

        y += lh;
        if (defaultMaxTimeField.DoField(y, "BRG.maxtimecutoff".Translate(), ref Settings.MaxTimeCutoff))
        {
            Messages.Message("BRG.maxtimecutoffchange".Translate(Settings.MaxTimeCutoff),
                MessageTypeDefOf.NeutralEvent);
        }

        y += lh;
        rect = new Rect(x, y, 360, lh);
        Widgets.CheckboxLabeled(rect, "BRG.limitfive".Translate(), ref Settings.LimitToX5);
        TooltipHandler.TipRegion(rect, "BRG.limitfiveinfo".Translate());
        y += lh;
        rect = new Rect(x, y, 360, lh);
        Widgets.CheckboxLabeled(rect, "BRG.contextmenu".Translate(), ref Settings.UseRightClickMenu);
        TooltipHandler.TipRegion(rect,
            "BRG.contextmenuinfo".Translate());
        y += lh;
        rect = new Rect(x, y, 360, lh);
        Widgets.CheckboxLabeled(rect, "BRG.qualityitems".Translate(), ref Settings.MakeBulkForQuality);
        TooltipHandler.TipRegion(rect,
            "BRG.qualityitemsinfo".Translate());
        if (Settings.MakeBulkForQuality)
        {
            y += lh;
            rect = new Rect(x, y, 360, lh);
            Widgets.CheckboxLabeled(rect, "BRG.sameforall".Translate(), ref Settings.UseSameQualityForAll);
            TooltipHandler.TipRegion(rect,
                "BRG.sameforallinfo".Translate());
        }

        y += (2 * lh) + 2;

        if (Current.Game != null)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "BRG.world".Translate());
            y += Text.LineHeight + 2;
            Text.Font = GameFont.Small;

            var bills = Ad2.FindRecipesUses();
            var s = "BRG.removerecipes".Translate(bills.Count);
            var w = Text.CalcSize(s).x + 64;
            if (Widgets.ButtonText(new Rect(x, y, w, 32), s))
            {
                foreach (var bill in bills)
                {
                    bill.billStack.Delete(bill);
                }

                Messages.Message("BRG.billsremoved".Translate(bills.Count), MessageTypeDefOf.NeutralEvent);
            }
        }

        if (currentVersion == null)
        {
            return;
        }

        y += Text.LineHeight + 20;
        GUI.contentColor = Color.gray;
        Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "BRG.modversion".Translate(currentVersion));
        GUI.contentColor = Color.white;
    }

    private class NumField(int min = 0, int max = 120)
    {
        private string buffer;

        public bool DoField(float y, string label, ref int val)
        {
            var lh = Text.LineHeight;
            buffer ??= val.ToString();

            float x = 0;
            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, 200, lh), label);
            Text.Anchor = anchor;
            x += 200;
            buffer = Widgets.TextField(new Rect(x, y, 60, lh), buffer);
            x += 60;
            if (!Widgets.ButtonText(new Rect(x, y, 100, lh), "BRG.apply".Translate()))
            {
                return false;
            }

            if (int.TryParse(buffer, out var resInt))
            {
                val = Util.Clamp(resInt, min, max);
                buffer = val.ToString();
                return true;
            }

            buffer = val.ToString();

            return false;
        }
    }
}