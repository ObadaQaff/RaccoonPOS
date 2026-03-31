using RaccoonWarehouse.Categories;
using RaccoonWarehouse.SubCategories;

namespace RaccoonWarehouse.Navigation.Modules
{
    public sealed class CategoriesDashboardActionHandler : IDashboardActionHandler
    {
        public bool CanHandle(string actionKey)
        {
            return actionKey is
                "Categories.Create" or
                "Categories.List" or
                "SubCategories.Create" or
                "SubCategories.List";
        }

        public Task ExecuteAsync(string actionKey, DashboardActionContext context)
        {
            switch (actionKey)
            {
                case "Categories.Create":
                    WindowManager.Show<CreateCategory>();
                    break;
                case "Categories.List":
                    WindowManager.Show<CategoriesTable>();
                    break;
                case "SubCategories.Create":
                    WindowManager.Show<CreateSubCategory>();
                    break;
                case "SubCategories.List":
                    WindowManager.Show<SubCategoryTable>();
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
