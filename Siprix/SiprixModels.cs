#pragma warning disable IDE1006, IDE0060, IDE0017
#define WPF_PROJECT
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Text.Json;


namespace Siprix;

#if WPF_PROJECT
using AppDispatcher = System.Windows.Threading.Dispatcher;
#else
using AppDispatcher = System.Windows.Forms.Control;
#endif

using JsonDict = Dictionary<string, object>;

/////////////////////////////////////////////////////////////////
/// AccountModel
public class AccountModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    readonly ObjModel parent_;
    AccData accData_;

    public AccountModel(AccData accData, ObjModel parent)
    {
        this.accData_= accData;
        this.parent_ = parent;            

        RegState = (accData.ExpireTime == 0) ? RegState.Removed : RegState.InProgress;
        RegText  = (accData.ExpireTime == 0) ? "Removed"        : "In progress...";
                    
        RegisterCommand   = new RelayCommand(Register,   CanRegister);
        UnRegisterCommand = new RelayCommand(UnRegister, CanUnRegister);
    }
            
    public AccData AccData     { get { return accData_; } }
    public string Uri          { get { return accData_.SipExtension + "@" + accData_.SipServer; } }
    public uint ID             { get { return accData_.MyAccId; } }        
    public bool IsWaiting      { get { return (RegState == RegState.InProgress); } }
    public bool HasSecureMedia { get { return (accData_.SecureMediaMode != null) && 
                                              (accData_.SecureMediaMode != SecureMedia.Disabled); } }
    public string   RegText  { get; private set; }
    public RegState RegState { get; private set; }
            
    public ICommand RegisterCommand   { get; private set; }
    public ICommand UnRegisterCommand { get; private set; }
    
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool Equals(AccountModel? other) { return (this.ID == other?.ID); }

    public void Update(AccData accData)
    {
        this.accData_ = accData;
    }

    bool CanRegister()   { return (RegState != RegState.InProgress); }
    bool CanUnRegister() { return (RegState != RegState.InProgress)&&(RegState != RegState.Removed); }

    void Register()
    {    
        //Send register request (use 300sec as expire time when account not registered)      
        uint? expireSec = accData_.ExpireTime;
        if ((expireSec == null) || (expireSec == 0)) { expireSec = 300; }
        refreshRegistration(expireSec.Value);
    }

    void UnRegister()
    {
        refreshRegistration(0);
    }

    int refreshRegistration(uint expireSec)
    {
        string cmd = (expireSec != 0) ? "Register" : "Unregister";
        int err = (expireSec != 0) ? parent_.Core.Account_Register(ID, expireSec)
                                   : parent_.Core.Account_Unregister(ID);
                                     
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't {cmd} AccId:{ID} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        //Update UI
        accData_.ExpireTime = expireSec;
        RegState = RegState.InProgress;
        NotifyPropertyChanged(nameof(RegState));
        NotifyPropertyChanged(nameof(IsWaiting));

        //Save changes
        parent_.PostSaveAccountsChanges();
        parent_.Logs?.Print($"{cmd}ing accId:{ID}");
        return err;
    }
    
    //Event raised by SDK
    internal void OnAccountRegState(RegState state, string response)
    {
        RegState = state;
        RegText  = response;
        NotifyPropertyChanged(nameof(RegText));
        NotifyPropertyChanged(nameof(RegState));
        NotifyPropertyChanged(nameof(IsWaiting));
    }

    public JsonDict storeToJson()
    {
        JsonDict dict = new();
        
        dict.Add("SipServer",    accData_.SipServer);
        dict.Add("SipExtension", accData_.SipExtension);
        dict.Add("SipPassword",  accData_.SipPassword);
        dict.Add("ExpireTime",   accData_.ExpireTime);

        if (accData_.SipAuthId         != null) dict.Add("SipAuthId",         accData_.SipAuthId);
        if (accData_.SipProxyServer    != null) dict.Add("SipProxyServer",    accData_.SipProxyServer);
                                                                              
        if (accData_.UserAgent         != null) dict.Add("UserAgent",         accData_.UserAgent);
        if (accData_.DisplayName       != null) dict.Add("DisplayName",       accData_.DisplayName);
        if (accData_.InstanceId        != null) dict.Add("InstanceId",        accData_.InstanceId);
        if (accData_.RingToneFile      != null) dict.Add("RingToneFile",      accData_.RingToneFile);

        if (accData_.SecureMediaMode   != null) dict.Add("SecureMedia",       accData_.SecureMediaMode.Value);
        if (accData_.UseSipSchemeForTls!= null) dict.Add("UseSipSchemeForTls",accData_.UseSipSchemeForTls.Value);
        if (accData_.RtcpMuxEnabled    != null) dict.Add("RtcpMuxEnabled",    accData_.RtcpMuxEnabled.Value);
        if (accData_.KeepAliveTime     != null) dict.Add("KeepAliveTime",     accData_.KeepAliveTime.Value);
                                                                    
        if (accData_.TranspProtocol    != null) dict.Add("TranspProtocol",    accData_.TranspProtocol.Value);
        if (accData_.TranspPort        != null) dict.Add("TranspPort",        accData_.TranspPort.Value);
        if (accData_.TranspTlsCaCert   != null) dict.Add("TranspTlsCaCert",   accData_.TranspTlsCaCert);
        if (accData_.TranspBindAddr    != null) dict.Add("TranspBindAddr",    accData_.TranspBindAddr);
        if (accData_.TranspPreferIPv6  != null) dict.Add("TranspPreferIPv6",  accData_.TranspPreferIPv6.Value);
        if (accData_.RewriteContactIp  != null) dict.Add("RewriteContactIp",  accData_.RewriteContactIp.Value);
        if (accData_.VerifyIncomingCall != null) dict.Add("VerifyIncomingCall", accData_.VerifyIncomingCall.Value);
                                                                              
        if (accData_.AudioCodecs       != null) dict.Add("AudioCodecs",       accData_.AudioCodecs);
        if (accData_.VideoCodecs       != null) dict.Add("VideoCodecs",       accData_.VideoCodecs);
        if (accData_.Xheaders          != null) dict.Add("Xheaders",          accData_.Xheaders);
        return dict;            

    }//storeToJson

    internal static AccData loadFromJson(JsonElement elem)
    {
        AccData accData = new();

        foreach (JsonProperty prop in elem.EnumerateObject())
        {
            bool isString = (prop.Value.ValueKind == JsonValueKind.String);
            string strVal = isString ? prop.Value.GetString()! : "";

            switch (prop.Name)
            {
                case "SipServer":          accData.SipServer      = strVal; break;
                case "SipExtension":       accData.SipExtension   = strVal; break;
                case "SipPassword":        accData.SipPassword    = strVal; break;
                case "ExpireTime":         accData.ExpireTime     = prop.Value.GetUInt32(); break;

                case "SipAuthId":          accData.SipAuthId      = strVal; break;
                case "SipProxyServer":     accData.SipProxyServer = strVal; break;
                
                case "UserAgent":          accData.UserAgent      = strVal; break;
                case "DisplayName":        accData.DisplayName    = strVal; break;                    
                case "InstanceId":         accData.InstanceId     = strVal; break;
                case "RingToneFile":       accData.RingToneFile   = strVal; break;
                
                
                case "SecureMedia":        accData.SecureMediaMode    = (SecureMedia)(prop.Value.GetUInt16()); break;
                case "UseSipSchemeForTls": accData.UseSipSchemeForTls = prop.Value.GetBoolean(); break;
                case "RtcpMuxEnabled":     accData.RtcpMuxEnabled     = prop.Value.GetBoolean(); break;
                case "KeepAliveTime":      accData.KeepAliveTime      = prop.Value.GetUInt32(); break;
                case "TranspProtocol":     accData.TranspProtocol     = (SipTransport)(prop.Value.GetUInt16()); break;
                case "TranspPort":         accData.TranspPort         = prop.Value.GetUInt16(); break;
                case "TranspTlsCaCert":    accData.TranspTlsCaCert    = strVal; break;
                case "TranspPreferIPv6":   accData.TranspPreferIPv6   = prop.Value.GetBoolean(); break;
                case "RewriteContactIp":   accData.RewriteContactIp   = prop.Value.GetBoolean(); break;
                case "AudioCodecs":                         
                {
                    accData.AudioCodecs = [];
                    foreach (JsonElement cElem in prop.Value.EnumerateArray())
                    {
                        accData.AudioCodecs.Add((AudioCodec)cElem.GetInt32());
                    }
                    break;
                }

                case "VideoCodecs": 
                {
                    accData.VideoCodecs = [];
                    foreach (JsonElement cElem in prop.Value.EnumerateArray())
                    {
                        accData.VideoCodecs.Add((VideoCodec)cElem.GetInt32());
                    }
                    break;
                }

                case "Xheaders":                     
                {
                    accData.Xheaders = [];
                    foreach (JsonProperty xProp in prop.Value.EnumerateObject())
                    {
                        if(xProp.Value.ValueKind == JsonValueKind.String)
                        {
                            accData.Xheaders.Add(prop.Name, xProp.Value.GetString()!);
                        }
                    }
                    break;
                }
            }//switch
        }//for

        return accData;
    }

}//AccountModel


