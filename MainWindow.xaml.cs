using Siprix;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace SampleWpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Initialize SDK
            Siprix.ObjModel.Instance.Initialize(App.Current.Dispatcher);

            Siprix.ObjModel.Instance.Calls.PropertyChanged += onCalls_PropertyChanged;

            //Set data context
            lbSubscriptions.DataContext = Siprix.ObjModel.Instance.Subscriptions;
            tbNetworkLost.DataContext = Siprix.ObjModel.Instance.Networks;
            lbAccounts.DataContext    = Siprix.ObjModel.Instance.Accounts;
            lbCalls.DataContext       = Siprix.ObjModel.Instance.Calls;
            tbLogs.DataContext        = Siprix.ObjModel.Instance.Logs;
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            //Uninitialize SDK
            Siprix.ObjModel.Instance.UnInitialize();
        }

        private void onCalls_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CallsListModel.LastIncomingCallId))
            {
                //Switch tab to 'Calls' when received incoming call
                if (mainTabCtrl.SelectedIndex != 1) {
                    mainTabCtrl.SelectedIndex = 1;
                }
            }
        }

        private void AccountAdd_Click(object sender, RoutedEventArgs e)
        {
            AddAccountWindow wnd = new();
            wnd.ShowDialog();
        }

        private void SubscriptionAdd_Click(object sender, RoutedEventArgs e)
        {
            AddSubscriptionWindow wnd = new();
            wnd.ShowDialog();
        }

        private void AccountEdit_Click(object sender, RoutedEventArgs e)
        {
            MenuItem? mnu = sender as MenuItem;
            if ((mnu == null) || (mnu.Tag == null)) return;
            uint accID = (uint)mnu.Tag;

            AccData? accData = Siprix.ObjModel.Instance.Accounts.GetData(accID);
            if (accData == null) return;

            AddAccountWindow wnd = new(accData);
            wnd.ShowDialog();
        }

        private void AccountDelete_Click(object sender, RoutedEventArgs e)
        {
            //Get selected
            MenuItem? mnu = sender as MenuItem;
            if ((mnu == null) || (mnu.Tag == null)) return;
            uint accID = (uint)mnu.Tag;

            //Confirm deleting
            MessageBoxResult result = System.Windows.MessageBox.Show(this, "Confirm deleting account?", "Confirmation", 
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes) return;

            //Delete
            int err = Siprix.ObjModel.Instance.Accounts.Delete(accID);
            if (err != Module.kNoErr)
            {
                System.Windows.MessageBox.Show(this, Siprix.ObjModel.Instance.ErrorText(err), "Information", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SubscriptionDelete_Click(object sender, RoutedEventArgs e)
        {
            //Get selected
            MenuItem? mnu = sender as MenuItem;
            if ((mnu == null) || (mnu.Tag == null)) return;
            uint subscrID = (uint)mnu.Tag;

            //Confirm deleting
            MessageBoxResult result = System.Windows.MessageBox.Show(this, "Confirm deleting subscription?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes) return;

            //Delete
            int err = Siprix.ObjModel.Instance.Subscriptions.Delete(subscrID);
            if (err != Module.kNoErr)
            {
                System.Windows.MessageBox.Show(this, Siprix.ObjModel.Instance.ErrorText(err), "Information", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ButtonMenu_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button? btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            ContextMenu contextMenu = btn.ContextMenu;
            if (contextMenu == null) return;

            contextMenu.PlacementTarget = btn;
            contextMenu.Placement = PlacementMode.Left;
            contextMenu.HorizontalOffset = btn.ActualWidth;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var url = e.Uri.ToString();
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
    }
}