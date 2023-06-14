#region Usings

using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Extensions;
using UltimatePlaylist.Services.Common.AppleMusic.Enums;
using UltimatePlaylist.Services.Common.Interfaces.AppleMusic;
using UltimatePlaylist.Services.Common.Interfaces.AppleMusic.Client;
using UltimatePlaylist.Services.Common.Models.AppleMusic;
using UltimatePlaylist.Services.Common.Models.AppleMusic.Request;
using UltimatePlaylist.Services.Common.Models.AppleMusic.Responses;

#endregion

namespace UltimatePlaylist.Services.AppleMusic.Client
{
    public class AppleMusicPlaylistClientService : AppleMusicBaseClient, IAppleMusicPlaylistClientService
    {
        private const string BaseRequestUri = "me/library";

        public AppleMusicPlaylistClientService(
            IHttpClientFactory clientFactory,
            Lazy<IAppleMusicTokenService> appleMusicTokenServiceProvider,
            Lazy<IAppleMusicConnectionService> appleMusicConnectionServiceProvider,
            Lazy<ILogger<AppleMusicBaseClient>> loggerProvider)
            : base(clientFactory, appleMusicTokenServiceProvider, appleMusicConnectionServiceProvider, loggerProvider)
        {
        }

        #region Public Methods

        public async Task<Result<AppleLibraryPlaylistResponse>> GetAllLibraryPlaylists(string userToken, Guid userExternalId, IReadOnlyCollection<AppleLibraryPlaylistRelationship> include = null, AppleMusicPageOptions pageOptions = null, string locale = null)
        {
            var setTokenResult = SetUserTokenHeader(userToken);
            if (setTokenResult.IsFailure)
            {
                return Result.Failure<AppleLibraryPlaylistResponse>(ErrorType.AppleMusicSetUserTokenFailure.ToString());
            }

            return await GetAllLibraryResources<AppleLibraryPlaylistResponse, AppleLibraryPlaylistRelationship>(AppleMusicResurceType.Playlists, userExternalId, include, pageOptions, locale);
        }

        public async Task<Result<AppleDataResponseRoot<AppleMusicPlaylistDataResponseModel>>> FetchPlaylistsAsync(string userToken, Guid userExternalId, AppleMusicPageOptions pageOptions = null)
        {
            var setTokenResult = SetUserTokenHeader(userToken);
            if (setTokenResult.IsFailure)
            {
                return Result.Failure<AppleDataResponseRoot<AppleMusicPlaylistDataResponseModel>>(ErrorType.AppleMusicSetUserTokenFailure.ToString());
            }

            return await GetAllLibraryResources<AppleDataResponseRoot<AppleMusicPlaylistDataResponseModel>, AppleLibraryPlaylistRelationship>(AppleMusicResurceType.Playlists, userExternalId, null, pageOptions, null);
        }

        public async Task<Result<AppleMusicPlaylistDataResponseModel>> CreateNewLibraryResources<TResponse>(
            string userToken,
            Guid userExternalId,
            AppleMusicResurceType libraryResource,
            AppleMusicCreatePlaylistRequestModel appleMusicCreatePlaylistRequestModel)
        {
            var setTokenResult = SetUserTokenHeader(userToken);
            if (setTokenResult.IsFailure)
            {
                return Result.Failure<AppleMusicPlaylistDataResponseModel>(ErrorType.AppleMusicSetUserTokenFailure.ToString());
            }

            return await Post<AppleMusicCreatePlaylistRequestModel, AppleDataResponseRoot<AppleMusicPlaylistDataResponseModel>>($"{BaseRequestUri}/{libraryResource.GetValue()}", appleMusicCreatePlaylistRequestModel, userExternalId)
                .Map(response => response.Data[0]);
        }

        
        public async Task<Result<AppleSearchResponse>> GetAllSongs(Guid userExternalId, string queryParam)
        {            
            return await GetSongs<AppleSearchResponse>(userExternalId, queryParam);
        }

        public async Task<Result<AppleSongIdResponse>> GetSongByID(Guid userExternalId, string songID)
        {
            return await GetSongByID<AppleSongIdResponse>(userExternalId, songID);
        }

        #endregion

        #region Private methods

        private async Task<Result<TResponse>> GetAllLibraryResources<TResponse, TRelationshipEnum>(AppleMusicResurceType libraryResource, Guid userExternalId, IReadOnlyCollection<TRelationshipEnum> include = null, AppleMusicPageOptions pageOptions = null, string locale = null)
            where TRelationshipEnum : IConvertible
        {
            var queryString = new Dictionary<string, string>();
            if (include is not null && include.Any())
            {
                queryString.Add("include", string.Join(",", include.Select(x => x.GetValue())));
            }

            return await Get<TResponse>($"{BaseRequestUri}/{libraryResource.GetValue()}", userExternalId, queryString, pageOptions, locale);
        }

        private async Task<Result<TResponse>> GetSongs<TResponse>(Guid userExternalId, string queryParams)
        {            
            return await Get<TResponse>($"catalog/us/search?types=songs&term={queryParams}", userExternalId, null, null, null);
        }

        private async Task<Result<TResponse>> GetSongByID<TResponse>(Guid userExternalId, string songID)
        {            
            return await Get<TResponse>($"catalog/us/songs/{songID}", userExternalId, null, null, null);
        }

        #endregion
    }
}