/////////////////////////////////////////////////////////////////
/// AccountsListModel

public class AccountsListModel(ObjModel parent_) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    readonly ObservableCollection<AccountModel> collection_ = [];
    AccountModel? selAccount_;

    public ObservableCollection<AccountModel> Collection { get { return collection_; } }

    public AccountModel? SelectedAccount { 
        get { return selAccount_;  }
        set { selAccount_ = value; NotifyPropertyChanged(nameof(SelectedAccount)); }
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public AccountModel? FindByUri(string uri)
    {
        return collection_.Where(a => a.Uri == uri).FirstOrDefault();
    }

    public uint GetAccId(string uri)
    {
        var accModel = FindByUri(uri);
        return (accModel == null) ? ErrorCode.kInvalidId : accModel.ID;
    }

    public string GetUri(uint accId)
    {
        var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
        return (accModel == null) ? "?" : accModel.Uri;
    }

    public bool HasSecureMedia(uint accId)
    {
        var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
        return accModel != null && accModel.HasSecureMedia;
    }

    public AccData? GetData(uint accId)
    {
        var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
        return accModel?.AccData;
    }

    public int Add(AccData accData, bool saveChanges=true)
    {   
        parent_.Logs?.Print($"Adding new account: {accData.SipExtension}@{accData.SipServer}");

        int err = parent_.Core.Account_Add(accData);
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't add account Err: {err} {parent_.ErrorText(err)}");
            return err;
        }

        AccountModel acc = new(accData, parent_);
        collection_.Add(acc);

        selAccount_ ??= acc;

        parent_.Logs?.Print($"Added successfully with id: {acc.ID}");
        if (saveChanges) parent_.PostSaveAccountsChanges();
        return err;
    }

    public int Delete(AccountModel acc)
    {
        int err = parent_.Core.Account_Delete(acc.ID);
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't delete account Err: {err} {parent_.ErrorText(err)}");
            return err;
        }

        collection_.Remove(acc);

        if (selAccount_ == acc)
            selAccount_ = (collection_.Count > 0) ? collection_[0] : null;

        parent_.PostSaveAccountsChanges();

        parent_.Logs?.Print($"Deleted account accId:{acc.ID}");
        return err;
    }

    public int Update(AccData accData)
    {
        var accModel = collection_.Where(a => a.ID == accData.MyAccId).FirstOrDefault();
        if (accModel == null)
        {
            parent_.Logs?.Print("Account with specified id not found");
            return -1;
        }

        int err = parent_.Core.Account_Update(accData, accData.MyAccId);
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't update account: {err} {{parent_.ErrorText(err)}}");
            return err;
        }

        accModel.Update(accData);
        
        parent_.PostSaveAccountsChanges();
        parent_.Logs?.Print($"Updated account accId:{accData.MyAccId}");
        return err;
    }

    //Event raised by SDK
    internal void OnAccountRegState(uint accId, RegState state, string response)
    {
        var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
        accModel?.OnAccountRegState(state, response);
        parent_.Logs?.Print($"OnAccountRegState accId:{accId} state:{state} response:{response}");
    }

    public void StoreToJson()
    {
        List<JsonDict> jsonList = [];
        foreach(var accModel in collection_) jsonList.Add(accModel.storeToJson());

        SampleWpf.Properties.Settings.Default.accounts = JsonSerializer.Serialize(jsonList);
        SampleWpf.Properties.Settings.Default.Save();
    }
    public void LoadFromJson()
    {
        parent_.Logs?.Print("Loading accounts...");
        string jsonString = SampleWpf.Properties.Settings.Default.accounts;

        if (jsonString.Length != 0)
        {
            collection_.Clear();
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    this.Add(AccountModel.loadFromJson(element), false);
                }
            }
        }
        parent_.Logs?.Print($"Loaded {Collection.Count} accounts");
    }

}//AccountsListModel


/////////////////////////////////////////////////////////////////
/// CallModel
public class CallModel : INotifyPropertyChanged
{
    readonly uint myCallId_;
    readonly string accUri_;     //Account URI used to accept/make this call
    readonly string remoteExt_;  //Phone number(extension) of remote side  
    readonly bool isIncoming_;
    readonly bool hasSecureMedia_;
            
    CallState callState_;
    HoldState holdState_;
    DateTime startTime_;

    string displName_ = "";      //Contact name
    string receivedDtmf_ = "";
    string duration_ = "";
    //string response_ = "";
    bool micMuted_ = false;
    bool camMuted_ = false;
    bool withVideo_;
    bool isFilePlaying_ = false;
    bool isFileRecording_ = false;
    readonly List<uint> playerIds_ = [];        

    public event PropertyChangedEventHandler? PropertyChanged;
    readonly ObjModel parent_;
    
    public CallModel(uint myCallId, string accUri, string remoteExt, 
                    bool isIncoming, bool hasSecureMedia, bool withVideo,
                    ObjModel parent)
    {
        myCallId_   = myCallId;
        accUri_     = accUri;
        remoteExt_  = remoteExt;
        withVideo_  = withVideo;
        isIncoming_ = isIncoming;
        hasSecureMedia_ = hasSecureMedia;

        callState_ = isIncoming ? CallState.Ringing : CallState.Dialing;

        parent_    = parent;

        AcceptCommand   = new RelayCommand(() => Accept(true));
        RejectCommand   = new RelayCommand(() => Reject());
        SwitchToCommand = new RelayCommand(() => SwitchTo());
        HoldCommand     = new RelayCommand(() => Hold());
        MuteMicCommand  = new RelayCommand(() => MuteMic(!micMuted_));
        MuteCamCommand  = new RelayCommand(() => MuteCam(!camMuted_));
        HangupCommand   = new RelayCommand(() => Bye());
    }
    
    public string NameAndExt { get { 
        return (displName_.Length == 0) ? remoteExt_ : $"{displName_} ({remoteExt_})"; } 
    }
   
    public uint   ID                { get { return myCallId_;       } }
    public CallState CallState      { get { return callState_;      } }
    public HoldState HoldState      { get { return holdState_;      } }
    public string Duration          { get { return duration_;       } }
    public bool   HasSecureMedia    { get { return hasSecureMedia_; } }
    public bool   IsIncoming        { get { return isIncoming_;     } }
    public bool   IsSwitchedCall    { get { return parent_.Calls.IsSwitchedCall(ID); } }
    public bool   IsWaiting         { get { return (callState_ != CallState.Connected) &&(callState_ != CallState.Held); } }
    public bool   IsLocalHold       { get { return (holdState_ == HoldState.Local) || (holdState_ == HoldState.LocalAndRemote); } }
    public bool   IsConnected       { get { return (callState_ == CallState.Connected); } }
    public bool   IsRinging         { get { return (callState_ == CallState.Ringing);   } }
    public bool   IsFilePlaying     { get { return isFilePlaying_; } }
    public bool   IsFileRecording   { get { return isFileRecording_; } }        
    public bool   WithVideo         { get { return withVideo_;      } }
    public bool   IsMicMuted        { get { return micMuted_;       } }
    public bool   IsCamMuted        { get { return camMuted_;       } }
    public string AccUri            { get { return accUri_;         } }
    public string ReceivedDtmf      { get { return receivedDtmf_;   } }

