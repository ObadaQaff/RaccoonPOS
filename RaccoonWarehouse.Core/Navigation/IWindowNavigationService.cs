namespace RaccoonWarehouse.Navigation
{
    public interface IWindowNavigationService
    {
        void Show(string windowKey, WindowSizeType size = WindowSizeType.MediumRectangle);
    }
}
