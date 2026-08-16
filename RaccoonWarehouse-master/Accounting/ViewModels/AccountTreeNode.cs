using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace RaccoonWarehouse.Accounting.ViewModels
{
    public partial class AccountTreeNode : ObservableObject
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? AccountCode { get; set; }
        public int? AccountLevel { get; set; }
        public string? AccountNature { get; set; }
        public string? AccountCategory { get; set; }
        public string? AccountTypeCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPosting { get; set; }
        public bool IsActive { get; set; }
        public int? ParentAccountId { get; set; }

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool isNew;

        [ObservableProperty]
        private string editName = string.Empty;

        public ObservableCollection<AccountTreeNode> Children { get; } = new();

        public string DisplayCode => string.IsNullOrWhiteSpace(AccountCode) ? Code : AccountCode;
        public string DisplayText => $"[{DisplayCode}] {Name}";
        public string PostingIcon => IsPosting ? "●" : "▶";

        public Brush CategoryBrush => (AccountCategory ?? string.Empty).ToLowerInvariant() switch
        {
            "asset" => Brushes.DodgerBlue,
            "liability" => Brushes.DarkOrange,
            "equity" => Brushes.MediumPurple,
            "revenue" => Brushes.ForestGreen,
            "expense" => Brushes.IndianRed,
            _ => Brushes.Black
        };

        public Brush StatusBrush
        {
            get
            {
                if (!IsActive)
                    return Brushes.Gray;
                if (IsNew)
                    return Brushes.LimeGreen;
                return Brushes.Black;
            }
        }

        public TextDecorationCollection? TextDecoration =>
            !IsActive ? TextDecorations.Strikethrough : null;

        public void TouchVisuals()
        {
            OnPropertyChanged(nameof(DisplayCode));
            OnPropertyChanged(nameof(DisplayText));
            OnPropertyChanged(nameof(PostingIcon));
            OnPropertyChanged(nameof(CategoryBrush));
            OnPropertyChanged(nameof(StatusBrush));
            OnPropertyChanged(nameof(TextDecoration));
        }
    }
}