    public ICommand AcceptCommand   { get; private set; }
    public ICommand RejectCommand   { get; private set; }
    public ICommand SwitchToCommand { get; private set; }
    public ICommand HoldCommand     { get; private set; }
    public ICommand MuteMicCommand  { get; private set; }
    public ICommand MuteCamCommand { get; private set; }
    public ICommand HangupCommand   { get; private set; }

    public bool CanAccept           { get { return callState_ == CallState.Ringing; } }
    public bool CanReject           { get { return callState_ == CallState.Ringing; } }        
    public bool CanHold             { get { return (callState_ == CallState.Connected) || (callState_ == CallState.Held); } }
    public bool CanMuteMic          { get { return callState_ == CallState.Connected; } }
    public bool CanMuteCam          { get { return (callState_ == CallState.Connected) && withVideo_ ; } }
    public bool CanHangup           { get { return callState_ != CallState.Ringing; }}
    public bool CanSwitchTo         { get { return !IsSwitchedCall;  } }

    public void setDisplName(string name) { displName_ = name;  NotifyPropertyChanged(nameof(NameAndExt)); }
    void setMicMuted(bool muted)          { micMuted_  = muted; NotifyPropertyChanged(nameof(IsMicMuted)); }
    void setCamMuted(bool muted)          { camMuted_  = muted; NotifyPropertyChanged(nameof(IsCamMuted)); }
    void setWithVideo(bool video)         { withVideo_ = video; NotifyPropertyChanged(nameof(WithVideo)); }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void setCallState(CallState newState)
    {
        callState_ = newState;
        NotifyPropertyChanged(nameof(CallState));
        NotifyPropertyChanged(nameof(IsWaiting));
        NotifyPropertyChanged(nameof(IsConnected));
        NotifyPropertyChanged(nameof(IsRinging));
        NotifyPropertyChanged(nameof(CanAccept));
        NotifyPropertyChanged(nameof(CanReject));
        NotifyPropertyChanged(nameof(CanHold));
        NotifyPropertyChanged(nameof(CanMuteMic));
        NotifyPropertyChanged(nameof(CanMuteCam));
    }

    private void setHoldState(HoldState holdState)
    {
        holdState_ = holdState;
        NotifyPropertyChanged(nameof(HoldState));
        NotifyPropertyChanged(nameof(IsLocalHold));
    }

    public bool Equals(CallModel? other) { 
        return (this.myCallId_ == other?.myCallId_); 
    }
    
    public void CalcDuration()
    {
        if (callState_ != CallState.Connected) return;

        TimeSpan span = (DateTime.Now - startTime_);
        duration_ = (span.Hours !=0) ? string.Format("{0}:{1:D2}:{2:D2}", span.Hours, span.Minutes, span.Seconds)
                                     : string.Format("{0:D2}:{1:D2}", span.Minutes, span.Seconds);
        NotifyPropertyChanged(nameof(Duration));
    }

    public int SwitchTo()
    {
        return parent_.Calls.SwitchTo(myCallId_);
    }

