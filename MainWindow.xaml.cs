using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;

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
            lbMessages.DataContext    = Siprix.ObjModel.Instance.Messages;
            cbMsgAccounts.DataContext = Siprix.ObjModel.Instance.Accounts;
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
            if (e.PropertyName == nameof(Siprix.CallsListModel.LastIncomingCallId))
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

            Siprix.AccData? accData = Siprix.ObjModel.Instance.Accounts.GetData(accID);
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
            if (err != Siprix.Module.kNoErr)
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
            if (err != Siprix.Module.kNoErr)
            {
                System.Windows.MessageBox.Show(this, Siprix.ObjModel.Instance.ErrorText(err), "Information", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MessageSend_Click(object sender, RoutedEventArgs e)
        {
            //Check empty
            if ((txMsgBody.Text.Length == 0) ||
                (txMsgDestExt.Text.Length == 0) ||
                (cbMsgAccounts.SelectedItem == null)) return;

            //Get data from controls
            Siprix.MsgData msgData = new();
            msgData.ToExt = txMsgDestExt.Text;
            msgData.FromAccId = ((Siprix.AccountModel)cbMsgAccounts.SelectedItem).ID;
            msgData.Body = txMsgBody.Text;

            //Try to send
            Siprix.ObjModel.Instance.Messages.Send(msgData);            
        }

        private void MessageDelete_Click(object sender, RoutedEventArgs e)
        {
            //Get selected
            MenuItem? mnu = sender as MenuItem;
            if ((mnu == null) || (mnu.Tag == null)) return;
            uint msgID = (uint)mnu.Tag;

            //Confirm deleting
            MessageBoxResult result = System.Windows.MessageBox.Show(this, "Confirm deleting message?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes) return;

            //Delete
            int err = Siprix.ObjModel.Instance.Messages.Delete(msgID);
            if (err != Siprix.Module.kNoErr)
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