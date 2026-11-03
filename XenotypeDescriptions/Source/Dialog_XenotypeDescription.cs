using UnityEngine;
using Verse;

namespace LocalRimworld.XenotypeDescriptions
{
    internal sealed class Dialog_XenotypeDescription : Window
    {
        private readonly DescriptionPrompt prompt;
        private readonly string xenotypeName;
        private readonly string placeholder;
        private string description;
        private Vector2 scroll;

        public override Vector2 InitialSize { get { return new Vector2(640f, 480f); } }

        internal Dialog_XenotypeDescription(string xenotypeName, DescriptionPrompt prompt, string initialDescription = null, string placeholder = null)
        {
            this.xenotypeName = xenotypeName;
            this.prompt = prompt;
            this.placeholder = placeholder ?? "UniqueXenotypeDesc".Translate().ToString();
            description = initialDescription ?? "";
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false; // Enter inserts a newline in the vanilla text area.
            closeOnCancel = true;
            doCloseX = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            GameFont oldFont = Text.Font;
            TextAnchor oldAnchor = Text.Anchor;
            Color oldColor = GUI.color;
            try
            {
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0f, 0f, inRect.width - 28f, 38f), "XGD_Title".Translate());
                Text.Font = GameFont.Small;
                string hint = "XGD_Hint".Translate(xenotypeName);
                float hintHeight = Text.CalcHeight(hint, inRect.width);
                Widgets.Label(new Rect(0f, 46f, inRect.width, hintHeight), hint);
                float top = 46f + hintHeight + 16f;
                Rect box = new Rect(0f, top, inRect.width, inRect.height - top - 54f);
                string visibleText = description.Length == 0 ? placeholder : description;
                Rect view = new Rect(0f, 0f, box.width - 20f,
                    Mathf.Max(box.height, Text.CalcHeight(visibleText + "\n", box.width - 32f) + 24f));
                Widgets.BeginScrollView(box, ref scroll, view);
                description = Widgets.TextArea(view, description);
                if (description.Length == 0)
                {
                    GUI.color = ColoredText.SubtleGrayColor;
                    Widgets.Label(new Rect(view.x + 4f, view.y + 3f, view.width - 8f, view.height - 6f), placeholder);
                    GUI.color = oldColor;
                }
                Widgets.EndScrollView();

                float width = (inRect.width - 24f) / 3f;
                float y = inRect.height - 38f;
                if (Widgets.ButtonText(new Rect(0f, y, width, 38f), "XGD_Back".Translate())) Close();
                if (Widgets.ButtonText(new Rect(width + 12f, y, width, 38f), "XGD_Clear".Translate())) description = "";
                if (Widgets.ButtonText(new Rect((width + 12f) * 2f, y, width, 38f), "XGD_Confirm".Translate()))
                {
                    // Resolve before closing: PostClose also handles Esc / X / Back.
                    prompt.Confirm(description);
                    Close();
                }
            }
            finally { Text.Font = oldFont; Text.Anchor = oldAnchor; GUI.color = oldColor; }
        }

        public override void PostClose()
        {
            base.PostClose();
            prompt.Cancel();
        }
    }
}