    public int Bye()
    {
        parent_.Logs?.Print($"Ending callId:{myCallId_}");
        int err = parent_.Core.Call_Bye(myCallId_);
        if (err == ErrorCode.kNoErr) setCallState(CallState.Disconnecting);
        else  parent_.Logs?.Print($"Cant Bye callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;
    }

    public int Accept(bool withVideo)
    {
        parent_.Logs?.Print($"Accepting callId:{myCallId_}");
        int err = parent_.Core.Call_Accept(myCallId_, withVideo);
        if (err == ErrorCode.kNoErr) setCallState(CallState.Accepting);
        else parent_.Logs?.Print($"Cant Accept callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;
    }

    public int Reject()
    {
        parent_.Logs?.Print($"Rejecting callId:{myCallId_}");
        int err = parent_.Core.Call_Reject(myCallId_);//Send '486 Busy now'
        if (err == ErrorCode.kNoErr) setCallState(CallState.Rejecting);
        else parent_.Logs?.Print($"Cant Reject callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;
    }

    public int MuteMic(bool mute)
    {
        parent_.Logs?.Print($"Set mic mute={mute} of call {myCallId_}");
        int err = parent_.Core.Call_MuteMic(myCallId_, mute);
        if (err == ErrorCode.kNoErr) setMicMuted(mute);
        else parent_.Logs?.Print($"Cant MuteMic callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;
    }

    public int MuteCam(bool mute)
    {
        parent_.Logs?.Print($"Set camera mute={mute} of call {myCallId_}");
        int err = parent_.Core.Call_MuteCam(myCallId_, mute);
        if (err == ErrorCode.kNoErr) setCamMuted(mute);
        else parent_.Logs?.Print($"Cant MuteCam callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;
    }

    public int SendDtmf(string tone)
    {
        parent_.Logs?.Print($"Sending dtmf callId:{myCallId_} tone:{tone}");
        int err = parent_.Core.Call_SendDtmf(myCallId_, tone, 200, 50, DtmfMethod.DTMF_RTP);
        if (err != ErrorCode.kNoErr) 
           parent_.Logs?.Print($"Cant SendDtmf callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;        
    }

    public int PlayFile(string pathToMp3File, bool loop)
    {
        parent_.Logs?.Print($"Starting play file callId:{myCallId_} {pathToMp3File}");
        uint playerId = 0;
        int err = parent_.Core.Call_PlayFile(myCallId_, pathToMp3File, loop, ref playerId);
        if (err != ErrorCode.kNoErr)
            parent_.Logs?.Print($"Cant PlayFile callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        else
            playerIds_.Add(playerId);
        return err;
    }

    public int StopPlayFile()
    {
        int retErr = ErrorCode.kNoErr;
        foreach(var playerId in playerIds_)
        {
            parent_.Logs?.Print($"Stop play file in callId:{myCallId_} playerId:{playerId}");
            int err = parent_.Core.Call_StopFile(playerId);
            if (err != ErrorCode.kNoErr) {
                parent_.Logs?.Print($"Can't StopPlayFile Err:{err} {parent_.ErrorText(err)}");
                retErr = err;
            }
        }
        return retErr;
    }

    public int RecordFile(string path)
    {
        parent_.Logs?.Print($"Start record file for callId:{myCallId_} path:{path}");
        int err = parent_.Core.Call_RecordFile(myCallId_, path);
        if (err != ErrorCode.kNoErr)
            parent_.Logs?.Print($"Cant StartRecording Err:{err} {parent_.ErrorText(err)}");
        else
            isFileRecording_ = true;
        NotifyPropertyChanged(nameof(IsFileRecording));
        return err;
    }

    public int StopRecordFile()
    {
        parent_.Logs?.Print($"Stop record file for callId:{myCallId_}");
        int err = parent_.Core.Call_StopRecordFile(myCallId_);
        if (err != ErrorCode.kNoErr)
            parent_.Logs?.Print($"Cant StopRecording Err:{err} {parent_.ErrorText(err)}");
        else
            isFileRecording_ = false;
        NotifyPropertyChanged(nameof(IsFileRecording));
        return err;
    }

    public int Hold()
    {
        parent_.Logs?.Print($"Hold callId:{myCallId_}");
        int err = parent_.Core.Call_Hold(myCallId_);
        if (err == ErrorCode.kNoErr) setCallState(CallState.Holding);
        else parent_.Logs?.Print($"Cant MuteMic callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;
    }

    public int TransferBlind(string toExt)
    {
        parent_.Logs?.Print($"Transfer blind callId:{myCallId_} to:{toExt}");
        if (toExt.Length==0) return -1;

        int err = parent_.Core.Call_TransferBlind(myCallId_, toExt);
        if (err == ErrorCode.kNoErr) setCallState(CallState.Transferring);
        else parent_.Logs?.Print($"Cant TransferBlind callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;
    }

    public int TransferAttended(uint toCallId)
    {
        parent_.Logs?.Print($"Transfer attended callId:{myCallId_} to callId:{toCallId}");
        int err = parent_.Core.Call_TransferAttended(myCallId_, toCallId);
        if (err == ErrorCode.kNoErr) setCallState(CallState.Transferring);
        else parent_.Logs?.Print($"Cant TransferAttended callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
        return err;            
    }
    public int SetVideoWindow(IntPtr hwnd)
    {
        parent_.Logs?.Print($"SetVideoWindow callId:{myCallId_} hwnd:{hwnd}");
        return parent_.Core.Call_SetVideoWindow(myCallId_, hwnd);
    }

    public string GetSipHeader(string hdrName)
    {
        return parent_.Core.Call_GetSipHeader(myCallId_, hdrName);
    }

    //Events raised by SDK
    internal void OnCallProceeding(string response)
    {
        //response_ = response;
        setCallState(CallState.Proceeding);
    }

    internal void OnCallConnected(string hdrFrom, string hdrTo, bool withVideo)
    {
        startTime_ = DateTime.Now;
        setWithVideo(withVideo);
        setCallState(CallState.Connected);
    }

    internal void OnCallTransferred(uint statusCode)
    {
        setCallState(CallState.Connected);
    }

    internal void OnCallDtmfReceived(ushort tone)
    {
        if(tone == 10) { receivedDtmf_ += '*'; }else
        if(tone == 11) { receivedDtmf_ += '#'; }
        else           { receivedDtmf_ += tone.ToString(); }
        NotifyPropertyChanged(nameof(ReceivedDtmf));
    }

    internal void OnCallHeld(HoldState holdState)
    {
        setHoldState(holdState);
        setCallState((holdState_ == HoldState.None) ? CallState.Connected : CallState.Held);            
    }

    internal void OnCallSwitched()
    {
        NotifyPropertyChanged(nameof(IsSwitchedCall));
        NotifyPropertyChanged(nameof(CanSwitchTo));
    }

    internal void OnPlayerState(uint playerId, PlayerState state)
    {
        if (!playerIds_.Contains(playerId)) return;
        
        if((state == PlayerState.PlayerStopped) || (state == PlayerState.PlayerFailed))
            playerIds_.Remove(playerId);

        bool prevPlayingState = isFilePlaying_;
        isFilePlaying_ = playerIds_.Count > 0;

        if(prevPlayingState != isFilePlaying_)
            NotifyPropertyChanged(nameof(IsFilePlaying));
    }

}//CallModel


/////////////////////////////////////////////////////////////////
/// CallsListModel

public class CallsListModel(ObjModel parent_) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    readonly ObservableCollection<CallModel> collection_ = [];

    CallModel? switchedCall_;
    uint lastIncomingCallId_ = ErrorCode.kInvalidId;
    bool confModeStarted_ = false;

    public ObservableCollection<CallModel> Collection { get { return collection_; } }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public delegate void CallTerminatedAppHandler(uint callId, uint statusCode);
    public event CallTerminatedAppHandler? CallTerminated;

    public bool IsSwitchedCall(uint callId)
    {
        return switchedCall_ != null && (switchedCall_.ID == callId);
    }

    public uint LastIncomingCallId() { return lastIncomingCallId_; }

    public CallModel? SwitchedCall
    {
        get { return switchedCall_; }
        private set { switchedCall_ = value; NotifyPropertyChanged(); }
    }

    public bool ConfModeStarted
    {
        get { return confModeStarted_; }
        private set { confModeStarted_ = value; NotifyPropertyChanged(); }
    }

    public void CalcDuration()
    {
        foreach(var c in collection_) c.CalcDuration();
    }

    public int Invite(DestData dest)
    {
        parent_.Logs?.Print($"Trying to invite {dest.ToExt} from account:{dest.FromAccId}");

        int err = parent_.Core.Call_Invite(dest);
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't invite Err: {err} {parent_.ErrorText(err)}");
            return err;
        }

        string accUri       = parent_.Accounts.GetUri(dest.FromAccId);
        bool hasSecureMedia = parent_.Accounts.HasSecureMedia(dest.FromAccId);

        CallModel newCall = new(dest.MyCallId, accUri, dest.ToExt, false, hasSecureMedia, dest.WithVideo, parent_);
        collection_.Add(newCall);
        parent_.Cdrs.Add(newCall);
        
        parent_.PostResolveContactName(newCall);
        return err;
    }

    public int SwitchTo(uint callId)
    {
        parent_.Logs?.Print($"Switching mixer to callId:{callId}");

        int err = parent_.Core.Mixer_SwitchToCall(callId);
        if (err == ErrorCode.kNoErr) ConfModeStarted = false;
        else parent_.Logs?.Print($"Cant switch to callId:{callId} Err:{err} {parent_.ErrorText(err)}");
        //Value '_switchedCallId' will set in the callback 'onSwitched'
        return err;
    }

    public int MakeConference()
    {
        //TODO
        if(confModeStarted_) {
            uint callId = (switchedCall_!=null) ? switchedCall_.ID : 0;
            parent_.Logs?.Print($"Ending conference, switch mixer to callId: {callId}");
            int err = parent_.Core.Mixer_SwitchToCall(callId);
            ConfModeStarted = false;
            return err;
        }
        else {
            parent_.Logs?.Print("Joining all calls to conference");
            int err = parent_.Core.Mixer_MakeConference();
            if (err == ErrorCode.kNoErr) ConfModeStarted = true;
            else parent_.Logs?.Print($"Cant make conference Err:{err} {parent_.ErrorText(err)}");
            return err;
        }
    }

    public bool CanMakeConference { get { return ConfModeStarted || collection_.Count > 1; } }

    public int SetPreviowVideoWindow(IntPtr hwnd)
    {
        parent_.Logs?.Print($"SetPreviowVideoWindow hwnd:{hwnd}");
        return parent_.Core.Call_SetVideoWindow(ErrorCode.kInvalidId, hwnd);
    }

    bool hasConnectedFewCalls()
    {
        int counter = 0;
        foreach(var m in collection_)
            counter += m.IsConnected ? 1 : 0;

        return counter > 1;
    }

    void setLastIncomingCallId(uint id) { lastIncomingCallId_ = id; NotifyPropertyChanged(nameof(LastIncomingCallId)); }


    //Events raised by SDK
    internal void OnCallIncoming(uint callId, uint accId, bool withVideo, string hdrFrom, string hdrTo)
    {
        parent_.Logs?.Print($"onIncoming callId:{callId} accId:{accId} from:{hdrFrom} to:{hdrTo}");

        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
        if (callModel != null) return;//Call already exists, skip

        string accUri       = parent_.Accounts.GetUri(accId);
        bool hasSecureMedia = parent_.Accounts.HasSecureMedia(accId);

        CallModel newCall = new(callId, accUri, parseExt(hdrFrom), isIncoming:true, hasSecureMedia, withVideo, parent_);
        newCall.setDisplName(parseDisplayName(hdrFrom));
        collection_.Add(newCall);

        setLastIncomingCallId(callId);

        SwitchedCall ??= newCall;//Set new value only when current one is null

        parent_.Cdrs.Add(newCall);
        //_postResolveContactName(newCall); //TODO add '_postResolveContactName'
    }

    internal void OnCallConnected(uint callId, string hdrFrom, string hdrTo, bool withVideo)
    {
        //string nonce = parent_.Core.Call_GetNonce(callId);//Get nonce received from server during last auth
        parent_.Logs?.Print($"onConnected callId:{callId} from:{hdrFrom} to:{hdrTo}");
        parent_.Cdrs.SetConnected(callId, hdrFrom, hdrTo, withVideo);

        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
        callModel?.OnCallConnected(hdrFrom, hdrTo, withVideo);
    }

    internal void OnCallTerminated(uint callId, uint statusCode)
    {
        parent_.Logs?.Print($"onTerminated callId:{callId} statusCode:{statusCode}");

        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
        if (callModel != null)
        {
            parent_.Cdrs.SetTerminated(callId, statusCode, callModel.Duration);

            collection_.Remove(callModel);

            if (ConfModeStarted && !hasConnectedFewCalls())
                ConfModeStarted = false;
        }
        
        parent_.PostAction(new Action(() => {
            CallTerminated?.Invoke(callId, statusCode);
        }));
    }

    internal void OnCallProceeding(uint callId, string response)
    {
        parent_.Logs?.Print($"onProceeding callId:{callId} response:{response}");
        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
        callModel?.OnCallProceeding(response);
    }

    internal void OnCallTransferred(uint callId, uint statusCode)
    {
        parent_.Logs?.Print($"onTransferred callId:{callId} statusCode:{statusCode}");
        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
        callModel?.OnCallTransferred(statusCode);
    }

    internal void OnCallRedirected(uint origCallId, uint relatedCallId, string referTo)
    {
       parent_.Logs?.Print($"onRedirected origCallId:{origCallId} relatedCallId:{relatedCallId} to:{referTo}");

       //Find 'origCallId'
       var origCall = collection_.Where(a => a.ID == origCallId).FirstOrDefault();
       if (origCall == null) return;

       //Clone 'origCallId' and add to collection of calls as related one           
       CallModel relatedCall = new(relatedCallId, origCall.AccUri, parseExt(referTo), isIncoming:false, 
                                   origCall.HasSecureMedia, origCall.WithVideo, parent_);
       collection_.Add(relatedCall);
    }

    internal void OnCallDtmfReceived(uint callId, ushort tone)
    {
        parent_.Logs?.Print($"onDtmfReceived callId:{callId} tone:{tone}");

        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
        callModel?.OnCallDtmfReceived(tone);
    }

    internal void OnCallHeld(uint callId, HoldState state)
    {
        parent_.Logs?.Print($"onHeld callId:{callId} {state}");

        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
        callModel?.OnCallHeld(state);
    }

    internal void OnCallSwitched(uint callId)
    {
        parent_.Logs?.Print($"onSwitched callId:{callId}");

        var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();            
        SwitchedCall = callModel;

        foreach (var c in collection_) c.OnCallSwitched();
    }

    internal void OnPlayerState(uint playerId, PlayerState state)
    {
        parent_.Logs?.Print($"onPlayerState playerId:{playerId} state:{state}");

        foreach (var c in collection_) c.OnPlayerState(playerId, state);
    }

    public static string parseExt(string uri)
    {
        //URI format: "displName" <sip:EXT@domain:port>
        int startIndex = uri.IndexOf(':');
        if (startIndex == -1) return "";

        int endIndex = uri.IndexOf('@', startIndex + 1);
        return (endIndex == -1) ? "" : uri.Substring(startIndex + 1, endIndex - startIndex - 1);
    }

    static string parseDisplayName(string uri)
    {
        //URI format: "DisplName" <sip:ext@domain:port>
        int startIndex = uri.IndexOf('"');
        if (startIndex == -1) return "";

        int endIndex = uri.IndexOf('"', startIndex + 1);
        return (endIndex == -1) ? "" : uri.Substring(startIndex + 1, endIndex - startIndex - 1);
    }

}//CallsListModel

public enum BLFState { Trying, Proceeding, Early, Terminated, Confirmed, Unknown, SubscriptionDestroyed };

/////////////////////////////////////////////////////////////////
/// SubscriptionModel

public class SubscriptionModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    SubscriptionState internalState_ = SubscriptionState.Created;
    readonly SubscrData subData_ = new();

    public static SubscriptionModel BLF()
    {
        SubscriptionModel blfModel = new();
        blfModel.subData_.MimeSubType = "dialog-info+xml";
        blfModel.subData_.EventType = "dialog";
        return blfModel;
    }

    public SubscrData Data { get { return subData_; } }
    public uint ID { get { return subData_.MySubId; } }
    public string ToExt { get { return subData_.ToExt; } set { subData_.ToExt = value; } }
    public uint AccId { get { return subData_.FromAccId; } set { subData_.FromAccId = value; } }
    public bool IsWaiting { get { return (internalState_ == SubscriptionState.Created); } }
    public bool IsBlinking { get { return (BLFState == BLFState.Early); } }
    public string AccUri { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public BLFState BLFState { get; private set; } = BLFState.Unknown;        

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool Equals(SubscriptionModel? other) { return (this.ID == other?.ID); }

    //Event raised by SDK
    internal void OnSubscriptionState(SubscriptionState state, string resp)
    {
        internalState_ = state;

        //Parse 'response' (contains XML body received in NOTIFY request)
        // and use parsed attributes for UI rendering
        int startIndex = resp.IndexOf("<state");
        if (startIndex != -1)
        {
            startIndex = resp.IndexOf('>', startIndex);
            int endIndex = resp.IndexOf("</state>", startIndex);
            string blfStateStr = resp.Substring(startIndex + 1, endIndex- startIndex-1);
            BLFState = blfStateStr switch
            {
                "trying" => BLFState.Trying,
                "proceeding" => BLFState.Proceeding,
                "early" => BLFState.Early,
                "terminated" => BLFState.Terminated,
                "confirmed" => BLFState.Confirmed,
                _ => BLFState.Unknown,
            };
        }

        if (state == SubscriptionState.Destroyed)
            BLFState = BLFState.SubscriptionDestroyed;

        NotifyPropertyChanged(nameof(BLFState));
        NotifyPropertyChanged(nameof(IsWaiting));
        NotifyPropertyChanged(nameof(IsBlinking));
    }

    public JsonDict storeToJson()
    {
        JsonDict dict = new();
        dict.Add("ToExt",       subData_.ToExt);
        dict.Add("MimeSubType", subData_.MimeSubType);
        dict.Add("EventType",   subData_.EventType);            
        dict.Add("AccUri",      AccUri);
        dict.Add("Label",       Label);

        if (subData_.ExpireTime != null) dict.Add("ExpireTime", subData_.ExpireTime);
        return dict;

    }//storeToJson

    internal static SubscriptionModel loadFromJson(JsonElement elem)
    {
        SubscriptionModel m = new();

        foreach (JsonProperty prop in elem.EnumerateObject())
        {
            bool isString = (prop.Value.ValueKind == JsonValueKind.String);
            string strVal = isString ? prop.Value.GetString()! : "";

            switch (prop.Name)
            {
                case "ToExt":       m.subData_.ToExt       = strVal; break;
                case "MimeSubType": m.subData_.MimeSubType = strVal; break;                    
                case "EventType":   m.subData_.EventType   = strVal; break;
                case "ExpireTime":  m.subData_.ExpireTime  = prop.Value.GetUInt32(); break;
                case "Label":       m.Label  = strVal; break;
                case "AccUri":      m.AccUri = strVal; break;
            }//switch
        }//for

        return m;

    }//loadFromJson

}//SubscriptionModel


/////////////////////////////////////////////////////////////////
/// SubscriptionsListModel
public class SubscriptionsListModel(ObjModel parent_)
{
    readonly ObservableCollection<SubscriptionModel> collection_ = [];

    public ObservableCollection<SubscriptionModel> Collection { get { return collection_; } }

    public int Add(SubscriptionModel subscr, bool saveChanges = true)
    {
        parent_.Logs?.Print($"Adding new subscription ext:{subscr.ToExt} accId:@{subscr.AccId}");

        //When accUri present - model loaded from json, search accId as it might be changed
        if (subscr.AccUri.Length != 0) { subscr.AccId  = parent_.Accounts.GetAccId(subscr.AccUri); }
        else                           { subscr.AccUri = parent_.Accounts.GetUri(subscr.AccId); }

        //Add
        int err = parent_.Core.Subscription_Add(subscr.Data);
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't add subscription Err: {err} {parent_.ErrorText(err)}");
            return err;
        }

        collection_.Add(subscr);

        parent_.Logs?.Print($"Added successfully with id: {subscr.ID}");
        if (saveChanges) parent_.PostSaveSubscriptionChanges();
        return err;
    }

    public int Delete(SubscriptionModel sub)
    {
        int err = parent_.Core.Subscription_Delete(sub.ID);
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't delete subscription Err: {err} {parent_.ErrorText(err)}");
            return err;
        }

        collection_.Remove(sub);

        parent_.PostSaveSubscriptionChanges();

        parent_.Logs?.Print($"Deleted subscription subId:{sub.ID}");
        return err;
    }

    internal void OnSubscriptionState(uint subId, SubscriptionState state, string response)
    {
        var subModel = collection_.Where(a => a.ID == subId).FirstOrDefault();
        subModel?.OnSubscriptionState(state, response);
        parent_.Logs?.Print($"OnSubscriptionState subId:{subId} state:{state} response:{response}");
    }

    public void StoreToJson()
    {
        List<JsonDict> jsonList = [];
        foreach (var subModel in collection_) jsonList.Add(subModel.storeToJson());

        SampleWpf.Properties.Settings.Default.subscriptions = JsonSerializer.Serialize(jsonList);
        SampleWpf.Properties.Settings.Default.Save();
    }

    public void LoadFromJson()
    {
        parent_.Logs?.Print("Loading subscriptions...");
        string jsonString = SampleWpf.Properties.Settings.Default.subscriptions;

        if (jsonString.Length != 0)
        {
            collection_.Clear();
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    this.Add(SubscriptionModel.loadFromJson(element), false);
                }
            }
        }
        parent_.Logs?.Print($"Loaded {Collection.Count} subscription");
    }

}//SubscriptionsListModel

/////////////////////////////////////////////////////////////////
/// MessageModel
public class MessageModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    readonly MsgData msgData_ = new();     

    public MessageModel(MsgData data, string accUri, bool isIncoming = false, bool isWaiting=false)
    {
        msgData_ = data;
        AccUri = accUri;
        IsIncoming = isIncoming;
        IsWaiting = isWaiting;
    }

    public uint ID { get { return msgData_.MyMsgId; } }
    public bool IsIncoming { get; private set; }
    public bool IsWaiting { get; private set; }
    public bool SentSuccess { get; private set; }
    public string ToExt { get { return msgData_.ToExt; }  }
    public uint AccId { get { return msgData_.FromAccId; } set { msgData_.FromAccId = value; } }
    public string AccUri { get; private set; } = string.Empty;
    public string From { get { return IsIncoming ? msgData_.ToExt : AccUri; } }
    public string To { get { return IsIncoming ? AccUri : msgData_.ToExt; } }
    public string Body { get { return msgData_.Body; } }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal void OnMessageSentState(bool success, string response)
    {
        IsWaiting = false;
        SentSuccess = success;
        NotifyPropertyChanged(nameof(IsWaiting));
        NotifyPropertyChanged(nameof(SentSuccess));
    }

    internal JsonDict storeToJson()
    {
        JsonDict dict = new();
        dict.Add("Body", msgData_.Body);
        dict.Add("FromAccId", msgData_.FromAccId);
        dict.Add("ToExt", msgData_.ToExt);
        dict.Add("AccUri", AccUri);
        dict.Add("IsIncoming", IsIncoming);
        dict.Add("SentSuccess", SentSuccess);
        return dict;

    }//storeToJson

    internal static MessageModel loadFromJson(JsonElement elem, uint internalMsgId)
    {
        MsgData msgData = new() { MyMsgId = internalMsgId };
        MessageModel m = new(msgData, "");
        
        foreach (JsonProperty prop in elem.EnumerateObject())
        {
            bool isString = (prop.Value.ValueKind == JsonValueKind.String);
            string strVal = isString ? prop.Value.GetString()! : "";
        
            switch (prop.Name)
            {
                case "Body": m.msgData_.Body = strVal; break;
                case "FromAccId": m.msgData_.FromAccId = prop.Value.GetUInt32(); break;
                case "ToExt": m.msgData_.ToExt = strVal; break;
                case "AccUri": m.AccUri = strVal; break;
                case "IsIncoming": m.IsIncoming = prop.Value.GetBoolean(); break;
                case "SentSuccess": m.SentSuccess = prop.Value.GetBoolean(); break;
            }//switch
        }//for

        return m;
    }

}//MessageModel

/////////////////////////////////////////////////////////////////
/// MessagesListModel
public class MessagesListModel(ObjModel parent_)
{
    readonly ObservableCollection<MessageModel> collection_ = [];
    readonly int kMaxItems = 25;
    uint internalMsgId = 0;

    public ObservableCollection<MessageModel> Collection { get { return collection_; } }

    public int Send(MsgData msg)
    {
        parent_.Logs?.Print($"Sending new message to ext:{msg.ToExt} accId:@{msg.FromAccId}");

        //Add
        int err = parent_.Core.Message_Send(msg);
        if (err != ErrorCode.kNoErr)
        {
            parent_.Logs?.Print($"Can't send message Err: {err} {parent_.ErrorText(err)}");
            return err;
        }

        MessageModel msgModel = new(msg, parent_.Accounts.GetUri(msg.FromAccId), isWaiting:true);
        collection_.Add(msgModel);

        parent_.Logs?.Print($"Posted successfully with id: {msg.MyMsgId}");

        if (collection_.Count > kMaxItems) collection_.RemoveAt(0);
        parent_.PostSaveMessagesChanges();
        return err;
    }

    public int Delete(MessageModel msg)
    {
        if(!collection_.Remove(msg)) return -1;

        parent_.PostSaveMessagesChanges();

        parent_.Logs?.Print($"Deleted message msgId:{msg.ID}");
        return ErrorCode.kNoErr;
    }

    internal void OnMessageSentState(uint messageId, bool success, string response)
    {
        parent_.Logs?.Print($"OnMessageSentState msgId:{messageId} success:{success} response:{response}");
        var msgModel = collection_.Where(a => a.ID == messageId).FirstOrDefault();
        msgModel?.OnMessageSentState(success, response);

        parent_.PostSaveMessagesChanges();
    }
    internal void OnMessageIncoming(uint accId, string hdrFrom, string body)
    {
        parent_.Logs?.Print($"OnMessageIncoming accId:{accId} hdrFrom:{hdrFrom} body:'{body}'");

        MsgData msg = new();
        msg.Body = body;
        msg.FromAccId = accId;            
        msg.ToExt = CallsListModel.parseExt(hdrFrom);
        msg.MyMsgId = getInternalMsgId();

        MessageModel msgModel = new(msg, parent_.Accounts.GetUri(accId), isIncoming:true);
        collection_.Add(msgModel);

        if (collection_.Count > kMaxItems) collection_.RemoveAt(0);
        parent_.PostSaveMessagesChanges();
    }

    private uint getInternalMsgId() { return --internalMsgId; }

    public void StoreToJson()
    {
        List<JsonDict> jsonList = [];
        foreach (var msgModel in collection_) jsonList.Add(msgModel.storeToJson());

        SampleWpf.Properties.Settings.Default.messages = JsonSerializer.Serialize(jsonList);
        SampleWpf.Properties.Settings.Default.Save();
    }
    public void LoadFromJson()
    {
        string jsonString = SampleWpf.Properties.Settings.Default.messages;
        if (jsonString.Length != 0)
        {
            collection_.Clear();
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    MessageModel msgModel = MessageModel.loadFromJson(element, getInternalMsgId());
                    msgModel.AccId = parent_.Accounts.GetAccId(msgModel.AccUri);//get accountId by saved URI
                    collection_.Add(msgModel);
                }
            }
            parent_.Logs?.Print($"Loaded {Collection.Count} messages");
        }
    }

}//MessagesListModel


public enum CdrState { IncomingConnected, IncomingMissed, OutgoingConnected, OutgoingMissed };

/////////////////////////////////////////////////////////////////
/// CDR = CallDetailRecord model (contains attributes of recent call, serializes them to/from json)
public class CdrModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public CdrModel() { }
    public CdrModel(CallModel c)
    {
        ID = c.ID;
        AccUri = c.AccUri;
        RemoteExt = c.NameAndExt;
        IsIncoming = c.IsIncoming;
        WithVideo = c.WithVideo;
    }

