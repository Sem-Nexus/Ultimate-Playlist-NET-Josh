#region Usings

using AutoMapper;
using CSharpFunctionalExtensions;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Filters.Models;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Common.Mvc.Interface;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.User;
using UltimatePlaylist.Services.Common.Models;
using UltimatePlaylist.Services.Common.Models.Song;
using UltimatePlaylist.Services.Common.Models.UserManagment;

#endregion

namespace UltimatePlaylist.Services.UserManagement
{
    public class UserManagementService : IUserManagementService
    {
        #region Private field(s)

        private readonly Lazy<IRepository<User>> UserRepositoryProvider;
        private readonly Lazy<IMapper> MapperProvider;
        private readonly Lazy<IUserActiveService> UserActiveServiceProvider;
        private readonly Lazy<IUserManagementProcedureRepository> UserManagementProcedureRepositoryProvider;

        #endregion

        #region Constructor(s)

        public UserManagementService(
            Lazy<IRepository<User>> userRepositoryProvider,
            Lazy<IMapper> mapperProvider,
            Lazy<IUserActiveService> userActiveServiceProvider,
            Lazy<IUserManagementProcedureRepository> userManagementProcedureRepositoryProvider)
        {
            UserRepositoryProvider = userRepositoryProvider;
            MapperProvider = mapperProvider;
            UserActiveServiceProvider = userActiveServiceProvider;
            UserManagementProcedureRepositoryProvider = userManagementProcedureRepositoryProvider;
        }

        #endregion

        #region Private properties

        private IRepository<User> UserRepository => UserRepositoryProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        private IUserActiveService UserActiveService => UserActiveServiceProvider.Value;

        private IUserManagementProcedureRepository UserManagementProcedureRepository => UserManagementProcedureRepositoryProvider.Value;

        #endregion

        #region Public method(s)

        public async Task<Result<PaginatedReadServiceModel<UserListItemReadServiceModel>>> UsersListAsync(
            Pagination pagination,
            IEnumerable<FilterModel> filter)
        {
            var count = await UserManagementProcedureRepository.UsersCount(pagination, filter);

            return await UserManagementProcedureRepository.GetUsersManagementList(pagination, filter)
                .Map(users => Mapper.Map<IReadOnlyList<UserListItemReadServiceModel>>(users))
                .Map(users => new PaginatedReadServiceModel<UserListItemReadServiceModel>(users, pagination, count));
        }

        public async Task<Result<bool>> ChangeUserStatus(Guid userExternalId, bool isActive)
        {
            var specification = new UserSpecification()
                .ByExternalId(userExternalId)
                .OnlyUsers();

            return await GetUser(specification)
                .Tap(user => ChangeStatus(user, isActive))
                .Tap(async user => await UserRepository.UpdateAndSaveAsync(user))
                .Map(user => user.IsActive);
        }

        public async Task<Result<bool>> ChangeIsAgeVerified(Guid userExternalId, bool isAgeVerified)
        {
            var specification = new UserSpecification()
                   .ByExternalId(userExternalId)
                   .OnlyUsers();

            return await GetUser(specification)
                .Tap(user => user.IsAgeVerified = isAgeVerified)
                .Tap(async user => await UserRepository.UpdateAndSaveAsync(user))
                .Map(user => user.IsAgeVerified);
        }

        public async Task<Result<ListenersStatisticsReadServiceModel>> GetListenersStatistics(ListenersReadServiceModel serviceModel)
        {
            return await UserManagementProcedureRepository.GetListenersStatistics(serviceModel)
                .Map(statistics => Mapper.Map<ListenersStatisticsReadServiceModel>(statistics));
        }

        public async Task<Result<Engagement>> GetEngagementStatics(EngagementFilterServiceModel filter)
        {
            return await UserManagementProcedureRepository.GetEngagementStatics(filter)
                .Map(statistics => Mapper.Map<Engagement>(statistics));
        }

        public async Task<Result<MedianUsersAge>> GetUsersMedianAge()
        {
            return await UserManagementProcedureRepository.GetUsersMedianAge()
                        .Map(statistics => Mapper.Map<MedianUsersAge>(statistics));
        }
        

        #endregion

        #region Private method(s)

        private async Task<Result<IReadOnlyList<User>>> GetUsersListAsync(Pagination pagination, UserSpecification specification)
        {
            specification = specification
                .WithAvatar()
                .Pagination(pagination);

            var users = await UserRepository.ListAsync(specification);

            return Result.Success(users);
        }

        private async Task<Result<User>> GetUser(UserSpecification specification)
        {
            var user = await UserRepository.FirstOrDefaultAsync(specification);

            return Result.SuccessIf(user != null, ErrorMessages.UserDoesNotExist)
                .Map(() => user);
        }

        private void ChangeStatus(User user, bool isActive)
        {
            user.IsActive = isActive;
            if (!isActive)
            {
                user.RefreshTokenHash = null;
            }

            UserActiveService.UpdateActiveStatusInStore(user.ExternalId, user.IsActive);
        }

        #endregion
    }
}
