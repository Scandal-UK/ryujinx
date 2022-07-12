using Avalonia.Controls;
using Avalonia.Interactivity;
using Ryujinx.Ava.UI.ViewModels;
using System;

namespace Ryujinx.Ava.UI.Views.Settings
{
    public partial class SettingsNetworkView : UserControl
    {
        public SettingsViewModel ViewModel;

        public SettingsNetworkView()
        {
            InitializeComponent();
        }

        private void GenLdnPassButton_OnClick(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            byte[] code = new byte[4];
            random.NextBytes(code);
            uint codeUint = BitConverter.ToUInt32(code);
            ViewModel.LdnPassphrase = $"Ryujinx-{codeUint:x8}";
        }

        private void ClearLdnPassButton_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.LdnPassphrase = "";
        }
    }
}