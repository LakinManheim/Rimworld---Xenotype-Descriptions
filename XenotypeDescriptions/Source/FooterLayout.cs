using UnityEngine;

namespace LocalRimworld.XenotypeDescriptions
{
    internal static class FooterLayout
    {
        internal static Rect DescriptionButton(Rect footer, Vector2 buttonSize)
        {
            return new Rect(footer.xMax - buttonSize.x * 2f - 10f, footer.y, buttonSize.x, buttonSize.y);
        }

        internal static Rect Duration(Rect footer, Vector2 buttonSize)
        {
            float left = footer.x + buttonSize.x + 10f;
            float right = DescriptionButton(footer, buttonSize).x - 10f;
            return new Rect(left, footer.y, Mathf.Max(0f, right - left), buttonSize.y);
        }
    }
}
