namespace PurplePen.Livelox.ApiContracts
{
    class ImportableEvent
    {
        public string Name { get; set; }

        public TimeInterval TimeInterval { get; set; }

        public string TimeZone { get; set; }

        public Map[] Maps { get; set; }

        public string[] CourseDataFileNames { get; set; }

        public string[] CourseImageFileNames { get; set; }

        public Event ImportedEvent { get; set; }
        public ImportableEventLink Link { get; set; }
    }
}