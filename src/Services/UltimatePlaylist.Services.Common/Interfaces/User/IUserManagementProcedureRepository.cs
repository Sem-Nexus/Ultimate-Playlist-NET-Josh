#region Usings

using CSharpFunctionalExtensions;
using UltimatePlaylist.Common.Filters.Models;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Models.UserManagment;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.User
{
    public interface IUserManagementProcedureRepository
    {
        Task<int> UsersCount(Pagination pagination, IEnumerable<FilterModel> filter);

        Task<Result<List<UserManagementProcedureView>>> GetUsersManagementList(Pagination pagination, IEnumerable<FilterModel> filter);

        Task<Result<ListenersStatisticsProcedureView>> GetListenersStatistics(ListenersReadServiceModel serviceModel);

        Task<Result<Engagement>> GetEngagementStatics();
    }
}
