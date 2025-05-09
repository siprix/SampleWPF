﻿using System.Globalization;
using System.Windows;
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
            Siprix.HoldState? holdState = value as Siprix.HoldState?;
            if (holdState == null) return System.Windows.Data.Binding.DoNothing;            
            return (holdState == Siprix.HoldState.Local) ||
                   (holdState == Siprix.HoldState.LocalAndRemote) ? Icons.play_arrow : Icons.pause;
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
            Siprix.HoldState? holdState = value as Siprix.HoldState?;
            if (holdState == null) return System.Windows.Data.Binding.DoNothing;
            return (holdState == Siprix.HoldState.Local) ||
                   (holdState == Siprix.HoldState.LocalAndRemote) ? "Unhold" : "Hold";
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
            Siprix.RegState? regState = value as Siprix.RegState?;
            if (regState == null) return System.Windows.Data.Binding.DoNothing;

            switch (regState)
            {
                case Siprix.RegState.Success:    return Icons.cloud_done;
                case Siprix.RegState.Failed:     return Icons.cloud_off;
                case Siprix.RegState.InProgress: return Icons.sync;
                default:                         return Icons.done;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return Siprix.RegState.Success;
        }
    }


    /// [RegState ColorConverter] ////////////////////////////////////////////////////////////////

    public class RegStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            Siprix.RegState? regState = value as Siprix.RegState?;
            if (regState == null) return System.Windows.Data.Binding.DoNothing;

            switch (regState)
            {
                case Siprix.RegState.Success:    return new SolidColorBrush(Colors.Green);
                case Siprix.RegState.Failed:     return new SolidColorBrush(Colors.Red);
                case Siprix.RegState.InProgress: return new SolidColorBrush(Colors.Blue);
                default: return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return Siprix.RegState.Success;
        }
    }

    /// [MessageSentColorConverter ColorConverter] ////////////////////////////////////////////////////////////////

    public class MessageSentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            bool? sentSuccess = value as bool?;
            return new SolidColorBrush((sentSuccess==true) ? Colors.BlueViolet : Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }    


    /// [BLFState ColorConverter] ////////////////////////////////////////////////////////////////

    public class BLFStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            Siprix.BLFState? blfState = value as Siprix.BLFState?;
            if (blfState == null) return System.Windows.Data.Binding.DoNothing;

            switch (blfState)
            {
                case Siprix.BLFState.SubscriptionDestroyed: return new SolidColorBrush(Colors.Gray);
                case Siprix.BLFState.Terminated:
                case Siprix.BLFState.Unknown: return new SolidColorBrush(Colors.Green); //Ready to make call
                default: return new SolidColorBrush(Colors.Red); //Call in progress
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            return Siprix.BLFState.Unknown;
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

    /// [InverseBooleanToVisibilityConverter] //////////////////////////////////////////////////////////////// <summary>

    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }

}