    public uint ID { get; private set; }/// Id if the CallModel, generated this record
    public string RemoteExt { get; private set; } = "";     /// Phone number(extension) of remote side
    public string AccUri { get; private set; } = "";/// Account URI
    public string Duration { get; private set; } = "-";/// Duration of the call
    public bool WithVideo { get; private set; } /// Has call video
    public bool IsIncoming { get; private set; }/// Was call incoming
    public bool IsConnected { get; private set; }/// Was call connected
    public string DisplName { get; private set; } = "";
    public uint StatusCode { get; private set; } /// Status code assigned when call ended
    public DateTime MadeAt { get; private set; } = DateTime.Now; /// DateTime when call has been initiated/received

    public string MadeAtDate { get { return MadeAt.ToString("MMM dd, HH:mm tt"); } }  /// Formatted MadeAt

    public CdrState State
    {
        get {
            if (IsIncoming) return IsConnected ? CdrState.IncomingConnected : CdrState.IncomingMissed;                                       
            else            return IsConnected ? CdrState.OutgoingConnected : CdrState.OutgoingMissed;          
        }
    }

    internal JsonDict storeToJson()
    {
        JsonDict dict = new();
        dict.Add("AccUri",     AccUri);
        dict.Add("RemoteExt",  RemoteExt);
        dict.Add("StatusCode", StatusCode);
        dict.Add("IsIncoming", IsIncoming);
        dict.Add("IsConnected",IsConnected);
        dict.Add("Duration",   Duration);
        dict.Add("MadeAt",     MadeAt.ToBinary());
        dict.Add("WithVideo",  WithVideo);
        return dict;
    }

