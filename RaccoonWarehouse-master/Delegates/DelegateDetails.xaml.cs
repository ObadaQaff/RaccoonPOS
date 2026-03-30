using RaccoonWarehouse.Application.Service.Delegates;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;

namespace RaccoonWarehouse.Delegates
{
    public partial class DelegateDetails : Window
    {
        private readonly IDelegateService _delegateService;

        public DelegateDetails(IDelegateService delegateService)
        {
            _delegateService = delegateService;
            InitializeComponent();
            UiText.ApplyWindow(this);
        }

        public async void Initialize(int delegateId)
        {
            var result = await _delegateService.GetByIdAsync(delegateId);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(result.Message ?? UiText.T("تعذر تحميل التفاصيل.", "Failed to load the details."));
                Close();
                return;
            }

            var dto = result.Data;
            NameText.Text = dto.FullName;
            MetaText.Text = UiText.IsEnglish
                ? $"Region: {(dto.AreaName ?? dto.RegionId?.ToString() ?? "—")} | Hire Date: {(dto.HireDate?.ToString("yyyy-MM-dd") ?? "—")}"
                : $"المنطقة: {(dto.AreaName ?? dto.RegionId?.ToString() ?? "—")} | تاريخ التعيين: {(dto.HireDate?.ToString("yyyy-MM-dd") ?? "—")}";
            CodeText.Text = dto.Code;
            StatusText.Text = dto.Status.ToString();
            TypeText.Text = dto.DelegateType.ToString();
            PhoneText.Text = dto.PhoneNumber ?? "—";
            UserText.Text = dto.UserName ?? UiText.T("بدون مستخدم", "No User");
            InvoiceCountText.Text = dto.InvoiceCount.ToString();
            SalesText.Text = dto.TotalSales.ToString("0.##");
            UpdatedText.Text = dto.UpdatedDate.ToString("yyyy-MM-dd hh:mm tt");
            NotesText.Text = string.IsNullOrWhiteSpace(dto.Notes) ? "—" : dto.Notes;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
