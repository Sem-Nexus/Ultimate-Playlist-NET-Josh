#region Usings

using System.Web;
using AutoMapper;
using CSharpFunctionalExtensions;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Interface;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Entities.Dsp;
using UltimatePlaylist.Database.Infrastructure.Entities.Dsp.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.File;
using UltimatePlaylist.Database.Infrastructure.Entities.File.Specifications;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Files;
using UltimatePlaylist.Services.Common.Interfaces.Profile;
using UltimatePlaylist.Services.Common.Models.Email;
using UltimatePlaylist.Services.Common.Models.Files;
using UltimatePlaylist.Services.Common.Models.Profile;
using UltimatePlaylist.Services.Email.Jobs;

#endregion

namespace UltimatePlaylist.Services.Personalization
{
    public class UserProfileService : IUserProfileService
    {
        #region Private members

        private readonly Lazy<UserManager<User>> UserManagerProvider;

        private readonly Lazy<IRepository<User>> UserRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<UserDspEntity>> UserDspRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<AvatarFileEntity>> AvatarFileRepositoryProvider;

        private readonly Lazy<IReadOnlyRepository<GenderEntity>> GenderRepositoryProvider;

        private readonly Lazy<IAvatarFileService> AvatarFileServiceProvider;

        private readonly Lazy<IMapper> MapperProvider;

        private readonly EmailConfig EmailConfig;

        private readonly Lazy<IBackgroundJobClient> BackgroundJobProvider;

        private readonly Lazy<IUserBlacklistTokenStore> UserBlacklistTokenStoreProvider;

        private readonly EFContext _context;

        #endregion

        #region Constructor(s)

        public UserProfileService(
            Lazy<UserManager<User>> userManagerProvider,
            Lazy<IRepository<User>> userRepositoryProvider,            
            Lazy<IReadOnlyRepository<UserDspEntity>> userDspRepositoryProvider,
            Lazy<IAvatarFileService> avatarFileServiceProvider,
            Lazy<IReadOnlyRepository<AvatarFileEntity>> avatarFileRepositoryProvider,
            Lazy<IReadOnlyRepository<GenderEntity>> genderRepositoryProvider,
            Lazy<IMapper> mapperProvider,
            IOptions<EmailConfig> emailOptions,
            Lazy<IBackgroundJobClient> backgroundJobClientProvider,
            EFContext context,
            Lazy<IUserBlacklistTokenStore> userBlacklistTokenStoreProvider)
        {
            UserManagerProvider = userManagerProvider;
            UserRepositoryProvider = userRepositoryProvider;            
            UserDspRepositoryProvider = userDspRepositoryProvider;
            AvatarFileServiceProvider = avatarFileServiceProvider;
            AvatarFileRepositoryProvider = avatarFileRepositoryProvider;
            GenderRepositoryProvider = genderRepositoryProvider;
            MapperProvider = mapperProvider;
            EmailConfig = emailOptions.Value;
            BackgroundJobProvider = backgroundJobClientProvider;
            UserBlacklistTokenStoreProvider = userBlacklistTokenStoreProvider;
            _context = context;
        }

        #endregion

        #region Properties

        private UserManager<User> UserManager => UserManagerProvider.Value;

        private IRepository<User> UserRepository => UserRepositoryProvider.Value;

        private IReadOnlyRepository<UserDspEntity> UserDspRepository => UserDspRepositoryProvider.Value;

        private IAvatarFileService AvatarFileService => AvatarFileServiceProvider.Value;

        private IReadOnlyRepository<AvatarFileEntity> AvatarFileRepository => AvatarFileRepositoryProvider.Value;

        private IReadOnlyRepository<GenderEntity> GenderRepository => GenderRepositoryProvider.Value;

        private IMapper Mapper => MapperProvider.Value;

        private IBackgroundJobClient BackgroundJobClient => BackgroundJobProvider.Value;

        private IUserBlacklistTokenStore UserBlacklistTokenStore => UserBlacklistTokenStoreProvider.Value;

        #endregion

        #region Public Method(s)

        public async Task<Result<UserProfileInfoReadServiceModel>> GetUserInfoAsync(Guid userExternalId)
        {
            return await GetUser(userExternalId)
                .Bind(async user => await PrepareUserProfileInfoAsync(user));
        }
        public async Task<Result> DeactivateUserAsync(Guid userExternalId)
        {
            return await GetUser(userExternalId)
                 .Check(async user => await DeactivateUser(user));
        }