    internal static CdrModel loadFromJson(JsonElement elem)
    {
        CdrModel c = new();

        foreach (JsonProperty prop in elem.EnumerateObject())
        {
            bool isString = (prop.Value.ValueKind == JsonValueKind.String);
            string strVal = isString ? prop.Value.GetString()! : "";

            switch (prop.Name)
            {
                case "AccUri":      c.AccUri = strVal; break;
                case "RemoteExt":   c.RemoteExt = strVal; break;
                case "StatusCode":  c.StatusCode = prop.Value.GetUInt32(); break;
                case "IsIncoming":  c.IsIncoming = prop.Value.GetBoolean(); break;
                case "IsConnected": c.IsConnected = prop.Value.GetBoolean(); break;
                case "Duration":    c.Duration = strVal; break;
                case "MadeAt":      c.MadeAt = DateTime.FromBinary(prop.Value.GetInt64()); break;
            }
        }
        return c;
    }
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void SetConnected(bool withVideo)
    {
        WithVideo = withVideo;
        IsConnected = true;
        NotifyPropertyChanged(nameof(WithVideo));
        NotifyPropertyChanged(nameof(IsConnected));
    }

    public void SetTerminated(uint statusCode, string duration)
    {
        //DisplName = displName;
        StatusCode = statusCode;
        Duration = duration;
        //NotifyPropertyChanged(nameof(DisplName));
        NotifyPropertyChanged(nameof(StatusCode));
        NotifyPropertyChanged(nameof(Duration));
    }

}//CdrModel

