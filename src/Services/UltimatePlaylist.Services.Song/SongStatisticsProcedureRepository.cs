#region Usings

using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.Song;
using UltimatePlaylist.Services.Common.Models.Song;

#endregion

namespace UltimatePlaylist.Services.Song
{
    public class SongStatisticsProcedureRepository : ISongStatisticsProcedureRepository
    {
        private readonly EFContext Context;

        public SongStatisticsProcedureRepository(EFContext context)
        {
            Context = context;
        }

        public async Task<int> GeneralSongsCount(SongsAnalyticsFilterServiceModel filter)
        {
            var genres = filter.MusicGenres.Any() ? string.Join(',', filter.MusicGenres) : string.Empty;
            var builder = new StringBuilder();
            builder.Append("[dbo].[SongsAnalyticsCount]");
            builder.Append($"@Genres = '{genres}',");
            builder.Append($"@Licensor = '{filter.Licensor}'");

            var data = await Context
                .GeneralSongsCountProcedureViews
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return data?.FirstOrDefault()?.SongsCount ?? 0;
        }

        public async Task<Result<List<GeneralSongDataProcedureView>>> GetGeneralSongsData(
            Pagination pagination,
            SongsAnalyticsFilterServiceModel filter)
        {
            var minAge = filter.Age?.Min(x => x.MinAge) ?? null;
            var maxAge = filter.Age?.Max(x => x.MaxAge) ?? null;
            var minBirthDate = minAge.HasValue ? DateTime.UtcNow.AddYears(-minAge.Value) : DateTime.UtcNow;
            var maxBirthDate = maxAge.HasValue ? DateTime.UtcNow.AddYears(-maxAge.Value) : DateTime.Parse("1800-01-01");
            var timeRange = filter.TimeRange ?? DateTime.Parse("1800-01-01");
            var genres = filter.MusicGenres.Any() ? string.Join(',', filter.MusicGenres) : string.Empty;
            var genders = filter.Genders.Any() ? string.Join(',', filter.Genders) : string.Empty;
            var builder = new StringBuilder();
            builder.Append("[dbo].[SongsAnalytics]");
            builder.Append($"@BirthDateMin = '{GetDate(minBirthDate)}',");
            builder.Append($"@BirthDateMax = '{GetDate(maxBirthDate)}',");
            builder.Append($"@Gender = '{genders}',");
            builder.Append($"@ZipCode = '{filter.ZipCode}',");
            builder.Append($"@TimeRange = '{GetDate(timeRange)}',");
            builder.Append($"@Skip = '{pagination?.Skip ?? 0}',");
            builder.Append($"@Take = '{pagination?.PageSize ?? 20}',");
            builder.Append($"@SortType = '{pagination?.OrderBy}',");
            builder.Append($"@Genres = '{genres}',");
            builder.Append($"@Licensor = '{filter.Licensor}',");
            builder.Append($"@SearchValue = '{pagination?.SearchValue}'");

            var data = await Context
                .GeneralSongDataProcedureViews
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return Result.Success(data);
        }

        public async Task<Result<List<GeneralSongsAnalyticsFileInformationView>>> GetFileSongsData(
            Pagination pagination,
            SongsAnalyticsFilterServiceModel filter)
        {
            var builder = new StringBuilder();
            builder.Append("[dbo].[SongsAnalyticsFileInformation]");
            builder.Append($"@BirthDateMin = '{DateTime.UtcNow}',");
            builder.Append($"@BirthDateMax = '{DateTime.Parse("1800-01-01")}',");
            builder.Append($"@Gender = '',");
            builder.Append($"@ZipCode = '',");
            builder.Append($"@TimeRange = '{DateTime.Parse("1800-01-01")}',");
            builder.Append($"@Skip = '{0}',");
            builder.Append($"@Take = '{9999}',");
            builder.Append($"@SortType = '',");
            builder.Append($"@Genres = '',");
            builder.Append($"@Licensor = ''");

            var data = await Context
                .GeneralSongsAnalyticsFileInformationViews
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return Result.Success(data);
        }

        private string GetDate(DateTime date)
        {
            return date.ToString("yyyy'-'MM'-'dd");
        }
    }
}
