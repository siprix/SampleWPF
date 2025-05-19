using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SampleWpf;

public partial class CallRecentListControl : UserControl
{
    readonly Siprix.ObjModel objModel_;
    readonly Siprix.DestData data_ = new();

    public delegate void CancelClickHandler();
    public event CancelClickHandler? CancelClick;

    public CallRecentListControl(Siprix.ObjModel om, bool modalMode = false)
    {
        InitializeComponent();
        objModel_ = om;

        lbCdrs.DataContext = om.Cdrs;        
        lbCdrs.SelectionChanged += CdrsList_SelectionChanged;

        cbAccounts.DataContext = om.Accounts;

        objModel_.Accounts.Collection.CollectionChanged += (_, _) => OnAccountsList_CollectionChanged();
        OnAccountsList_CollectionChanged();

        btnCancel.Visibility = modalMode ? Visibility.Visible : Visibility.Collapsed;

        txDestExt.TextChanged += (_,_) => DestExt_TextChanged();
        DestExt_TextChanged();
    }

    private void OnAccountsList_CollectionChanged()
    {
        bool accountsExists = (objModel_.Accounts.Collection.Count != 0);
        tbErrText.Visibility = accountsExists ? Visibility.Collapsed : Visibility.Visible;
        btnAudioCall.IsEnabled = accountsExists;
        btnVideoCall.IsEnabled = accountsExists;
        cbAccounts.IsEnabled = accountsExists;
        txDestExt.IsEnabled = accountsExists;
    }

    private void CdrsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (lbCdrs.SelectedItem is Siprix.CdrModel cdrModel)
        {
            txDestExt.Text = cdrModel.RemoteExt;
            cbAccounts.SelectedItem = objModel_.Accounts.FindByUri(cdrModel.AccUri);
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

    void RecentCallDelete_Click(object sender, EventArgs e)
    {
        if (sender is not MenuItem mnu) return;
        if (mnu?.DataContext is Siprix.CdrModel item) 
            objModel_.Cdrs.Delete(item);
    }

    void AddCall_Click(object sender, EventArgs e)
    {
        Invite(false);
    }
    void AddVideoCall_Click(object sender, EventArgs e)
    {
        Invite(true);
    }

    void Invite(bool withVideo)
    {
        //Check empty
        if (string.IsNullOrEmpty(txDestExt.Text) ||
            (cbAccounts.SelectedItem == null))
            return;

        //Get data from controls
        data_.ToExt = txDestExt.Text;
        data_.FromAccId = ((Siprix.AccountModel)cbAccounts.SelectedItem).ID;
        data_.WithVideo = withVideo;

        //Try to make call
        int err = objModel_.Calls.Invite(data_);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            tbErrText.Text = objModel_.ErrorText(err);
            tbErrText.Visibility = Visibility.Visible;
            return;
        }

        txDestExt.Text = "";
        CancelClick?.Invoke();
    }

    void Cancel_Click(object sender, EventArgs e)
    {
        CancelClick?.Invoke();
    }

    private void DestExt_TextChanged()
    {
        btnVideoCall.IsEnabled = txDestExt.Text.Length!=0;
        btnAudioCall.IsEnabled = txDestExt.Text.Length != 0;
    }
}
