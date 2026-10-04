namespace RaccoonWarehouse.Navigation
{
    public interface IWindowNavigationService
    {
        bool CanShow(string windowKey);
        void Show(string windowKey, WindowSizeType size = WindowSizeType.MediumRectangle);
    }
}
