using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace RoadsOfTheRim
{
    public class WITab_Caravan_Build : WITab
    {
        private float scrollViewHeight;

        private Vector2 scrollPosition;

        private List<Pawn> Pawns
        {
            get
            {
                return base.SelCaravan.PawnsListForReading;
            }
        }
        public WITab_Caravan_Build()
        {
            this.labelKey = "RoadsOfTheRim_WITab_Caravan_Build";
        }

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);
            Rect rect2 = new Rect(0f, 0f, rect.width - 16f, this.scrollViewHeight);
            float num = 0f;
            Widgets.BeginScrollView(rect, ref this.scrollPosition, rect2, true);
            this.DoColumnHeaders(ref num);
            this.DoRows(ref num, rect2, rect);
            if (Event.current.type == EventType.Layout)
            {
                this.scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            this.size = this.GetRawSize();
        }

        private void DoColumnHeaders(ref float curY)
        {
                Text.Anchor = TextAnchor.UpperCenter;
                GUI.color = Widgets.SeparatorLabelColor;
                Widgets.Label(new Rect(135f, 3f, 100f, 100f), "Work"); // TO TRANSLATE
                Widgets.Label(new Rect(255f, 3f, 100f, 100f), "Skill"); // TO TRANSLATE
                Widgets.Label(new Rect(375f, 3f, 100f, 100f), "Best road"); // TO TRANSLATE
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
        }

        private void DoRows(ref float curY, Rect scrollViewRect, Rect scrollOutRect)
        {
            List<Pawn> pawns = this.Pawns;
            bool flag = false;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.IsColonist)
                {
                    if (!flag)
                    {
                        Widgets.ListSeparator(ref curY, scrollViewRect.width, "CaravanColonists".Translate());
                        flag = true;
                    }
                    this.DoRow(ref curY, scrollViewRect, scrollOutRect, pawn);
                }
            }
            bool flag2 = false;
            for (int j = 0; j < pawns.Count; j++)
            {
                Pawn pawn2 = pawns[j];
                if (!pawn2.IsColonist)
                {
                    if (!flag2)
                    {
                        Widgets.ListSeparator(ref curY, scrollViewRect.width, "CaravanPrisonersAndAnimals".Translate());
                        flag2 = true;
                    }
                    this.DoRow(ref curY, scrollViewRect, scrollOutRect, pawn2);
                }
            }
        }

        private void DoRow(ref float curY, Rect viewRect, Rect scrollOutRect, Pawn p)
        {
            float num = this.scrollPosition.y - 50f;
            float num2 = this.scrollPosition.y + scrollOutRect.height;
            if (curY > num && curY < num2)
            {
                this.DoRow(new Rect(0f, curY, viewRect.width, 50f), p);
            }
            curY += 50f;
        }


        private void DoRow(Rect rect, Pawn p)
        {
            GUI.BeginGroup(rect);
            Rect rect2 = rect.AtZero();
            CaravanThingsTabUtility.DoAbandonButton(rect2, p, base.SelCaravan);
            rect2.width -= 24f;
            Widgets.InfoCardButton(rect2.width - 24f, (rect.height - 24f) / 2f, p);
            rect2.width -= 24f;
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }
            Rect rect3 = new Rect(4f, (rect.height - 27f) / 2f, 27f, 27f);
            Widgets.ThingIcon(rect3, p, 1f);
            Rect bgRect = new Rect(rect3.xMax + 4f, 16f, 100f, 18f);
            GenMapUI.DrawPawnLabel(p, bgRect, 1f, 100f, null, GameFont.Small, false, false);
            float num = bgRect.xMax;
            for (int i = 0; i < 3; i++)
            {
                Rect rect5 = new Rect(num, 0f, 100f, 50f);
                if (Mouse.IsOver(rect5))
                {
                    Widgets.DrawHighlight(rect5);
                }
                Text.Anchor = TextAnchor.MiddleCenter;
                string s = "";
                switch (i)
                {
                    case 0:
                        s = PawnBuildingUtility.ShowConstructionValue(p) ;
                        break;
                    case 1:
                        s = PawnBuildingUtility.ShowSkill(p);
                        break;
                    case 2:
                        s = PawnBuildingUtility.ShowBestRoad(p);
                        break;
                    default:
                        s = "-";
                        break;
                }
                Widgets.Label(rect5, s);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                TooltipHandler.TipRegion(rect5, s);
                num += 125f;
            }

            if (p.Downed)
            {
                GUI.color = new Color(1f, 0f, 0f, 0.5f);
                Widgets.DrawLineHorizontal(0f, rect.height / 2f, rect.width);
                GUI.color = Color.white;
            }
            GUI.EndGroup();
        }


        private Vector2 GetRawSize()
        {
            float num = 500f;
            Vector2 result;
            result.x = 127f + num + 16f;
            result.y = Mathf.Min(550f, this.PaneTopY - 30f);
            return result;
        }
    }
}
