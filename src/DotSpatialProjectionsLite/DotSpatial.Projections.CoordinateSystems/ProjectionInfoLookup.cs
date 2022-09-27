using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotSpatial.Projections
{
    using DotSpatial.Projections.AuthorityCodes;

    public static class ProjectionInfoLookup
    {
        /// <summary>
        /// Using the specified code, this will attempt to look up the related reference information from the appropriate authority code.
        /// </summary>
        /// <param name="authority"> The authority. </param>
        /// <param name="code">  The code. </param>
        /// <returns>ProjectionInfo</returns>
        /// <exception cref="ArgumentOutOfRangeException">Throws when there is no projection for given authority and code</exception>
        public static ProjectionInfo FromAuthorityCode(string authority, int code)
        {
            var pi = AuthorityCodeHandler.Instance[string.Format("{0}:{1}", authority, code)];
            if (pi != null) {
                // we need to copy the projection information because the Authority Codes implementation returns its one and only
                // in memory copy of the ProjectionInfo. Passing it to the caller might introduce unintended results.
                var info = ProjectionInfo.FromProj4String(pi.ToProj4String());
                info.Name = pi.Name;
                info.NoDefs = true;
                info.Authority = authority;
                info.AuthorityCode = code;
                info.EpsgCode = code;
                return info;
            }

            throw new ArgumentOutOfRangeException("authority", "Authority Code not found.");
        }

        /// <summary>
        /// Using the specified code, this will attempt to look up the related reference information
        ///   from the appropriate pcs code.
        /// </summary>
        /// <param name="epsgCode">
        /// The epsg Code.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Throws when there is no projection for given epsg code</exception>
        public static ProjectionInfo FromEpsgCode(int epsgCode)
        {
            return FromAuthorityCode("EPSG", epsgCode);
        }
    }
}
