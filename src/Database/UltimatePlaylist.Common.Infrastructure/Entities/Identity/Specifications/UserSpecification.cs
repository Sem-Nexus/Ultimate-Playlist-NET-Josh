#region usings

using UltimatePlaylist.Common.Filters.Enums.Conditions;
using UltimatePlaylist.Common.Filters.Models;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Specifications;

#endregion

namespace UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications
{
    public class UserSpecification : BaseSpecification<User>
    {
        #region Constructor(s)

        public UserSpecification(bool includeDeleted = false)
        {
            if (!includeDeleted)
            {
                AddCriteria(c => !c.IsDeleted);
            }
        }

        #endregion

        #region Filters

        public UserSpecification ByExternalId(Guid externalId)
        {
            AddCriteria(s => s.ExternalId == externalId);

            return this;
        }

        public UserSpecification ByExternalIds(Guid[] externalIds)
        {
            AddCriteria(s => externalIds.Contains(s.ExternalId));

            return this;
        }

        public UserSpecification OnlyUsers()
        {
            AddCriteria(s => s.Roles.Any(r => r.Role.Name == nameof(Common.Enums.UserRole.User)));
            return this;
        }

        public UserSpecification HasDeviceToken()
        {
            AddCriteria(s => !string.IsNullOrEmpty(s.DeviceToken));

            return this;
        }

        public UserSpecification IsNotificationEnabled()
        {
            AddCriteria(s => s.IsNotificationEnabled);

            return this;
        }

        public UserSpecification ByEmail(string email)
        {
            AddCriteria(s => s.Email == email);

            return this;
        }
        #endregion

        #region Include
        public UserSpecification WithAvatar()
        {
            AddInclude(s => s.AvatarFile);

            return this;
        }

        public UserSpecification WithGender()
        {
            AddInclude(s => s.Gender);

            return this;
        }

        public UserSpecification WithRoles()
        {
            AddInclude(s => s.Roles);

            return this;
        }

        #endregion

        #region Pagination

        public UserSpecification Pagination(Pagination pagination)
        {
            ApplyPaging(pagination);

            return this;
        }

        public UserSpecification Filter(IEnumerable<FilterModel> filter)
        {
            var skipFields = new List<string>() { "IsActive" };
            ApplyFilters(filter, AddCustomFilters, skipFields: skipFields);

            return this;
        }

        private void AddCustomFilters(FilterModel filter)
        {
            var isActiveFilter = filter.ValueFilters.FirstOrDefault(x => x.FieldName.Equals("isActive", StringComparison.OrdinalIgnoreCase));

            if (isActiveFilter != null && bool.TryParse(isActiveFilter.Value, out bool isActive))
            {
                if (isActiveFilter.Condition == ValueFilterCondition.Contains)
                {
                    AddToAnd(x => x.IsActive == isActive);
                }
            }
        }

        #endregion
    }
}
