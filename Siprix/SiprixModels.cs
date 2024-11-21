#define WPF_PROJECT
using Siprix;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Text.Json;
using System.Linq;
using System.Windows;


namespace Siprix
{
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
        Siprix.AccData accData_;        

        public AccountModel(Siprix.AccData accData, ObjModel parent)
        {
            this.accData_= accData;
            this.parent_ = parent;            

            RegState = (accData.ExpireTime == 0) ? RegState.Removed : RegState.InProgress;
            RegText  = (accData.ExpireTime == 0) ? "Removed"        : "In progress...";
                        
            RegisterCommand   = new RelayCommand(Register,   CanRegister);
            UnRegisterCommand = new RelayCommand(UnRegister, CanUnRegister);
        }
                
        public Siprix.AccData AccData   { get { return accData_; } }
        public string Uri               { get { return accData_.SipExtension + "@" + accData_.SipServer; } }
        public uint ID                  { get { return accData_.MyAccId; } }        
        public bool IsWaiting           { get { return (RegState == RegState.InProgress); } }
        public bool HasSecureMedia      { get { return (accData_.SecureMediaMode != null) && 
                                                       (accData_.SecureMediaMode != SecureMedia.Disabled); } }
        public string          RegText  { get; private set; }
        public Siprix.RegState RegState { get; private set; }
                
        public ICommand RegisterCommand   { get; private set; }
        public ICommand UnRegisterCommand { get; private set; }
        
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Equals(AccountModel? other) { return (this.ID == other?.ID); }

        public void Update(Siprix.AccData accData)
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
            int err = (expireSec != 0) ? parent_.Module.Account_Register(ID, expireSec)
                                       : parent_.Module.Account_Unregister(ID);
                                         