/////////////////////////////////////////////////////////////////
/// CDRs list model (contains list of recent calls, methods for managing them)
public class CdrsListModel(ObjModel parent_, int maxItems = 10)
{
    readonly ObservableCollection<CdrModel> collection_ = [];
    readonly int kMaxItems = maxItems;

    public ObservableCollection<CdrModel> Collection { get { return collection_; } }

    /// Add new recent call item based on specified CallModel
    public void Add(CallModel c)
    {
        CdrModel cdr = new(c);
        collection_.Insert(0, cdr);

        if ((kMaxItems > 0) && (collection_.Count > kMaxItems))
        {
            collection_.RemoveAt(collection_.Count-1);
        }
    }

    /// Set 'connected' and other attributes of the recent call item specified by callId
    public void SetConnected(uint callId, string from, string to, bool withVideo)
    {
        CdrModel? cdr = collection_.Where((c) => c.ID == callId).FirstOrDefault();
        cdr?.SetConnected(withVideo);
    }

    /// Set 'terminated' and other attributes of the recent call item specified by callId
    public void SetTerminated(uint callId, uint statusCode, string duration)
    {
        CdrModel? cdr = collection_.Where((c) => c.ID == callId).FirstOrDefault();
        cdr?.SetTerminated(statusCode, duration);
        parent_.PostSaveCdrChanges();
    }

    

    /// Remote recent call item by its index in the list
    public void Delete(CdrModel item)
    {
        if (collection_.Remove(item))
            parent_.PostSaveCdrChanges();
    }

    public void LoadFromJson()
    {
        parent_.Logs?.Print("Loading cdrs...");
        string jsonString = SampleWpf.Properties.Settings.Default.cdrs;

        if (jsonString.Length!=0)
        {
            collection_.Clear();
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    collection_.Add(CdrModel.loadFromJson(element));
                }
            }
            parent_.Logs?.Print($"Loaded {Collection.Count} cdrs");
        }
    }

    public void StoreToJson()
    {
        List<JsonDict> jsonList = [];
        foreach (var cdrModel in collection_) jsonList.Add(cdrModel.storeToJson());

        SampleWpf.Properties.Settings.Default.cdrs = JsonSerializer.Serialize(jsonList);
        SampleWpf.Properties.Settings.Default.Save();
    }

}//CdrsModel


/////////////////////////////////////////////////////////////////
/// DevicesModel

public class Device
{
    public Device(uint i, string n, string g) { Index = i;  Name = n; Guid = g; }
    public uint Index { get; private set; } = 0;
    public string Name { get; private set; } = "";
    public string Guid { get; private set; } = "";
}

public class DevicesModel(ObjModel parent_) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    readonly ObservableCollection<Device> playback_ = [];
    readonly ObservableCollection<Device> record_ = [];
    readonly ObservableCollection<Device> video_ = [];
    uint selPlaybackIndex_=0;
    uint selRecordIndex_ = 0;
    uint selVideoIndex_ = 0;
    public ObservableCollection<Device> Playback { get { return playback_; } }
    public ObservableCollection<Device> Record { get { return record_; } }
    public ObservableCollection<Device> Video { get { return video_; } }

    public uint SelectedPlayback { 
        get { return selPlaybackIndex_; }
        set { selPlaybackIndex_ = value; parent_.Core.Dvc_SetPlayoutDevice(value); }
    }
    public uint SelectedRecord {
        get { return selRecordIndex_; }
        set { selRecordIndex_ = value; parent_.Core.Dvc_SetRecordingDevice(value); } 
    }
    public uint SelectedVideo {
        get { return selVideoIndex_; }
        set { selVideoIndex_ = value; parent_.Core.Dvc_SetVideoDevice(value); }
    }

    public void Load()
    {
        LoadPlayback();
        LoadRecord();
        LoadVideo();
    }

    internal void LoadPlayback()
    {
        playback_.Clear();
        string guid="";
        uint playbackCount = parent_.Core.Dvc_GetPlayoutDevices();
        for(uint i=0; i < playbackCount; ++i)
        {
            string name = parent_.Core.Dvc_GetPlayoutDevice(i, ref guid);
            playback_.Add(new Device(i, name, guid));
        }
    }

    internal void LoadRecord()
    {
        record_.Clear(); 
        string guid = "";
        uint recordCount = parent_.Core.Dvc_GetRecordingDevices();
        for (uint i = 0; i < recordCount; ++i)
        {
            string name = parent_.Core.Dvc_GetRecordingDevice(i, ref guid);
            record_.Add(new Device(i, name, guid));
        }
    }

    internal void LoadVideo()
    {
        video_.Clear();
        string guid = "";
        uint videoCount = parent_.Core.Dvc_GetVideoDevices();
        for (uint i = 0; i < videoCount; ++i)
        {
            string name = parent_.Core.Dvc_GetVideoDevice(i, ref guid);
            video_.Add(new Device(i, name, guid));
        }
    }
    //Event raised by SDK
    internal void OnDevicesAudioChanged()
    {
        LoadPlayback();
        LoadRecord();
        parent_.Logs?.Print($"onDevicesAudioChanged");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Playback)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Record)));
    }

}//DevicesModel


/////////////////////////////////////////////////////////////////
/// NetworkModel

public class NetworkModel(ObjModel parent_) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool NetworkLost { get; private set; }

    //Event raised by SDK
    internal void OnNetworkStateChanged(string name, NetworkState state)
    {
        parent_.Logs?.Print($"onNetworkStateChanged name:{name} {state}");
        NetworkLost = (state == NetworkState.NetworkLost);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NetworkLost)));
    }

}//NetworkModel


/////////////////////////////////////////////////////////////////
/// LogsModel
public class LogsModel : INotifyPropertyChanged
{
    string logText_ = "";        
    public event PropertyChangedEventHandler? PropertyChanged;

