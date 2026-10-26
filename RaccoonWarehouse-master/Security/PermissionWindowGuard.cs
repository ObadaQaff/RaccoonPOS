using Microsoft.Extensions.DependencyInjection;
using RaccoonWarehouse.Application.Service.Permissions;
using RaccoonWarehouse.Application.Service.Users;
using RaccoonWarehouse.Domain.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace RaccoonWarehouse.Security;

public static class PermissionWindowGuard
{
    private static readonly Lazy<HashSet<string>> EditableResources = new(() => PermissionCatalog.All
        .Where(item => PermissionPresentationPolicy.Classify(item.Key) == PermissionKind.CreateEdit)
        .Select(item => PermissionPresentationPolicy.Resource(item.Key)).ToHashSet(StringComparer.OrdinalIgnoreCase));
    public static readonly DependencyProperty KeysProperty = DependencyProperty.RegisterAttached(
        "Keys", typeof(string), typeof(PermissionWindowGuard), new PropertyMetadata(null));
    private static readonly DependencyProperty PolicyReadOnlyProperty = DependencyProperty.RegisterAttached(
        "PolicyReadOnly", typeof(bool), typeof(PermissionWindowGuard), new PropertyMetadata(false));
    private static readonly DependencyProperty PolicyDisabledProperty = DependencyProperty.RegisterAttached(
        "PolicyDisabled", typeof(bool), typeof(PermissionWindowGuard), new PropertyMetadata(false));
    public static string? GetKeys(DependencyObject element) => (string?)element.GetValue(KeysProperty);
    public static void SetKeys(DependencyObject element, string value) => element.SetValue(KeysProperty, value);

    public static string EntryRule(Type type)
    {
        var screen = PermissionCoverageCatalog.Screens.FirstOrDefault(s => s.TypeName == type.FullName);
        if (screen == null) return "Permissions.Unmapped";
        if (screen.Resource == null) return "internal";
        if (type.Name == "PayInvoice") return "PurchaseInvoice.View|PurchaseOrder.View";
        if (type.Name is "EmployeeFeatureSettingsWindow" or "DelegateFeatureSettingsWindow" or "AccountingFeatureSettingsWindow" or "LanguageSettingsWindow")
            return "Settings.ManageSettings;Settings." + (type.Name switch { "EmployeeFeatureSettingsWindow" => "Employees", "DelegateFeatureSettingsWindow" => "Delegates", "AccountingFeatureSettingsWindow" => "Accounting", _ => "Language" });
        if (type.Name == "InsuranceSetupWindow") return "InsuranceContracts.View|InsuranceNetworks.View|InsurancePolicies.View";
        if (type.Name == "CreateUser") return "Users.Create|Customers.Create|Suppliers.Create;Users.ManageUsers|Customers.Create|Suppliers.Create";
        if (type.Name == "UpdateUser") return "Users.Edit|Customers.Edit|Suppliers.Edit;Users.ManageUsers|Customers.Edit|Suppliers.Edit";
        if (screen.Resource == "Lookup") return "POS.View|SalesInvoice.View|PurchaseInvoice.View|PurchaseOrder.View|PurchaseRequest.View|StockInVoucher.View|StockOutVoucher.View|Products.View|GoodsReceipt.View|InsuranceContracts.View|InsuranceClaims.View";
        if (type.Name == "FractionalSaleConfigurationWindow") return "POS.Create|Products.Edit";
        if (type.Name == "ProductUnitSaleConfigurationWindow") return "POS.ChangePrice|Products.ChangePrice";
        return screen.Resource + ".View" + (screen.EntryAction == "View" ? "" : ";" + screen.Resource + "." + screen.EntryAction);
    }

    public static bool? ShowDialog(Window window, string rule)
    {
        DemandRule(rule); SetKeys(window, rule); return window.ShowDialog();
    }
    public static void Show(Window window, string rule)
    {
        DemandRule(rule); SetKeys(window, rule); window.Show();
    }

    public static void DemandRule(string rule)
    {
        if (!Allowed(rule)) throw new UnauthorizedAccessException("ليس لديك الصلاحية المطلوبة. / Permission denied.");
    }

    public static void Demand(Window window) => DemandRule(GetKeys(window) ?? EntryRule(window.GetType()));

    public static void Demand(Type type)
    {
        var rule = EntryRule(type);
        if (rule != "internal" && !Allowed(rule))
            throw new UnauthorizedAccessException("ليس لديك صلاحية لفتح هذه الشاشة. / Permission denied for this screen.");
    }

    private static bool Allowed(string rule)
    {
        if (!PermissionCatalog.EnforcementEnabled)
            return true;

        if (rule == "internal") return true;
        if (System.Windows.Application.Current is not App app || app.ServiceProvider == null) return false;
        // A fresh scope avoids racing the window's scoped DbContext. Do not block
        // an async query on WPF's synchronization context.
        return Task.Run(async () =>
        {
            using var scope = app.ServiceProvider.CreateScope();
            var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
            foreach (var required in rule.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var allowed = false;
                foreach (var alternative in required.Split('|', StringSplitOptions.RemoveEmptyEntries))
                    if (await permissions.HasCurrentUserPermissionAsync(alternative).ConfigureAwait(false)) { allowed = true; break; }
                if (!allowed) return false;
            }
            return rule.Length > 0;
        }).GetAwaiter().GetResult();
    }

