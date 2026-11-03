using System.Runtime.CompilerServices;

namespace LocalRimworld.XenotypeDescriptions
{
    internal static class DescriptionStore
    {
        private sealed class Entry { internal string Text; }
        private static readonly ConditionalWeakTable<object, Entry> Entries = new ConditionalWeakTable<object, Entry>();

        internal static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        }

        internal static string Get(object owner)
        {
            Entry entry;
            return owner != null && Entries.TryGetValue(owner, out entry) ? entry.Text : null;
        }

        internal static void Set(object owner, string text)
        {
            if (owner == null) return;
            text = Normalize(text);
            if (text == null) Entries.Remove(owner);
            else Entries.GetOrCreateValue(owner).Text = text;
        }

        internal static void Copy(object source, object target) { Set(target, Get(source)); }
    }
}
