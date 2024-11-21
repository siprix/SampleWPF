using Siprix;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SampleWpf
{
    /// [MicMute IconConverter] ////////////////////////////////////////////////////////////////

    public class MicMuteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            bool? micMuted = value as bool?;
            if (micMuted == null) return System.Windows.Data.Binding.DoNothing;
            else                  return micMuted.Value ? Icons.mic_off : Icons.mic;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    /// [CamMute IconConverter] ////////////////////////////////////////////////////////////////

    public class CamMuteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            bool? camMuted = value as bool?;
            if (camMuted == null) return System.Windows.Data.Binding.DoNothing;
            else return camMuted.Value ? Icons.videocam_off : Icons.videocam;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    /// [Mute TextConverter] ////////////////////////////////////////////////////////////////

    public class MuteTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            bool? muted = value as bool?;
            if (muted == null) return System.Windows.Data.Binding.DoNothing;
            else return muted.Value ? "UnMute" : "Mute";
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }
        

    /// [CallDirection IconConverter] ////////////////////////////////////////////////////////////////

    public class CallDirectionIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            bool? isIncoming = value as bool?;
            if (isIncoming == null) return System.Windows.Data.Binding.DoNothing;
            else return isIncoming.Value ? Icons.call_received : Icons.call_made;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }


    /// [HoldState IconConverter] ////////////////////////////////////////////////////////////////

    public class HoldStateIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            HoldState? holdState = value as HoldState?;
            if (holdState == null) return System.Windows.Data.Binding.DoNothing;            
            return (holdState == HoldState.Local) ||
                   (holdState == HoldState.LocalAndRemote) ? Icons.play_arrow : Icons.pause;
        }
    
        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    /// [HoldState TextConverter] ////////////////////////////////////////////////////////////////

    public class HoldStateTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            HoldState? holdState = value as HoldState?;
            if (holdState == null) return System.Windows.Data.Binding.DoNothing;
            return (holdState == HoldState.Local) ||
                   (holdState == HoldState.LocalAndRemote) ? "Unhold" : "Hold";
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }


    /// [RegState IconConverter] ////////////////////////////////////////////////////////////////

    public class RegStateIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            RegState? regState = value as RegState?;
            if (regState == null) return System.Windows.Data.Binding.DoNothing;

            switch (regState)
            {
                case RegState.Success:    return Icons.cloud_done;
                case RegState.Failed:     return Icons.cloud_off;
                case RegState.InProgress: return Icons.sync;
                default:                         return Icons.done;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return RegState.Success;
        }
    }


    /// [RegState ColorConverter] ////////////////////////////////////////////////////////////////

    public class RegStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            RegState? regState = value as RegState?;
            if (regState == null) return System.Windows.Data.Binding.DoNothing;

            switch (regState)
            {
                case RegState.Success:    return new SolidColorBrush(Colors.Green);
                case RegState.Failed:     return new SolidColorBrush(Colors.Red);
                case RegState.InProgress: return new SolidColorBrush(Colors.Blue);
                default: return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return RegState.Success;
        }
    }


    /// [BLFState ColorConverter] ////////////////////////////////////////////////////////////////

    public class BLFStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            BLFState? blfState = value as BLFState?;
            if (blfState == null) return System.Windows.Data.Binding.DoNothing;

            switch (blfState)
            {
                case BLFState.SubscriptionDestroyed: return new SolidColorBrush(Colors.Gray);
                case BLFState.Terminated:
                case BLFState.Unknown: return new SolidColorBrush(Colors.Green); //Ready to make call
                default: return new SolidColorBrush(Colors.Red); //Call in progress
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return BLFState.Unknown;
        }
    }

    /// [SwitchedCall ColorConverter] ////////////////////////////////////////////////////////////////

    public class SwitchedCallColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            bool? isSwitchedCall = value as bool?;
            if (isSwitchedCall == null) return System.Windows.Data.Binding.DoNothing;
            //Highligh current call
            return isSwitchedCall.Value ? new SolidColorBrush(Colors.BlanchedAlmond) : new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }


}