        public async Task<Result<UserProfileInfoReadServiceModel>> UpdateUserProfileAsync(Guid userExternalId, EditUserProfileWriteServiceModel editUserProfileWriteServiceModel)
        {
            return await GetUser(userExternalId)
                .CheckIf(user => user.UserName != editUserProfileWriteServiceModel.Username, async user =>
                {
                    var existingUserWithUniqueUsername = await UserManager.FindByNameAsync(editUserProfileWriteServiceModel.Username);
                    if (existingUserWithUniqueUsername is not null)
                    {
                        return Result.Failure(ErrorMessages.UsernameTaken);
                    }

                    return Result.Success();
                })
                .CheckIf(user => user.Email != editUserProfileWriteServiceModel.Email, async user =>
                {
                    var existingUserWithUniqueEmail = await UserManager.FindByEmailAsync(editUserProfileWriteServiceModel.Email);
                    if (existingUserWithUniqueEmail is not null)
                    {
                        return Result.Failure(ErrorMessages.EmailTaken);
                    }

                    return Result.Success();
                })
                .TapIf(user => user.UserName != editUserProfileWriteServiceModel.Username, user =>
                {
                    user.UserName = editUserProfileWriteServiceModel.Username;
                    user.NormalizedUserName = editUserProfileWriteServiceModel.Username.ToUpper();
                })
                .Tap(user =>
                {
                    user.Name = editUserProfileWriteServiceModel.FirstName;
                    user.LastName = editUserProfileWriteServiceModel.LastName;
                    user.PhoneNumber = editUserProfileWriteServiceModel.PhoneNumber;
                    user.ZipCode = editUserProfileWriteServiceModel.ZipCode;
                    user.BirthDate = editUserProfileWriteServiceModel.BirthDate;
                    user.IsNotificationEnabled = editUserProfileWriteServiceModel.IsNotificationEnabled;
                    user.IsSmsPromotionalNotificationEnabled = editUserProfileWriteServiceModel.IsSmsPromotionalNotificationEnabled;
                })
                .Bind(user => SetUserEmail(user, editUserProfileWriteServiceModel.Email))
                .TapIf(user => user.Gender.ExternalId != editUserProfileWriteServiceModel.GenderExternalId, async user => user.GenderId = await GetGenderId(editUserProfileWriteServiceModel.GenderExternalId))
                .Check(async user => await AddOrUpdateAvatarAsync(editUserProfileWriteServiceModel.AvatarExternalId, user))
                .Bind(async user => await SaveUserUpdatedProfile(user))
                .Bind(async user => await SendEmailChangeIfRequired(user))
                .Bind(async user => await PrepareUserProfileInfoAsync(user));
        }

        public async Task<Result> AddOrUpdatePinAsync(Guid userExternalId, UserPinWriteServiceModel userPinWriteServiceModel)
        {
            return await GetUser(userExternalId)
                 .Check(async user => await AddOrUpdateUserPin(user, userPinWriteServiceModel.Pin));
        }

        public async Task<Result> IsPinCorrectAsync(Guid userExternalId, UserPinWriteServiceModel userPinWriteServiceModel)
        {
            return await GetUser(userExternalId)
                  .CheckIf(user => !string.IsNullOrEmpty(user.Pin), user => CheckPin(user.Pin, userPinWriteServiceModel.Pin))
                  .CheckIf(user => string.IsNullOrEmpty(user.Pin), user => Result.Success());
        }

        public async Task<Result> RemovePinAsync(Guid userExternalId)
        {
            return await GetUser(userExternalId)
                .Bind(async user => await RemovePin(user));
        }

        public async Task<Result<FileReadServiceModel>> SetOrUpdateAvatarAsync(Guid userExternalId, Stream fileStream, string fileName)
        {
            var savedFile = await AvatarFileService.SaveNewAvatarFileAsync(fileStream, fileName);

            if (savedFile is null)
            {
                return Result.Failure<FileReadServiceModel>(ErrorType.CannotSaveAvatarFile.ToString());
            }

            User user = default;

            return await GetUser(userExternalId)
                .Tap(userEntity => user = userEntity)
                .Bind(async _ => await GetSavedAvatarAsync(savedFile.ExternalId))
                .Check(async avatarFile => await UpdateUserAvatarAsync(user, avatarFile))
                .Map(_ => savedFile);
        }

