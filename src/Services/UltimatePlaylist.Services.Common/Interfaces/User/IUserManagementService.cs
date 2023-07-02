#region Usings

using CSharpFunctionalExtensions;
using UltimatePlaylist.Common.Filters.Models;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Models;
using UltimatePlaylist.Services.Common.Models.UserManagment;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.User
{
    public interface IUserManagementService
    {
        Task<Result<PaginatedReadServiceModel<UserListItemReadServiceModel>>> UsersListAsync(Pagination pagination, IEnumerable<FilterModel> filter);

        Task<Result<bool>> ChangeUserStatus(Guid userExternalId, bool isActive);

        Task<Result<bool>> ChangeIsAgeVerified(Guid userExternalId, bool isAgeVerified);

        Task<Result<ListenersStatisticsReadServiceModel>> GetListenersStatistics(ListenersReadServiceModel serviceModel);

        Task<Result<Engagement>> GetEngagementStatics();
    }
}
