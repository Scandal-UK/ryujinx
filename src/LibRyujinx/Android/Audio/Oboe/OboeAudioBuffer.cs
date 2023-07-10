namespace LibRyujinx.Shared.Audio.Oboe
{
    internal class OboeAudioBuffer
    {
        public readonly ulong DriverIdentifier;
        public readonly ulong SampleCount;
        public readonly byte[] Data;
        public ulong SamplePlayed;

        public OboeAudioBuffer(ulong driverIdentifier, byte[] data, ulong sampleCount)
        {
            DriverIdentifier = driverIdentifier;
            Data = data;
            SampleCount = sampleCount;
            SamplePlayed = 0;
        }
    }
}