            if (err != Siprix.Module.kNoErr)
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
            parent_.postSaveAccountsChanges();
            parent_.Logs?.Print($"{cmd}ing accId:{ID}");
            return err;
        }
        
        //Event raised by SDK
        public void OnAccountRegState(RegState state, string response)
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

        public static Siprix.AccData loadFromJson(JsonElement elem)
        {
            Siprix.AccData accData = new();

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

        }//loadFromJson

    }//AccountModel


    /////////////////////////////////////////////////////////////////
    /// AccountsListModel

    public class AccountsListModel(ObjModel parent)
    {
        readonly ObservableCollection<AccountModel> collection_ = new();
        readonly ObjModel parent_ = parent;
        AccountModel? selAccount_;

        public ObservableCollection<AccountModel> Collection { get { return collection_; } }

        public AccountModel? SelectedAccount { 
            get { return selAccount_;  }
            set { selAccount_ = value; }
        }

        public uint getAccId(string uri)
        {
            var accModel = collection_.Where(a => a.Uri == uri).FirstOrDefault();
            return (accModel == null) ? Siprix.Module.kInvalidId : accModel.ID;
        }
        public string getUri(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            return (accModel == null) ? "?" : accModel.Uri;
        }

        public bool hasSecureMedia(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            return (accModel == null) ? false : accModel.HasSecureMedia;
        }

        public AccData? GetData(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            return accModel?.AccData;
        }

        public int Add(Siprix.AccData accData, bool saveChanges=true)
        {   
            parent_.Logs?.Print($"Adding new account: {accData.SipServer}@{accData.SipExtension}");

            int err = parent_.Module.Account_Add(accData);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't add account Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            AccountModel acc = new(accData, parent_);
            collection_.Add(acc);

            selAccount_ ??= acc;

            parent_.Logs?.Print($"Added successfully with id: {acc.ID}");
            if (saveChanges) parent_.postSaveAccountsChanges();
            return err;
        }

        public int Delete(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            if (accModel == null) return -1;

            int err = parent_.Module.Account_Delete(accId);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't delete account Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            collection_.Remove(accModel);

            if (selAccount_ == accModel)
                selAccount_ = (collection_.Count > 0) ? collection_[0] : null;

            parent_.postSaveAccountsChanges();

            parent_.Logs?.Print($"Deleted account accId:{accId}");
            return err;
        }

        public int Update(Siprix.AccData accData)
        {
            var accModel = collection_.Where(a => a.ID == accData.MyAccId).FirstOrDefault();
            if (accModel == null)
            {
                parent_.Logs?.Print("Account with specified id not found");
                return -1;
            }

            int err = parent_.Module.Account_Update(accData, accData.MyAccId);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't update account: {err} {{parent_.ErrorText(err)}}");
                return err;
            }

            accModel.Update(accData);
            
            parent_.postSaveAccountsChanges();
            parent_.Logs?.Print($"Updated account accId:{accData.MyAccId}");
            return err;
        }

        //Event raised by SDK
        public void OnAccountRegState(uint accId, RegState state, string response)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            accModel?.OnAccountRegState(state, response);
            parent_.Logs?.Print($"OnAccountRegState accId:{accId} state:{state} response:{response}");
        }

        public string storeToJson()
        {
            List<JsonDict> jsonAccList = [];
            foreach(var accModel in collection_)
            {
                jsonAccList.Add(accModel.storeToJson());
            }
            return JsonSerializer.Serialize(jsonAccList);
        }
        public void loadFromJson(string jsonString)
        {
            if (jsonString.Length == 0) return;

            collection_.Clear();

            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    this.Add(AccountModel.loadFromJson(element), false);
                }
            }
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
        string response_ = "";
        bool micMuted_ = false;
        bool camMuted_ = false;
        bool withVideo_;
        bool isFilePlaying_ = false;
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
        public bool   IsConnected       { get { return (callState_ == CallState.Connected); } }
        public bool   IsRinging         { get { return (callState_ == CallState.Ringing);   } }
        public bool   IsFilePlaying     { get { return isFilePlaying_; } }
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


        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
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
            int err = parent_.Module.Call_Bye(myCallId_);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Disconnecting);
            else  parent_.Logs?.Print($"Cant Bye callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int Accept(bool withVideo)
        {
            parent_.Logs?.Print($"Accepting callId:{myCallId_}");
            int err = parent_.Module.Call_Accept(myCallId_, withVideo);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Accepting);
            else parent_.Logs?.Print($"Cant Accept callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int Reject()
        {
            parent_.Logs?.Print($"Rejecting callId:{myCallId_}");
            int err = parent_.Module.Call_Reject(myCallId_);//Send '486 Busy now'
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Rejecting);
            else parent_.Logs?.Print($"Cant Reject callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int MuteMic(bool mute)
        {
            parent_.Logs?.Print($"Set mic mute={mute} of call {myCallId_}");
            int err = parent_.Module.Call_MuteMic(myCallId_, mute);
            if (err == Siprix.Module.kNoErr) setMicMuted(mute);
            else parent_.Logs?.Print($"Cant MuteMic callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int MuteCam(bool mute)
        {
            parent_.Logs?.Print($"Set camera mute={mute} of call {myCallId_}");
            int err = parent_.Module.Call_MuteCam(myCallId_, mute);
            if (err == Siprix.Module.kNoErr) setCamMuted(mute);
            else parent_.Logs?.Print($"Cant MuteCam callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int SendDtmf(string tone)
        {
            parent_.Logs?.Print($"Sending dtmf callId:{myCallId_} tone:{tone}");
            int err = parent_.Module.Call_SendDtmf(myCallId_, tone, 200, 50, DtmfMethod.DTMF_RTP);
            if (err != Siprix.Module.kNoErr) 
               parent_.Logs?.Print($"Cant SendDtmf callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;        
        }

        public int PlayFile(String pathToMp3File, bool loop)
        {
            parent_.Logs?.Print($"Starting play file callId:{myCallId_} {pathToMp3File}");
            uint playerId = 0;
            int err = parent_.Module.Call_PlayFile(myCallId_, pathToMp3File, loop, ref playerId);
            if (err != Siprix.Module.kNoErr)
                parent_.Logs?.Print($"Cant PlayFile callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            else
                playerIds_.Add(playerId);
            return err;
        }

        public int StopPlayFile()
        {
            int retErr = Siprix.Module.kNoErr;
            foreach(var playerId in playerIds_)
            {
                parent_.Logs?.Print($"Stop play file in callId:{myCallId_} playerId:{playerId}");
                int err = parent_.Module.Call_StopFile(playerId);
                if (err != Siprix.Module.kNoErr) {
                    parent_.Logs?.Print($"Cant StopPlayFile Err:{err} {parent_.ErrorText(err)}");
                    retErr = err;
                }
            }
            return retErr;
        }

        public int Hold()
        {
            parent_.Logs?.Print($"Hold callId:{myCallId_}");
            int err = parent_.Module.Call_Hold(myCallId_);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Holding);
            else parent_.Logs?.Print($"Cant MuteMic callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int TransferBlind(String toExt)
        {
            parent_.Logs?.Print($"Transfer blind callId:{myCallId_} to:{toExt}");
            if (toExt.Length==0) return -1;

            int err = parent_.Module.Call_TransferBlind(myCallId_, toExt);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Transferring);
            else parent_.Logs?.Print($"Cant TransferBlind callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int TransferAttended(uint toCallId)
        {
            parent_.Logs?.Print($"Transfer attended callId:{myCallId_} to callId:{toCallId}");
            int err = parent_.Module.Call_TransferAttended(myCallId_, toCallId);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Transferring);
            else parent_.Logs?.Print($"Cant TransferAttended callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;            
        }
        public int SetVideoWindow(IntPtr hwnd)
        {
            parent_.Logs?.Print($"SetVideoWindow callId:{myCallId_} hwnd:{hwnd}");
            return parent_.Module.Call_SetVideoWindow(myCallId_, hwnd);
        }

        public string GetSipHeader(string hdrName)
        {
            return parent_.Module.Call_GetSipHeader(myCallId_, hdrName);
        }

        //Events raised by SDK
        public void OnCallProceeding(string response)
        {
            response_ = response;
            setCallState(CallState.Proceeding);
        }

        public void OnCallConnected(string hdrFrom, string hdrTo, bool withVideo)
        {
            startTime_ = DateTime.Now;
            setWithVideo(withVideo);
            setCallState(CallState.Connected);
        }

        public void OnCallTransferred(uint statusCode)
        {
            setCallState(CallState.Connected);
        }

        public void OnCallDtmfReceived(ushort tone)
        {
            if(tone == 10) { receivedDtmf_ += '*'; }else
            if(tone == 11) { receivedDtmf_ += '#'; }
            else           { receivedDtmf_ += tone.ToString(); }
            NotifyPropertyChanged(nameof(ReceivedDtmf));
        }

        public void OnCallHeld(HoldState holdState)
        {
            setHoldState(holdState);
            setCallState((holdState_ == HoldState.None) ? CallState.Connected : CallState.Held);            
        }

        public void OnCallSwitched()
        {
            NotifyPropertyChanged(nameof(IsSwitchedCall));
            NotifyPropertyChanged(nameof(CanSwitchTo));
        }

        public void OnPlayerState(uint playerId, PlayerState state)
        {
            if (playerIds_.Contains(playerId)) return;
            
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

    public class CallsListModel(ObjModel parent) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        readonly ObservableCollection<CallModel> collection_ = new();
        readonly ObjModel parent_ = parent;

        CallModel? switchedCall_;
        uint lastIncomingCallId_ = Siprix.Module.kInvalidId;
        bool confModeStarted_ = false;

        public ObservableCollection<CallModel> Collection { get { return collection_; } }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsSwitchedCall(uint callId)
        {
            return (switchedCall_ == null) ? false : (switchedCall_.ID == callId);
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

        public int Invite(Siprix.DestData dest)
        {
            parent_.Logs?.Print($"Trying to invite {dest.ToExt} from account:{dest.FromAccId}");

            int err = parent_.Module.Call_Invite(dest);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't invite Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            String accUri       = parent_.Accounts.getUri(dest.FromAccId);
            bool hasSecureMedia = parent_.Accounts.hasSecureMedia(dest.FromAccId);

            CallModel newCall = new(dest.MyCallId, accUri, dest.ToExt, false, hasSecureMedia, dest.WithVideo, parent_);
            collection_.Add(newCall);
            //_cdrs?.add(newCall);             //TODO add CDR
            
            parent_.postResolveContactName_(newCall);
            return err;
        }

        public int SwitchTo(uint callId)
        {
            parent_.Logs?.Print($"Switching mixer to callId:{callId}");

            int err = parent_.Module.Mixer_SwitchToCall(callId);
            if (err == Siprix.Module.kNoErr) ConfModeStarted = false;
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
                int err = parent_.Module.Mixer_SwitchToCall(callId);
                ConfModeStarted = false;
                return err;
            }
            else {
                parent_.Logs?.Print("Joining all calls to conference");
                int err = parent_.Module.Mixer_MakeConference();
                if (err == Siprix.Module.kNoErr) ConfModeStarted = true;
                else parent_.Logs?.Print($"Cant make conference Err:{err} {parent_.ErrorText(err)}");
                return err;
            }
        }

        public int SetPreviowVideoWindow(IntPtr hwnd)
        {
            parent_.Logs?.Print($"SetPreviowVideoWindow hwnd:{hwnd}");
            return parent_.Module.Call_SetVideoWindow(Siprix.Module.kInvalidId, hwnd);
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
        public void OnCallIncoming(uint callId, uint accId, bool withVideo, string hdrFrom, string hdrTo)
        {
            parent_.Logs?.Print($"onIncoming callId:{callId} accId:{accId} from:{hdrFrom} to:{hdrTo}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            if (callModel != null) return;//Call already exists, skip

            String accUri       = parent_.Accounts.getUri(accId);
            bool hasSecureMedia = parent_.Accounts.hasSecureMedia(accId);

            CallModel newCall = new(callId, accUri, parseExt(hdrFrom), true, hasSecureMedia, withVideo, parent_);
            newCall.setDisplName(parseDisplayName(hdrFrom));
            collection_.Add(newCall);

            setLastIncomingCallId(callId);

            SwitchedCall ??= newCall;//Set new value only when current one is null

            //_cdrs?.add(newCall);
            //_postResolveContactName(newCall); //TODO add '_postResolveContactName'
        }

        public void OnCallConnected(uint callId, string hdrFrom, string hdrTo, bool withVideo)
        {
            parent_.Logs?.Print($"onConnected callId:{callId} from:{hdrFrom} to:{hdrTo}");
            //_cdrs?.setConnected(callId, hdrFrom, hdrTo);

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallConnected(hdrFrom, hdrTo, withVideo);
        }

        public void OnCallTerminated(uint callId, uint statusCode)
        {
            parent_.Logs?.Print($"onTerminated callId:{callId} statusCode:{statusCode}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            //_cdrs?.setTerminated(callId, statusCode, callModel?.displName);

            if (callModel != null)
            {
                collection_.Remove(callModel);

                if (ConfModeStarted && !hasConnectedFewCalls())
                    ConfModeStarted = false;
            }
        }

        public void OnCallProceeding(uint callId, string response)
        {
            parent_.Logs?.Print($"onProceeding callId:{callId} response:{response}");
            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallProceeding(response);
        }

        public void OnCallTransferred(uint callId, uint statusCode)
        {
            parent_.Logs?.Print($"onTransferred callId:{callId} statusCode:{statusCode}");
            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallTransferred(statusCode);
        }

        public void OnCallRedirected(uint origCallId, uint relatedCallId, string referTo)
        {
           parent_.Logs?.Print($"onRedirected origCallId:{origCallId} relatedCallId:{relatedCallId} to:{referTo}");

           //Find 'origCallId'
           var origCall = collection_.Where(a => a.ID == origCallId).FirstOrDefault();
           if (origCall == null) return;

           //Clone 'origCallId' and add to collection of calls as related one           
           CallModel relatedCall = new CallModel(relatedCallId, origCall.AccUri, parseExt(referTo), false, 
                                                origCall.HasSecureMedia, origCall.WithVideo, 
                                                parent_);
           collection_.Add(relatedCall);
        }

        public void OnCallDtmfReceived(uint callId, ushort tone)
        {
            parent_.Logs?.Print($"onDtmfReceived callId:{callId} tone:{tone}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallDtmfReceived(tone);
        }

        public void OnCallHeld(uint callId, HoldState state)
        {
            parent_.Logs?.Print($"onHeld callId:{callId} {state}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallHeld(state);
        }

        public void OnCallSwitched(uint callId)
        {
            parent_.Logs?.Print($"onSwitched callId:{callId}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();            
            SwitchedCall = callModel;

            foreach (var c in collection_) c.OnCallSwitched();
        }

        public void OnPlayerState(uint playerId, PlayerState state)
        {
            parent_.Logs?.Print($"onPlayerState playerId:{playerId} state:{state}");

            foreach (var c in collection_) c.OnPlayerState(playerId, state);
        }

        static String parseExt(String uri)
        {
            //URI format: "displName" <sip:EXT@domain:port>
            int startIndex = uri.IndexOf(':');
            if (startIndex == -1) return "";

            int endIndex = uri.IndexOf('@', startIndex + 1);
            return (endIndex == -1) ? "" : uri.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        static String parseDisplayName(String uri)
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
        readonly Siprix.SubscrData subData_ = new();

        public static SubscriptionModel BLF()
        {
            SubscriptionModel blfModel = new();
            blfModel.subData_.MimeSubType = "dialog-info+xml";
            blfModel.subData_.EventType = "dialog";
            return blfModel;
        }

        public Siprix.SubscrData Data { get { return subData_; } }
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
        public void OnSubscriptionState(SubscriptionState state, string resp)
        {
            internalState_ = state;

            //Parse 'response' (contains XML body received in NOTIFY request)
            // and use parsed attributes for UI rendering
            int startIndex = resp.IndexOf("<state");
            if (startIndex != -1)
            {
                startIndex = resp.IndexOf(">", startIndex);
                int endIndex = resp.IndexOf("</state>", startIndex);
                String blfStateStr = resp.Substring(startIndex + 1, endIndex- startIndex-1);
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

        public static Siprix.SubscriptionModel loadFromJson(JsonElement elem)
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
    public class SubscriptionsListModel(ObjModel parent)
    {
        readonly ObservableCollection<SubscriptionModel> collection_ = new();
        readonly ObjModel parent_ = parent;

        public ObservableCollection<SubscriptionModel> Collection { get { return collection_; } }

        public int Add(SubscriptionModel subscr, bool saveChanges = true)
        {
            parent_.Logs?.Print($"Adding new subscription ext:{subscr.ToExt} accId:@{subscr.AccId}");

            //When accUri present - model loaded from json, search accId as it might be changed
            if (subscr.AccUri.Length != 0) { subscr.AccId  = parent_.Accounts.getAccId(subscr.AccUri); }
            else                           { subscr.AccUri = parent_.Accounts.getUri(subscr.AccId); }

            //Add
            int err = parent_.Module.Subscription_Add(subscr.Data);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't add subscription Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            collection_.Add(subscr);

            parent_.Logs?.Print($"Added successfully with id: {subscr.ID}");
            if (saveChanges) parent_.postSaveSubscriptionChanges();
            return err;
        }

        public int Delete(uint subId)
        {
            var subModel = collection_.Where(a => a.ID == subId).FirstOrDefault();
            if (subModel == null) return -1;

            int err = parent_.Module.Subscription_Delete(subId);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't delete subscription Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            collection_.Remove(subModel);

            parent_.postSaveSubscriptionChanges();

            parent_.Logs?.Print($"Deleted subscription subId:{subId}");
            return err;
        }

        public void OnSubscriptionState(uint subId, SubscriptionState state, string response)
        {
            var subModel = collection_.Where(a => a.ID == subId).FirstOrDefault();
            subModel?.OnSubscriptionState(state, response);
            parent_.Logs?.Print($"OnSubscriptionState subId:{subId} state:{state} response:{response}");
        }

        public string storeToJson()
        {
            List<JsonDict> jsonAccList = [];
            foreach (var subModel in collection_)
            {
                jsonAccList.Add(subModel.storeToJson());
            }
            return JsonSerializer.Serialize(jsonAccList);
        }
        public void loadFromJson(string jsonString)
        {
            if (jsonString.Length == 0) return;

            collection_.Clear();

            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    this.Add(SubscriptionModel.loadFromJson(element), false);
                }
            }
        }

    }//SubscriptionsListModel


    /////////////////////////////////////////////////////////////////
    /// NetworkModel

    public class NetworkModel(ObjModel parent) : INotifyPropertyChanged
    {
        readonly ObjModel parent_ = parent;
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool NetworkLost { get; private set; }

        //Event raised by SDK
        public void OnNetworkStateChanged(String name, NetworkState state)
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

        public void OnTrialModeNotified()
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
        readonly CallsListModel callsListModel_;
        readonly NetworkModel networkModel_;
        readonly LogsModel? logsModel_;

        readonly Siprix.Module module_ = new();//Create module

        private SiprixEventsHandler? eventHandler_;

        static ObjModel? instance = null;

        ObjModel()
        {
            //Create models
            logsModel_ = new LogsModel();
            subscrListModel_ = new SubscriptionsListModel(this);
            accountsListModel_ = new AccountsListModel(this);
            callsListModel_ = new CallsListModel(this);
            networkModel_ = new NetworkModel(this);
        }

        public SubscriptionsListModel Subscriptions { get { return subscrListModel_; } }
        public AccountsListModel Accounts { get { return accountsListModel_; } }
        public CallsListModel Calls    { get { return callsListModel_; } } 
        public NetworkModel Networks { get { return networkModel_; } }
        public LogsModel? Logs    { get { return logsModel_; } }
        public Siprix.Module Module { get { return module_; } }
        public string ErrorText(int err) { return module_.ErrorText(err);  }

        public static ObjModel Instance 
        {
            get 
            {
                instance ??= new ObjModel();
                return instance;
            }
        }

        public void Initialize(AppDispatcher dispatcher)
        {
            if (module_.IsInitialized())
                return;

            eventHandler_ = new SiprixEventsHandler(this, dispatcher);

            Siprix.IniData iniData = new();
            iniData.License = "...license-credentials...";
            iniData.SingleCallMode = false;
            iniData.LogLevelIde = Siprix.LogLevel.Debug;
            iniData.LogLevelFile = Siprix.LogLevel.Debug;
            iniData.WriteDmpUnhandledExc = true;
    
            int err = module_.Initialize(eventHandler_, iniData);

            if(err == Siprix.Module.kNoErr) {
                Logs?.Print("Siprix module initialized successfully");
                Logs?.Print($"Version: {module_.Version()}");

                loadSavedAccounts();
                loadSavedSubscriptions();
            }
            else{
                Logs?.Print($"Can't initialize Siprix module Err: {err} {ErrorText(err)}");
            }
        }

        public void UnInitialize()
        {
            int err = module_.UnInitialize();
            if (err == Siprix.Module.kNoErr){
                Logs?.Print("Siprix module uninitialized");
            }
            else{
                Logs?.Print($"Can't uninitialize Siprix module Err: {err} {ErrorText(err)}");
            }
        }

        internal void loadSavedAccounts()
        {
            try
            {
                Logs?.Print("Loading accounts...");
            
                accountsListModel_.loadFromJson(SampleWpf.Properties.Settings.Default.accounts);
            
                Logs?.Print($"Loaded {accountsListModel_.Collection.Count} accounts");
            }
            catch (Exception e) {
                Logs?.Print(e.Message);
            }
        }

        internal void loadSavedSubscriptions()
        {
            try
            {
                Logs?.Print("Loading subscriptions...");

                subscrListModel_.loadFromJson(SampleWpf.Properties.Settings.Default.subscriptions);

                Logs?.Print($"Loaded {subscrListModel_.Collection.Count} subscriptions");
            }
            catch (Exception e) {
                Logs?.Print(e.Message);
            }
        }


        internal void postSaveAccountsChanges()
        {
            eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => {
                string jsonStr = accountsListModel_.storeToJson();
            
                SampleWpf.Properties.Settings.Default.accounts = jsonStr;
                SampleWpf.Properties.Settings.Default.Save();
            }));
        }

        internal void postSaveSubscriptionChanges()
        {
            eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => {
                string jsonStr = subscrListModel_.storeToJson();

                SampleWpf.Properties.Settings.Default.subscriptions = jsonStr;
                SampleWpf.Properties.Settings.Default.Save();
            }));
        }

        internal void postResolveContactName_(CallModel newCall)
        {
            eventHandler_?.dispatcher_?.BeginInvoke(new Action(() => {
                //string str = newCall.NameAndExt;
                //TODO add 'ResolveContactName'
                //newCall.setDisplName
            }));
        }


        //Events raised by SDK
        class SiprixEventsHandler(ObjModel parent, AppDispatcher dispatcher) : Siprix.IEventDelegate
        {
            readonly public AppDispatcher dispatcher_ = dispatcher;
            readonly ObjModel parent_ = parent;

            public void OnTrialModeNotified()
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    parent_.logsModel_?.OnTrialModeNotified();
                }));
            }

            public void OnDevicesAudioChanged()
            {
                //TODO add
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

}//namespace SampleWpf