        public async Task<Result> DeleteUserAvatarAsync(Guid userExternalId)
        {
            return await GetUser(userExternalId)
                .Bind(async user => await RemoveAvatarAsync(user));
        }
        #endregion

        #region Private Method(s)

        private async Task<Result<UserProfileInfoReadServiceModel>> PrepareUserProfileInfoAsync(User user)
        {
            var mappedUser = Mapper.Map<UserProfileInfoReadServiceModel>(user);
            mappedUser.IsNotificationEnabled = user.IsNotificationEnabled && !string.IsNullOrEmpty(user.DeviceToken);

            return await Result.Success()
                .TapIf(!string.IsNullOrEmpty(user.NewNotConfirmedEmail), () => mappedUser.IsEmailChangingProcessStarted = true)
                .Bind(async () => await GetUserDspAsync(user.ExternalId)
                    .Tap(userDps => mappedUser.IsConnectedToAppleMusic = userDps.Any(s => s.Type == DspType.AppleMusic))
                    .Tap(userDps => mappedUser.IsConnectedToSpotify = userDps.Any(s => s.Type == DspType.Spotify)))
                .Map(_ => mappedUser);
        }

        private async Task<Result<User>> GetUser(Guid userExternalId)
        {
            var user = await UserRepository.FirstOrDefaultAsync(new UserSpecification()
                .ByExternalId(userExternalId)
                .WithAvatar()
                .WithGender());

            return Result.SuccessIf(user != null, ErrorType.CannotFindUser.ToString())
                .Map(() => user);
        }

        private async Task<Result> AddOrUpdateUserPin(User user, string pin)
        {
            user.Pin = pin;

            var updatedEntity = await UserRepository.UpdateAndSaveAsync(user);

            return Result.SuccessIf(updatedEntity != null && updatedEntity.Pin == pin, ErrorMessages.CannotSetPin);
        }

        private Result CheckPin(string userPin, string pinToCheck)
        {
            if (pinToCheck.Equals(userPin))
            {
                return Result.Success();
            }

            return Result.Failure(ErrorMessages.PinIsIncorrect);
        }

        private async Task<Result> RemovePin(User user)
        {
            user.Pin = null;
            var updated = await UserRepository.UpdateAndSaveAsync(user);

            return Result.SuccessIf(updated != null && string.IsNullOrEmpty(updated.Pin), ErrorMessages.CannotRemovePin);
        }

        private async Task<Result> DeactivateUser(User user)
        {
            var playlists = _context.UserPlaylists.Where(x => x.UserId == user.Id).ToList();
            foreach (var playlist in playlists)
            {
                var userSonglists = _context.UserPlaylistSongs.Where(x => x.UserPlaylistId == playlist.Id).ToList();
                userSonglists.ForEach(song =>
                {
                    _context.Tickets.Where(x => x.UserPlaylistSongId == song.Id).DeleteFromQuery();
                });
                _context.UserPlaylistSongs.Where(x => x.UserPlaylistId == playlist.Id).DeleteFromQuery();
            }
            _context.UserPlaylists.Where(x => x.UserId == user.Id).DeleteFromQuery();
            _context.UserDsps.Where(x => x.UserId == user.Id).DeleteFromQuery();
            _context.UserSongsHistory.Where(x => x.UserId == user.Id).DeleteFromQuery();

            var deactivateUser = new DeactivatedUsers()
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                GenderId = user.GenderId,
                LastName = user.LastName,
                ZipCode = user.ZipCode,
                LastActive = user.LastActive,
                Device = user.Device,
                Created = user.Created,
                ExternalId = user.ExternalId,
                Updated = DateTime.UtcNow,
                IsDeleted = user.IsDeleted,
            };

            await _context.DeactivatedUsers.AddAsync(deactivateUser);
            await _context.SaveChangesAsync();

