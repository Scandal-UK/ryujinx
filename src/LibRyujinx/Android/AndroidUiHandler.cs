using LibHac.Tools.Fs;
using Ryujinx.Common.Logging;
using Ryujinx.HLE;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;
using Ryujinx.HLE.Ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LibRyujinx.Android
{
    internal class AndroidUiHandler : IHostUiHandler, IDisposable
    {
        public IHostUiTheme HostUiTheme => throw new NotImplementedException();

        public ManualResetEvent _waitEvent;
        public ManualResetEvent _responseEvent;
        private bool _isDisposed;
        private bool _isOkPressed;
        private long _input;

        public AndroidUiHandler()
        {
            _waitEvent = new ManualResetEvent(false);
            _responseEvent = new ManualResetEvent(false);
        }

        public IDynamicTextInputHandler CreateDynamicTextInputHandler()
        {
            throw new NotImplementedException();
        }

        public bool DisplayErrorAppletDialog(string title, string message, string[] buttonsText)
        {
            LibRyujinx.setUiHandlerTitle(LibRyujinx.storeString(title ?? ""));
            LibRyujinx.setUiHandlerMessage(LibRyujinx.storeString(message ?? ""));
            LibRyujinx.setUiHandlerType(1);

            _responseEvent.Reset();
            Set();
            _responseEvent.WaitOne();

            return _isOkPressed;
        }

        public bool DisplayInputDialog(SoftwareKeyboardUiArgs args, out string userText)
        {
            LibRyujinx.setUiHandlerTitle(LibRyujinx.storeString("Software Keyboard"));
            LibRyujinx.setUiHandlerMessage(LibRyujinx.storeString(args.HeaderText ?? ""));
            LibRyujinx.setUiHandlerWatermark(LibRyujinx.storeString(args.GuideText ?? ""));
            LibRyujinx.setUiHandlerSubtitle(LibRyujinx.storeString(args.SubtitleText ?? ""));
            LibRyujinx.setUiHandlerInitialText(LibRyujinx.storeString(args.InitialText ?? ""));
            LibRyujinx.setUiHandlerMinLength(args.StringLengthMin);
            LibRyujinx.setUiHandlerMaxLength(args.StringLengthMax);
            LibRyujinx.setUiHandlerType(2);
            LibRyujinx.setUiHandlerKeyboardMode((int)args.KeyboardMode);

            _responseEvent.Reset();
            Set();
            _responseEvent.WaitOne();

            userText = _input != -1 ? LibRyujinx.GetStoredString(_input) : "";

            return _isOkPressed;
        }

        public bool DisplayMessageDialog(string title, string message)
        {
            LibRyujinx.setUiHandlerTitle(LibRyujinx.storeString(title ?? ""));
            LibRyujinx.setUiHandlerMessage(LibRyujinx.storeString(message ?? ""));
            LibRyujinx.setUiHandlerType(1);

            _responseEvent.Reset();
            Set();
            _responseEvent.WaitOne();

            return _isOkPressed;
        }

        public bool DisplayMessageDialog(ControllerAppletUiArgs args)
        {
            string playerCount = args.PlayerCountMin == args.PlayerCountMax ? $"exactly {args.PlayerCountMin}" : $"{args.PlayerCountMin}-{args.PlayerCountMax}";

            string message = $"Application requests **{playerCount}** player(s) with:\n\n"
                           + $"**TYPES:** {args.SupportedStyles}\n\n"
                           + $"**PLAYERS:** {string.Join(", ", args.SupportedPlayers)}\n\n"
                           + (args.IsDocked ? "Docked mode set. `Handheld` is also invalid.\n\n" : "")
                           + "_Please reconfigure Input now and then press OK._";

            return DisplayMessageDialog("Controller Applet", message);
        }

        public void ExecuteProgram(Switch device, ProgramSpecifyKind kind, ulong value)
        {
           // throw new NotImplementedException();
        }

        internal void Wait()
        {
            if (_isDisposed)
                return;
            _waitEvent.Reset();
            _waitEvent.WaitOne();
            _waitEvent.Reset();
        }

        internal void Set()
        {
            if (_isDisposed)
                return;
            _waitEvent.Set();
        }

        internal void SetResponse(bool isOkPressed, long input)
        {
            if (_isDisposed)
                return;
            _isOkPressed = isOkPressed;
            _input = input;
            _responseEvent.Set();
        }

        public void Dispose()
        {
            _isDisposed = true;
            _waitEvent.Set();
            _waitEvent.Set();
            _responseEvent.Dispose();
            _waitEvent.Dispose();
        }
    }
}
