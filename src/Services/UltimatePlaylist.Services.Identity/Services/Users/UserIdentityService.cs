#region Usings

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
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Security.Claims;

#endregion

namespace UltimatePlaylist.Services.Identity.Services.Users
{
    public class UserIdentityService : BaseIdentityService, IUserIdentityService
    {
        #region Private members

        private readonly Lazy<ILogger<UserIdentityService>> LoggerProvider;
        private readonly Lazy<IReadOnlyRepository<GenderEntity>> GenderRepositoryProvider;
        private const string AppleIdKeysEndpoint = "https://appleid.apple.com/auth/keys";
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
            user.Device = userLoginWriteServiceModel.Device;
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
            newUser.Device = request.Device;
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

        public async Task<Result<AuthenticationReadServiceModel>> ExternalLoginAsync(dynamic user, string provider, string device)
        {
 
            var existingUser = await UserManager.FindByEmailAsync(user);
            if (existingUser is not null)
            {
                if (existingUser.PasswordHash is not null)
                {
                    return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.EmailTaken);
                }

                existingUser.Device = device;
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
                await UserManager.AddLoginAsync(existingUser, new UserLoginInfo(provider, existingUser.UserName, existingUser.Email));
                return await GenerateAuthenticationResult(existingUser);

            }

            var gender = await GenderRepository.FirstOrDefaultAsync(new GenderSpecification()
             .ByExternalId(new Guid("989EE81B-8B8E-4FC9-BFE8-D81F19F8A6D7")));
            if (gender is null)
            {
                throw new LoginException(ErrorType.GenderDoesNotExist);
            }

            var newUserFromGoogle = new User()
            {
                UserName = "TemporaryUserName" + user,
                Email = user,
                Gender = gender,
                Device = device
            };

            var createdUser = await UserManager.CreateAsync(newUserFromGoogle);
            if (!createdUser.Succeeded)
            {
                throw new LoginException(ErrorType.UserNotCreated);
            }

            existingUser = await UserManager.FindByEmailAsync(user);
            var tokenData = await GenerateAuthenticationResult(existingUser);
            return new AuthenticationReadServiceModel
            {
                Token = tokenData.Value.Token,
                RefreshToken = tokenData.Value.RefreshToken,
                ConcurrencyStamp = existingUser.ConcurrencyStamp
            };
        }

        public async Task<Result<AuthenticationReadServiceModel>> CompleteRegisterAsync(UserCompleteRegistrationWriteServiceModel request)
        {
            
            var claims = GetUserTokenClaims(request.Token);
            var userIdClaim = claims.Claims.First(claim => claim.Type == "Id").Value;
            var userEmailClaim = claims.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;

            var existingUserWithUniqueUsername = await UserManager.FindByNameAsync(request.Username);
            if (existingUserWithUniqueUsername is not null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorMessages.UsernameTaken);
            }

            var gender = await GenderRepository.FirstOrDefaultAsync(new GenderSpecification()
                .ByExternalId(request.GenderExternalId));
            if (gender is null)
            {
                return Result.Failure<AuthenticationReadServiceModel>(ErrorType.GenderDoesNotExist.ToString());
            }

            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                request.PhoneNumber = string.Empty;
            }

            var user = await UserManager.FindByIdAsync(userIdClaim);

            request.Username = request.Username.Trim();
            user.Id = Int32.Parse(userIdClaim);
            user.UserName = request.Username;
            user.Name = request.FirstName;
            user.LastName = request.LastName;
            user.Email = userEmailClaim;
            user.Gender = gender;
            user.IsActive = true;
            user.ShouldNotificationBeEnabled = true;
            user.PhoneNumber = request.PhoneNumber;
            user.BirthDate = request.BirthDate;
            user.ZipCode = request.ZipCode;
            user.ExternalId = Guid.NewGuid();
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.ConcurrencyStamp = request.ConcurrencyStamp;
            user.EmailConfirmed = true;

            var updateUser = await UserManager.UpdateAsync(user);
            if (!updateUser.Succeeded)
            {
                throw new LoginException(ErrorType.BadRequest);
            }
            var roleResult = await UserManager.AddToRoleAsync(user, UserRole.User.ToString());
            if (!roleResult.Succeeded)
            {
                throw new LoginException(ErrorType.UserCantBeAddedToRole);
            }

            var token = await ExternalLoginAsync(userEmailClaim, request.Provider, user.Device);

            return token;
        }

        public async Task<Result<string>> ValidateGoogleToken(ExternalAuthenticationReadServiceModel request, UserCompleteRegistrationWriteServiceModel registerRequest)
        {
            var googleTokenRequest = Mapper.Map<ExternalAuthenticationReadServiceModel>(request);
            try
            {
                GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(googleTokenRequest.Token, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> {
                     "714215492201-sn3puh8a0v05415m5jqtlv4obhq2ov2f.apps.googleusercontent.com",
                     "714215492201-g6h6st7jdekseaj3ulq13bjtv3r6u4fb.apps.googleusercontent.com",
                     "714215492201-2mejgfgjoqa3o1eib6qfg4evqsh4j1v4.apps.googleusercontent.com"
                    }
                });

                if (registerRequest is not null)
                { 
                    var claims = GetUserTokenClaims(registerRequest.Token);
                    var email = claims.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;

                    if (payload.Email == email)
                    {
                        return Result.Success(payload.Email);
                    }
                    else
                    {
                        throw new LoginException(ErrorType.TokenInvalid);
                    }
                }

                return Result.Success(payload.Email);
            }
            catch (InvalidJwtException ex)
            {
                throw new InvalidJwtException(ex.Message);
            }
        }

        public async Task<Result<string>> ValidateAppleIdTokenAsync(ExternalAuthenticationReadServiceModel request, UserCompleteRegistrationWriteServiceModel registerRequest)
        {

            var tokenRequest = Mapper.Map<ExternalAuthenticationReadServiceModel>(request);

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "https://appleid.apple.com",
                    ValidAudience = "com.production.ultimateplaylist.app",
                    IssuerSigningKeys = await GetPublicKeys()
                };

                ClaimsPrincipal payload = handler.ValidateToken(tokenRequest.Token, validationParameters, out var validatedToken);
                var appleClaims = payload.Claims;
                var appleEmail = appleClaims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;

                if (registerRequest is not null)
                {                   
                    var claims = GetUserTokenClaims(registerRequest.Token);
                    var email = claims.Claims.First(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;

                    if (appleEmail == email)
                    {
                        return Result.Success(appleEmail);
                    }
                    else
                    {
                        throw new LoginException(ErrorType.TokenInvalid);
                    }
                }

                return Result.Success(appleEmail);
            }
            catch (InvalidJwtException ex)
            {
                throw new InvalidJwtException(ex.Message);
            }
        }

        public static async Task<IEnumerable<SecurityKey>> GetPublicKeys()
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(AppleIdKeysEndpoint);
            var responseContent = await response.Content.ReadAsStringAsync();

            var jwkSet = JsonConvert.DeserializeObject<JsonWebKeySet>(responseContent);

            var securityKeys = new List<SecurityKey>();
            foreach (var jwk in jwkSet.Keys)
            {
                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = Base64UrlEncoder.DecodeBytes(jwk.N),
                    Exponent = Base64UrlEncoder.DecodeBytes(jwk.E)
                });
                securityKeys.Add(new RsaSecurityKey(rsa));
            }

            return securityKeys;
        }

        private static JwtSecurityToken GetUserTokenClaims(string Token)
        {
            var stream = Token;
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(stream);
            var tokenS = jsonToken as JwtSecurityToken;
            return tokenS;
        }

        #endregion

    }
}