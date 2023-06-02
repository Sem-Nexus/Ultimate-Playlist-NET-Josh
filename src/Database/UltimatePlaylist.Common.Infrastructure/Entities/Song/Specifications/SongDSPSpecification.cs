#region Usings

using System;
using System.Collections.Generic;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Database.Infrastructure.Specifications;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Song.Specifications
{
    public class SongDSPSpecification : BaseSpecification<SongDSPEntity>
    {
        #region Constructor(s)

        public SongDSPSpecification(bool includeDeleted = false)
        {
            if (!includeDeleted)
            {
                AddCriteria(c => !c.IsDeleted);
            }
        }

        #endregion

        #region Filters

        public SongDSPSpecification BySongExternalId(Guid externalId)
        {
            AddCriteria(c => c.Song.ExternalId == externalId);

            return this;
        }

        public SongDSPSpecification ByDSPType(DspType dspType)
        {
            AddCriteria(c => c.DspType == dspType);

            return this;
        }

        #endregion

        public SongDSPSpecification OrderByCreatedDescending()
        {
            ApplyOrderByDescending(s => s.Created);

            return this;
        }

        #region Include

        public SongDSPSpecification WithSong()
        {
            AddInclude(c => c.Song);

            return this;
        }

        public SongDSPSpecification BySongId(long songId)
        {
            AddCriteria(c => c.SongId == songId);

            return this;

        }
        #endregion
    }
}
