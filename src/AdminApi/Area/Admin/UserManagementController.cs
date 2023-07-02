#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using UltimatePlaylist.AdminApi.Models.User;
using UltimatePlaylist.Common.Filters.Models;
using UltimatePlaylist.Common.Mvc.Attributes;
using UltimatePlaylist.Common.Mvc.Controllers;
using UltimatePlaylist.Common.Mvc.Paging;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.User;
using UltimatePlaylist.Services.Common.Models.UserManagment;
using static UltimatePlaylist.Common.Mvc.Consts.Consts;

#endregion

namespace UltimatePlaylist.AdminApi.Area.Admin
{
    [Area("Admin")]
    [Route("[controller]")]
    [ApiExplorerSettings(GroupName = AdminApiGroups.Administrator)]
    public class UserManagementController : BaseController
    {
        #region Private field(s)

        private readonly Lazy<IMapper> MapperProvider;
        private readonly Lazy<IUserManagementService> UserManagementServiceProvider;

        #endregion

        #region Constructor(s)

        public UserManagementController(
            Lazy<IMapper> mapperProvider,
            Lazy<IUserManagementService> userManagementServiceProvider)
        {
            MapperProvider = mapperProvider;
            UserManagementServiceProvider = userManagementServiceProvider;
        }

        #endregion

        #region Private properties

        private IMapper Mapper => MapperProvider.Value;

        private IUserManagementService UserManagementService => UserManagementServiceProvider.Value;

        #endregion

        #region GET

        [HttpGet("engagement-statics")]
        [ProducesEnvelope(typeof(Engagement), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSongDPS()
        {
            return await UserManagementService.GetEngagementStatics()
               .Map(songs => Mapper.Map<Engagement>(songs))
               .Finally(BuildEnvelopeResult);
        }

        #endregion

        #region PUT

        [HttpPut("statistics")]
        [ProducesEnvelope(typeof(ListenersStatisticsResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetListenersStatistics(ListenersStatisticsRequestModel request)
        {
            var serviceModel = Mapper.Map<ListenersReadServiceModel>(request);
            return await UserManagementService.GetListenersStatistics(serviceModel)
                .Map(statistics => Mapper.Map<ListenersStatisticsResponseModel>(statistics))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPut("filter")]
        [ProducesEnvelope(typeof(PaginatedResponse<UserListItemResponseModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Filter([FromBody] IEnumerable<FilterModel> filterModel)
        {
            return await UserManagementService.UsersListAsync(XPagination, filterModel)
                .Map(usersList => Mapper.Map<PaginatedResponse<UserListItemResponseModel>>(usersList))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPut("status")]
        [ProducesEnvelope(typeof(UserStatusResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangeStatus([FromBody] UserStatusRequestModel model)
        {
            return await UserManagementService.ChangeUserStatus(model.UserExternalId, model.IsActive)
                .Map(isActive => new UserStatusResponseModel() { IsActive = isActive })
                .Finally(BuildEnvelopeResult);
        }

        #endregion
    }
}