    public string LogText { get { return logText_; } }

    public void Print(string text)
    {
        this.logText_ += DateTime.Now.ToString("HH:mm:ss ");
        this.logText_ += text;            
        this.logText_ += System.Environment.NewLine;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogText)));
    }

    internal void OnTrialModeNotified()
    {
        Print("--- SIPRIX SDK is working in TRIAL mode ---");
    }

}//LogsModel


/////////////////////////////////////////////////////////////////
/// ObjModel

public class ObjModel
{
    readonly AccountsListModel accountsListModel_;
    readonly SubscriptionsListModel subscrListModel_;
    readonly MessagesListModel messagesListModel_;
    readonly CallsListModel callsListModel_;
    readonly CdrsListModel cdrsListModel_;
    readonly NetworkModel networkModel_;
    readonly DevicesModel devicesModel_;
    readonly LogsModel? logsModel_;

    readonly Siprix.CoreService core_;
    private EventsHandler? eventHandler_;

    public ObjModel()
    {
        //Create core service
        core_ = new();

        //Create models
        logsModel_ = new LogsModel();
        subscrListModel_ = new SubscriptionsListModel(this);
        accountsListModel_ = new AccountsListModel(this);
        messagesListModel_ = new MessagesListModel(this);
        callsListModel_ = new CallsListModel(this);
        cdrsListModel_ = new CdrsListModel(this);
        networkModel_ = new NetworkModel(this);
        devicesModel_ = new DevicesModel(this);
    }

    public SubscriptionsListModel Subscriptions { get { return subscrListModel_; } }
    public MessagesListModel Messages { get { return messagesListModel_; } }
    public AccountsListModel Accounts { get { return accountsListModel_; } }
    public CallsListModel Calls    { get { return callsListModel_; } } 
    public CdrsListModel Cdrs { get { return cdrsListModel_; } }
    public NetworkModel Networks { get { return networkModel_; } }
    public DevicesModel Devices { get { return devicesModel_; } }
    public LogsModel? Logs    { get { return logsModel_; } }
    public Siprix.CoreService Core { get { return core_; } }
    public string ErrorText(int err) { return core_.ErrorText(err);  }

    public int Initialize(AppDispatcher dispatcher)
    {
        if (core_.IsInitialized())
            return ErrorCode.kAlreadyInitialized;

        eventHandler_ = new EventsHandler(this, dispatcher);

        IniData iniData = new();
        iniData.License = "...license-credentials...";
        iniData.SingleCallMode = false;
        iniData.LogLevelIde = LogLevel.Debug;
        iniData.LogLevelFile = LogLevel.Debug;
        iniData.WriteDmpUnhandledExc = true;

        int err = core_.Initialize(eventHandler_, iniData);

        if (err != Siprix.ErrorCode.kNoErr) {
            Logs?.Print($"Can't initialize Siprix module Err: {err} {ErrorText(err)}");
            return err;
        }

        Logs?.Print("Siprix module initialized successfully");
        Logs?.Print($"Version: {core_.Version()}");

        //Configure video
        //VideoData vdoData = new();
        //vdoData.noCameraImgPath = "noCamera.jpg";//path to image, which SDK will send to remote side instead of camera
        //vdoData.bitrateKbps = 800;
        //vdoData.width = 640;
        //vdoData.height = 480;
        //core_.Dvc_SetVideoParams(vdoData);

        //Load saved models
        accountsListModel_.LoadFromJson();
        messagesListModel_.LoadFromJson();
        subscrListModel_.LoadFromJson();
        cdrsListModel_.LoadFromJson();
        return err;
    }

    public void UnInitialize()
    {
        int err = core_.UnInitialize();
        if (err == ErrorCode.kNoErr)
            Logs?.Print("Siprix module uninitialized");
        else
            Logs?.Print($"Can't uninitialize Siprix module Err: {err} {ErrorText(err)}");
    }

    internal void PostSaveAccountsChanges()
    {
        eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => { accountsListModel_.StoreToJson(); }));
    }

    internal void PostSaveSubscriptionChanges()
    {
        eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => { subscrListModel_.StoreToJson(); }));
    }

    internal void PostSaveMessagesChanges()
    {
        eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => { messagesListModel_.StoreToJson(); }));
    }

    internal void PostSaveCdrChanges() 
    {
        eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => { cdrsListModel_.StoreToJson(); })); 
    }

    internal void PostResolveContactName(CallModel newCall)
    {
        eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => {
            //string str = newCall.NameAndExt;
            //TODO add 'ResolveContactName'
            //newCall.setDisplName
        }));
    }

    internal void PostAction(Action action)
    {
        eventHandler_?.dispatcher_?.BeginInvoke(action);
    }

    //Events raised by SDK
    class EventsHandler(ObjModel parent_, AppDispatcher dispatcher) : Siprix.IEventDelegate
    {
        readonly public AppDispatcher dispatcher_ = dispatcher;

        public void OnTrialModeNotified()
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.logsModel_?.OnTrialModeNotified();
            }));
        }

        public void OnDevicesAudioChanged()
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.devicesModel_.OnDevicesAudioChanged();
            }));
        }

        public void OnAccountRegState(uint accId, RegState state, string response)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.accountsListModel_.OnAccountRegState(accId, state, response);
            }));
        }

        public void OnSubscriptionState(uint subId, SubscriptionState state, string response)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.subscrListModel_.OnSubscriptionState(subId, state, response);
            }));
        }

        public void OnNetworkState(string name, NetworkState state)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.networkModel_.OnNetworkStateChanged(name, state);
            }));
        }

        public void OnPlayerState(uint playerId, PlayerState state)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnPlayerState(playerId, state);
            }));
        }

        public void OnRingerState(bool start)
        {
        }

        public void OnCallIncoming(uint callId, uint accId, bool withVideo, string hdrFrom, string hdrTo)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallIncoming(callId, accId, withVideo, hdrFrom, hdrTo);
            }));
        }

        public void OnCallConnected(uint callId, string hdrFrom, string hdrTo, bool withVideo)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallConnected(callId, hdrFrom, hdrTo, withVideo);
            }));
        }

        public void OnCallTerminated(uint callId, uint statusCode)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallTerminated(callId, statusCode);
            }));
        }

        public void OnCallProceeding(uint callId, string response)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallProceeding(callId, response);
            }));
        }

        public void OnCallTransferred(uint callId, uint statusCode)
        {
            dispatcher_?.BeginInvoke(new Action(() =>{
                parent_.callsListModel_.OnCallTransferred(callId, statusCode);
            }));
        }

        public void OnCallRedirected(uint origCallId, uint relatedCallId, string referTo)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallRedirected(origCallId, relatedCallId, referTo);
            }));
        }

        public void OnCallDtmfReceived(uint callId, ushort tone)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallDtmfReceived(callId, tone);
            }));
        }

        public void OnCallHeld(uint callId, HoldState state)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallHeld(callId, state);
            }));
        }

        public void OnCallSwitched(uint callId)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.callsListModel_.OnCallSwitched(callId);
            }));
        }

        public void OnMessageSentState(uint messageId, bool success, string response)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.messagesListModel_.OnMessageSentState(messageId, success, response);
            }));
        }

        public void OnMessageIncoming(uint accId, string hdrFrom, string body)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                parent_.messagesListModel_.OnMessageIncoming(accId, hdrFrom, body);
            }));
        }
    }

}//ObjModel


/// A command whose sole purpose is to relay its functionality to other
/// objects by invoking delegates.
public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{   
    readonly Func<bool>? canExecute_ = canExecute;
    readonly Action execute_ = execute;

    [DebuggerStepThrough]
    public bool CanExecute(object? parameter)
    {
        return (canExecute_ == null) || canExecute_();
    }

    public event EventHandler? CanExecuteChanged
    {
#if WPF_PROJECT
        add{
            if (canExecute_ != null)
                CommandManager.RequerySuggested += value;
        }
        remove { 
            if (canExecute_ != null)
                CommandManager.RequerySuggested -= value;
        }
#else
        add{
        }
        remove{
        }
#endif
    }

    public void Execute(object? parameter)
    {
        execute_();
    }

}//RelayCommand
