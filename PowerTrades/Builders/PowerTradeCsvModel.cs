namespace PowerTrades.Builders
{
    public class PowerTradeCsvModel
    {
        private DateTime volumeDate;
        private DateTime volumeDateUtc;

        public double Volume { get; set; }
        public DateTime VolumeDate { get => volumeDate; set => volumeDate = value; }

        public DateTime VolumeDateUtc { get =>  VolumeDate.ToUniversalTime(); set => volumeDateUtc = value ; }
    }
}