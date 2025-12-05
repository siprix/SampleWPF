#pragma warning disable CA1806, CA2101, IDE1006, SYSLIB1054
using System.Runtime.InteropServices;
using System.Text;

namespace Siprix
{
    using AccountId = uint;
    using SubscriptionId = uint;
    using MessageId = uint;
    using PlayerId = uint;
    using CallId = uint;
    
    public enum LogLevel : byte
    {
        Stack = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        NoLog = 5
    }

    public enum RegState : byte
    {
        Success = 0, //Registeration success
        Failed,    //Registration failed
        Removed,   //Registration removed
        InProgress
    };

    public enum SubscriptionState : byte
    {
        Created = 0, //Subscription just created and waiting response
        Updated,    //(received NOTIFY)
        Destroyed   //Received error (timeout) on initial SUBSCRIBE or app unsubscribed it
    };

    public enum SecureMedia : byte
    {
        Disabled = 0,        
        SdesSrtp,
        DtlsSrtp,
    };

    public enum SipTransport : byte
    {
        UDP = 0,
        TCP,
        TLS,
    };

    public enum DtmfMethod : byte
    {
        DTMF_RTP = 0,
        DTMF_INFO
    };

    public enum AudioCodec : byte
    {
        Opus = 65,
        ISAC16 = 66,
        ISAC32 = 67,
        G722 = 68,
        ILBC = 69,
        PCMU = 70,
        PCMA = 71,
        DTMF = 72,
        CN   = 73,
        G729 = 74
    };

    public enum VideoCodec : byte
    {
        H264 = 80,
        VP8 = 81,
        VP9 = 82,
        AV1 = 83
    };


    public enum HoldState : byte
    {
        None = 0,
        Local = 1,
        Remote = 2,
        LocalAndRemote = 3
    };

    public enum PlayerState : byte
    {
        PlayerStarted = 0,
        PlayerStopped = 1,
        PlayerFailed = 2,
    };

    public enum NetworkState : byte
    {
        NetworkLost = 0,
        NetworkRestored = 1,
        NetworkSwitched = 2
    };

    public enum CallState
    {
        Dialing,      //Outgoing call just initiated
        Proceeding,   //Outgoing call in progress, received 100Trying or 180Ringing

        Ringing,      //Incoming call just received
        Rejecting,    //Incoming call rejecting after invoke 'call.reject'
        Accepting,    //Incoming call aceepting after invoke 'call.accept'

        Connected,    //Call successfully established, RTP is flowing

        Disconnecting,//Call disconnecting after invoke 'call.bye'

        Holding,      //Call holding (renegotiating RTP stream states)
        Held,         //Call held, RTP is NOT flowing

        Transferring, //Call transferring
    }


    public class IniData
    {
        public string?   License;
        public LogLevel? LogLevelFile;
        public LogLevel? LogLevelIde;
        public bool?     ShareUdpTransport;
        public bool?     WriteDmpUnhandledExc;
        public bool?     TlsVerifyServer;
        public bool?     SingleCallMode;
        public ushort?   RtpStartPort;
        public string?   HomeFolder;
        public string?   BrandName;
        public bool?     UseDnsSrv;
        public bool?     RecordStereo;
        public bool?     VideoCallEnabled;
        public bool?     Aes128Sha32Enabled;
        public bool?     VUmeterEnabled;

        public List<string>? DnsServers;

    }//IniData


    public class AccData
    {
        public AccountId MyAccId = 0;//Assigned by module in 'Account_Add'
        public string  SipServer ="";
        public string  SipExtension = "";
        public string  SipPassword = "";
         
        public uint    ExpireTime = 300;
        public string? SipAuthId;
        public string? SipProxyServer;

        public string? UserAgent;
        public string? DisplayName;
        public string? InstanceId;
        public string? RingToneFile;

        public SecureMedia?   SecureMediaMode;
        public bool?          UseSipSchemeForTls;
        public bool?          RtcpMuxEnabled;
        public uint?          KeepAliveTime;
        public bool?          IceEnabled;
        public string?        StunServer;

        public SipTransport?  TranspProtocol;
        public ushort?        TranspPort;
        public string?        TranspTlsCaCert;
        public string?        TranspBindAddr;
        public bool?          TranspPreferIPv6;
        public bool?          RewriteContactIp;
        public bool?          VerifyIncomingCall;
        
        public List<AudioCodec>? AudioCodecs;
        public List<VideoCodec>? VideoCodecs;
        public Dictionary<string, string>? Xheaders;

    }//AccData


    public class DestData
    {
        public CallId    MyCallId = 0;     //Assigned by module in 'Call_Invite'
        public string    ToExt = "";
        public AccountId FromAccId = 0;
        public bool      WithVideo = false;
        public string?   DisplayName;
        public int?      InviteTimeout;
        public Dictionary<string, string>? Xheaders;
    }

    public class SubscrData
    {
        public SubscriptionId MySubId = 0;     //Assigned by module in 'Subscription_Add'
        public string    ToExt = "";
        public AccountId FromAccId = 0;
        public string    MimeSubType="";
        public string    EventType="";
        public uint?     ExpireTime;
    }

    public class MsgData
    {
        public MessageId MyMsgId = 0;     //Assigned by module in 'Message_Send'
        public string ToExt = "";
        public AccountId FromAccId = 0;
        public string Body = "";
    }

    public class VideoData
    {
        public string? noCameraImgPath;/// Path to jpg file path to the jpg file with image, which library will send when video device not available.          
        public int?  framerateFps;/// Capturer framerate (by default 15)
        public int?  bitrateKbps;/// Encoder bitrate, allows specify video bandwith (by default 600)        
        public int?  height;/// Capturer video frame height (by default 480)
        public int?  width;/// Capturer video frame width (by default 600)
        public int?  rotation;/// Capturer video frame rotation degrees (by default 0, allowed values 0,90,180,270)
    }

    public static class ErrorCode
    {
        public const uint kInvalidId = 0;

        public const int kNoErr               = 0;
        public const int kAlreadyInitialized  = -1000;
        public const int kNotInitialized      = -1001;
        public const int kInitializeFailure   = -1002;
        public const int kObjectNull          = -1003;
        public const int kArgumentNull        = -1004;
        public const int kNotImplemented      = -1005;

        public const int kBadSipServer        = -1010;
        public const int kBadSipExtension     = -1011;
        public const int kBadSecureMediaMode  = -1012;
        public const int kBadTranspProtocol   = -1013;
        public const int kBadTranspPort       = -1014;

        public const int kDuplicateAccount    = -1021;
        public const int kAccountNotFound     = -1022;
        public const int kAccountHasCalls     = -1023;
        public const int kAccountDoenstMatch  = -1024;
        public const int kSingleAccountMode   = -1025;
        public const int kAccountHasSubscr    = -1026;

