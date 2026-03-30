using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RaccoonWarehouse.Helpers.Localization;

namespace RaccoonWarehouse.Common.Loading
{
    /// <summary>
    /// Interaction logic for LoadingWindow_v2.xaml
    /// </summary>
    public partial class LoadingWindow_v2 : Window
    {
        public LoadingWindow_v2()
        {
            InitializeComponent();
            MessageText.Text = UiText.T("جاري تجهيز مساحة العمل...", "Preparing your workspace...");
            HintText.Text = UiText.T("يتم تحميل لوحة التحكم والعمليات", "Loading dashboard and actions");
            UiText.ApplyWindow(this);
        }
    }
}
