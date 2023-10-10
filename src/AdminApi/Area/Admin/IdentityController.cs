#region Usings

using System;
using System.Threading.Tasks;
using AutoMapper;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UltimatePlaylist.AdminApi.Models.Identity;
using UltimatePlaylist.Common.Mvc.Attributes;
using UltimatePlaylist.Common.Mvc.Controllers;
using UltimatePlaylist.Services.Common.Interfaces.Identity;
using UltimatePlaylist.Services.Common.Models.Identity;
using static UltimatePlaylist.Common.Mvc.Consts.Consts;

#endregion

namespace UltimatePlaylist.AdminApi.Area.Admin
{
    [Area("Admin")]
    [Route("[controller]")]
    [ApiExplorerSettings(GroupName = AdminApiGroups.Administrator)]
    public class IdentityController : BaseController
    {
        #region Private Members

        private readonly Lazy<IAdministratorIdentityService> IdentityServiceProvider;
        private readonly Lazy<IMapper> MapperProvider;

        #endregion

        #region Constructor(s)

        public IdentityController(
            Lazy<IAdministratorIdentityService> identityServiceProvider,
            Lazy<IMapper> mapperProvider)
        {
            MapperProvider = mapperProvider;
            IdentityServiceProvider = identityServiceProvider;
        }

        #endregion

        #region Private Properites

        private IMapper Mapper => MapperProvider.Value;

        private IAdministratorIdentityService IdentityService => IdentityServiceProvider.Value;

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
            return await IdentityService.RefreshAsync(XToken, request.RefreshToken, request.Device)
                .Map(authResponse => Mapper.Map<AuthenticationResponseModel>(authResponse))
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("registration-confirmation")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> RegistrationConfirmationAsync([FromBody] ConfirmEmailRequestModel request)
        {
            var confirmEmailRequestDto = Mapper.Map<ConfirmEmailWriteServiceModel>(request);

            return await IdentityService.RegistrationConfirmationAsync(confirmEmailRequestDto)
                .Finally(BuildEnvelopeResult);
        }

        [HttpPost("email-changed-confirmation")]
        [ProducesEmptyEnvelope(StatusCodes.Status200OK)]
        public async Task<IActionResult> EmailChangedConfirmationAsync([FromBody] EmailChangedConfirmationRequestModel request)
        {
            var confirmEmailChangedRequestDto = Mapper.Map<EmailChangedConfirmationWriteServiceModel>(request);
            confirmEmailChangedRequestDto.IsFromWeb = true;

            return await IdentityService.EmailChangedConfirmationAsync(confirmEmailChangedRequestDto)
                .Finally(BuildEnvelopeResult);
        }

        #endregion
    }
}