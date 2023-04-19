#region Usings

using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using UltimatePlaylist.Services.Common.Models.Identity;
using Google.Apis.Auth;
using System.Security.Claims;
#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.Identity
{
    public interface IUserIdentityService
    {
        Task<Result<AuthenticationReadServiceModel>> LoginAsync(UserLoginWriteServiceModel userLoginWriteServiceModel);

        Task<Result> RegisterAsync(UserRegistrationWriteServiceModel registrationRequest);
        
        Task<Result<AuthenticationReadServiceModel>> LoginGoogleAsync(dynamic user);

        Task<Result<string>> ValidateGoogleToken(GoogleAuthenticationReadServiceModel user, UserCompleteRegistrationWriteServiceModel request);
        
        Task<Result<string>> ValidateAppleIdTokenAsync(GoogleAuthenticationReadServiceModel user, UserCompleteRegistrationWriteServiceModel request);
        
        Task<Result> CompleteRegisterAsync(UserCompleteRegistrationWriteServiceModel registrationRequest);
        
        Task<Result<AuthenticationReadServiceModel>> ChangePasswordAsync(ChangePasswordWriteServiceModel request);

        Task<Result<AuthenticationReadServiceModel>> RefreshAsync(string token, string refreshToken);

        Task<Result<AuthenticationReadServiceModel>> RegistrationConfirmationAsync(ConfirmEmailWriteServiceModel confirmEmailRequestDto);

        Task<Result<AuthenticationReadServiceModel>> EmailChangedConfirmationAsync(EmailChangedConfirmationWriteServiceModel request);

        Task<Result> ResetPasswordAsync(ResetPasswordWriteServiceModel resetPasswordRequest);

        Task<Result> ResetPasswordAsync(string email);

        Task<Result> SendEmailActivationAsync(string email);       
    }
}