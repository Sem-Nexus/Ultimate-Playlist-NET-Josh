#region Usings

using CSharpFunctionalExtensions;
using UltimatePlaylist.Services.Common.AppleMusic.Enums;
using UltimatePlaylist.Services.Common.Models.AppleMusic;
using UltimatePlaylist.Services.Common.Models.AppleMusic.Request;
using UltimatePlaylist.Services.Common.Models.AppleMusic.Responses;

#endregion

namespace UltimatePlaylist.Services.Common.Interfaces.AppleMusic.Client
{
    public interface IAppleMusicPlaylistClientService
    {
        Task<Result<AppleLibraryPlaylistResponse>> GetAllLibraryPlaylists(
            string userToken,
            Guid userExternalId,
            IReadOnlyCollection<AppleLibraryPlaylistRelationship> include = null,
            AppleMusicPageOptions pageOptions = null,
            string locale = null);

        Task<Result<AppleDataResponseRoot<AppleMusicPlaylistDataResponseModel>>> FetchPlaylistsAsync(string userToken, Guid userExternalId, AppleMusicPageOptions pageOptions = null);

        Task<Result<AppleMusicPlaylistDataResponseModel>> CreateNewLibraryResources<TResponse>(
            string userToken,
            Guid userExternalId,
            AppleMusicResurceType libraryResource,
            AppleMusicCreatePlaylistRequestModel appleMusicCreatePlaylistRequestModel);

        Task<Result<AppleSearchResponse>> GetAllSongs(Guid userExternalId, string queryParam);

        Task<Result<AppleSongIdResponse>> GetSongByID(Guid userExternalId, string songID);

    }
}
