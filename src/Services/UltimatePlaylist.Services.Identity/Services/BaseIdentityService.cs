#region Usings

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using CSharpFunctionalExtensions;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UltimatePlaylist.Common;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Extensions;
using UltimatePlaylist.Common.Mvc.Exceptions;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity.Specifications;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Services.Common.Interfaces.Identity;
using UltimatePlaylist.Services.Common.Models.Email;
using UltimatePlaylist.Services.Common.Models.Identity;
using UltimatePlaylist.Services.Email.Jobs;

#endregion

namespace UltimatePlaylist.Services.Identity.Services
{
    public abstract class BaseIdentityService
    {
        #region Private members

        private readonly AuthConfig Config;
        private readonly EmailConfig EmailConfig;
        private readonly byte[] JwtKey;

        private readonly Lazy<UserManager<User>> UserManagerProvider;
        private readonly Lazy<IMapper> MapperProvider;
        private readonly Lazy<IBackgroundJobClient> BackgroundJobProvider;
        private readonly Lazy<IUserRetrieverService> UserRetrieverServiceProvider;
        private readonly Lazy<IRepository<User>> UserRepositoryProvider;

        #endregion

        #region Constructor(s)

        public BaseIdentityService(
            Lazy<UserManager<User>> userManagerProvider,
            Lazy<IMapper> mapperProvider,
            Lazy<IBackgroundJobClient> backgroundJobClientProvider,
            Lazy<IUserRetrieverService> userRetrieverServiceProvider,
            Lazy<IRepository<User>> userRepositoryProvider,
            IOptions<AuthConfig> authOptions,
            IOptions<EmailConfig> emailOptions)
        {
            Config = authOptions.Value;
            EmailConfig = emailOptions.Value;
            BackgroundJobProvider = backgroundJobClientProvider;
            JwtKey = Encoding.ASCII.GetBytes(Config.JWT.Key);

            UserRetrieverServiceProvider = userRetrieverServiceProvider;
            UserManagerProvider = userManagerProvider;
            MapperProvider = mapperProvider;
            UserRepositoryProvider = userRepositoryProvider;
        }

        #endregion

        #region Properties
        protected IMapper Mapper => MapperProvider.Value;

        protected UserManager<User> UserManager => UserManagerProvider.Value;

        protected IBackgroundJobClient BackgroundJobClient => BackgroundJobProvider.Value;

        protected IUserRetrieverService UserRetriever => UserRetrieverServiceProvider.Value;

        protected IRepository<User> UserRepository => UserRepositoryProvider.Value;

        #endregion

        #region Public methods

        public string GetUserId(string token)
        {
            var principalClaims = GetPrincipalFromToken(token);
            var userIdClaim = principalClaims.FindFirst(JwtClaims.Id)?.Value;
            return userIdClaim;
        }

        public async Task<Result<AuthenticationReadServiceModel>> RefreshAsync(string token, string refreshToken, string Device)
        {
            var principalClaims = GetPrincipalFromToken(token);
            var userIdClaim = principalClaims.FindFirst(JwtClaims.ExternalId)?.Value;
            var userEmailClaim = principalClaims.FindFirst(JwtClaims.Email)?.Value;
            var userId = new Guid(userIdClaim!).Decode(userEmailClaim!);
            
            var user = await UserManager.Users.FirstOrDefaultAsync(u => u.ExternalId.Equals(userId));
            if (user is null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorType.UserDoesNotExist.ToString());
            }