    public static string? PayrollActionRule(string expression, string file)
    {
        var explicitKey = System.Text.RegularExpressions.Regex.Match(expression, @"Require\(""([A-Za-z.]+)""");
        if (explicitKey.Success) return explicitKey.Groups[1].Value;
        if (expression.Contains("Approve")) return "Payroll.Approve";
        if (expression.Contains("Correct")) return "Payroll.Correct";
        if (expression.Contains("Adjust")) return "Payroll.Adjust";
        if (expression.Contains("Preview") || expression.Contains("SaveDraft") || expression.Contains("Calculate")) return "Payroll.Calculate";
        if (expression.Contains("SaveAsync")) return file.Contains("EmployeeEditor") ? "Employees.Create|Employees.Edit" : "Payroll.Calculate";
        if (expression.Contains("AddPolicy") || expression.Contains("ReviewPolicy") || expression.Contains("AddSalary")) return "Payroll.Configure";
        if (expression.Contains("RevealSocialSecurityNumber")) return "Employees.SensitiveView";
        return null;
    }

    public static void Register()
    {
        EventManager.RegisterClassHandler(typeof(ButtonBase), ButtonBase.ClickEvent, new RoutedEventHandler(CheckClick), true);
        EventManager.RegisterClassHandler(typeof(MenuItem), MenuItem.ClickEvent, new RoutedEventHandler(CheckClick), true);
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent, new RoutedEventHandler(Loaded), true);
        UserSession.PermissionsChanged += () =>
        {
            var app = System.Windows.Application.Current;
            if (app == null || app.Dispatcher.HasShutdownStarted) return;
            app.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (Window window in app.Windows.Cast<Window>().ToArray())
                {
                    if (!Allowed(GetKeys(window) ?? EntryRule(window.GetType()))) { window.Close(); continue; }
                    if (window is RaccoonWarehouse.Settings.ReportPermissionsManager manager) manager.SetEditingAllowed(Allowed("Permissions.ManageRoles"));
                    foreach (var control in Descendants(window).OfType<FrameworkElement>()) Apply(control);
                }
            }));
        };
    }

    private static void CheckClick(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject element && GetKeys(element) is { Length: > 0 } rule && !Allowed(rule))
            e.Handled = true;
    }
    private static void Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && ReferenceEquals(sender, e.OriginalSource))
        {
            if (element is Window window && !Allowed(GetKeys(window) ?? EntryRule(window.GetType())))
            { window.Close(); e.Handled = true; return; }
            Apply(element);
        }
    }
    private static void Apply(FrameworkElement element)
    {
        if (GetKeys(element) is not { Length: > 0 } rule)
        {
            var owner = Window.GetWindow(element);
            var resource = owner == null ? null : PermissionCoverageCatalog.Screens.FirstOrDefault(x => x.TypeName == owner.GetType().FullName)?.Resource;
            if (resource != null && resource != "Lookup" && EditableResources.Value.Contains(resource))
            {
                var allowed = Allowed(resource + ".Create|" + resource + ".Edit");
                if (element is DataGrid grid)
                {
                    if (!allowed && !grid.IsReadOnly) { grid.SetValue(PolicyReadOnlyProperty, true); grid.SetCurrentValue(DataGrid.IsReadOnlyProperty, true); }
                    else if (allowed && (bool)grid.GetValue(PolicyReadOnlyProperty)) { grid.SetValue(PolicyReadOnlyProperty, false); grid.SetCurrentValue(DataGrid.IsReadOnlyProperty, false); }
                }
                else if (element is TextBox box && !IsSearchInput(box))
                {
                    if (!allowed && !box.IsReadOnly) { box.SetValue(PolicyReadOnlyProperty, true); box.SetCurrentValue(TextBox.IsReadOnlyProperty, true); }
                    else if (allowed && (bool)box.GetValue(PolicyReadOnlyProperty)) { box.SetValue(PolicyReadOnlyProperty, false); box.SetCurrentValue(TextBox.IsReadOnlyProperty, false); }
                }
            }
            return;
        }
        if (!Allowed(rule))
        {
            if (element.IsEnabled) element.SetValue(PolicyDisabledProperty, true);
            element.SetCurrentValue(UIElement.IsEnabledProperty, false);
        }
        else if ((bool)element.GetValue(PolicyDisabledProperty))
        {
            element.SetValue(PolicyDisabledProperty, false);
            element.SetCurrentValue(UIElement.IsEnabledProperty, true);
        }
    }
    private static bool IsSearchInput(TextBox box)
    {
        var name = box.Name;
        for (DependencyObject? parent = VisualTreeHelper.GetParent(box); parent != null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is ComboBox combo) { name += combo.Name; break; }
            if (parent is Window) break;
        }
        return name.Contains("Search", StringComparison.OrdinalIgnoreCase) || name.Contains("Filter", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Barcode", StringComparison.OrdinalIgnoreCase) || name == "PlanContractIdTextBox";
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
