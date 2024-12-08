using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO;
using ZXing;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.Pickers;
using ZXing.Common;

namespace MangoSpot8
{
    public partial class SettingsPage : PhoneApplicationPage
    {

        public SettingsPage()
        {
            InitializeComponent();
  

        }

        private void quailty_Checked(object sender, RoutedEventArgs e)
        {
            Settings.LowQualityAudio = true; 
        }

        private void quailty_Unchecked(object sender, RoutedEventArgs e)
        {
            Settings.LowQualityAudio = false; 
        }

    }
}