using RaccoonWarehouse.Core.Localization;

namespace RaccoonWarehouse.Helpers.Localization
{
    public sealed class UiTextLocalizer : IUiTextLocalizer
    {
        public string T(string arabic, string english)
        {
            return UiText.T(arabic, english);
        }

        public string Translate(string text)
        {
            return UiText.Translate(text);
        }
    }
}
