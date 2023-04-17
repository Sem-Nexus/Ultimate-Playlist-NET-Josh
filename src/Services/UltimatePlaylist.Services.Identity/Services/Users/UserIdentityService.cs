﻿#region Usings

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CSharpFunctionalExtensions;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Exceptions;
using UltimatePlaylist.Common.Mvc.Interface;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Identity;
using UltimatePlaylist.Services.Common.Models.Identity;
using UserRole = UltimatePlaylist.Common.Enums.UserRole;
using Google.Apis.Auth;
#endregion

namespace UltimatePlaylist.Services.Identity.Services.Users
{
    public class UserIdentityService : BaseIdentityService, IUserIdentityService
    {
        #region Private members

        private readonly Lazy<ILogger<UserIdentityService>> LoggerProvider;
        private readonly Lazy<IReadOnlyRepository<GenderEntity>> GenderRepositoryProvider;        
        #endregion

        #region Constructor(s)

        public UserIdentityService(
            Lazy<ILogger<UserIdentityService>> loggerProvider,
            Lazy<UserManager<User>> userManagerProvider,
            Lazy<IMapper> mapperProvider,
            Lazy<IBackgroundJobClient> backgroundJobClientProvider,
            Lazy<IUserRetrieverService> userRetrieverServiceProvider,
            Lazy<IRepository<User>> userRepositoryProvider,
            IOptions<AuthConfig> authOptions,
            IOptions<EmailConfig> emailOptions,
            Lazy<IReadOnlyRepository<GenderEntity>> genderRepositoryProvider)
            : base(userManagerProvider, mapperProvider, backgroundJobClientProvider, userRetrieverServiceProvider, userRepositoryProvider, authOptions, emailOptions)
        {
            LoggerProvider = loggerProvider;
            GenderRepositoryProvider = genderRepositoryProvider;
        }

        #endregion

        #region Properties

        private ILogger<UserIdentityService> Logger => LoggerProvider.Value;

        private IReadOnlyRepository<GenderEntity> GenderRepository => GenderRepositoryProvider.Value;

        #endregion

        #region Traditional Login

        public async Task<Result<AuthenticationReadServiceModel>> LoginAsync(UserLoginWriteServiceModel userLoginWriteServiceModel)
        {
            var user = await UserManager
                .Users
                .Include(x => x.Roles)
                .ThenInclude(x => x.Role)
                .Where(p => p.Roles.Any(x => x.Role.Name == nameof(UserRole.User)))
                .FirstOrDefaultAsync(u => u.NormalizedEmail.Equals(userLoginWriteServiceModel.Email.ToUpper()));

            if (user is null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.InvalidEmailOrPassword);
            }

            if (!user.EmailConfirmed)
            {
                await SendConfirmationRequestEmail(user);
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.EmailNotConfirmed);
            }

