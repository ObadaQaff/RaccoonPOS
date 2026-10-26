using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;
using RaccoonWarehouse.Application.Service.Settings;
using RaccoonWarehouse.Data;
using RaccoonWarehouse.Domain.Settings;
using RaccoonWarehouse.Helpers.Localization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace RaccoonWarehouse.Settings;

public sealed class DocumentBrandingWindow : Window
{
    private readonly DocumentBrandingService _service;
    private readonly ApplicationDbContext _db;
    private readonly ComboBox _branch=new(){DisplayMemberPath="Name",SelectedValuePath="Id",MinHeight=32};
    private readonly ComboBox _width=new(){ItemsSource=new[]{58,80},SelectedItem=80,MinHeight=32};
    private readonly CheckBox _cut=new();
    private readonly Dictionary<string,TextBox> _fields=new();
    private readonly Image _logo=new(){Height=70,Stretch=Stretch.Uniform};
    private DocumentBranding _value=new();
    private int? _loadedBranch;
    private bool _loading;
    private bool _loaded;
    public DocumentBrandingWindow(DocumentBrandingService service,ApplicationDbContext db)
    {
        RaccoonWarehouse.Security.PermissionWindowGuard.Demand(GetType());
        (_service,_db)=(service,db);Title=UiText.T("إعدادات الطباعة وهوية الصيدلية","Printing and business identity");
        Width=760;Height=800;WindowStartupLocation=WindowStartupLocation.CenterScreen;FlowDirection=UiText.CurrentFlowDirection;Background=new SolidColorBrush(Color.FromRgb(238,244,248));
        var panel=new StackPanel{Margin=new Thickness(24)};
        panel.Children.Add(new TextBlock{Text=Title,FontSize=24,FontWeight=FontWeights.Bold,Margin=new Thickness(0,0,0,16)});
        Field(panel,UiText.T("النطاق / الفرع","Scope / Branch"),_branch);
        panel.Children.Add(new TextBlock{Text=UiText.T("إعداد الفرع مستقل بالكامل. احفظ قبل تغيير النطاق. تستخدم المستندات القديمة بلا فرع الإعداد العام.","Branch settings are independent. Save before switching scope. Documents without a branch use the general settings."),TextWrapping=TextWrapping.Wrap});
        Add(panel,"Name","اسم الصيدلية / المنشأة","Business display name");Add(panel,"Address","العنوان","Address");Add(panel,"Phone","الهاتف","Phone");Add(panel,"Tax","الرقم الضريبي","Tax number");Add(panel,"Info","معلومات إضافية","Additional information");Add(panel,"Footer","نص التذييل","Footer text");
        panel.Children.Add(_logo);var upload=new Button{Content=UiText.T("اختيار شعار PNG / JPEG","Choose PNG / JPEG logo"),Margin=new Thickness(0,6,0,6)};upload.Click+=ChooseLogo;panel.Children.Add(upload);
        var remove=new Button{Content=UiText.T("إزالة الشعار","Remove logo")};remove.Click+=(_,_)=>{_value.LogoBase64=null;_logo.Source=null;};panel.Children.Add(remove);
        Field(panel,UiText.T("عرض ورق الإيصال (مم)","Receipt paper width (mm)"),_width);
        _cut.Content=UiText.T("إرسال أمر قص ESC/POS بعد الطباعة (للطابعة المتوافقة فقط)","Send ESC/POS cut after printing (compatible printers only)");panel.Children.Add(_cut);
        var save=new Button{Content=UiText.T("حفظ","Save"),Padding=new Thickness(14,8,14,8),Margin=new Thickness(0,14,0,0)};
        save.Click+=async(_,_)=>{if(_loading)return;save.IsEnabled=false;try{
            if(!_loaded || (int?)_branch.SelectedValue!=_loadedBranch)throw new InvalidOperationException(UiText.T("انتظر تحميل الفرع.","Wait for branch settings to load."));
            _value.DisplayName=_fields["Name"].Text.Trim();_value.Address=_fields["Address"].Text.Trim();_value.Phone=_fields["Phone"].Text.Trim();_value.TaxNumber=_fields["Tax"].Text.Trim();_value.AdditionalInformation=_fields["Info"].Text.Trim();_value.Footer=_fields["Footer"].Text.Trim();_value.ThermalPaperWidthMm=(int)_width.SelectedItem;_value.EscPosCutEnabled=_cut.IsChecked==true;
            await _service.SaveAsync(_value,_loadedBranch);MessageBox.Show(UiText.T("تم الحفظ. ستظهر الهوية في المستندات الجديدة.","Saved. Newly generated documents use the updated identity."));
        }catch(Exception ex){MessageBox.Show(ex.Message);}finally{save.IsEnabled=true;}};panel.Children.Add(save);
        Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        Loaded+=async(_,_)=>{try{var rows=await _db.Branches.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new Scope(x.Id,x.Name)).ToListAsync();rows.Insert(0,new Scope(null,UiText.T("عام — مستندات بلا فرع","General — documents without branch")));_branch.ItemsSource=rows;_branch.SelectedIndex=0;}catch(Exception ex){MessageBox.Show(ex.Message);}};
        _branch.SelectionChanged+=async(_,_)=>{_loading=true;_loaded=false;IsEnabled=false;try{_loadedBranch=(int?)_branch.SelectedValue;_value=await _service.GetAsync(_loadedBranch);_fields["Name"].Text=_value.DisplayName;_fields["Address"].Text=_value.Address;_fields["Phone"].Text=_value.Phone;_fields["Tax"].Text=_value.TaxNumber;_fields["Info"].Text=_value.AdditionalInformation;_fields["Footer"].Text=_value.Footer;_width.SelectedItem=_value.ThermalPaperWidthMm;_cut.IsChecked=_value.EscPosCutEnabled;ShowLogo();_loaded=true;}catch(Exception ex){MessageBox.Show(ex.Message);}finally{_loading=false;IsEnabled=true;}};
    }
    private void Add(Panel panel,string key,string ar,string en){var box=new TextBox{MinHeight=32,MaxLength=1000,TextWrapping=TextWrapping.Wrap};_fields[key]=box;Field(panel,UiText.T(ar,en),box);}
    private static void Field(Panel panel,string label,UIElement input){panel.Children.Add(new TextBlock{Text=label,Margin=new Thickness(0,9,0,4),FontWeight=FontWeights.SemiBold});panel.Children.Add(input);}
    private void ChooseLogo(object sender,RoutedEventArgs e)
    {
        var dialog=new OpenFileDialog{Filter="PNG / JPEG|*.png;*.jpg;*.jpeg"};if(dialog.ShowDialog(this)!=true)return;
        try{
            if(new FileInfo(dialog.FileName).Length>1024*1024)throw new ArgumentException(UiText.T("الصورة أكبر من 1 ميجابايت.","Image exceeds 1 MB."));
            var bytes=File.ReadAllBytes(dialog.FileName);using var stream=new MemoryStream(bytes);var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
            if(decoder.Frames[0].PixelWidth>6000||decoder.Frames[0].PixelHeight>6000)throw new ArgumentException(UiText.T("أبعاد الصورة كبيرة؛ صغّرها أولاً.","Image dimensions are too large; resize first."));
            var candidate=Convert.ToBase64String(bytes);var old=_value.LogoBase64;_value.LogoBase64=candidate;try{DocumentBrandingService.Validate(_value);ShowLogo();}catch{_value.LogoBase64=old;throw;}
        }catch(Exception ex){MessageBox.Show(ex.Message);}
    }
    private void ShowLogo(){_logo.Source=null;if(string.IsNullOrWhiteSpace(_value.LogoBase64))return;using var stream=new MemoryStream(Convert.FromBase64String(_value.LogoBase64));var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();_logo.Source=image;}
    private sealed record Scope(int? Id,string Name);
}