            if (user.RefreshTokenHash != refreshToken)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorType.TokenInvalid.ToString());
            }
            user.Device = Device;

            return await GenerateAuthenticationResult(user);
        }

        public async Task<Result<AuthenticationReadServiceModel>> ChangePasswordAsync(ChangePasswordWriteServiceModel request)
        {
            var userId = UserRetriever.GetUserExternalId().ToString();

            var user = await UserManager.Users.FirstOrDefaultAsync(u => u.ExternalId.Equals(new Guid(userId)));
            if (user is null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.ChangePasswordFailed);
            }

            var result = await UserManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.ChangePasswordFailed);
            }

            return await GenerateAuthenticationResult(user);
        }

        public async Task<Result> ResetPasswordAsync(ResetPasswordWriteServiceModel request)
        {
            var decodedToken = DecodeFrom64(request.Token);
            var tokenPayload = JsonSerializer.Deserialize<EmailTokenReadServiceModel>(decodedToken);
            var user = await UserManager.FindByEmailAsync(tokenPayload.Email);

            if (user is null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.ChangePasswordFailed);
            }

            if (user.ResetTokenCodeExiprationDate < DateTime.UtcNow)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.ChangePasswordFailed);
            }

            if (user.ResetToken != HttpUtility.UrlDecode(tokenPayload.Token))
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.ChangePasswordFailed);
            }

            var changePasswordToken = await UserManager.GeneratePasswordResetTokenAsync(user);
            var result = await UserManager.ResetPasswordAsync(user, changePasswordToken, request.Password);

            if (!result.Succeeded)
            {
                return Result.Failure(ErrorMessages.ChangePasswordFailed);
            }

            return Result.Success();
        }

        public async Task<Result> ResetPasswordAsync(string email)
        {
            var user = await UserManager.FindByEmailAsync(email);

            if (user != null)
            {
                await SendPasswordResetEmail(user);
            }

            return Result.Success();
        }

        public async Task<Result<AuthenticationReadServiceModel>> RegistrationConfirmationAsync(ConfirmEmailWriteServiceModel request)
        {
            var decodedToken = DecodeFrom64(request.Token);
            var tokenPayload = JsonSerializer.Deserialize<EmailTokenReadServiceModel>(decodedToken);
            var user = await UserManager.Users.FirstOrDefaultAsync(u => u.Email.Equals(tokenPayload.Email));

            if (user is null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.UserDoesNotExist);
            }

            if (user.ConfirmationCodeExiprationDate < DateTime.UtcNow)
            {
                await SendConfirmationRequestEmail(user);
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.EmailConfirmationExpired);
            }

            if (user.UserToeknRequiredByWebConfirmation != HttpUtility.UrlDecode(tokenPayload.Token))
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.EmailConfirmationFailed);
            }

            user.EmailConfirmed = true;

            await UserRepository.UpdateAndSaveAsync(user);

            return await GenerateAuthenticationResult(user);
        }

        public async Task<Result<AuthenticationReadServiceModel>> EmailChangedConfirmationAsync(EmailChangedConfirmationWriteServiceModel request)
        {
            var decodedToken = DecodeFrom64(request.Token);
            var tokenPayload = JsonSerializer.Deserialize<EmailTokenReadServiceModel>(decodedToken);
            var user = await UserManager.Users.FirstOrDefaultAsync(u => u.NewNotConfirmedEmail.Equals(tokenPayload.Email));

            if (user is null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.UserDoesNotExist);
            }

            if (user.UserToeknRequiredByWebConfirmation != HttpUtility.UrlDecode(tokenPayload.Token))
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.EmailConfirmationFailed);
            }

            user.EmailConfirmed = true;
            user.Email = user.NewNotConfirmedEmail;
            user.NormalizedEmail = user.NewNotConfirmedEmail.ToUpper();
            user.NewNotConfirmedEmail = null;
            user.IsEmailChangeConfirmedFromWeb = request.IsFromWeb;
            user.UserToeknRequiredByWebConfirmation = null;

            await UserRepository.UpdateAndSaveAsync(user);

            return await GenerateAuthenticationResult(user);
        }

        public async Task<Result> SendEmailActivationAsync(string email)
        {
            var user = await UserManager.FindByEmailAsync(email);
            if (user is not null && !user.EmailConfirmed)
            {
                await SendConfirmationRequestEmail(user);
            }

            return Result.Success();
        }

        #endregion

        #region Protected methods

        #region Email

        protected async Task SendConfirmationRequestEmail(User user)
        {
            var confirmationToken = await UserManager.GenerateEmailConfirmationTokenAsync(user);

            user.ConfirmationCodeExiprationDate = DateTime.UtcNow.Add(EmailConfig.ConfirmationExpirationTime);
            user.UserToeknRequiredByWebConfirmation = confirmationToken;
            await UserManager.UpdateAsync(user);

            var request = new RegistrationConfirmationRequest
            {
                Email = user.Email,
                Name = user.Name,
                Token = HttpUtility.UrlEncode(confirmationToken),
            };
            BackgroundJobClient.Enqueue<EmailJob>(p => p.SendRegistrationConfirmationAsync(request));
        }

        protected async Task SendPasswordResetEmail(User user)
        {
            var token = GenerateToken();

            user.ResetTokenCodeExiprationDate = DateTime.UtcNow.Add(EmailConfig.ResetPasswordExpirationTime);
            user.ResetToken = token;
            await UserManager.UpdateAsync(user);

            var request = new ResetPasswordEmailRequset
            {
                Email = user.Email,
                Name = user.Name,
                Token = HttpUtility.UrlEncode(token),
            };
            BackgroundJobClient.Enqueue<EmailJob>(p => p.SendResetPasswordAsync(request));
        }
        #endregion

        protected async Task<Result<AuthenticationReadServiceModel>> GenerateAuthenticationResult(User user)
        {
            var jwtToken = await GenerateToken(user);
            var refreshToken = GenerateToken();

            user.RefreshTokenHash = refreshToken;            
            await UserManager.UpdateAsync(user);
            return await Task.FromResult(CreateResponse(jwtToken, refreshToken));
        }

        protected ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            try
            {
                token = token.Replace("Bearer ", string.Empty);
                var tokenValidationParameters = TokenValidation.GetTokenValidation(Config);
                tokenValidationParameters.ValidateLifetime = false;
                var principal = new JwtSecurityTokenHandler().ValidateToken(
                        token,
                        tokenValidationParameters,
                        out var securityToken);
                return !IsJwtWithValidSecurityAlgorithm(securityToken) ? null : principal;
            }
            catch
            {
                return null;
            }
        }

        private AuthenticationReadServiceModel CreateResponse(
            string token = null,
            string refreshToken = null)
        {
            return new AuthenticationReadServiceModel
            {
                Token = $"Bearer {token}",
                RefreshToken = refreshToken,
            };
        }

        #endregion

        #region Token

        private async Task<string> GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var claims = await GetUserClaims(user);
            var jwtToken = new JwtSecurityToken(
                Config.JWT.Issuer,
                claims: claims,
                expires: DateTime.UtcNow.Add(Config.JWT.TokenExpirationTime),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(JwtKey), SecurityAlgorithms.HmacSha256Signature));

            return tokenHandler.WriteToken(jwtToken);
        }

        private async Task<List<Claim>> GetUserClaims(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtClaims.Email, user.Email),
                new Claim(JwtClaims.ExternalId, user.ExternalId.Encode(user.Email).ToString()),
                new Claim(JwtClaims.IsPinRequired, (!string.IsNullOrEmpty(user.Pin)).ToString(), ClaimValueTypes.Boolean),
                new Claim(JwtClaims.Id, user.Id.ToString()),
            };

            claims.AddRange(await UserManager.GetClaimsAsync(user));

            var userRoles = await UserManager.GetRolesAsync(user);
            claims.Add(new Claim(JwtClaims.Role, userRoles.FirstOrDefault() ?? UltimatePlaylist.Common.Enums.UserRole.User.ToString()));

            return claims;
        }

        private string GenerateToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken jwtToken)
        {
            return jwtToken is JwtSecurityToken jwtSecurityToken &&
                   jwtSecurityToken.Header.Alg.Equals(
                       SecurityAlgorithms.HmacSha256Signature,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        private string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes = Convert.FromBase64String(encodedData);
            string returnValue = Encoding.ASCII.GetString(encodedDataAsBytes);

            return returnValue;
        }

        #endregion
    }
}
