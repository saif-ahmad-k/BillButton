using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Media;
using System.IO;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4.Data;
using BackgroundDesktopApplication.Models;
using System.Threading;
using Google.Apis.Util.Store;
using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BackgroundDesktopApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        
        DataContext db = new DataContext();
        static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        static string ApplicationName = "Google Sheets API .NET Quickstart";
        string gesturefile = @"../client_secret.json";
#pragma warning disable CS0169 // The field 'Window1.tb' is never used
        private TaskbarIcon tb;
#pragma warning restore CS0169 // The field 'Window1.tb' is never used
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //(none)
        private const uint MOD_ALT = 0x0001; //ALT
        private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        //CAPS LOCK:
        private const uint VK_CAPITAL = 0x14;
        public ObservableCollection<string> ItemList { get; set; }
    

        private IntPtr _windowHandle;
        private HwndSource _source;
        private bool cb;
        public Window1()
        {

            InitializeComponent();
            RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            reg.SetValue("backgroundApp", System.Reflection.Assembly.GetExecutingAssembly().Location.ToString());
            ReadGoogleSheet();
            cmbEmployee.ItemsSource = null;
            cmbEmployee.ItemsSource = db.tblEmployees.ToList();
            //cmbMatter.ItemsSource = null;
            //cmbMatter.ItemsSource = db.tblMatterCaseLists.ToList();
            //string text = cmbMatter.Text;
            //if (text == null) { cmbMatter.ItemsSource = db.tblMatterCaseLists.Select(x=>x.MatterName).ToList(); }
            //else { cmbMatter.ItemsSource = db.tblMatterCaseLists.Where(x=>x.MatterName.Contains(text)).ToList(); }
            ;
            searchcmb.ItemsSource = null;
            searchcmb.ItemsSource = db.tblMatterCaseLists.ToList();
            datePickerForm.SelectedDate = DateTime.Today;

        }





        public event PropertyChangedEventHandler PropertyChanged;
       
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_CONTROL, VK_CAPITAL); //CTRL + CAPS_LOCK
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_CAPITAL)
                            {
                                if (WindowState == WindowState.Minimized)
                                {
                                    this.WindowState = WindowState.Normal;
                                    this.Activate();
                                }
                                else
                                {
                                    this.WindowState = WindowState.Minimized;
                                    this.Activate();
                                }
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //public void cmbMatter_SelectionChanged(object sender, Gui.Controls.AutoComplete.AutoCompleteArgs args)
            //{
                
            //    //check
            //    if (string.IsNullOrEmpty(args.Pattern))
            //        args.CancelBinding = true;
            //    else
            //        args.DataSource = BackgroundDesktopApplication.GetMatter(args.Pattern);
            //}
            try
            {
                MediaPlayer Sound1 = new MediaPlayer();
                Sound1.Open(new Uri("mixkit-magical-coin-win-1936.wav", UriKind.Relative));
                Sound1.Play();

                tblTrackTime obj = new tblTrackTime();
                obj.Date = datePickerForm.SelectedDate.Value.Date;
                obj.Duration = txtDuration.Text;
                obj.Note = txtNote.Text;
                tblEmployee selectedEmployee = (tblEmployee)cmbEmployee.SelectedItem;
                obj.UserID = selectedEmployee.UserId;
                tblMatterCaseList selectedMatter = (tblMatterCaseList)searchcmb.SelectedItem;
                obj.MatterName = selectedMatter.MatterName;
                obj.MatterIDColio = selectedMatter.MatterIDColio;
                obj.MatterIDPodio = selectedMatter.MatterIDPodio;
                obj.MatterIDSlack = selectedMatter.MatterIDSlack;
                db.tblTrackTimes.Add(obj);
                db.SaveChanges();

                var gsh = new GoogleSheetsHelper(gesturefile, "17cE64M4FnXGzGXHT-po0VtXkiMUU64kBQrrEB0Jr3vk");

                var rows = new List<GoogleSheetRow>();
                foreach (var track in db.tblTrackTimes.ToList())
                {
                    var emp = db.tblEmployees.Find(track.UserID);
                    var newRow = new GoogleSheetRow();
                    var matterNameCell = new GoogleSheetCell() { CellValue = track.MatterName };
                    var userCell = new GoogleSheetCell() { CellValue = emp.FirstName };
                    var UserIdPodioCell = new GoogleSheetCell() { CellValue = track.UserID.ToString() };
                    var DateCell = new GoogleSheetCell() { CellValue = track.Date.Value.Date.ToString() };
                    var NoteCell = new GoogleSheetCell() { CellValue = track.Note };
                    var DurationCell = new GoogleSheetCell() { CellValue = track.Duration };
                    var MatterIdPodioCell = new GoogleSheetCell() { CellValue = track.MatterIDPodio };
                    var MatterIdClioCell = new GoogleSheetCell() { CellValue = track.MatterIDColio };
                    var MatterIdSlackCell = new GoogleSheetCell() { CellValue = track.MatterIDSlack };
                    newRow.Cells.AddRange(new List<GoogleSheetCell>() { matterNameCell, userCell, UserIdPodioCell, DateCell, NoteCell, DurationCell, MatterIdPodioCell, MatterIdClioCell, MatterIdSlackCell });
                    rows.Add(newRow);
                }

                gsh.AddCells(new GoogleSheetParameters() { SheetName = "Sheet1", RangeColumnStart = 1, RangeRowStart = 2 }, rows);

                MessageBox.Show("Sheet Updated");
            }catch(Exception ex)
            {
                MessageBox.Show("Error! " + ex.Message);
            }

            searchcmb.SelectedItem = null;
            txtNote.Text = null;
            txtDuration.Text = null;




        }
        private void ReadGoogleSheet()
        {
            

            UserCredential credential;

            using (var stream =
                new FileStream(gesturefile, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "client_secret.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Google Sheets API service.
            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            // Define request parameters.
            String spreadsheetId = "1IZlPybH5H2bq6L75synOo4BOUfapJX19VcVSer6gv2Y"; //input spreadsheet Matter Case List (Need to populate Matter ID from it)
            String spreadsheet2Id = "1DEBZ0nZIQq4LSwHMR38sVmhdWP2wWvNTOclt00_u-no"; //inpuit sheet employee (Need to populate drop down employee from it)
            String range = "Sheet1";
            SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(spreadsheetId, range);
            SpreadsheetsResource.ValuesResource.GetRequest request2 =
                    service.Spreadsheets.Values.Get(spreadsheet2Id, range);

            // Prints the names and majors of students in a sample spreadsheet:
            // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
            ValueRange response = request.Execute();
            ValueRange response2 = request2.Execute();

            IList<IList<Object>> matterCaseList = response.Values;
            IList<IList<Object>> employees = response2.Values;

            using(DataContext dc = new DataContext())
            {
                int matCount = 0;
                foreach(var item in matterCaseList)
                {
                    if (matCount > 0)
                    {
                        var name = item[0].ToString();
                        if (dc.tblMatterCaseLists.Where(p => p.MatterName == name).FirstOrDefault() == null)
                        {
                            tblMatterCaseList obj = new tblMatterCaseList();
                            obj.MatterName = item[0].ToString();
                            if (item[1] != null)
                            {
                                obj.MatterIDPodio = item[1].ToString();

                            }
                            if (item.Count > 2)
                            {
                                if (item[2] != null)
                                {
                                    obj.MatterIDColio = item[2].ToString();
                                }
                            }
                            //var abc= matterCaseList[i][2];
                            if (item.Count > 3)
                            {
                                if (item[3] != null)
                                {
                                    obj.MatterIDSlack = item[3].ToString();
                                }
                            }
                            dc.tblMatterCaseLists.Add(obj);
                        }
                        
                    }
                    matCount++;
                }
                
                dc.SaveChanges();
                for (int i = 0; i < employees.Count(); i++)
                {
                    if (i > 0)
                    {
                        var userId = Convert.ToInt32(employees[i][2].ToString());
                        if (dc.tblEmployees.Where(p=>p.UserId== userId).FirstOrDefault() == null)
                        {
                            tblEmployee obj = new tblEmployee();
                            obj.FirstName = employees[i][0].ToString();
                            obj.LastName = employees[i][1].ToString();
                            obj.UserId = Convert.ToInt32(employees[i][2].ToString());
                            dc.tblEmployees.Add(obj);
                        }
                        
                    }
                }
                dc.SaveChanges();
                //for (int i = 0; i < trackedTime.Count(); i++)
                //{
                //    if (i > 0)
                //    {
                //        //tblTrackTime obj = new tblTrackTime();
                //        //obj.na = matterCaseList[i][0].ToString();
                //        //obj.LastName = matterCaseList[i][1].ToString();
                //        //obj.UserId = Convert.ToInt32(matterCaseList[i][2].ToString());
                //        //dc.tblEmployees.Add(obj);
                //    }
                //}
                //dc.SaveChanges();
            }

        }


        private void cmbMatter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //public partial class MainWindow : INotifyPropertyChanged
        //{
        //    private string _searchTextText;
        //    private object cb;

        //    //public MainWindow()
        //    //{
        //    //    ItemList = new ObservableCollection<string>();
        //    //    for (var i = 0; i < 10; i++)
        //    //    {
        //    //        ItemList.Add($"Item {i}");
        //    //    }
                
        //    //  // InitializeComponent();
        //    //}

        //    public event PropertyChangedEventHandler PropertyChanged;

        //    //private void Cb_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        //    //{
        //    //    cb = true;
        //    //}

        //   // public ObservableCollection<string> ItemList { get; set; }

        //    public string SearchTextText
        //    {
        //        get =>this. _searchTextText;
        //        set
        //        {
        //            if (_searchTextText == value) return;
        //           this._searchTextText = value;
                    
        //            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchTextText)));
                   
        //}
        //    }
        //}

        //private void Cb_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        //{
        //    cb = true;
        //}

        //public event PropertyChangedEventHandler PropertyChanged;
    }

}

