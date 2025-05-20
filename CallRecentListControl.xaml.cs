using System.Windows.Controls;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Effects;
using System.Windows.Media;

namespace SampleWpf;

public partial class CallRecentListControl : UserControl
{
    readonly Siprix.ObjModel objModel_;
    readonly Siprix.DestData data_ = new();

    public delegate void CancelHandler();
    public event CancelHandler? OnCancel;

    DropShadowEffect? modalShadowEffect_;

    public CallRecentListControl(Siprix.ObjModel om)
    {
        InitializeComponent();
        objModel_ = om;

        lbCdrs.DataContext = om.Cdrs;        
        lbCdrs.SelectionChanged += CdrsList_SelectionChanged;

        cbAccounts.DataContext = om.Accounts;

        objModel_.Accounts.Collection.CollectionChanged += (_, _) => OnAccountsList_CollectionChanged();
        OnAccountsList_CollectionChanged();

        btnCancel.Visibility = Visibility.Collapsed;

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
        OnCancel?.Invoke();
    }

    void Cancel_Click(object sender, EventArgs e)
    {
        OnCancel?.Invoke();
    }

    public void SetDialogMode(bool dialogMode)
    {
        btnCancel.Visibility = dialogMode ? Visibility.Visible : Visibility.Collapsed;
        brdShadow.BorderThickness = new Thickness(dialogMode? 1: 0);
        brdShadow.Margin = new Thickness(dialogMode ? 5 : 0);
        brdShadow.Effect = dialogMode ? GetModalShadowEffect() : null;
    }

    private void DestExt_TextChanged()
    {
        btnVideoCall.IsEnabled = txDestExt.Text.Length!=0;
        btnAudioCall.IsEnabled = txDestExt.Text.Length != 0;
    }

    public void ShowDialpad_Click(object sender, EventArgs e)
    {
        gridDtmf.Visibility = (gridDtmf.Visibility == Visibility.Visible) ?
                                Visibility.Collapsed : Visibility.Visible;
    }

    public void DtmfSend_Click(object sender, EventArgs e)
    {
        if (sender is System.Windows.Controls.Button btnSender)
            txDestExt.Text += (string)btnSender.Content;
    }

    DropShadowEffect GetModalShadowEffect()
    {
        if (modalShadowEffect_ == null)
        {
            modalShadowEffect_ = new DropShadowEffect
            {
                Color = new Color { A = 255, R = 211, G = 211, B = 211 },
                //Direction = 320,
                //ShadowDepth = 0,
                Opacity = 1
            };
        }
        return modalShadowEffect_;
    }

    private void txDestExt_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        gridDtmf.Visibility = Visibility.Collapsed;
    }
}