        public const int kDestNumberEmpty     = -1030;
        public const int kDestNumberSpaces    = -1031;
        public const int kDestNumberScheme    = -1032;
        public const int kDestBadFormat       = -1033;
        public const int kDestSchemeMismatch  = -1034;
        public const int kOnlyOneCallAllowed  = -1035;    

        public const int kCallNotFound        = -1040;
        public const int kCallNotIncoming     = -1041;
        public const int kCallAlreadyAnswered = -1042;
        public const int kCallNotConnected    = -1043;
        public const int kBadDtmfStr          = -1044;
        public const int kFileDoesntExists    = -1045;
        public const int kFileExtMp3Expected  = -1046;
        public const int kCallAlreadySwitched = -1047;
        public const int kCallAlredyMuted     = -1048;
        public const int kCallRecAlredyStarted= -1049;
        public const int kCallRecNotStarted   = -1050;
        public const int kCallCantReferBlind  = -1051;
        public const int kCallReferInProgress = -1052;
        public const int kCallCantReferAtt    = -1053;
        public const int kCallReferAttSameId  = -1054;
        public const int kConfRequires2Calls  = -1055;
        public const int kCallIsHolding       = -1056;
        public const int kRndrAlreadyAssigned = -1057;
        public const int kSipHeaderNotFound   = -1058;
     
        public const int kBadDeviceIndex      = -1070;

        public const int kEventTypeCantBeEmpty= -1080;
        public const int kSubTypeCantBeEmpty  = -1081;
        public const int kSubscrDoesntExist   = -1082;
        public const int kSubscrAlreadyExist  = -1083;

        public const int kMsgBodyCantBeEmpty  = -1085;

        public const int kMicPermRequired     = -1111;
    }

    public interface IEventDelegate
    {
        void OnTrialModeNotified();
        void OnDevicesAudioChanged();
        
        void OnAccountRegState(AccountId accId, RegState state, string response);
        void OnSubscriptionState(SubscriptionId subId, SubscriptionState state, string response);
        void OnNetworkState(string name, NetworkState state);
        void OnPlayerState(PlayerId playerId, PlayerState state);
        void OnRingerState(bool start);
        
        void OnCallIncoming(CallId callId, AccountId accId, bool withVideo, string hdrFrom, string hdrTo);
        void OnCallConnected(CallId callId, string hdrFrom, string hdrTo, bool withVideo);
        void OnCallTerminated(CallId callId, uint statusCode);
        void OnCallProceeding(CallId callId, string response);
        void OnCallTransferred(CallId callId, uint statusCode);
        void OnCallRedirected(CallId origCallId, CallId relatedCallId, string referTo);
        void OnCallDtmfReceived(CallId callId, ushort tone);
        void OnCallHeld(CallId callId, HoldState state);
        void OnCallSwitched(CallId callId);

        void OnMessageSentState(MessageId messageId, bool success, string response);
        void OnMessageIncoming(MessageId messageId, AccountId accId, string hdrFrom, string body);

        void OnSipNotify(AccountId accId, string hdrEvent, string body);
        void OnVuMeterLevel(int micLevel, int spkLevel);
    }


    public class CoreService : IDisposable
    {
        IntPtr modulePtr_;
        IEventDelegate? eventDelegate_;
        const string DllName = "siprix.dll";

        private readonly OnTrialModeNotified   onTrialModeNotified_;
        private readonly OnDevicesAudioChanged onDevicesAudioChanged_;

        private readonly OnAccountRegState     onAccountRegState_;
        private readonly OnSubscriptionState   onSubscriptionState_;
        private readonly OnNetworkState        onNetworkState_;
        private readonly OnPlayerState         onPlayerState_;
        private readonly OnRingerState         onRingerState_;

        private readonly OnCallIncoming        onCallIncoming_;
        private readonly OnCallConnected       onCallConnected_;
        private readonly OnCallTerminated      onCallTerminated_;
        private readonly OnCallProceeding      onCallProceeding_;
        private readonly OnCallTransferred     onCallTransferred_;
        private readonly OnCallRedirected      onCallRedirected_;
        private readonly OnCallDtmfReceived    onCallDtmfReceived_;
        private readonly OnCallHeld            onCallHeld_;
        private readonly OnCallSwitched        onCallSwitched_;

        private readonly OnMessageSentState    onMessageSentState_;
        private readonly OnMessageIncoming     onMessageIncoming_;

        private readonly OnSipNotify           onSipNotify_;
        private readonly OnVuMeterLevel        onVuMeterLevel_;


        public CoreService()
        {
            onTrialModeNotified_   = new OnTrialModeNotified     (OnTrialModeNotifiedCallback);
            onDevicesAudioChanged_ = new OnDevicesAudioChanged   (OnDevicesAudioChangedCallback);

            onAccountRegState_     = new OnAccountRegState       (OnAccountRegStateCallback);
            onSubscriptionState_   = new OnSubscriptionState     (OnSubscriptionStateCallback);
            onNetworkState_        = new OnNetworkState          (OnNetworkStateCallback);
            onPlayerState_         = new OnPlayerState           (OnPlayerStateCallback);
            onRingerState_         = new OnRingerState           (OnRingerStateCallback);
                                    
            onCallIncoming_        = new OnCallIncoming          (OnCallIncomingCallback);
            onCallConnected_       = new OnCallConnected         (OnCallConnectedCallback);
            onCallTerminated_      = new OnCallTerminated        (OnCallTerminatedCallback);
            onCallProceeding_      = new OnCallProceeding        (OnCallProceedingCallback);
            onCallTransferred_     = new OnCallTransferred       (OnCallTransferredCallback);
            onCallRedirected_      = new OnCallRedirected        (OnCallRedirectedCallback);
            onCallDtmfReceived_    = new OnCallDtmfReceived      (OnCallDtmfReceivedCallback);
            onCallHeld_            = new OnCallHeld              (OnCallHeldCallback);
            onCallSwitched_        = new OnCallSwitched          (OnCallSwitchedCallback);

            onMessageSentState_   = new OnMessageSentState       (OnMessageSentStateCallback);
            onMessageIncoming_    = new OnMessageIncoming        (OnMessageIncomingCallback);

            onSipNotify_          = new OnSipNotify              (OnSipNotifyCallback);
            onVuMeterLevel_       = new OnVuMeterLevel           (OnVuMeterLevelCallback);
        }

        ~CoreService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {            
            }
                        
            Module_UnInitialize(modulePtr_);
            modulePtr_ = IntPtr.Zero;
        }


        //---------------------------------------------
        //Module