            if (!user.IsActive)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.UserInActive);
            }

            var userHasValidPassword = await UserManager.CheckPasswordAsync(user, userLoginWriteServiceModel.Password);
            if (!userHasValidPassword)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.InvalidEmailOrPassword);
            }

            user.IsEmailChangeConfirmedFromWeb = false;
            await UserRepository.UpdateAndSaveAsync(user);

            return await GenerateAuthenticationResult(user);
        }

        public async Task<Result> RegisterAsync(UserRegistrationWriteServiceModel request)
        {
            var existingUser = await UserManager.FindByEmailAsync(request.Email);
            if (existingUser is not null)
            {
                return Result.Failure(ErrorMessages.EmailTaken);
            }

            var existingUserWithUniqueUsername = await UserManager.FindByNameAsync(request.Username);
            if (existingUserWithUniqueUsername is not null)
            {
                return Result.Failure(ErrorMessages.UsernameTaken);
            }

            var gender = await GenderRepository.FirstOrDefaultAsync(new GenderSpecification()
                .ByExternalId(request.GenderExternalId));
            if (gender is null)
            {
                return Result.Failure(ErrorType.GenderDoesNotExist.ToString());
            }

            request.Username = request.Username.Trim();
            var newUser = Mapper.Map<User>(request);
            newUser.Gender = gender;
            newUser.IsActive = true;
            newUser.ShouldNotificationBeEnabled = true;
            if (string.IsNullOrEmpty(newUser.PhoneNumber))
            {
                newUser.PhoneNumber = string.Empty;
            }

            var createdUser = await UserManager.CreateAsync(newUser);
            if (!createdUser.Succeeded)
            {
                return Result.Failure(ErrorType.UserNotCreated.ToString());
            }

            await UserManager.AddPasswordAsync(newUser, request.Password);
            await UserManager.UpdateAsync(newUser);

            var roleResult = await UserManager.AddToRoleAsync(newUser, UserRole.User.ToString());
            if (!roleResult.Succeeded)
            {
                return Result.Failure(ErrorType.UserCantBeAddedToRole.ToString());
            }

            await SendConfirmationRequestEmail(newUser);
            return Result.Success();
        }

        public async Task<Result<AuthenticationReadServiceModel>> LoginGoogleAsync(GoogleJsonWebSignature.Payload user)
        {

            var existingUser = await UserManager.FindByEmailAsync(user.Email);
            if (existingUser is not null)
            {
                if (existingUser.PasswordHash is not null)
                {
                    return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.EmailTaken);
                }

                if (!existingUser.EmailConfirmed)
                {
                    if (existingUser.ZipCode == null || existingUser.BirthDate == DateTime.MinValue)
                    {
                        var token = await GenerateAuthenticationResult(existingUser);
                        return new AuthenticationReadServiceModel
                        {
                            Token = token.Value.Token,
                            RefreshToken = token.Value.RefreshToken,
                            ConcurrencyStamp = existingUser.ConcurrencyStamp
                        };
                    }
                    else
                    {
                        await SendConfirmationRequestEmail(existingUser);
                        return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.EmailNotConfirmed);

                    }

                }
                await UserManager.AddLoginAsync(existingUser, new UserLoginInfo("Google", existingUser.UserName, existingUser.Email));
                return await GenerateAuthenticationResult(existingUser);

            }

            var gender = await GenderRepository.FirstOrDefaultAsync(new GenderSpecification()
             .ByExternalId(new Guid("989EE81B-8B8E-4FC9-BFE8-D81F19F8A6D7")));
            if (gender is null)
            {
                throw new LoginException(ErrorType.GenderDoesNotExist);
            }

            string userName = user.GivenName.Replace(" ", string.Empty);
            var newUserFromGoogle = new User()
            {
                UserName = "TemporaryUserName" + userName,
                Email = user.Email,
                Gender = gender
            };

            var createdUser = await UserManager.CreateAsync(newUserFromGoogle);
            if (!createdUser.Succeeded)
            {
                throw new LoginException(ErrorType.UserNotCreated);
            }

            existingUser = await UserManager.FindByEmailAsync(user.Email);
            var tokenData = await GenerateAuthenticationResult(existingUser);
            return new AuthenticationReadServiceModel
            {
                Token = tokenData.Value.Token,
                RefreshToken = tokenData.Value.RefreshToken,
                ConcurrencyStamp = existingUser.ConcurrencyStamp
            };
        }

        public async Task<Result> CompleteRegisterAsync(UserCompleteRegistrationWriteServiceModel request)
        {

            var principalClaims = GetPrincipalFromToken(request.Token);
            var userIdClaim = principalClaims.FindFirst(JwtClaims.Id)?.Value;
            var userEmailClaim = principalClaims.FindFirst(JwtClaims.Email)?.Value;

            var existingUserWithUniqueUsername = await UserManager.FindByNameAsync(request.Username);
            if (existingUserWithUniqueUsername is not null)
            {
                return Result.Failure(ErrorMessages.UsernameTaken);
            }

            var gender = await GenderRepository.FirstOrDefaultAsync(new GenderSpecification()
                .ByExternalId(request.GenderExternalId));
            if (gender is null)
            {
                return Result.Failure(ErrorType.GenderDoesNotExist.ToString());
            }

            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                request.PhoneNumber = string.Empty;
            }

            var updatedUser = new User()
            {
                Id = Int32.Parse(userIdClaim),
                UserName = request.Username,
                Name = request.FirstName,
                LastName = request.LastName,
                Email = userEmailClaim,
                Gender = gender,
                IsActive = true,
                ShouldNotificationBeEnabled = true,
                PhoneNumber = request.PhoneNumber,
                BirthDate = request.BirthDate,
                ZipCode = request.ZipCode,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = request.ConcurrencyStamp
            };

            var updateUser = await UserManager.UpdateAsync(updatedUser);

            if (!updateUser.Succeeded)
            {
                throw new LoginException(ErrorType.BadRequest);
            }
            var roleResult = await UserManager.AddToRoleAsync(updatedUser, UserRole.User.ToString());
            if (!roleResult.Succeeded)
            {
                throw new LoginException(ErrorType.UserCantBeAddedToRole);
            }

            await SendConfirmationRequestEmail(updatedUser);
            return Result.Success();
        }

        public async Task<Result<GoogleJsonWebSignature.Payload>> ValidateGoogleToken(GoogleAuthenticationReadServiceModel request, UserCompleteRegistrationWriteServiceModel registerRequest)
        {
            var googleTokenRequest = Mapper.Map<GoogleAuthenticationReadServiceModel>(request);
            try
            {
                GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(googleTokenRequest.Token, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> {
                     "714215492201-g6h6st7jdekseaj3ulq13bjtv3r6u4fb.apps.googleusercontent.com"
                 }
                });

                if (registerRequest is not null)
                {
                    var principalClaims = GetPrincipalFromToken(registerRequest.Token);
                    var userEmailClaim = principalClaims.FindFirst(JwtClaims.Email)?.Value;
                    if (payload.Email == userEmailClaim)
                    {
                        return Result.Success(payload);
                    }
                    else
                    {
                        throw new LoginException(ErrorType.TokenInvalid);
                    }
                }

                return Result.Success(payload);
            }
            catch (InvalidJwtException ex)
            {
                throw new InvalidJwtException(ex.Message);
            }
        }

        #endregion

    }
}