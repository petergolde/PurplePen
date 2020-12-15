using System;

namespace PurplePen.Livelox.ApiContracts
{
    class Event
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public Organisation[] Organisers { get; set; }

        public TimeInterval TimeInterval { get; set; }

        public Country Country { get; set; }

        public DateTime? HiddenUntil { get; set; }

        public DateTime PublicationTime { get; set; }

        public OrganisationRoleType[] OwnerRoles { get; set; }

        public Person[] OwnerPersons { get; set; }

    }
}