        public int Initialize(IEventDelegate eventDelegate, IniData iniData)
        {
            if(modulePtr_ == IntPtr.Zero)
                modulePtr_ = Module_Create();

            int err = Module_Initialize(modulePtr_, getNative(iniData));
            if(err != ErrorCode.kNoErr) return err;
            
            eventDelegate_ = eventDelegate;

            Callback_SetTrialModeNotified(modulePtr_,    onTrialModeNotified_);
            Callback_SetDevicesAudioChanged(modulePtr_,  onDevicesAudioChanged_);

            Callback_SetAccountRegState(modulePtr_,      onAccountRegState_);
            Callback_SetSubscriptionState(modulePtr_,    onSubscriptionState_);
            Callback_SetNetworkState(modulePtr_,         onNetworkState_);
            Callback_SetPlayerState(modulePtr_,          onPlayerState_);
            Callback_SetRingerState(modulePtr_,          onRingerState_);

            Callback_SetCallProceeding(modulePtr_,       onCallProceeding_);
            Callback_SetCallTerminated(modulePtr_,       onCallTerminated_);
            Callback_SetCallConnected(modulePtr_,        onCallConnected_);
            Callback_SetCallIncoming(modulePtr_,         onCallIncoming_);
            Callback_SetCallDtmfReceived(modulePtr_,     onCallDtmfReceived_);
            Callback_SetCallTransferred(modulePtr_,      onCallTransferred_);
            Callback_SetCallRedirected(modulePtr_,       onCallRedirected_);
            Callback_SetCallSwitched(modulePtr_,         onCallSwitched_);
            Callback_SetCallHeld(modulePtr_,             onCallHeld_);

            Callback_SetMessageSentState(modulePtr_,     onMessageSentState_);
            Callback_SetMessageIncoming(modulePtr_,      onMessageIncoming_);

            Callback_SetSipNotify(modulePtr_,            onSipNotify_);
            Callback_SetVuMeterLevel(modulePtr_,         onVuMeterLevel_);
            return err;
        }

        public int UnInitialize()
        {
            return Module_UnInitialize(modulePtr_);
        }

        public bool IsInitialized()
        {
            return (modulePtr_ != IntPtr.Zero) && Module_IsInitialized(modulePtr_);
        }

        public string HomeFolder()
        {
            IntPtr strPtr = (modulePtr_ != IntPtr.Zero) ? Module_HomeFolder(modulePtr_) : 0;
            string? path = Marshal.PtrToStringUTF8(strPtr);
            return path ?? "";
        }

        public string Version()
        {
            IntPtr strPtr = (modulePtr_ != IntPtr.Zero) ? Module_Version(modulePtr_) : 0;
            string? ver = Marshal.PtrToStringUTF8(strPtr);
            return ver ?? "";
        }

        public uint VersionCode()
        {
            return (modulePtr_ != IntPtr.Zero) ? Module_VersionCode(modulePtr_) : 0;
        }

        public void WriteLog(string text)
        {
            if (modulePtr_ != IntPtr.Zero) Module_WriteLog(modulePtr_, text);
        }

        public string ErrorText(int code)
        {
            IntPtr strPtr = GetErrorText(code);
            string? ver = Marshal.PtrToStringUTF8(strPtr);
            return ver ?? "";
        }

        /// [Account] ///////////////////////////////////////////////////////////////////////////////////////////////
        public int Account_Add(AccData accData)
        {
            return Account_Add(modulePtr_, getNative(accData), ref accData.MyAccId);
        }

        public int Account_Update(AccData accData, AccountId accId)
        {
            return Account_Update(modulePtr_, getNative(accData), accId);
        }

        public int Account_GetRegState(AccountId accId, ref RegState state)
        {
            return Account_GetRegState(modulePtr_, accId, ref state);
        }

        public int Account_Register(AccountId accId, uint expireTime)
        {
            return Account_Register(modulePtr_, accId, expireTime);
        }

        public int Account_Unregister(AccountId accId)
        {
            return Account_Unregister(modulePtr_, accId);
        }

        public int Account_Delete(AccountId accId)
        {
            return Account_Delete(modulePtr_, accId);
        }


        /// [Calls] ///////////////////////////////////////////////////////////////////////////////////////////////

        public int Call_Invite(DestData dest)
        {
            return Call_Invite(modulePtr_, getNative(dest), ref dest.MyCallId);
        }

        public int Call_Reject(CallId callId, uint statusCode=486)
        {
            return Call_Reject(modulePtr_, callId, statusCode);
        }

        public int Call_Accept(CallId callId, bool withVideo)
        {
            return Call_Accept(modulePtr_, callId, withVideo);
        }

        public int Call_Hold(CallId callId)
        {
            return Call_Hold(modulePtr_, callId);
        }

        public int Call_GetHoldState(CallId callId, ref HoldState state)
        {
            return Call_GetHoldState(modulePtr_, callId, ref state);
        }

        public string Call_GetSipHeader(CallId callId, string hdrName)
        {
            uint hdrValLen = 0;
            Call_GetSipHeader(modulePtr_, callId, hdrName, null, ref hdrValLen);
            if (hdrValLen > 0)
            {
                var sb = new StringBuilder((int)(hdrValLen+1));
                Call_GetSipHeader(modulePtr_, callId, hdrName, sb, ref hdrValLen);
                return sb.ToString();
            }
            else return string.Empty;
        }

        public string Call_GetStats(CallId callId)
        {
            uint initLen = 2000, requiredLen = initLen;
            var sb = new StringBuilder((int)initLen);
            int err = Call_GetStats(modulePtr_, callId, sb, ref requiredLen);
            if (err != ErrorCode.kNoErr) return string.Empty;

            bool longerStrRequired = (requiredLen > initLen);
            sb.Length = (int)requiredLen;
            if (longerStrRequired)
                Call_GetStats(modulePtr_, callId, sb, ref requiredLen);

            return sb.ToString();
        }

        public string Call_GetNonce(CallId callId)
        {
            uint nonceValLen = 0;
            Call_GetNonce(modulePtr_, callId, null, ref nonceValLen);
            if (nonceValLen > 0)
            {
                var sb = new StringBuilder((int)(nonceValLen+1));
                Call_GetNonce(modulePtr_, callId, sb, ref nonceValLen);
                return sb.ToString();
            }
            else return string.Empty;
        }


        public int Call_MuteMic(CallId callId, bool mute)
        {
            return Call_MuteMic(modulePtr_, callId, mute);
        }

        public int Call_MuteCam(CallId callId, bool mute)
        {
            return Call_MuteCam(modulePtr_, callId, mute);
        }        

        public int Call_SendDtmf(CallId callId, string dtmfs, 
            Int16 durationMs, Int16 intertoneGapMs, DtmfMethod method)
        {
            return Call_SendDtmf(modulePtr_, callId, dtmfs, durationMs, intertoneGapMs, method);
        }

        public int Call_PlayFile(CallId callId, string pathToMp3File, bool loop, ref PlayerId playerId)
        {
            return Call_PlayFile(modulePtr_, callId, pathToMp3File, loop, ref playerId);
        }

        public int Call_StopFile(PlayerId playerId)
        {
            return Call_StopFile(modulePtr_, playerId);
        }

        public int Call_RecordFile(CallId callId, string pathToMp3File)
        {
            return Call_RecordFile(modulePtr_, callId, pathToMp3File);
        }

        public int Call_StopRecordFile(CallId callId)
        {
            return Call_StopRecordFile(modulePtr_, callId);
        }

