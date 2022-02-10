using Mlie;
using RimWorld;
using UnityEngine;
using Verse;

namespace Ad2mod;

internal class Ad2Mod : Mod
{
    public static Ad2Settings settings;
    private static string currentVersion;
    private readonly NumField defaultThresholdField = new NumField();

    public Ad2Mod(ModContentPack content) : base(content)
    {
        settings = GetSettings<Ad2Settings>();
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(
                ModLister.GetActiveModWithIdentifier("Mlie.BulkRecipeGenerator"));
    }

    public override string SettingsCategory()
    {
        return "Bulk recipe generator";
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var x = inRect.x;
        var y = inRect.y;
        var LH = Text.LineHeight;
        y += LH;
        var rect = new Rect(x, y, 360, LH);
        Widgets.CheckboxLabeled(rect, "BRG.logging".Translate(), ref settings.verboseLogging);
        TooltipHandler.TipRegion(rect, "BRG.logginginfo".Translate());
        y += LH;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(x, y, 200, Text.LineHeight), "BRG.global".Translate());
        y += Text.LineHeight + 2;
        Text.Font = GameFont.Small;
        if (defaultThresholdField.DoField(y, "BRG.defaulttime".Translate(), ref settings.defaultThreshold))
        {
            Messages.Message("BRG.defaulttimechange".Translate(settings.defaultThreshold),
                MessageTypeDefOf.NeutralEvent);
        }

        TooltipHandler.TipRegion(new Rect(x, y, 360, LH),
            "BRG.noeffect".Translate());

        y += LH;
        rect = new Rect(x, y, 360, LH);
        Widgets.CheckboxLabeled(rect, "BRG.limitfive".Translate(), ref settings.limitToX5);
        TooltipHandler.TipRegion(rect, "BRG.limitfiveinfo".Translate());
        y += LH;
        rect = new Rect(x, y, 360, LH);
        Widgets.CheckboxLabeled(rect, "BRG.contextmenu".Translate(), ref settings.useRightClickMenu);
        TooltipHandler.TipRegion(rect,
            "BRG.contextmenuinfo".Translate());
        y += LH;
        rect = new Rect(x, y, 360, LH);
        Widgets.CheckboxLabeled(rect, "BRG.qualityitems".Translate(), ref settings.makeBulkForQuality);
        TooltipHandler.TipRegion(rect,
            "BRG.qualityitemsinfo".Translate());
        if (settings.makeBulkForQuality)
        {
            y += LH;
            rect = new Rect(x, y, 360, LH);
            Widgets.CheckboxLabeled(rect, "BRG.sameforall".Translate(), ref settings.useSameQualityForAll);
            TooltipHandler.TipRegion(rect,
                "BRG.sameforallinfo".Translate());
        }

        y += (2 * LH) + 2;

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
    //Game lastGame;

    private class NumField
    {
        private readonly int min, max;
        private string buffer;

        public NumField(int min = 0, int max = 120)
        {
            this.min = min;
            this.max = max;
        }

        public bool DoField(float y, string label, ref int val)
        {
            var LH = Text.LineHeight;
            if (buffer == null)
            {
                buffer = val.ToString();
            }

            float x = 0;
            var anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y, 200, LH), label);
            Text.Anchor = anchor;
            x += 200;
            buffer = Widgets.TextField(new Rect(x, y, 60, LH), buffer);
            x += 60;
            if (!Widgets.ButtonText(new Rect(x, y, 100, LH), "BRG.apply".Translate()))
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