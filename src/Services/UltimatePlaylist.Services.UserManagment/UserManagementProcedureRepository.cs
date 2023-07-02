#region Usings

using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using UltimatePlaylist.Common.Const;
using UltimatePlaylist.Common.Extensions;
using UltimatePlaylist.Common.Filters.Enums.Conditions;
using UltimatePlaylist.Common.Filters.Models;
using UltimatePlaylist.Common.Models;
using UltimatePlaylist.Database.Infrastructure.Context;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Services.Common.Interfaces.User;
using UltimatePlaylist.Services.Common.Models.UserManagment;

#endregion

namespace UltimatePlaylist.Services.UserManagement
{
    public class UserManagementProcedureRepository : IUserManagementProcedureRepository
    {
        private readonly EFContext Context;

        public UserManagementProcedureRepository(EFContext context)
        {
            Context = context;
        }

        public async Task<int> UsersCount(Pagination pagination, IEnumerable<FilterModel> filter)
        {
            var isActive = GetIsActive(filter);
            var IsDeleted = GetIsDeleted(filter);
            var builder = new StringBuilder();
            builder.Append("[dbo].[UserManagementViewCount]");
            builder.Append($"@SearchValue = '{pagination.SearchValue}',");
            builder.Append($"@IsDeleted = '{IsDeleted}',");
            builder.Append($"@IsActive = '{isActive}'");

            var data = await Context
                .UserManagementProcedureCountViews
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return data?.FirstOrDefault()?.UserCount ?? 0;
        }

        public async Task<Result<List<UserManagementProcedureView>>> GetUsersManagementList(
            Pagination pagination,
            IEnumerable<FilterModel> filter)
        {
            var isActive = GetIsActive(filter);
            var isDeleted = GetIsDeleted(filter);
            var timeRange = GetTimeRange(filter) ?? DateTime.Parse("1800-01-01");
            var builder = new StringBuilder();
            builder.Append("[dbo].[UserManagementView]");
            builder.Append($"@SearchValue = '{pagination.SearchValue ?? string.Empty}',");
            builder.Append($"@TimeRange = '{GetDate(timeRange)}',");
            builder.Append($"@Skip = '{pagination?.Skip ?? 0}',");
            builder.Append($"@Take = '{pagination?.PageSize ?? 20}',");
            builder.Append($"@SortType = '{pagination?.OrderBy}',");
            builder.Append($"@IsDeleted = '{isDeleted}',");
            builder.Append($"@IsActive = '{isActive}'");

            var data = await Context
                .UserManagementProcedureViews
                .FromSqlRaw(builder.ToString())
                .ToListAsync();

            return Result.Success(data);
        }

        public async Task<Result<ListenersStatisticsProcedureView>> GetListenersStatistics(ListenersReadServiceModel serviceModel)
        {
            var from = serviceModel.From ?? DateTime.Parse("1800-01-01");
            var to = serviceModel.To ?? DateTime.UtcNow;
            var builder = new StringBuilder();
            builder.Append("[dbo].[ListenersStatistics]");
            builder.Append($"@From = '{GetDate(from)}',");
            builder.Append($"@To = '{GetDate(to)}'");

            var data = await Context
                .ListenersStatisticsProcedureViews
                .FromSqlRaw(builder.ToString())
                .ToListAsync();
            var result = data.FirstOrDefault();
            return Result.SuccessIf(result != null, result, ErrorMessages.CouldNotRetrieveUsersStatistics);
        }

        private string GetDate(DateTime date)
        {
            return date.ToString("yyyy'-'MM'-'dd");
        }

        private bool? GetIsActive(IEnumerable<FilterModel> filter)
        {
            var isActiveFilter = filter.SelectMany(x => x.ValueFilters).FirstOrDefault(x => x.FieldName.Equals("isActive", StringComparison.OrdinalIgnoreCase));

            if (isActiveFilter != null && bool.TryParse(isActiveFilter.Value, out bool isActive))
            {
                if (isActiveFilter.Condition == ValueFilterCondition.Contains)
                {
                    return isActive;
                }
            }

            return null;
        }

        private bool? GetIsDeleted(IEnumerable<FilterModel> filter)
        {
            var isDeletedFilter = filter.SelectMany(x => x.ValueFilters).FirstOrDefault(x => x.FieldName.Equals("isDeleted", StringComparison.OrdinalIgnoreCase));

            if (isDeletedFilter != null && bool.TryParse(isDeletedFilter.Value, out bool isDeleted))
            {
                if (isDeletedFilter.Condition == ValueFilterCondition.Contains)
                {
                    return isDeleted;
                }
            }

            return null;
        }

        private DateTime? GetTimeRange(IEnumerable<FilterModel> filter)
        {
            var dateTimeFilters = filter.SelectMany(x => x.QuantityFilters).FirstOrDefault(x => x.FieldName.Equals("timeRange", StringComparison.OrdinalIgnoreCase));
            return dateTimeFilters != null ? Convert.ToInt64(dateTimeFilters?.Value).FromUnixTimestamp() : null;
        }

        public async Task<Result<Engagement>> GetEngagementStatics()
        {

            var birthDateMax = DateTime.UtcNow;
            var birthDateMin = DateTime.Parse("1800-01-01");

            var builder = new StringBuilder();
            builder.Append("[dbo].[Engagement]");
            builder.Append($"@BirthDateMax = '{GetDate(birthDateMin)}',");
            builder.Append($"@BirthDateMin = '{GetDate(birthDateMax)}',");
            builder.Append($"@Gender = '',");
            builder.Append($"@ZipCode = ''");

            var result = await Context
                .Engagement
                .FromSqlRaw(builder.ToString()).ToListAsync();

            return Result.Success(result.FirstOrDefault());

        }
    }
}