        public int Call_TransferBlind(CallId callId, string toExt)
        {
            return Call_TransferBlind(modulePtr_, callId, toExt);
        }

        public int Call_TransferAttended(CallId fromCallId, CallId toCallId)
        {
            return Call_TransferAttended(modulePtr_, fromCallId, toCallId);
        }

        public int Call_SetVideoWindow(CallId callId, IntPtr hwnd)
        {
            return Call_SetVideoWindow(modulePtr_, callId, hwnd);
        }

        public int Call_Bye(CallId callId)
        {
            return Call_Bye(modulePtr_, callId);
        }

        public int Call_Renegotiate(CallId callId)
        {
            return Call_Renegotiate(modulePtr_, callId);
        }

        public int Call_UpgradeToVideo(CallId callId)
        {
            return Call_UpgradeToVideo(modulePtr_, callId);
        }

        public int Call_StopRingtone()
        {
            return Call_StopRingtone(modulePtr_);
        }

        /// [Mixer] ///////////////////////////////////////////////////////////////////////////////////////////////
        public int Mixer_SwitchToCall(CallId callId)
        {
            return Mixer_SwitchToCall(modulePtr_, callId);
        }

        public int Mixer_MakeConference()
        {
            return Mixer_MakeConference(modulePtr_);
        }

        /// [Messages] ///////////////////////////////////////////////////////////////////////////////////////////////
        public int Message_Send(MsgData msgData)
        {
            return Message_Send(modulePtr_, getNative(msgData), ref msgData.MyMsgId);
        }

        /// [Subscriptions] ///////////////////////////////////////////////////////////////////////////////////////////////
        public int Subscription_Add(SubscrData subData)
        {
            return Subscription_Create(modulePtr_, getNative(subData), ref subData.MySubId);
        }

        public int Subscription_Delete(SubscriptionId subId)
        {
            return Subscription_Destroy(modulePtr_, subId);
        }


        /// [Devices] ///////////////////////////////////////////////////////////////////////////////////////////////
        public int Dvc_SetVideoParams(VideoData vdoData)
        {
            return Dvc_SetVideoParams(modulePtr_, getNative(vdoData));
        }

        public uint Dvc_GetPlayoutDevices()
        {
            uint numberOfDevices=0;
            int err = Dvc_GetPlayoutDevices(modulePtr_, ref numberOfDevices);
            return (err==ErrorCode.kNoErr) ? numberOfDevices : 0;
        }
        
        public uint Dvc_GetRecordingDevices()
        {
            uint numberOfDevices = 0;
            int err = Dvc_GetRecordingDevices(modulePtr_, ref numberOfDevices);
            return (err == ErrorCode.kNoErr) ? numberOfDevices : 0;
        }
        
        public uint Dvc_GetVideoDevices()
        {
            uint numberOfDevices = 0;
            int err = Dvc_GetVideoDevices(modulePtr_, ref numberOfDevices);
            return (err == ErrorCode.kNoErr) ? numberOfDevices : 0;
        }

        public string Dvc_GetPlayoutDevice(uint index, ref string guid)
        {
            uint nameLen = 64, guidLen = 64;
            var nameBuilder = new StringBuilder((int)(nameLen + 1));
            var guidBuilder = new StringBuilder((int)(guidLen + 1));
            Dvc_GetPlayoutDevice(modulePtr_, index, nameBuilder, nameLen, guidBuilder, guidLen);
            guid = guidBuilder.ToString();
            return nameBuilder.ToString();
        }
        
        public string Dvc_GetRecordingDevice(uint index, ref string guid)
        {
            uint nameLen = 64, guidLen = 64;
            var nameBuilder = new StringBuilder((int)(nameLen + 1));
            var guidBuilder = new StringBuilder((int)(guidLen + 1));
            Dvc_GetRecordingDevice(modulePtr_, index, nameBuilder, nameLen, guidBuilder, guidLen);
            guid = guidBuilder.ToString();
            return nameBuilder.ToString();
        }
        
        public string Dvc_GetVideoDevice(uint index, ref string guid)
        {
            uint nameLen = 64, guidLen = 64;
            var nameBuilder = new StringBuilder((int)(nameLen + 1));
            var guidBuilder = new StringBuilder((int)(guidLen + 1));
            Dvc_GetVideoDevice(modulePtr_, index, nameBuilder, nameLen, guidBuilder, guidLen);
            guid = guidBuilder.ToString();
            return nameBuilder.ToString();
        }
        
        public int Dvc_SetPlayoutDevice(uint index)
        {
            return Dvc_SetPlayoutDevice(modulePtr_, index);
        }

        public int Dvc_SetRecordingDevice(uint index)
        {
            return Dvc_SetRecordingDevice(modulePtr_, index);
        }

        public int Dvc_SetVideoDevice(uint index)
        {
            return Dvc_SetVideoDevice(modulePtr_, index);
        }

