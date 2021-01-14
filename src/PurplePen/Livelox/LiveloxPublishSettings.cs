namespace PurplePen.Livelox
{
    class LiveloxPublishSettings
    {
        public double largeScaleMapResolution { get; set; } = 2.5;
        public double smallScaleMapResolution { get; set; } = 0.75;

        public LiveloxPublishSettings Clone()
        {
            return (LiveloxPublishSettings)base.MemberwiseClone();
        }

        public static bool IsLargeScaleMap(double scale)
        {
            return scale <= 5000;
        }

        public double GetResolution(double scale)
        {
            return IsLargeScaleMap(scale)
                ? largeScaleMapResolution
                : smallScaleMapResolution;
        }
    }
}