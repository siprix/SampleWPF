using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace SampleWpf;

///Provides controls for manipulating current/switched call    
public partial class CallSwitchedControl : System.Windows.Controls.UserControl
{
    enum UiMode { eUndef, eMain, eDtmf, eTransferBlind, eTranferAtt };

    Siprix.CallModel? callModel_;
    readonly Siprix.CallsListModel calls_;        
    readonly Siprix.ObjModel objModel_;
    readonly Dictionary<Siprix.CallState, UiMode> uiModes_ = [];
    readonly System.Windows.Threading.DispatcherTimer callDurationTimer_ = new();
    readonly VideoControlHost[] receivedVideoHost_;
    readonly Border[] receivedVideoBorders_;

    readonly VideoControlHost previewVideoHost_ = new();

    public delegate void AddCallHandler();
    public event AddCallHandler? OnAddCall;

    public CallSwitchedControl(Siprix.ObjModel objModel)
    {
        InitializeComponent();
        objModel_ = objModel;

        calls_ = calls_ = objModel_.Calls;

        calls_.Collection.CollectionChanged += onCalls_CollectionChanged;
        calls_.PropertyChanged += onCalls_PropertyChanged;
        calls_.CallTerminated += onCalls_CallTerminated;

        callDurationTimer_.Tick += onCallDurationTimer_Tick;
        callDurationTimer_.Interval = TimeSpan.FromSeconds(1);

        receivedVideoBorders_ = new Border[4];
        receivedVideoHost_ = new VideoControlHost[receivedVideoBorders_.Length];

        receivedVideoBorders_[0] = receivedVideo;
        receivedVideoBorders_[1] = receivedVideo1;
        receivedVideoBorders_[2] = receivedVideo2;
        receivedVideoBorders_[3] = receivedVideo3;

        for (int i = 0; i < receivedVideoBorders_.Length; ++i)
        {
            receivedVideoHost_[i] = new VideoControlHost();
            receivedVideoBorders_[i].Child = receivedVideoHost_[i];
        }
        previewVideo.Child = previewVideoHost_;
    }

    private void onCalls_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        callDurationTimer_.IsEnabled = (calls_.Collection.Count > 0);
        
