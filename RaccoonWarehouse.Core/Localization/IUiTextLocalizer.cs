namespace RaccoonWarehouse.Core.Localization
{
    public interface IUiTextLocalizer
    {
        string T(string arabic, string english);

        string Translate(string text);
    }
}
