#region Usings

using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Filters.Models;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Specifications;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Games.Specifications
{
    public class WinningAlternateSpecification : BaseSpecification<WinningAlternateEntity>
    {
        #region Constructor(s)

        public WinningAlternateSpecification(bool includeDeleted = false)
        {
            if (!includeDeleted)
            {
                AddCriteria(c => !c.IsDeleted);
            }
        }

        #endregion

        #region Filters

        public WinningAlternateSpecification ById(long id)
        {
            AddCriteria(t => t.Id == id);

            return this;
        }

        public WinningAlternateSpecification ByTodaysWinning()
        {
            string timezoneId = "US Eastern Standard Time";
            var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timezoneId); // converted utc timezone to EST timezone
            TimeZoneInfo targetTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            double offsetHours = targetTimezone.GetUtcOffset(DateTime.UtcNow).TotalHours;
            AddCriteria(s => s.Created.AddHours(offsetHours).Date.Equals(now.Date));

            return this;
        }

        public WinningAlternateSpecification ByPastWinnings()
        {
            string timezoneId = "US Eastern Standard Time";
            var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timezoneId); // converted utc timezone to EST timezone
            TimeZoneInfo targetTimezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
            double offsetHours = targetTimezone.GetUtcOffset(DateTime.UtcNow).TotalHours;
            AddCriteria(s => !s.Created.AddHours(offsetHours).Date.Equals(now.Date));

            return this;
        }

        public WinningAlternateSpecification ByExternalId(Guid externalId)
        {
            AddCriteria(t => t.ExternalId.Equals(externalId));

            return this;
        }

        public WinningAlternateSpecification ByUserExternalId(Guid userExternalId)
        {
            AddCriteria(t => t.Winner.ExternalId.Equals(userExternalId));

            return this;
        }

        public WinningAlternateSpecification ByGameId(long gameId)
        {
            AddCriteria(t => t.GameId == gameId);

            return this;
        }

        public WinningAlternateSpecification ByStatus(WinningStatus status)
        {
            AddCriteria(t => t.Status == status);

            return this;
        }

        public WinningAlternateSpecification ByNotCollectedStatus()
        {
            AddCriteria(t => t.Status != WinningStatus.Paid && t.Status != WinningStatus.Rejected);

            return this;
        }

        #endregion

        #region Includes

        public WinningAlternateSpecification WithUser()
        {
            AddInclude(t => t.Winner);
            AddInclude(t => t.Winner.AvatarFile);

            return this;
        }

        public WinningAlternateSpecification WithGame()
        {
            AddInclude(t => t.Game);

            return this;
        }

        #endregion

        #region Pagination

        public WinningAlternateSpecification Pagination(Pagination pagination = null)
        {
            if (pagination == null)
            {
                ApplyOrderBy(c => c.Created, true);
            }
            else
            {
                ApplyOrderBy(pagination);
                ApplyPaging(pagination);
            }

            return this;
        }

        public WinningAlternateSpecification Filter(IEnumerable<FilterModel> filter)
        {
            ApplyFilters(filter);

            return this;
        }

        public WinningAlternateSpecification Search(string searchValue)
        {
            if (!string.IsNullOrEmpty(searchValue))
            {
                AddCriteria(x => x.Winner.UserName.Contains(searchValue)
                    || x.Winner.Name.Contains(searchValue)
                    || x.Winner.LastName.Contains(searchValue)
                    || x.Winner.PhoneNumber.Contains(searchValue)
                    || x.Winner.Email.Contains(searchValue));
            }

            return this;
        }

        public WinningAlternateSpecification Filter(
            WinningStatus? winningStatus,
            GameType? gameType,
            int? minAge,
            int? maxAge,
            bool? isAgeVerified)
        {
            if (winningStatus.HasValue)
            {
                AddCriteria(x => x.Status == winningStatus);
            }

            if (gameType.HasValue)
            {
                AddCriteria(x => x.Game.Type == gameType);
            }

            if (isAgeVerified.HasValue)
            {
                AddCriteria(x => x.Winner.IsAgeVerified == isAgeVerified);
            }

            if (minAge.HasValue || maxAge.HasValue)
            {
                var maxBirthDate = minAge.HasValue ? DateTime.UtcNow.AddYears(-minAge.Value) : DateTime.UtcNow;
                var minBirthDate = maxAge.HasValue ? DateTime.UtcNow.AddYears(-maxAge.Value) : DateTime.Parse("1800-01-01");

                AddCriteria(x => x.Winner.BirthDate >= minBirthDate && x.Winner.BirthDate <= maxBirthDate);
            }

            return this;
        }

        private void ApplyOrderBy(Pagination pagination)
        {
            if (string.IsNullOrEmpty(pagination.OrderBy))
            {
                return;
            }

            switch (pagination.OrderBy)
            {
                case "created":
                    ApplyOrderBy(p => p.Created, pagination.Descending);
                    break;
            }
        }

        #endregion
    }
}
