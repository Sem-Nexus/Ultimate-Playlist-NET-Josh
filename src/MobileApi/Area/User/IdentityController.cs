#region Usings

using System;
using System.Threading.Tasks;
using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using UltimatePlaylist.Common.Mvc.Attributes;
using UltimatePlaylist.Common.Mvc.Controllers;
using UltimatePlaylist.MobileApi.Models.Identity;
using UltimatePlaylist.Services.Common.Interfaces.Identity;
using UltimatePlaylist.Services.Common.Models.Identity;
using static UltimatePlaylist.Common.Mvc.Consts.Consts;
using Google.Apis.Auth;
#endregion

namespace UltimatePlaylist.MobileApi.Areas.User
{
    [Area("User")]
    [Route("[controller]")]
    [ApiExplorerSettings(GroupName = MobileApiGroups.User)]
    public class IdentityController : BaseController
    {
        #region Private Members

        private readonly Lazy<IUserIdentityService> IdentityServiceProvider;
        private readonly Lazy<IMapper> MapperProvider;
        private readonly Lazy<ILogger<IdentityController>> LoggerProvider;
        #endregion

        #region Constructor(s)

        public IdentityController(
            Lazy<IUserIdentityService> identityServiceProvider,
            Lazy<ILogger<IdentityController>> loggerProvider,
            Lazy<IMapper> mapperProvider)
        {
            LoggerProvider = loggerProvider;
            MapperProvider = mapperProvider;
            IdentityServiceProvider = identityServiceProvider;
        }

        #endregion

        #region Private Properites

        private IMapper Mapper => MapperProvider.Value;

        private IUserIdentityService IdentityService => IdentityServiceProvider.Value;
        private ILogger<IdentityController> Logger => LoggerProvider.Value;

        #endregion

        #region GET

        [HttpGet("password-reset")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> PasswordResetAsync([FromQuery] SendResetPasswordRequestModel request)
        {
            return await IdentityService.ResetPasswordAsync(request.Email)
                .Finally(BuildEnvelopeResult);
        }

        [HttpGet("resend-email-confirmation")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> ResendEmailConfirmationAsync([FromQuery] SendEmailConfirmationRequestModel request)
        {
            return await IdentityService.SendEmailActivationAsync(request.Email)
                .Finally(BuildEnvelopeResult);
        }

        #endregion

        #region POST

        [HttpPost("login")]
        [ProducesEnvelope(typeof(AuthenticationResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoginAsync([FromBody] UserLoginRequestModel request)
        {
            var serviceModel = Mapper.Map<UserLoginWriteServiceModel>(request);

            return await IdentityService.LoginAsync(serviceModel)
                .Map(loginServiceModel => Mapper.Map<AuthenticationResponseModel>(loginServiceModel))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("password-reset")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> PasswordResetdAsync([FromBody] ResetPasswordRequestModel request)
        {
            var resetPasswordRequest = Mapper.Map<ResetPasswordWriteServiceModel>(request);

            return await IdentityService.ResetPasswordAsync(resetPasswordRequest)
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("password-change")]
        [ProducesEnvelope(typeof(AuthenticationResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequestModel request)
        {
            var serviceRequest = Mapper.Map<ChangePasswordWriteServiceModel>(request);

            return await IdentityService.ChangePasswordAsync(serviceRequest)
                .Map(authResponse => Mapper.Map<AuthenticationResponseModel>(authResponse))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("refresh-token")]
        [ProducesEnvelope(typeof(AuthenticationResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshAsync([FromBody] RefreshTokenRequestModel request)
        {
            return await IdentityService.RefreshAsync(XToken, request.RefreshToken)
                .Map(authResponse => Mapper.Map<AuthenticationResponseModel>(authResponse))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("register")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationRequestModel request)
        {
            return await IdentityService.RegisterAsync(Mapper.Map<UserRegistrationWriteServiceModel>(request))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("registration-confirmation")]
        [ProducesEnvelope(typeof(AuthenticationResponseModel), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegistrationConfirmationAsync([FromBody] ConfirmEmailRequestModel request)
        {
            var confirmEmailRequestDto = Mapper.Map<ConfirmEmailWriteServiceModel>(request);

            return await IdentityService.RegistrationConfirmationAsync(confirmEmailRequestDto)
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("email-changed-confirmation")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> RegistratioConfirmationAsync([FromBody] EmailChangedConfirmationRequestModel request)
        {
            var confirmEmailChangedRequestDto = Mapper.Map<EmailChangedConfirmationWriteServiceModel>(request);

            return await IdentityService.EmailChangedConfirmationAsync(confirmEmailChangedRequestDto)
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost]
        [Route("external-login")]
        public async Task<IActionResult> LoginGoogleAsync([FromBody] GoogleAuthenticationReadServiceModel request)
        {
            if (request.Provider == "Google")
            {
                var googlePayload = await IdentityService.ValidateGoogleToken(request, null);
                return await IdentityService.LoginGoogleAsync(Mapper.Map<string>(googlePayload.Value))
                    .Map(loginServiceModel => Mapper.Map<AuthenticationReadServiceModel>(loginServiceModel))
                    .Finally(BuildEnvelopeResult);
            } else
            {
                var applePayload = await IdentityService.ValidateAppleIdTokenAsync(request, null);
                
                return await IdentityService.LoginGoogleAsync(Mapper.Map<string>(applePayload.Value))
                    .Map(loginServiceModel => Mapper.Map<AuthenticationReadServiceModel>(loginServiceModel))
                    .Finally(BuildEnvelopeResult);
            }
           
        }

        [HttpPost("complete-register")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> CompleteRegisterAsync([FromBody] UserCompleteRegistrationWriteServiceModel request)
        {
            var googleTokenRequest = new GoogleAuthenticationReadServiceModel()
            {
                Token = request.ExternalToken
            };
            UserCompleteRegistrationWriteServiceModel registerRequest = Mapper.Map<UserCompleteRegistrationWriteServiceModel>(request);

            if (request.Provider == "Google")
            {
                await IdentityService.ValidateGoogleToken(googleTokenRequest, registerRequest);
            }
            else
            {
                await IdentityService.ValidateAppleIdTokenAsync(googleTokenRequest, registerRequest);
            }
            return await IdentityService.CompleteRegisterAsync(Mapper.Map<UserCompleteRegistrationWriteServiceModel>(registerRequest))
                .Finally(BuildEnvelopeResult);
        }
        #endregion
    }
}