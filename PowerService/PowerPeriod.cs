namespace PowerService
{
    public struct PowerPeriod
    {
        public int Period { get; }

        public double Volume { get; private set; }

        public PowerPeriod(int period)
        {
            Period = period;
            Volume = 0.0;
        }

        public void SetVolume(double volume)
        {
            Volume = volume;
        }
    }

}