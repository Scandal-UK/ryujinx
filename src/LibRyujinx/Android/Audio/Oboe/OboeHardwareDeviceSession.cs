using Ryujinx.Audio.Backends.Common;
using Ryujinx.Audio.Common;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Threading;

namespace LibRyujinx.Shared.Audio.Oboe
{
    internal class OboeHardwareDeviceSession : HardwareDeviceSessionOutputBase
    {
        private OboeHardwareDeviceDriver _driver;
        private bool _isClosed;
        private bool _isWorkerActive;
        private Queue<OboeAudioBuffer> _queuedBuffers;
        private bool _isActive;
        private ulong _playedSampleCount;
        private Thread _workerThread;
        private ManualResetEvent _updateRequiredEvent;
        private IntPtr _session;
        private object _queueLock = new object();
        private object _trackLock = new object();

        public OboeHardwareDeviceSession(OboeHardwareDeviceDriver driver, IVirtualMemoryManager memoryManager, SampleFormat requestedSampleFormat, uint requestedSampleRate, uint requestedChannelCount, float requestedVolume) : base(memoryManager, requestedSampleFormat, requestedSampleRate, requestedChannelCount)
        {
            _driver = driver;
            _isActive = false;
            _playedSampleCount = 0;
            _isWorkerActive = true;
            _queuedBuffers = new Queue<OboeAudioBuffer>();
            _updateRequiredEvent = driver.GetUpdateRequiredEvent();

            _session = OboeInterop.CreateSession((int)requestedSampleFormat, requestedSampleRate, requestedChannelCount);

            _workerThread = new Thread(Update);
            _workerThread.Name = $"HardwareDeviceSession.Android.Track";
            _workerThread.Start();

            SetVolume(requestedVolume);
        }

        public override void UnregisterBuffer(AudioBuffer buffer) { }

        public unsafe void Update(object ignored)
        {
            while (_isWorkerActive)
            {
                bool needUpdate = false;

                bool hasBuffer;

                OboeAudioBuffer buffer;

                lock (_queueLock)
                {
                    hasBuffer = _queuedBuffers.TryPeek(out buffer);
                }

                while (hasBuffer)
                {
                    StartIfNotPlaying();

                    if (_isClosed)
                        break;

                    fixed(byte* ptr = buffer.Data)
                        OboeInterop.WriteToSession(_session, (ulong)ptr, buffer.SampleCount);

                    lock (_queueLock)
                    {
                        _playedSampleCount += buffer.SampleCount;

                        _queuedBuffers.TryDequeue(out _);
                    }

                    needUpdate = true;

                    lock (_queueLock)
                    {
                        hasBuffer = _queuedBuffers.TryPeek(out buffer);
                    }
                }

                if (needUpdate)
                {
                    _updateRequiredEvent.Set();
                }

                // No work
                Thread.Sleep(5);
            }

        }

        public override void Dispose()
        {
            if (_session == 0)
                return;

            PrepareToClose();

            OboeInterop.CloseSession(_session);

            _session = 0;
        }

        public override void PrepareToClose()
        {
            _isClosed = true;
            _isWorkerActive = false;
            _workerThread?.Join();
            Stop();
        }

        private void StartIfNotPlaying()
        {
            lock (_trackLock)
            {
                if (_isClosed)
                    return;

                if (OboeInterop.IsPlaying(_session) == 0)
                {
                    Start();
                }
            }
        }

        public override void QueueBuffer(AudioBuffer buffer)
        {
            lock (_queueLock)
            {
                OboeAudioBuffer driverBuffer = new OboeAudioBuffer(buffer.DataPointer, buffer.Data, GetSampleCount(buffer));

                _queuedBuffers.Enqueue(driverBuffer);

                if (_isActive)
                {
                    StartIfNotPlaying();
                }
            }
        }

        public override float GetVolume()
        {
            return OboeInterop.GetSessionVolume(_session);
        }

        public override ulong GetPlayedSampleCount()
        {
            lock (_queueLock)
            {
                return _playedSampleCount;
            }
        }

        public override void SetVolume(float volume)
        {
            volume = 1;
            OboeInterop.SetSessionVolume(_session, volume);
        }

        public override void Start()
        {
            if (_isClosed)
                return;

            OboeInterop.StartSession(_session);
        }

        public override void Stop()
        {
            OboeInterop.StopSession(_session);
        }

        public override bool WasBufferFullyConsumed(AudioBuffer buffer)
        {
            lock (_queueLock)
            {
                if (!_queuedBuffers.TryPeek(out OboeAudioBuffer driverBuffer))
                {
                    return true;
                }

                return driverBuffer.DriverIdentifier != buffer.DataPointer;
            }
        }
    }
}