        /// [Module] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Module_Create();
        [DllImport(DllName)]
        private static extern int Module_Initialize(IntPtr modulePtr, IntPtr iniDataPtr);
        [DllImport(DllName)]
        private static extern int Module_UnInitialize(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern bool Module_IsInitialized(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern IntPtr Module_HomeFolder(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern IntPtr Module_Version(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern uint Module_VersionCode(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern void Module_WriteLog(IntPtr modulePtr,
                        [MarshalAs(UnmanagedType.LPUTF8Str)] string text);
        [DllImport(DllName)]
        private static extern IntPtr GetErrorText(int code);


        /// [Ini] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Ini_GetDefault();
        [DllImport(DllName)]
        private static extern void Ini_SetLicense(IntPtr ini, [MarshalAs(UnmanagedType.LPUTF8Str)] string license);
        [DllImport(DllName)]
        private static extern void Ini_SetLogLevelFile(IntPtr ini, LogLevel logLevel);
        [DllImport(DllName)]
        private static extern void Ini_SetLogLevelIde(IntPtr ini, LogLevel logLevel);
        [DllImport(DllName)]
        private static extern void Ini_SetShareUdpTransport(IntPtr ini, bool shareUdpTransport);
        [DllImport(DllName)]
        private static extern void Ini_SetAllocStrArg(IntPtr ini, bool callbackAllocStringArgs);
        [DllImport(DllName)]
        private static extern void Ini_SetUseExternalRinger(IntPtr ini, bool useExternalRinger);
        [DllImport(DllName)]
        private static extern void Ini_SetDmpOnUnhandledExc(IntPtr ini, bool writeDmpUnhandledExc);
        [DllImport(DllName)]
        private static extern void Ini_SetTlsVerifyServer(IntPtr ini, bool tlsVerifyServer);
        [DllImport(DllName)]
        private static extern void Ini_SetSingleCallMode(IntPtr ini, bool singleCallMode);
        [DllImport(DllName)]
        private static extern void Ini_SetRtpStartPort(IntPtr ini, ushort rtpStartPort);
        [DllImport(DllName)]
        private static extern void Ini_SetHomeFolder(IntPtr ini, [MarshalAs(UnmanagedType.LPUTF8Str)] string homeFolder);
        [DllImport(DllName)]
        private static extern void Ini_SetBrandName(IntPtr ini, [MarshalAs(UnmanagedType.LPUTF8Str)] string brandName);
        [DllImport(DllName)]
        private static extern void Ini_AddDnsServer(IntPtr ini, [MarshalAs(UnmanagedType.LPUTF8Str)]  string dns);
        [DllImport(DllName)]
        private static extern void Ini_SetUseDnsSrv(IntPtr ini, bool enabled);
        [DllImport(DllName)]
        private static extern void Ini_SetRecordStereo(IntPtr ini, bool enabled);
        [DllImport(DllName)]
        private static extern void Ini_SetVideoCallEnabled(IntPtr ini, bool enabled);
        [DllImport(DllName)]
        private static extern void Ini_SetAes128Sha32Enabled(IntPtr ini, bool enabled);
        [DllImport(DllName)]
        private static extern void Ini_SetVUmeterEnabled(IntPtr ini, bool enabled);

        private static IntPtr getNative(IniData iniData)
        {
            IntPtr ptr = Ini_GetDefault();
            if (iniData.License              != null) Ini_SetLicense(ptr,           iniData.License);
            if (iniData.LogLevelFile         != null) Ini_SetLogLevelFile(ptr,      iniData.LogLevelFile.Value);
            if (iniData.LogLevelIde          != null) Ini_SetLogLevelIde(ptr,       iniData.LogLevelIde.Value);
            if (iniData.ShareUdpTransport    != null) Ini_SetShareUdpTransport(ptr, iniData.ShareUdpTransport.Value);
            if (iniData.WriteDmpUnhandledExc != null) Ini_SetDmpOnUnhandledExc(ptr, iniData.WriteDmpUnhandledExc.Value);
            if (iniData.SingleCallMode       != null) Ini_SetSingleCallMode(ptr,    iniData.SingleCallMode.Value);
            if (iniData.RtpStartPort         != null) Ini_SetRtpStartPort(ptr,      iniData.RtpStartPort.Value);
            if (iniData.HomeFolder           != null) Ini_SetHomeFolder(ptr,        iniData.HomeFolder);
            if (iniData.BrandName            != null) Ini_SetBrandName(ptr,         iniData.BrandName);
            if (iniData.UseDnsSrv            != null) Ini_SetUseDnsSrv(ptr,         iniData.UseDnsSrv.Value);
            if (iniData.RecordStereo         != null) Ini_SetRecordStereo(ptr,      iniData.RecordStereo.Value);
            if (iniData.VideoCallEnabled     != null) Ini_SetVideoCallEnabled(ptr,  iniData.VideoCallEnabled.Value);  
            if (iniData.Aes128Sha32Enabled   != null) Ini_SetAes128Sha32Enabled(ptr,iniData.Aes128Sha32Enabled.Value);
            if (iniData.VUmeterEnabled       != null) Ini_SetVUmeterEnabled(ptr,    iniData.VUmeterEnabled.Value);

            if (iniData.DnsServers != null)
            {
                foreach (var dns in iniData.DnsServers) 
                    Ini_AddDnsServer(ptr, dns);
            }
            return ptr;
        }

        /// [Acc] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Acc_GetDefault();
        [DllImport(DllName)]
        private static extern void Acc_SetSipServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipServer);
        [DllImport(DllName)]
        private static extern void Acc_SetSipExtension(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipExtension);
        [DllImport(DllName)]
        private static extern void Acc_SetSipAuthId(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipAuthId);
        [DllImport(DllName)]
        private static extern void Acc_SetSipPassword(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipPassword);
        [DllImport(DllName)]
        private static extern void Acc_SetExpireTime(IntPtr acc, uint expireTime);
        [DllImport(DllName)]
        private static extern void Acc_SetSipProxyServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipProxyServer);
        
        [DllImport(DllName)]        
        private static extern void Acc_SetStunServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string stunServer);
        [DllImport(DllName)]
        private static extern void Acc_SetTurnServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string turnServer);
        [DllImport(DllName)]
        private static extern void Acc_SetTurnUser(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string turnUser);
        [DllImport(DllName)]
        private static extern void Acc_SetTurnPassword(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string turnPassword);

        [DllImport(DllName)]
        private static extern void Acc_SetUserAgent(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string userAgent);
        [DllImport(DllName)]
        private static extern void Acc_SetDisplayName(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string displayName);
        [DllImport(DllName)]
        private static extern void Acc_SetInstanceId(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string instanceId);
        [DllImport(DllName)]
        private static extern void Acc_SetRingToneFile(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string ringTonePath);        

        [DllImport(DllName)]
        private static extern void Acc_SetSecureMediaMode(IntPtr acc, SecureMedia mode);
        [DllImport(DllName)]
        private static extern void Acc_SetUseSipSchemeForTls(IntPtr acc, bool useSipSchemeForTls);
        [DllImport(DllName)]
        private static extern void Acc_SetRtcpMuxEnabled(IntPtr acc, bool rtcpMuxEnabled);
        [DllImport(DllName)]
        private static extern void Acc_SetIceEnabled(IntPtr acc, bool iceEnabled);

        [DllImport(DllName)]
        private static extern void Acc_SetKeepAliveTime(IntPtr acc, uint keepAliveTimeSec);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspProtocol(IntPtr acc, SipTransport transp);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspPort(IntPtr acc, ushort transpPort);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspTlsCaCert(IntPtr acc, 
                                    [MarshalAs(UnmanagedType.LPUTF8Str)] string pathToCaCertPem);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspBindAddr(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string ipAddr);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspPreferIPv6(IntPtr acc, bool prefer);

        [DllImport(DllName)]
        private static extern void Acc_AddXHeader(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string header,
                                                              [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
        [DllImport(DllName)]
        private static extern void Acc_SetRewriteContactIp(IntPtr acc, bool enabled);

        [DllImport(DllName)]
        private static extern void Acc_SetVerifyIncomingCall(IntPtr acc, bool enabled);


        [DllImport(DllName)]
        private static extern void Acc_AddAudioCodec(IntPtr acc, AudioCodec codec);
        [DllImport(DllName)]
        private static extern void Acc_AddVideoCodec(IntPtr acc, VideoCodec codec);
        [DllImport(DllName)]
        private static extern void Acc_ResetAudioCodecs(IntPtr acc);
        [DllImport(DllName)]
        private static extern void Acc_ResetVideoCodecs(IntPtr acc);
        
        private static IntPtr getNative(AccData accData)
        {
            IntPtr ptr = Acc_GetDefault();
            Acc_SetSipServer(ptr,    accData.SipServer);
            Acc_SetSipExtension(ptr, accData.SipExtension);
            Acc_SetSipPassword(ptr,  accData.SipPassword);
            Acc_SetExpireTime(ptr,   accData.ExpireTime);

            if (accData.SipAuthId         != null) Acc_SetSipAuthId(ptr,          accData.SipAuthId);
            if (accData.SipProxyServer    != null) Acc_SetSipProxyServer(ptr,     accData.SipProxyServer);

            if (accData.UserAgent         != null) Acc_SetUserAgent(ptr,          accData.UserAgent);
            if (accData.DisplayName       != null) Acc_SetDisplayName(ptr,        accData.DisplayName);
            if (accData.InstanceId        != null) Acc_SetInstanceId(ptr,         accData.InstanceId);
            if (accData.RingToneFile      != null) Acc_SetRingToneFile(ptr,       accData.RingToneFile);

            if (accData.SecureMediaMode   != null) Acc_SetSecureMediaMode(ptr,    accData.SecureMediaMode.Value);
            if (accData.UseSipSchemeForTls!= null) Acc_SetUseSipSchemeForTls(ptr, accData.UseSipSchemeForTls.Value);
            if (accData.RtcpMuxEnabled    != null) Acc_SetRtcpMuxEnabled(ptr,     accData.RtcpMuxEnabled.Value);
            if (accData.KeepAliveTime     != null) Acc_SetKeepAliveTime(ptr,      accData.KeepAliveTime.Value);
            if (accData.IceEnabled        != null) Acc_SetIceEnabled(ptr,         accData.IceEnabled.Value);
            if (accData.StunServer        != null) Acc_SetStunServer(ptr,         accData.StunServer);

            if (accData.TranspProtocol    != null) Acc_SetTranspProtocol(ptr,     accData.TranspProtocol.Value);
            if (accData.TranspPort        != null) Acc_SetTranspPort(ptr,         accData.TranspPort.Value);
            if (accData.TranspTlsCaCert   != null) Acc_SetTranspTlsCaCert(ptr,    accData.TranspTlsCaCert);
            if (accData.TranspBindAddr    != null) Acc_SetTranspBindAddr(ptr,     accData.TranspBindAddr);
            if (accData.TranspPreferIPv6  != null) Acc_SetTranspPreferIPv6(ptr,   accData.TranspPreferIPv6.Value);
            if (accData.RewriteContactIp  != null) Acc_SetRewriteContactIp(ptr,   accData.RewriteContactIp.Value);
            if (accData.VerifyIncomingCall!= null) Acc_SetVerifyIncomingCall(ptr, accData.VerifyIncomingCall.Value);

            if (accData.AudioCodecs != null)
            {
                Acc_ResetAudioCodecs(ptr);
                foreach (var ac in accData.AudioCodecs)
                    Acc_AddAudioCodec(ptr, ac);
            }

            if (accData.VideoCodecs != null)
            {
                Acc_ResetVideoCodecs(ptr);
                foreach (var vc in accData.VideoCodecs)
                    Acc_AddVideoCodec(ptr, vc);
            }

            if (accData.Xheaders != null)
            {
                foreach (var hdr in accData.Xheaders) 
                    Acc_AddXHeader(ptr, hdr.Key, hdr.Value);
            }

            return ptr;
        }


        /// [Accounts] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern int Account_Add(IntPtr module, IntPtr acc, ref AccountId accId);
        [DllImport(DllName)]
        private static extern int Account_Update(IntPtr module, IntPtr acc, AccountId accId);
        [DllImport(DllName)]
        private static extern int Account_GetRegState(IntPtr module, AccountId accId, ref RegState state);
        [DllImport(DllName)]
        private static extern int Account_Register(IntPtr module, AccountId accId, uint expireTime);
        [DllImport(DllName)]
        private static extern int Account_Unregister(IntPtr module, AccountId accId);
        [DllImport(DllName)]
        private static extern int Account_Delete(IntPtr module, AccountId accId);


        /// [Dest] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Dest_GetDefault();
        [DllImport(DllName)]
        private static extern void Dest_SetExtension(IntPtr dest, [MarshalAs(UnmanagedType.LPUTF8Str)] string extension);
        [DllImport(DllName)]
        private static extern void Dest_SetAccountId(IntPtr dest, AccountId accId);
        [DllImport(DllName)]
        private static extern void Dest_SetVideoCall(IntPtr dest, bool video);
        [DllImport(DllName)]
        private static extern void Dest_SetInviteTimeout(IntPtr dest, int inviteTimeoutSec);
        [DllImport(DllName)]
        private static extern void Dest_SetDisplayName(IntPtr dest, [MarshalAs(UnmanagedType.LPUTF8Str)] string displayName);
        [DllImport(DllName)]
        private static extern void Dest_AddXHeader(IntPtr dest, [MarshalAs(UnmanagedType.LPUTF8Str)] string header,
                                                                [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
        private static IntPtr getNative(DestData destData)
        {
            IntPtr ptr = Dest_GetDefault();
            Dest_SetExtension(ptr, destData.ToExt);
            Dest_SetAccountId(ptr, destData.FromAccId);
            Dest_SetVideoCall(ptr, destData.WithVideo);
            
            if (destData.InviteTimeout != null) 
                Dest_SetInviteTimeout(ptr, destData.InviteTimeout.Value);

            if (destData.DisplayName != null)
                Dest_SetDisplayName(ptr, destData.DisplayName);

            if (destData.Xheaders != null)
            {
                foreach (var hdr in destData.Xheaders)
                    Dest_AddXHeader(ptr, hdr.Key, hdr.Value);
            }

            return ptr;
        }

        /// [Calls] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern int Call_Invite(IntPtr module, IntPtr destination, ref CallId callId);
        [DllImport(DllName)]
        private static extern int Call_Reject(IntPtr module, CallId callId, uint statusCode);
        [DllImport(DllName)]
        private static extern int Call_Accept(IntPtr module, CallId callId, bool withVideo);
        [DllImport(DllName)]
        private static extern int Call_Hold(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern int Call_GetHoldState(IntPtr module, CallId callId, ref HoldState state);
        [DllImport(DllName)]
        private static extern int Call_GetNonce(IntPtr module, CallId callId, 
                                       [MarshalAs(UnmanagedType.LPStr)] StringBuilder? nonceVal, 
                                        ref uint nonceValLen);
        [DllImport(DllName)]
        private static extern int Call_GetSipHeader(IntPtr module, CallId callId,
                                [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrName,
                                [MarshalAs(UnmanagedType.LPStr)] StringBuilder? hdrVal, 
                                ref uint hdrValLen);
        [DllImport(DllName)]
        private static extern int Call_GetStats(IntPtr module, CallId callId,
                                [MarshalAs(UnmanagedType.LPStr)] StringBuilder? statsVal,
                                ref uint statsValLen);

        [DllImport(DllName)]
        private static extern int Call_MuteMic(IntPtr module, CallId callId, bool mute);
        [DllImport(DllName)]
        private static extern int Call_MuteCam(IntPtr module, CallId callId, bool mute);
        [DllImport(DllName)]
        private static extern int Call_SendDtmf(IntPtr module, CallId callId,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string dtmfs, 
                                        Int16 durationMs, Int16 intertoneGapMs, DtmfMethod method);
        [DllImport(DllName)]
        private static extern int Call_PlayFile(IntPtr module, CallId callId, 
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string pathToMp3File, bool loop,
                                        ref PlayerId playerId);
        [DllImport(DllName)]
        private static extern int Call_StopFile(IntPtr module, PlayerId playerId);
        [DllImport(DllName)]
        private static extern int Call_RecordFile(IntPtr module, CallId callId,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string pathToMp3File);
        [DllImport(DllName)]
        private static extern int Call_StopRecordFile(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern int Call_TransferBlind(IntPtr module, CallId callId,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string toExt);
        [DllImport(DllName)]
        private static extern int Call_TransferAttended(IntPtr module, CallId fromCallId, 
                                        CallId toCallId);
        [DllImport(DllName)]
        private static extern int Call_SetVideoWindow(IntPtr module, CallId callId, IntPtr hwnd);

        [DllImport(DllName)]
        private static extern int Call_Bye(IntPtr module, CallId callId);

        [DllImport(DllName)]
        private static extern int Call_Renegotiate(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern int Call_UpgradeToVideo(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern int Call_StopRingtone(IntPtr module);

        /// [Mixer] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern int Mixer_SwitchToCall(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern int Mixer_MakeConference(IntPtr module);


        /// [Messages] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern int Message_Send(IntPtr module, IntPtr msg, ref MessageId msgId);
        

        /// [MsgData] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Msg_GetDefault();
        [DllImport(DllName)]
        private static extern void Msg_SetExtension(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string extension);
        [DllImport(DllName)]
        private static extern void Msg_SetAccountId(IntPtr sub, SubscriptionId subId);
        [DllImport(DllName)]
        private static extern void Msg_SetBody(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string body);

        private static IntPtr getNative(MsgData msgData)
        {
            IntPtr ptr = Msg_GetDefault();
            Msg_SetExtension(ptr, msgData.ToExt);
            Msg_SetAccountId(ptr, msgData.FromAccId);
            Msg_SetBody(ptr, msgData.Body);
            return ptr;
        }

        /// [Subscriptions] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern int Subscription_Create(IntPtr module, IntPtr sub, ref SubscriptionId subId);
        [DllImport(DllName)]
        private static extern int Subscription_Destroy(IntPtr module, SubscriptionId subId);


        /// [SubscrData] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Subscr_GetDefault();
        [DllImport(DllName)]
        private static extern void Subscr_SetExtension(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string extension);
        [DllImport(DllName)]
        private static extern void Subscr_SetAccountId(IntPtr sub, SubscriptionId subId);
        [DllImport(DllName)]
        private static extern void Subscr_SetMimeSubtype(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string mimeType);
        [DllImport(DllName)]
        private static extern void Subscr_SetEventType(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string eventType);
        [DllImport(DllName)]
        private static extern void Subscr_SetExpireTime(IntPtr dest, uint expireTimeSec);
        
        private static IntPtr getNative(SubscrData subData)
        {
            IntPtr ptr = Subscr_GetDefault();
            Subscr_SetExtension(ptr,   subData.ToExt);
            Subscr_SetAccountId(ptr,   subData.FromAccId);
            Subscr_SetMimeSubtype(ptr, subData.MimeSubType);
            Subscr_SetEventType(ptr,   subData.EventType);

            if (subData.ExpireTime != null)
                Subscr_SetExpireTime(ptr, subData.ExpireTime.Value);

            return ptr;
        }

        /// [VideoData] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Vdo_GetDefault();
        [DllImport(DllName)]
        private static extern void Vdo_SetNoCameraImgPath(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string pathToJpg);
        [DllImport(DllName)]
        private static extern void Vdo_SetFramerate(IntPtr sub, int fps);
        [DllImport(DllName)]
        private static extern void Vdo_SetBitrate(IntPtr sub, int bitrateKbps);
        [DllImport(DllName)]
        private static extern void Vdo_SetHeight(IntPtr sub, int height);
        [DllImport(DllName)]
        private static extern void Vdo_SetWidth(IntPtr dest, int width);
        [DllImport(DllName)]
        private static extern void Vdo_SetRotation(IntPtr dest, int degrees);

        private static IntPtr getNative(VideoData vdoData)
        {
            IntPtr ptr = Vdo_GetDefault();
            if (vdoData.noCameraImgPath != null) Vdo_SetNoCameraImgPath(ptr, vdoData.noCameraImgPath);
            if (vdoData.framerateFps != null)    Vdo_SetFramerate(ptr, vdoData.framerateFps.Value);
            if (vdoData.bitrateKbps != null)     Vdo_SetBitrate(ptr, vdoData.bitrateKbps.Value);
            if (vdoData.height != null)          Vdo_SetHeight(ptr, vdoData.height.Value);
            if (vdoData.width != null)           Vdo_SetWidth(ptr, vdoData.width.Value);
            if (vdoData.rotation != null)        Vdo_SetRotation(ptr, vdoData.rotation.Value);
            return ptr;
        }


        /// [Devices] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern int Dvc_SetVideoParams(IntPtr module, IntPtr vdo);

        [DllImport(DllName)]
        private static extern int Dvc_GetPlayoutDevices(IntPtr module, ref uint numberOfDevices);
        [DllImport(DllName)]
        private static extern int Dvc_GetRecordingDevices(IntPtr module, ref uint numberOfDevices);
        [DllImport(DllName)]
        private static extern int Dvc_GetVideoDevices(IntPtr module, ref uint numberOfDevices);

        [DllImport(DllName)]
        private static extern int Dvc_GetPlayoutDevice(IntPtr module, uint index,
                          [MarshalAs(UnmanagedType.LPStr)] StringBuilder? name, uint nameLength,
                          [MarshalAs(UnmanagedType.LPStr)] StringBuilder? guid, uint guidLength);
        [DllImport(DllName)]
        private static extern int Dvc_GetRecordingDevice(IntPtr module, uint index,
                          [MarshalAs(UnmanagedType.LPStr)] StringBuilder? name, uint nameLength,
                          [MarshalAs(UnmanagedType.LPStr)] StringBuilder? guid, uint guidLength);
        [DllImport(DllName)]
        private static extern int Dvc_GetVideoDevice(IntPtr module, uint index,
                          [MarshalAs(UnmanagedType.LPStr)] StringBuilder? name, uint nameLength,
                          [MarshalAs(UnmanagedType.LPStr)] StringBuilder? guid, uint guidLength);

        [DllImport(DllName)]
        private static extern int Dvc_SetPlayoutDevice(IntPtr module, uint index);

        [DllImport(DllName)]
        private static extern int Dvc_SetRecordingDevice(IntPtr module, uint index);

        [DllImport(DllName)]
        private static extern int Dvc_SetVideoDevice(IntPtr module, uint index);
        

        /// [Callbacks] ///////////////////////////////////////////////////////////////////////////////////////////////
        private delegate void OnTrialModeNotified();
        private delegate void OnDevicesAudioChanged();
        private delegate void OnAccountRegState(AccountId accId, RegState state, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
        private delegate void OnSubscriptionState(SubscriptionId subId, SubscriptionState state,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
        private delegate void OnNetworkState([MarshalAs(UnmanagedType.LPUTF8Str)] string name,
                                            NetworkState state);
        private delegate void OnPlayerState(PlayerId playerId, PlayerState state);
        private delegate void OnRingerState(bool start);

        private delegate void OnCallIncoming(CallId callId, AccountId accId, bool withVideo, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo);
        private delegate void OnCallConnected(CallId callId, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo,
                                            bool withVideo);
        private delegate void OnCallTerminated(CallId callId, uint statusCode);
        private delegate void OnCallProceeding(CallId callId, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
        private delegate void OnCallTransferred(CallId callId, uint statusCode);
        private delegate void OnCallRedirected(CallId origCallId, CallId relatedCallId, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string referTo);
        private delegate void OnCallDtmfReceived(CallId callId, ushort tone);
        private delegate void OnCallHeld(CallId callId, HoldState state);
        private delegate void OnCallSwitched(CallId callId);

        private delegate void OnMessageSentState(MessageId messageId, bool success,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
        private delegate void OnMessageIncoming(MessageId messageId, AccountId accId,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string body);

        private delegate void OnSipNotify(AccountId accId,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrEvent,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string body);

        private delegate void OnVuMeterLevel(int micLevel, int spkLevel);

        [DllImport(DllName)]
        private static extern int Callback_SetTrialModeNotified(IntPtr module, OnTrialModeNotified callback);
        [DllImport(DllName)]
        private static extern int Callback_SetDevicesAudioChanged(IntPtr module, OnDevicesAudioChanged callback);
        [DllImport(DllName)]
        private static extern int Callback_SetAccountRegState(IntPtr module, OnAccountRegState callback);
        [DllImport(DllName)]
        private static extern int Callback_SetSubscriptionState(IntPtr module, OnSubscriptionState callback);
        [DllImport(DllName)]
        private static extern int Callback_SetNetworkState(IntPtr module, OnNetworkState callback);
        [DllImport(DllName)]
        private static extern int Callback_SetPlayerState(IntPtr module, OnPlayerState callback);
        [DllImport(DllName)]
        private static extern int Callback_SetRingerState(IntPtr module, OnRingerState callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallProceeding(IntPtr module, OnCallProceeding callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallTerminated(IntPtr module, OnCallTerminated callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallConnected(IntPtr module, OnCallConnected callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallIncoming(IntPtr module, OnCallIncoming callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallDtmfReceived(IntPtr module, OnCallDtmfReceived callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallTransferred(IntPtr module, OnCallTransferred callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallRedirected(IntPtr module, OnCallRedirected callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallSwitched(IntPtr module, OnCallSwitched callback);
        [DllImport(DllName)]
        private static extern int Callback_SetCallHeld(IntPtr module, OnCallHeld callback);
        [DllImport(DllName)]
        private static extern int Callback_SetMessageSentState(IntPtr module, OnMessageSentState callback);
        [DllImport(DllName)]
        private static extern int Callback_SetMessageIncoming(IntPtr module, OnMessageIncoming callback);

        [DllImport(DllName)]
        private static extern int Callback_SetSipNotify(IntPtr module, OnSipNotify callback);
        [DllImport(DllName)]
        private static extern int Callback_SetVuMeterLevel(IntPtr module, OnVuMeterLevel callback);

        void OnTrialModeNotifiedCallback()
        {
            eventDelegate_?.OnTrialModeNotified();
        }

        void OnDevicesAudioChangedCallback()
        {
            eventDelegate_?.OnDevicesAudioChanged();
        }

        void OnAccountRegStateCallback(AccountId accId, RegState state,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string response)
        {
            eventDelegate_?.OnAccountRegState(accId, state, response);
        }
                
        void OnSubscriptionStateCallback(SubscriptionId subId, SubscriptionState state,
                                         [MarshalAs(UnmanagedType.LPUTF8Str)] string response)
        {
            eventDelegate_?.OnSubscriptionState(subId, state, response);
        }

        void OnNetworkStateCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string name,
                           NetworkState state)
        {
            eventDelegate_?.OnNetworkState(name, state);
        }

        void OnPlayerStateCallback(PlayerId playerId, PlayerState state)
        {
            eventDelegate_?.OnPlayerState(playerId, state);
        }

        void OnRingerStateCallback(bool start)
        {
            eventDelegate_?.OnRingerState(start);
        }

        void OnCallIncomingCallback(CallId callId, AccountId accId, bool withVideo,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo)
        {
            eventDelegate_?.OnCallIncoming(callId, accId, withVideo, hdrFrom, hdrFrom);
        }

        void OnCallConnectedCallback(CallId callId,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo,
                           bool withVideo)
        {
            eventDelegate_?.OnCallConnected(callId, hdrFrom, hdrFrom, withVideo);
        }

        void OnCallTerminatedCallback(CallId callId, uint statusCode)
        {
            eventDelegate_?.OnCallTerminated(callId, statusCode);
        }

        void OnCallProceedingCallback(CallId callId,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string response)
        {
            eventDelegate_?.OnCallProceeding(callId, response);
        }

        void OnCallTransferredCallback(CallId callId, uint statusCode)
        {
            eventDelegate_?.OnCallTransferred(callId, statusCode);
        }

        void OnCallRedirectedCallback(CallId origCallId, CallId relatedCallId,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string referTo)
        {
            eventDelegate_?.OnCallRedirected(origCallId, relatedCallId, referTo);
        }

        void OnCallDtmfReceivedCallback(CallId callId, ushort tone)
        {
            eventDelegate_?.OnCallDtmfReceived(callId, tone);
        }


        void OnCallHeldCallback(CallId callId, HoldState state)
        {
            eventDelegate_?.OnCallHeld(callId, state);
        }

        void OnCallSwitchedCallback(CallId callId)
        {
            eventDelegate_?.OnCallSwitched(callId);
        }

        void OnMessageSentStateCallback(MessageId messageId, bool success, string response)
        {
            eventDelegate_?.OnMessageSentState(messageId, success, response);
        }

        void OnMessageIncomingCallback(MessageId messageId, AccountId accId, string hdrFrom, string body)
        {
            eventDelegate_?.OnMessageIncoming(messageId, accId, hdrFrom, body);
        }

        void OnSipNotifyCallback(AccountId accId, string hdrEvent, string body)
        {
            eventDelegate_?.OnSipNotify(accId, hdrEvent, body);
        }

        void OnVuMeterLevelCallback(int micLevel, int spkLevel)
        {
            eventDelegate_?.OnVuMeterLevel(micLevel, spkLevel);
        }

    }//Module
}
