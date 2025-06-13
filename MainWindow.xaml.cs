using Siprix;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;

namespace SampleWpf;

public partial class MainWindow : Window
{
    Siprix.ObjModel objModel_ = null!;
    CallSwitchedControl switchedCallCtrl_ = null!;
    CallRecentListControl callRecentListCtrl_ = null!;
    public MainWindow()
    {
        InitializeComponent();
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        //Initialize SDK
        objModel_ = new();
        objModel_.Initialize(App.Current.Dispatcher);

        //Assign listener
        objModel_.Calls.PropertyChanged += OnCalls_PropertyChanged;
        objModel_.Calls.Collection.CollectionChanged += (_,_) => OnCalls_CollectionChanged();

        //Set data context
        lbSubscriptions.DataContext = objModel_.Subscriptions;
        tbNetworkLost.DataContext = objModel_.Networks;
        lbMessages.DataContext    = objModel_.Messages;
        cbMsgAccounts.DataContext = objModel_.Accounts;
        lbAccounts.DataContext    = objModel_.Accounts;
        lbCalls.DataContext       = objModel_.Calls;
        tbLogs.DataContext        = objModel_.Logs;

        switchedCallCtrl_ = new CallSwitchedControl(objModel_);
        switchedCallCtrl_.OnAddCall += CallAdd_Click;
        CallsGrid.Children.Add(switchedCallCtrl_);
        Grid.SetRow(switchedCallCtrl_, 1);

        callRecentListCtrl_ = new CallRecentListControl(objModel_);
        callRecentListCtrl_.OnCancel += CallAddCancel_Click;
        CallsGrid.Children.Add(callRecentListCtrl_);
        Grid.SetRowSpan(callRecentListCtrl_, 2);
        Panel.SetZIndex(callRecentListCtrl_, 2);

        OnCalls_CollectionChanged();

        //Devices
        objModel_.Devices.Load();
        cbVideo.DataContext    = objModel_.Devices;
        cbRecord.DataContext   = objModel_.Devices;
        cbPlayback.DataContext = objModel_.Devices;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        //Uninitialize SDK
        objModel_.UnInitialize();
    }

    private void OnCalls_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Siprix.CallsListModel.LastIncomingCallId))
        {
            //Switch tab to 'Calls' when received incoming call
            if (mainTabCtrl.SelectedIndex != 1) {
                mainTabCtrl.SelectedIndex = 1;
            }
        }
    }

    private void OnCalls_CollectionChanged()
    {
        bool callsListEmpty = (objModel_.Calls.Collection.Count == 0);
        callRecentListCtrl_.Visibility = callsListEmpty ? Visibility.Visible : Visibility.Collapsed;
        callRecentListCtrl_.SetDialogMode(!callsListEmpty);

        switchedCallCtrl_.Visibility = callsListEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void CallAdd_Click()
    {
        callRecentListCtrl_.Visibility = Visibility.Visible;
    }

    private void CallAddCancel_Click()
    {
        callRecentListCtrl_.Visibility = Visibility.Collapsed;
    }

    private void AccountAdd_Click(object sender, RoutedEventArgs e)
    {
        AddAccountWindow wnd = new(objModel_);
        wnd.ShowDialog();
    }

    private void SubscriptionAdd_Click(object sender, RoutedEventArgs e)
    {
        AddSubscriptionWindow wnd = new(objModel_);
        wnd.ShowDialog();
    }

    private void AccountEdit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender is not MenuItem mnu) || (mnu.Tag == null)) return;
        uint accID = (uint)mnu.Tag;

        Siprix.AccData? accData = objModel_.Accounts.GetData(accID);
        if (accData == null) return;

        AddAccountWindow wnd = new(objModel_, accData);
        wnd.ShowDialog();
    }

    private void AccountDelete_Click(object sender, RoutedEventArgs e)
    {
        //Get selected
        if(sender is not MenuItem mnu) return;
        if (mnu?.DataContext is not Siprix.AccountModel acc) return;

        //Confirm deleting
        MessageBoxResult result = System.Windows.MessageBox.Show(this, "Confirm deleting account?", "Confirmation", 
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (result != MessageBoxResult.Yes) return;

        //Delete
        int err = objModel_.Accounts.Delete(acc);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            System.Windows.MessageBox.Show(this, objModel_.ErrorText(err), "Information", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void SubscriptionDelete_Click(object sender, RoutedEventArgs e)
    {
        //Get selected
        if(sender is not MenuItem mnu) return;
        if (mnu?.DataContext is not Siprix.SubscriptionModel subscr) return;

        //Confirm deleting
        MessageBoxResult result = System.Windows.MessageBox.Show(this, "Confirm deleting subscription?", "Confirmation",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (result != MessageBoxResult.Yes) return;

        //Delete
        int err = objModel_.Subscriptions.Delete(subscr);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            System.Windows.MessageBox.Show(this, objModel_.ErrorText(err), "Information", 
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
        objModel_.Messages.Send(msgData);            
    }

    private void MessageDelete_Click(object sender, RoutedEventArgs e)
    {
        //Get selected
        if (sender is not MenuItem mnu) return;        
        if (mnu?.DataContext is not Siprix.MessageModel msg) return;

        //Confirm deleting
        MessageBoxResult result = System.Windows.MessageBox.Show(this, "Confirm deleting message?", "Confirmation",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (result != MessageBoxResult.Yes) return;

        //Delete
        int err = objModel_.Messages.Delete(msg);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            System.Windows.MessageBox.Show(this, objModel_.ErrorText(err), "Information",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ButtonMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;

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