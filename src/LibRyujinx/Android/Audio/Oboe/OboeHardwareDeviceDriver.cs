using Ryujinx.Audio;
using Ryujinx.Audio.Common;
using Ryujinx.Audio.Integration;
using Ryujinx.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Ryujinx.Audio.Integration.IHardwareDeviceDriver;

namespace LibRyujinx.Shared.Audio.Oboe
{
    internal class OboeHardwareDeviceDriver : IHardwareDeviceDriver
    {
        private readonly ManualResetEvent _updateRequiredEvent;
        private readonly ManualResetEvent _pauseEvent;
        private readonly ConcurrentDictionary<OboeHardwareDeviceSession, byte> _sessions;

        public OboeHardwareDeviceDriver()
        {
            _updateRequiredEvent = new ManualResetEvent(false);
            _pauseEvent = new ManualResetEvent(true);
            _sessions = new ConcurrentDictionary<OboeHardwareDeviceSession, byte>();
        }

        public static bool IsSupported => true;

        public ManualResetEvent GetUpdateRequiredEvent()
        {
            return _updateRequiredEvent;
        }

        public ManualResetEvent GetPauseEvent()
        {
            return _pauseEvent;
        }

        public IHardwareDeviceSession OpenDeviceSession(Direction direction, IVirtualMemoryManager memoryManager, SampleFormat sampleFormat, uint sampleRate, uint channelCount, float volume)
        {
            if (channelCount == 0)
            {
                channelCount = 2;
            }

            if (sampleRate == 0)
            {
                sampleRate = Constants.TargetSampleRate;
            }

            if (direction != Direction.Output)
            {
                throw new NotImplementedException("Input direction is currently not implemented on Oboe backend!");
            }

            OboeHardwareDeviceSession session = new OboeHardwareDeviceSession(this, memoryManager, sampleFormat, sampleRate, channelCount, volume);

            _sessions.TryAdd(session, 0);

            return session;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (OboeHardwareDeviceSession session in _sessions.Keys)
                {
                    session.Dispose();
                }

                _pauseEvent.Dispose();

                _sessions.Clear();
            }
        }

        public bool SupportsSampleRate(uint sampleRate)
        {
            return true;
        }

        public bool SupportsSampleFormat(SampleFormat sampleFormat)
        {
            return sampleFormat != SampleFormat.Adpcm;
        }

        public bool SupportsChannelCount(uint channelCount)
        {
            return channelCount == 1 || channelCount == 2 || channelCount == 4 || channelCount == 6;
        }

        public bool SupportsDirection(Direction direction)
        {
            return direction == Direction.Output;
        }
    }
}