        ConferenceMenu.IsEnabled = (calls_.Collection.Count > 1);//when have 2 or more calls
    }

    private void onCalls_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(Siprix.CallsListModel.SwitchedCall))
        {
            if (callModel_ != null) {
                callModel_.PropertyChanged -= onCall_PropertyChanged;
                callModel_.SetVideoWindow(IntPtr.Zero);
            }

            callModel_  = calls_.SwitchedCall;                
            DataContext = callModel_;

            resetUiModes();
            updateVisibility();

            if (callModel_ != null) {
                callModel_.PropertyChanged += onCall_PropertyChanged;
                callModel_.SetVideoWindow(receivedVideoHost_[0].Hwnd);
            }

            calls_.SetPreviowVideoWindow(previewVideoHost_.Hwnd);
        }

        if (e.PropertyName == nameof(Siprix.CallsListModel.ConfModeStarted))
        {
            ConferenceMenu.Header = calls_.ConfModeStarted ? "End conference" : "Make conference";
            SetVideoWindowConfMode();
        }
    }

    private void onCalls_CallTerminated(uint callId, uint statusCode)
    {
        //if(statusCode==403)
        //{
        //    string pathToDemoFile = AppDomain.CurrentDomain.BaseDirectory + "Resources\\music.mp3";
        //
        //    var uri = new Uri(pathToDemoFile, UriKind.RelativeOrAbsolute);
        //    var player = new MediaPlayer();
        //    player.Open(uri);
        //    player.Play();
        //}
    }

    private void onCall_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Siprix.CallModel.CallState)||
           (e.PropertyName == nameof(Siprix.CallModel.HoldState)))
        {
            updateVisibility();
        }
    }

    void resetUiModes()
    {
        uiModes_[Siprix.CallState.Ringing]  = UiMode.eUndef;
        uiModes_[Siprix.CallState.Connected]= UiMode.eMain;
        uiModes_[Siprix.CallState.Held]     = UiMode.eMain;
    }

    UiMode getUiMode(Siprix.CallState? state)
    {
        UiMode mode = UiMode.eUndef;
        if (state == null) return mode;

        uiModes_.TryGetValue(state.Value, out mode);
        return mode;
    }

    void setUiMode(Siprix.CallState? state, UiMode mode)
    {
        if (state == null) return;
        
        if ((state == Siprix.CallState.Ringing) && (mode == UiMode.eMain))
            mode = UiMode.eUndef;

        uiModes_[state.Value] = mode;
    }

    void updateVisibility()
    {
        tbNameAndExt.Visibility = (callModel_ == null) ? Visibility.Collapsed : Visibility.Visible;
        spDetails.Visibility    = (callModel_ == null) ? Visibility.Collapsed : Visibility.Visible;

        //Ringing
        bool isConnected = (callModel_ != null) && callModel_.IsConnected;
        bool isRinging   = (callModel_ != null) && callModel_.IsRinging;
        bool isVideo     = (callModel_ != null) && callModel_.WithVideo;
        spAcceptReject.Visibility = isRinging   ? Visibility.Visible : Visibility.Collapsed;
        bnDtmfMode.Visibility     = isConnected ? Visibility.Visible : Visibility.Collapsed;

        //Video
        gridVideo.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;

        //Connected display depending on input mode
        UiMode uiMode = getUiMode(callModel_?.CallState);
        bnRedirect.Visibility   = (uiMode == UiMode.eUndef)&&isRinging ? Visibility.Visible : Visibility.Collapsed;
        gridDtmf.Visibility     = (uiMode == UiMode.eDtmf)          ? Visibility.Visible : Visibility.Collapsed;
        gridMain.Visibility     = (uiMode == UiMode.eMain)          ? Visibility.Visible : Visibility.Collapsed;
        gridTransfer.Visibility = (uiMode == UiMode.eTransferBlind) ? Visibility.Visible : Visibility.Collapsed;
                    
        //bnMakeCall.Visibility = (callModel_ == null) ? Visibility.Visible : Visibility.Collapsed;
        bnHangup.Visibility   = (callModel_ == null)|| isRinging ? Visibility.Collapsed : Visibility.Visible;

        if (callModel_ != null)
            bnTransfer.Content = callModel_.IsRinging ? "Redirect" : "Transfer";
    }

    private void onCallDurationTimer_Tick(object? sender, EventArgs e)
    {
        calls_.CalcDuration();
    }

    private void DtmfMode_Click(object sender, RoutedEventArgs e)
    {
        UiMode uiMode = getUiMode(callModel_?.CallState);
        if (uiMode == UiMode.eDtmf)
        {
            //Hide DTMF keys if displayed                
            setUiMode(callModel_?.CallState, UiMode.eMain);
        }
        else
        {
            //Show DTMF keys
            tbSentDtmf.Text = "";
            setUiMode(callModel_?.CallState, UiMode.eDtmf);
        }
        updateVisibility();
    }

    private void DtmfSend_Click(object sender, RoutedEventArgs e)
    {
        if(sender is System.Windows.Controls.Button btnSender)
            tbSentDtmf.Text += (string)btnSender.Content;
    }

    private void TransferBlindMode_Click(object sender, RoutedEventArgs e)
    {
        UiMode uiMode = getUiMode(callModel_?.CallState);
        if (uiMode == UiMode.eTransferBlind)
        {
            //Hide transfer/redirect edit if displayed
            tbTransferToExt.Text = "";
            setUiMode(callModel_?.CallState, UiMode.eMain);
        }
        else
        {
            //Show transfer/redirect edit if displayed
            setUiMode(callModel_?.CallState, UiMode.eTransferBlind);
        }
        updateVisibility();
    }

    private void TransferBlind_Click(object sender, RoutedEventArgs e)
    {
        callModel_?.TransferBlind(tbTransferToExt.Text);
    }

    private void PlayFile_Click(object sender, RoutedEventArgs e)
    {
        if (callModel_ == null) return;

        if(callModel_.IsFilePlaying)
        {
            callModel_.StopPlayFile();
        }
        else
        {
            string pathToDemoFile = AppDomain.CurrentDomain.BaseDirectory + "Resources\\music.mp3";
            callModel_.PlayFile(pathToDemoFile, false);
        }
    }

    private void RecordFile_Click(object sender, RoutedEventArgs e)
    {
        if ((callModel_ == null)) return;

        if (callModel_.IsFileRecording)
        {
            callModel_.StopRecordFile();
        }
        else
        {
            string recFile = AppDomain.CurrentDomain.BaseDirectory + DateTime.Now.ToString("yyyyMMdd_hhmmss.mp3");
            callModel_.RecordFile(recFile);
        }   
    }

    private void Conference_Click(object sender, RoutedEventArgs e)
    {
        calls_.MakeConference();
    }

    private void SetVideoWindowConfMode()
    {
        //Set/unset video windows for other calls
        for (int i = 1; i < Math.Min(calls_.Collection.Count, receivedVideoHost_.Length); ++i)
        {
            calls_.Collection[i].SetVideoWindow(calls_.ConfModeStarted ? receivedVideoHost_[i].Hwnd : IntPtr.Zero);
            receivedVideoBorders_[i].Visibility = calls_.ConfModeStarted ? Visibility.Visible : Visibility.Collapsed;
        }

        //Hide rest video windows 
        for(int j = calls_.Collection.Count; j < receivedVideoHost_.Length; ++j)
        {
            receivedVideoBorders_[j].Visibility = Visibility.Collapsed;
        }

        if(!calls_.ConfModeStarted && calls_.SwitchedCall!=null)
            calls_.SwitchedCall.SetVideoWindow(receivedVideoHost_[0].Hwnd);

        calls_.SetPreviowVideoWindow(previewVideoHost_.Hwnd);
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

    private void AddCall_Click(object sender, RoutedEventArgs e)
    {
        OnAddCall?.Invoke();
    }
}

public class VideoControlHost : HwndHost
{
    internal const int
        WsChild = 0x40000000,
        WsVisible = 0x10000000,
        HostId = 0x00000002;
    
    private IntPtr _hwndHost;

    public IntPtr Hwnd { get { return _hwndHost; } }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _hwndHost = CreateWindowEx(0, "static", "",
            WsChild | WsVisible, 0, 0, 100, 100,
            hwndParent.Handle, (IntPtr)HostId, IntPtr.Zero, IntPtr.Zero);

        return new HandleRef(this, _hwndHost);
    }

    protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        handled = false;
        return IntPtr.Zero;
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyWindow(hwnd.Handle);
    }

    //PInvoke declarations
    [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
    internal static extern IntPtr CreateWindowEx(int dwExStyle,
        string lpszClassName, string lpszWindowName, int style,
        int x, int y, int width, int height,
        IntPtr hwndParent, IntPtr hMenu, IntPtr hInst, IntPtr pvParam);

    [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
    internal static extern bool DestroyWindow(IntPtr hwnd);
}//VideoControlHost
