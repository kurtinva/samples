﻿/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Globalization;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IoTCoreDefaultApp
{
    public sealed partial class MainPage : Page
    {
        private CoreDispatcher MainPageDispatcher;
        private DispatcherTimer timer;
        private ConnectedDevicePresenter connectedDevicePresenter;

        public MainPage()
        {
            this.InitializeComponent();

            MainPageDispatcher = Window.Current.Dispatcher;

            UpdateBoardInfo();
            UpdateNetworkInfo();
            UpdateDateTime();
            UpdateConnectedDevices();

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            timer = new DispatcherTimer();
            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Start();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Constants.HasDoneOOBEKey))
            {
                ApplicationData.Current.LocalSettings.Values[Constants.HasDoneOOBEKey] = Constants.HasDoneOOBEValue;
            }

            base.OnNavigatedTo(e);
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                UpdateNetworkInfo();
            });
        }

        private void timer_Tick(object sender, object e)
        {
            UpdateDateTime();
        }

        private void UpdateBoardInfo()
        {
            BoardName.Text = DeviceInfoPresenter.GetBoardName();
            BoardImage.Source = new BitmapImage(DeviceInfoPresenter.GetBoardImageUri());

            ulong version = 0;
            if (!ulong.TryParse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion, out version))
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                OSVersion.Text = loader.GetString("OSVersionNotAvailable");
            }
            else
            {
                OSVersion.Text = String.Format(CultureInfo.InvariantCulture,"{0}.{1}.{2}.{3}",
                    (version & 0xFFFF000000000000) >> 48,
                    (version & 0x0000FFFF00000000) >> 32,
                    (version & 0x00000000FFFF0000) >> 16,
                    version & 0x000000000000FFFF);
            }
        }

        private void UpdateDateTime()
        {
            var t = DateTime.Now;
            this.CurrentTime.Text = t.ToString("t", CultureInfo.CurrentCulture);
        }

        private async void UpdateNetworkInfo()
        {
            this.DeviceName.Text = DeviceInfoPresenter.GetDeviceName();
            this.IPAddress1.Text = NetworkPresenter.GetCurrentIpv4Address();
            this.NetworkName1.Text = NetworkPresenter.GetCurrentNetworkName() ?? "Not connected";
            this.NetworkInfo.ItemsSource = await NetworkPresenter.GetNetworkInformation();
        }

        private void UpdateConnectedDevices()
        {
            connectedDevicePresenter = new ConnectedDevicePresenter(MainPageDispatcher);
            this.ConnectedDevices.ItemsSource = connectedDevicePresenter.GetConnectedDevices();
        }

        private void ShutdownButton_Clicked(object sender, RoutedEventArgs e)
        {
            ShutdownDropdown.IsOpen = true;
        }

        private void SettingsButton_Clicked(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(Settings));
        }

        private void Tutorials_Clicked(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(TutorialMainPage));
        }

        private void ShutdownHelper(ShutdownKind kind)
        {
            ShutdownManager.BeginShutdown(kind, TimeSpan.FromSeconds(0.5));
        }

        private void ShutdownListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var item = e.ClickedItem as FrameworkElement;
            if (item == null)
            {
                return;
            }
            switch (item.Name)
            {
                case "ShutdownOption":
                    ShutdownHelper(ShutdownKind.Shutdown);
                    break;
                case "RestartOption":
                    ShutdownHelper(ShutdownKind.Restart);
                    break;
            }
        }
    }
}
