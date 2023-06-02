#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Database.Infrastructure.Entities.Song;
using UltimatePlaylist.Database.Infrastructure.Entities.Song.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket.Specifications;
using UltimatePlaylist.Database.Infrastructure.Specifications;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Specifications
{
    public class SongSocialMediaSpecification : BaseSpecification<SongSocialMediaEntity>
    {
        #region Constructor(s)

        public SongSocialMediaSpecification(bool includeDeleted = false)
        {
            if (!includeDeleted)
            {
                AddCriteria(c => !c.IsDeleted);
            }
        }

        #endregion

        public SongSocialMediaSpecification BySongId(long songId)
        {
            AddCriteria(c => c.SongId == songId);

            return this;
        }

        public SongSocialMediaSpecification ByType(SocialMediaType type)            
        {
            AddCriteria(c => c.Type == type);

            return this;
        }

    }
}
