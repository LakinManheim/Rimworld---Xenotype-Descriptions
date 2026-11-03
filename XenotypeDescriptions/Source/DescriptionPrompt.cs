using System;

namespace LocalRimworld.XenotypeDescriptions
{
    internal sealed class DescriptionPrompt
    {
        private readonly Action<string> commit;
        private readonly Action cancel;
        private bool resolved;

        internal DescriptionPrompt(Action<string> commit, Action cancel)
        {
            this.commit = commit;
            this.cancel = cancel;
        }

        internal void Confirm(string text)
        {
            if (resolved) return;
            resolved = true;
            commit(DescriptionStore.Normalize(text));
        }

        internal void Cancel()
        {
            if (resolved) return;
            resolved = true;
            cancel();
        }
    }
}
