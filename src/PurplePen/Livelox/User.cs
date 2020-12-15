namespace PurplePen.Livelox
{
    class User
    {
        public long PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public OAuth2TokenInformation TokenInformation { get; set; }

        public override string ToString()
        {
            return $"{FirstName} {LastName}".Trim();
        }
    }
}