            var result = await UserManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _context.UserRoles.Where(x => x.UserId == user.Id).DeleteFromQuery();
                _context.Users.Where(x => x.ExternalId == user.ExternalId).DeleteFromQuery();
            }
            return Result.SuccessIf(result != null, ErrorMessages.CannotDeactivateUser);
        }

        private async Task<Result<IReadOnlyList<UserDspEntity>>> GetUserDspAsync(Guid userExternalId)
        {
            var userDsps = await UserDspRepository.ListAsync(new UserDspSpecification()
                .ByUserExternalId(userExternalId)
                .ByActive()
                .OrderByCreatedDescending());

            return Result.Success(userDsps);
        }

        private async Task<Result<AvatarFileEntity>> GetSavedAvatarAsync(Guid avatarExternalId)
        {
            var avatar = await AvatarFileRepository.FirstOrDefaultAsync(new AvatarFileSpecification()
                    .ByExternalId(avatarExternalId));

            return Result.SuccessIf(avatar != null, avatar, ErrorType.CannotSaveAvatarFile.ToString());
        }

        private async Task<Result> UpdateUserAvatarAsync(User user, AvatarFileEntity avatarFile)
        {
            user.AvatarFileId = avatarFile.Id;

            var updated = await UserRepository.UpdateAndSaveAsync(user);

            return Result.SuccessIf(updated != null && updated.AvatarFileId.HasValue, ErrorMessages.CannotSetUserAvatar);
        }

        private async Task<Result> RemoveAvatarAsync(User user)
        {
            if (user.AvatarFile != null)
            {
                var fileExternalId = user.AvatarFile.ExternalId;
                user.AvatarFileId = null;
                var updated = await UserRepository.UpdateAndSaveAsync(user);

                await AvatarFileService.RemoveAvatarAsync(fileExternalId);

                return Result.SuccessIf(updated != null, ErrorMessages.CannotRemoveUserAvatar);
            }

            return Result.Failure(ErrorMessages.UserDoesNotHaveAvatar);
        }

        private async Task<long?> GetGenderId(Guid genderExternalId)
        {
            var gender = await GenderRepository.FirstOrDefaultAsync(new GenderSpecification()
                .ByExternalId(genderExternalId));

            return gender != null ? gender.Id : null;
        }

        private async Task<Result> AddOrUpdateAvatarAsync(Guid? avatarExternalId, User user)
        {
            if (!avatarExternalId.HasValue || (user.AvatarFile is null && !avatarExternalId.HasValue))
            {
                user.AvatarFileId = null;
                return Result.Success();
            }

            var avatarFile = await AvatarFileRepository.FirstOrDefaultAsync(new AvatarFileSpecification()
                .ByExternalId(avatarExternalId.Value));

            if (avatarFile != null)
            {
                user.AvatarFileId = avatarFile.Id;
                return Result.Success();
            }

            return Result.Failure(ErrorMessages.CannotSetUserAvatar);
        }

        private async Task<Result<User>> SaveUserUpdatedProfile(User user)
        {
            var updated = await UserRepository.UpdateAndSaveAsync(user);
            return Result.SuccessIf(updated != null && updated.Updated != null, updated, ErrorMessages.CannotUpdateUserProfile);
        }

        private async Task SendEmailChangingInfoToOldEmailAddress(User user)
        {
            var request = new EmailChangingInfoRequest
            {
                Email = user.Email,
                Name = user.Name,
            };
            BackgroundJobClient.Enqueue<EmailJob>(p => p.SendEmailChangingInfoToOldEmailAddressAsync(request));
        }

        private async Task SendConfirmationForEmailChanged(User user)
        {
            var oldEmail = user.Email;
            user.Email = user.NewNotConfirmedEmail;
            var confirmationToken = await UserManager.GenerateEmailConfirmationTokenAsync(user);
            user.Email = oldEmail;

            user.ConfirmationCodeExiprationDate = DateTime.UtcNow.Add(EmailConfig.ConfirmationExpirationTime);
            user.UserToeknRequiredByWebConfirmation = confirmationToken;
            await UserManager.UpdateAsync(user);

            var request = new EmailChangeConfirmationRequest
            {
                Email = user.NewNotConfirmedEmail,
                Name = user.Name,
                Token = HttpUtility.UrlEncode(confirmationToken),
            };
            BackgroundJobClient.Enqueue<EmailJob>(p => p.SendUpdateProfileConfirmationAsync(request));
        }

        private Result<User> SetUserEmail(User user, string email)
        {
            if (user.Email == email)
            {
                user.Email = email;

                return Result.Success(user);
            }

            user.NewNotConfirmedEmail = email;

            return Result.Success(user);
        }

        private async Task<Result<User>> SendEmailChangeIfRequired(User user)
        {
            if (!string.IsNullOrEmpty(user.NewNotConfirmedEmail))
            {
                user.EmailConfirmed = false;
                await SendEmailChangingInfoToOldEmailAddress(user);
                await SendConfirmationForEmailChanged(user);
            }

            return user;
        }

        #endregion
    }
}
