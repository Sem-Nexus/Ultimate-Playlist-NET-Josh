﻿#region Usings

using CSharpFunctionalExtensions;
using DocumentFormat.OpenXml.VariantTypes;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UltimatePlaylist.Common.Config;
using UltimatePlaylist.Common.Enums;
using UltimatePlaylist.Common.Mvc.Helpers;
using UltimatePlaylist.Database.Infrastructure.Entities.Games;
using UltimatePlaylist.Database.Infrastructure.Repositories.Interfaces;
using UltimatePlaylist.Games.Interfaces;
using UltimatePlaylist.Games.Models.Raffle;
using UltimatePlaylist.Services.Common.Interfaces.Games;
using UltimatePlaylist.Services.Common.Interfaces.Ticket;
using System;
using System.Linq;
using StackExchange.Redis;
using UltimatePlaylist.Games.Models.Lottery;
using UltimatePlaylist.Database.Infrastructure.Entities.Ticket;
using OfficeOpenXml;
using Microsoft.Graph;
using System.Net.Mail;
using System.Net.Mime;
using DocumentFormat.OpenXml.Wordprocessing;
using UltimatePlaylist.Services.Common.Models.Games;
using DocumentFormat.OpenXml.Drawing;
using Azure.Core;
using System.Web;
using UltimatePlaylist.Services.Common.Models.Email;
using UltimatePlaylist.Services.Common.Interfaces;
using OfficeOpenXml.Style;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UltimatePlaylist.Database.Infrastructure.Views;
using UltimatePlaylist.Database.Infrastructure.Entities.Identity;

#endregion

namespace UltimatePlaylist.Services.Games.Jobs
{
    public class DailyCashTicketsServiceAndRaffleJob
    {
        private const int Selections = 18;

        #region Private members

        private readonly Lazy<IDailyCashTicketsService> DailyCashTicketsServiceProvider;

        private readonly Lazy<IRaffleService> RaffleServiceProvider;

        private readonly Lazy<IRepository<DailyCashDrawingEntity>> DailyCashDrawingRepositoryProvider;

        private readonly Lazy<IWinningsService> WinningsServiceProvider;

        private readonly Lazy<IWinningsInfoService> WinningsInfoServiceProvider;

        private readonly Lazy<ILogger<DailyCashGameJob>> LoggerProvider;
        
        private readonly Lazy<ITicketProcedureRepository> TicketProcedureRepositoryProvider;

        private readonly IEmailService EmailService;

        private readonly PlaylistConfig PlaylistConfig;

        #endregion

        #region Constructor(s)

        public DailyCashTicketsServiceAndRaffleJob(
            Lazy<IDailyCashTicketsService> dailyCashTicketsServiceProvider,
            Lazy<IRaffleService> raffleServiceProvider, 
            Lazy<IRepository<DailyCashDrawingEntity>> dailyCashDrawingRepositoryProvider,
            Lazy<IWinningsService> winningsServiceProvider,
            Lazy<ILogger<DailyCashGameJob>> loggerProvider,            
            Lazy<ITicketProcedureRepository> ticketProcedureRepositoryProvider,
            Lazy<IWinningsInfoService> winningsInfoServiceProvider,
            IEmailService emailService,
            IOptions<PlaylistConfig> playlistConfig)
        {
            DailyCashTicketsServiceProvider = dailyCashTicketsServiceProvider;
            RaffleServiceProvider = raffleServiceProvider;
            DailyCashDrawingRepositoryProvider = dailyCashDrawingRepositoryProvider;
            WinningsServiceProvider = winningsServiceProvider;
            LoggerProvider = loggerProvider;            
            TicketProcedureRepositoryProvider = ticketProcedureRepositoryProvider;
            PlaylistConfig = playlistConfig.Value;
            WinningsInfoServiceProvider = winningsInfoServiceProvider;
            EmailService = emailService;
        }
        #endregion

        #region Properties

        private IDailyCashTicketsService DailyCashTicketsService => DailyCashTicketsServiceProvider.Value;

        private IRaffleService RaffleService => RaffleServiceProvider.Value;

        private IRepository<DailyCashDrawingEntity> DailyCashDrawingRepository => DailyCashDrawingRepositoryProvider.Value;

        private IWinningsService WinningsService => WinningsServiceProvider.Value;

        private ILogger<DailyCashGameJob> Logger => LoggerProvider.Value;

        private ITicketProcedureRepository TicketProcedureRepository => TicketProcedureRepositoryProvider.Value;

        private IWinningsInfoService WinningsInfoService => WinningsInfoServiceProvider.Value;

        protected IEmailService EmailServices => EmailService;

        public static DailyCashDrawingEntity Game;

        #endregion

        #region Public methods

        public async Task<object> RunDailyCashGame()
        {
            var result = new List<LotteryWinnersReadServiceModel>();            
            Object[] obj = {
                    new {first = 0 , last = 0},
                    new {first = 0 , last = 0}
             };
            try
            {
                result = await GetTicketsAndWinners();
                if (result[1].Counter > 0)
                {
                    var jobId = BackgroundJob.Schedule(() => CreateGame(), TimeSpan.FromMinutes(2));
                    BackgroundJob.ContinueJobWith(jobId, () => AddWinnersAndFilterTickets(result[0]), JobContinuationOptions.OnlyOnSucceededState);                    
                    BackgroundJob.ContinueJobWith(jobId, () => AddAlternateWinners(result[1]), JobContinuationOptions.OnlyOnSucceededState);                    
                    obj[0] = new { first = result[0].LotteryWinnersReadServiceModels.Last().Id, last = result[0].LotteryWinnersReadServiceModels.First().Id };
                    obj[1] = new { first = result[1].LotteryWinnersReadServiceModels.Last().Id, last = result[1].LotteryWinnersReadServiceModels.First().Id };
                    return obj;
                } else
                {
                    return "No Winners";
                }

            }
            catch (Exception ex)
            {
                await EmailServices.SendEmailCrashJob("GetTicketsAndWinners " + ex?.Message);
                return ex;
            }
        }

        public async Task<List<LotteryWinnersReadServiceModel>> GetTicketsAndWinners()
        {
            List<LotteryWinnersReadServiceModel> lotteryWinnersList = new List<LotteryWinnersReadServiceModel>();
            List<RaffleUserTicketReadServiceModel> dailyCashTickets = default;

            try
            {
                var tickets = await DailyCashTicketsService.GetTicketsForDailyCashAsyncSP();
                int getWinnersLists = 0;
                do
                {                    
                    if (tickets.Value.Any())
                    {
                        dailyCashTickets = tickets.Value;
                        var winners = RaffleService.GetRaffleWinners(dailyCashTickets, Selections).Value;

                        if (!winners.Any())
                        {
                            Logger.LogError($"Error getting raffle winners");
                            throw new Exception("Error in GetRaffleWinners");
                        }

                        var response = new LotteryWinnersReadServiceModel();
                        response.RaffleUserTicketReadServiceModel = winners;
                        response.LotteryWinnersReadServiceModels = dailyCashTickets;
                        response.Counter = winners.Count();

                        lotteryWinnersList.Add(response);

                        getWinnersLists++;                       
                    }
                    else
                    {
                        throw new Exception(ErrorType.NoTicketsForGame.ToString());
                    }                    
                } while ((getWinnersLists < 2));

                return lotteryWinnersList;
            }
            catch (Exception ex)
            {
                await EmailServices.SendEmailCrashJob("GetTicketsAndWinners " + ex?.Message);                
                return null;
            }


        }

        public async Task<long> CreateGame()
        {
            DailyCashDrawingEntity game = default;

            var todayDate = DateTimeHelper.ToTodayUTCTimeForTimeZoneRelativeTime("US Eastern Standard Time");
            TimeSpan dateOffSet = TimeSpan.Parse("0:00:00");
            var currentDate = todayDate.Add(PlaylistConfig.StartDateOffSet);

            try
            {
                var result = await Result.Success()
                .Tap(async () => game = await DailyCashDrawingRepository.AddAsync(new DailyCashDrawingEntity()
                {
                    GameDate = currentDate,
                }));
            }
            catch (Exception ex)
            {
                await EmailServices.SendEmailCrashJob("CreateGame " + ex?.Message);
                throw new Exception("Error in CreateGame");
            }
            Game = game;
            return Game.Id;

        }

        public Task AddWinnersAndFilterTickets(LotteryWinnersReadServiceModel result)
        {
            var jobId = BackgroundJob.Schedule(() => AddWinnersForDailyCashAsync(result), TimeSpan.FromMinutes(2));            
            BackgroundJob.ContinueJobWith(jobId, () => GetFilteredTickets(result), JobContinuationOptions.OnlyOnSucceededState);
            return Task.CompletedTask;
        }

        public async Task<long> AddWinnersForDailyCashAsync(LotteryWinnersReadServiceModel result)
        {
                
            if (Game != null)
            {
                try
                {
                    await WinningsService.AddWinnersForDailyCashAsync(result.RaffleUserTicketReadServiceModel.Select(i => i.UserExternalId).ToList(), Game.Id);
                }
                catch (Exception ex)
                {
                    await EmailServices.SendEmailCrashJob("AddWinnersForDailyCashAsync " + ex?.Message);
                    throw new Exception("Error in AddWinnersForDailyCashAsync");
                }
            }

            return Game.Id;
        }


        public async Task<object> GetFilteredTickets(LotteryWinnersReadServiceModel result)
        {

            List<TicketEntity> ticketList = await TicketProcedureRepository.GetDailyTicketsForRaffle();
            var obj = new { first = ticketList.First().Id, last = ticketList.Last().Id, count = ticketList.Count() };

            var job = BackgroundJob.Schedule(() => TicketProcedureRepository.MarkDailyTicketsAsUsed(obj.first, obj.last), TimeSpan.FromMinutes(2));
            BackgroundJob.ContinueJobWith(job, () => CompleteGame(result), JobContinuationOptions.OnlyOnSucceededState);

            return obj;
        }
        #endregion

        public async Task<long> CompleteGame(LotteryWinnersReadServiceModel result)
        {
            try
            {
               // await GamesWinningCollectionService.RemoveArray(result.LotteryWinnersReadServiceModels.Select(t => t.UserExternalId));
                Game.IsFinished = true;
                await DailyCashDrawingRepository.UpdateAndSaveAsync(Game);
            }
            catch (Exception ex)
            {
                await EmailServices.SendEmailCrashJob("CompleteGame " + ex?.Message);
                throw new Exception("Error in CompleteGame");
            }
           

            return Game.Id;
        }

        public async Task RemoveInternalUsersTickets()
        {        
            await TicketProcedureRepository.RemoveInternalUserTickets();
            return;
        }

        public async Task SendEmailWithWinners()
        {
            await TicketProcedureRepository.RemoveInternalUserTickets();
            return;
        }

        public async Task<List<WinnersInformationEntity>> CreateExcelAndSendEmail()
        {            
           
            List<WinnersInformationEntity> winners = await TicketProcedureRepository.GetWinnersData();
            List<WinnersAlternateInformationEntity> winnersAlternate = await TicketProcedureRepository.GetWinnersAlternateData();

            if (winners.Any())
            {
                DateTime endDay = DateTime.Now.AddDays(-1);
                using (var package = new ExcelPackage())
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Winners");                   

                    worksheet.Cells[1, 1].Value = "Prize Tier";
                    worksheet.Cells[1, 2].Value = "Player Id";
                    worksheet.Cells[1, 3].Value = "User Name";
                    worksheet.Cells[1, 4].Value = "First Name";
                    worksheet.Cells[1, 5].Value = "Last Name";
                    worksheet.Cells[1, 6].Value = "Email";
                    worksheet.Cells[1, 7].Value = "ZipCode";
                    worksheet.Cells[1, 8].Value = "Phone Number";
                    worksheet.Cells[1, 9].Value = "Gender";
                    worksheet.Cells[1, 10].Value = "Game Id";
                    worksheet.Cells[1, 11].Value = "Win Date";
                    worksheet.Cells[1, 12].Value = "BirthDate";
                    worksheet.Cells[1, 13].Value = "Wins Count";
                    worksheet.Cells[1, 14].Value = "Total Wins Amount";
                    worksheet.Cells[1, 15].Value = "Register Date";

                    for (int column = 1; column <= 15; column++)
                    {
                        using (var range = worksheet.Cells[1, column, worksheet.Dimension.End.Row, column])
                        {
                            range.Style.Font.Bold = true;
                            range.AutoFitColumns();
                        }
                    }

                    for (int i = 0; i < winners.Count; i++)
                    {
                        WinnersInformationEntity data = winners[i];
                        worksheet.Cells[i + 2, 1].Value = data.PrizeTier;
                        worksheet.Cells[i + 2, 2].Value = data.PlayerId;
                        worksheet.Cells[i + 2, 3].Value = data.UserName;
                        worksheet.Cells[i + 2, 4].Value = data.FirstName;
                        worksheet.Cells[i + 2, 5].Value = data.LastName;
                        worksheet.Cells[i + 2, 6].Value = data.Email;
                        worksheet.Cells[i + 2, 7].Value = data.ZipCode;
                        worksheet.Cells[i + 2, 8].Value = data.PhoneNumber;
                        worksheet.Cells[i + 2, 9].Value = data.Gender;
                        worksheet.Cells[i + 2, 10].Value = data.GameId;
                        worksheet.Cells[i + 2, 11].Value = endDay.ToString("yyyy-MM-dd");
                        worksheet.Cells[i + 2, 12].Value = DateTime.Parse(data.BirthDate.ToString()).ToString("yyyy-MM-dd");
                        worksheet.Cells[i + 2, 13].Value = data.WinsCount;
                        worksheet.Cells[i + 2, 14].Value = data.TotalWinsAmount;
                        worksheet.Cells[i + 2, 15].Value = DateTime.Parse(data.RegisterDate.ToString()).ToString("yyyy-MM-dd");
                        worksheet.Cells.AutoFitColumns();
                        worksheet.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    if (winnersAlternate.Any())
                    {
                        #region Worksheet2
                        ExcelWorksheet worksheet2 = package.Workbook.Worksheets.Add("WinnersAlternate");

                        worksheet2.Cells[1, 1].Value = "Prize Tier";
                        worksheet2.Cells[1, 2].Value = "Player Id";
                        worksheet2.Cells[1, 3].Value = "User Name";
                        worksheet2.Cells[1, 4].Value = "First Name";
                        worksheet2.Cells[1, 5].Value = "Last Name";
                        worksheet2.Cells[1, 6].Value = "Email";
                        worksheet2.Cells[1, 7].Value = "ZipCode";
                        worksheet2.Cells[1, 8].Value = "Phone Number";
                        worksheet2.Cells[1, 9].Value = "Gender";
                        worksheet2.Cells[1, 10].Value = "Game Id";
                        worksheet2.Cells[1, 11].Value = "Win Date";
                        worksheet2.Cells[1, 12].Value = "BirthDate";
                        worksheet2.Cells[1, 13].Value = "Wins Count";
                        worksheet2.Cells[1, 14].Value = "Total Wins Amount";
                        worksheet2.Cells[1, 15].Value = "Register Date";

                        for (int column = 1; column <= 15; column++)
                        {
                            using (var range = worksheet2.Cells[1, column, worksheet2.Dimension.End.Row, column])
                            {
                                range.Style.Font.Bold = true;
                                range.AutoFitColumns();
                            }
                        }

                        for (int i = 0; i < winnersAlternate.Count; i++)
                        {
                            WinnersAlternateInformationEntity data = winnersAlternate[i];
                            worksheet2.Cells[i + 2, 1].Value = data.PrizeTier;
                            worksheet2.Cells[i + 2, 2].Value = data.PlayerId;
                            worksheet2.Cells[i + 2, 3].Value = data.UserName;
                            worksheet2.Cells[i + 2, 4].Value = data.FirstName;
                            worksheet2.Cells[i + 2, 5].Value = data.LastName;
                            worksheet2.Cells[i + 2, 6].Value = data.Email;
                            worksheet2.Cells[i + 2, 7].Value = data.ZipCode;
                            worksheet2.Cells[i + 2, 8].Value = data.PhoneNumber;
                            worksheet2.Cells[i + 2, 9].Value = data.Gender;
                            worksheet2.Cells[i + 2, 10].Value = data.GameId;
                            worksheet2.Cells[i + 2, 11].Value = endDay.ToString("yyyy-MM-dd");
                            worksheet2.Cells[i + 2, 12].Value = DateTime.Parse(data.BirthDate.ToString()).ToString("yyyy-MM-dd");
                            worksheet2.Cells[i + 2, 13].Value = data.WinsCount;
                            worksheet2.Cells[i + 2, 14].Value = data.TotalWinsAmount;
                            worksheet2.Cells[i + 2, 15].Value = DateTime.Parse(data.RegisterDate.ToString()).ToString("yyyy-MM-dd");
                            worksheet2.Cells.AutoFitColumns();
                            worksheet2.Cells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }
                        #endregion

                    }

                    byte[] excelBytes = package.GetAsByteArray();
                    var excelBase64 = Convert.ToBase64String(excelBytes);

                    await FillExcelUserReport();
                    await EmailServices.SendEmailWithExcelAttachment("", "Winners Results", excelBase64);
                }

            }

            return winners;
        }

        public async Task<int> FillExcelUserReport()
        {
            int lastRow = 0;            
            string date = DateTime.Now.AddDays(-1).ToString("M/dd/yyyy");
            try
            {

                UserCountView totalUsers = await TicketProcedureRepository.GetTotalUsers();
                List<DailyUsersView> dailyUsers = await TicketProcedureRepository.GetDailyUsersAdded();
                List<DeactivatedUsers> dailyDeactivateUsers = await TicketProcedureRepository.GetDeactivateUsersAdded();
                var credential = GoogleCredential.FromFile((@"C:\home\site\wwwroot\ultimate-play-list-sn-b736279e45c3.json"));

                var service = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "ultimate-play-list-sn"
                });

                await AddUserStatsRow(service, lastRow, totalUsers, date);
                AddUsersRow(service, dailyUsers);
                await AddUsersWithTickers(service);
                await AddDeactivateUsersRow(service, dailyDeactivateUsers);

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }

            return lastRow;
        }

        public int GetPreviousValues(SheetsService service, string range, string spreadsheetId)
        {
            SpreadsheetsResource.ValuesResource.GetRequest request =
            service.Spreadsheets.Values.Get(spreadsheetId, range);
            ValueRange response = request.Execute();
            int newUsers = 0;
            
            IList<IList<object>> values = response.Values;

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    newUsers = Int32.Parse(row[3].ToString());
                }
            }

            return newUsers;
        }

        public async Task<int>AddUserStatsRow (SheetsService service, int lastRow, UserCountView totalUsers, string date)
        {
            try
            {
                string spreadsheetId = PlaylistConfig.ExcelSheetId;
                var sheetName = "User Stats";

                var range = $"{sheetName}!A1:A";
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

                var response = request.Execute();
                var values = response.Values;

                lastRow = values?.Count ?? 0;
                lastRow++;
                
                var dailyTickets = await TicketProcedureRepository.GetDailyTicketsForRaffleCount();

                var percentage = (totalUsers.ActiveUsers / totalUsers.UserCount) * 100;
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object> { date, totalUsers.Registrations, totalUsers.ActiveUsers, totalUsers.UserCount, null, totalUsers.Android, totalUsers.IOS, dailyTickets }
                    }
                };

                var valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"{sheetName}!A{lastRow}:Z{lastRow}");
                updateRequest.ValueInputOption = valueInputOption;

                var updateResponse = updateRequest.Execute();

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message.ToString());
            }
         
            return lastRow;
        }

        public void AddUsersRow(SheetsService service, List<DailyUsersView> totalUsers)
        {
            try
            {
                string spreadsheetId = PlaylistConfig.ExcelSheetId;
                var sheetName = "All Users";

                var range = $"{sheetName}!A2";

                var rows = new List<IList<object>>();
                foreach (var user in totalUsers)
                {
                    var row = new List<object>
                    {
                        user.Id,
                        user.Email,
                        user.UserName,
                        user.Name,
                        user.LastName,
                        user.PhoneNumber,
                        user.ZipCode,
                        user.EmailConfirmed,
                        user.IsActive,
                        user.IsDeleted,
                        user.BirthDate,
                        user.LastActive,
                        user.Updated,
                        user.Created,
                        user.Device
                    };

                    rows.Add(row);
                }

                var valueRange = new ValueRange
                {
                    Values = rows
                };

                var valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                valueRange.Range = "All Users!A2";
                var appendRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                appendRequest.ValueInputOption = valueInputOption;

                var appendResponse = appendRequest.Execute();

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message.ToString());
            }
        }

        public async Task<ActiveUsers> AddUsersWithTickers(SheetsService service)
        {
            try
            {
                string spreadsheetId = PlaylistConfig.ExcelSheetId;
                var sheetName = "User Stats";

                var range = $"{sheetName}!H2:N2";

                var totalUsers = await TicketProcedureRepository.GetActiveUserTokensCount();

                int sevenDaysAgo = totalUsers.Value.TotalActiveUsers7;
                int thirtyDaysAgo = totalUsers.Value.TotalActiveUsers30;
                int uniqueSevenDaysAgo = totalUsers.Value.TotalUniqueActiveUsers7;
                int uniqueThirtyDaysAgo = totalUsers.Value.TotalUniqueActiveUsers30;
                int uniqueDeactivate = totalUsers.Value.TotalUniqueDeactiveUsers;
                int uniqueDeactivateSevenDaysAgo = totalUsers.Value.TotalUniqueDeactiveUsers7;
                int uniqueDeactivateThirtyDaysAgo = totalUsers.Value.TotalUniqueDeactiveUsers30;

                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object> { sevenDaysAgo, thirtyDaysAgo, uniqueSevenDaysAgo, uniqueThirtyDaysAgo, uniqueDeactivate, uniqueDeactivateSevenDaysAgo, uniqueDeactivateThirtyDaysAgo }
                    }
                };


                var valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);

                updateRequest.ValueInputOption = valueInputOption;

                var appendResponse = updateRequest.Execute();

                return totalUsers.Value;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message.ToString());
            }
        }

        public async Task<long> AddAlternateWinners(LotteryWinnersReadServiceModel result)
        {

            if (Game != null)
            {
                try
                {
                    await WinningsService.AddAlternateWinnersForDailyCashAsync(result.RaffleUserTicketReadServiceModel.Select(i => i.UserExternalId).ToList(), Game.Id);
                }
                catch (Exception ex)
                {
                    await EmailServices.SendEmailCrashJob("AddAlternateWinnersForDailyCashAsync " + ex?.Message);
                    throw new Exception("Error in AddAlternateWinnersForDailyCashAsync");
                }
            } 
            return Game.Id;
        }

        public async Task AddDeactivateUsersRow(SheetsService service, List<DeactivatedUsers> totalUsers)
        {
            try
            {
                string spreadsheetId = PlaylistConfig.ExcelSheetId;
                var sheetName = "Deactivated Users";

                var range = $"{sheetName}!A2";

                var rows = new List<IList<object>>();
                foreach (var user in totalUsers)
                {
                    var row = new List<object>
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.PhoneNumber,
                        user.Name,
                        user.LastName,
                        user.GenderId,
                        user.ZipCode,
                        DateTime.Parse(user.LastActive.ToString()).ToString("yyyy-MM-dd"),
                        DateTime.Parse(user.Created.ToString()).ToString("yyyy-MM-dd"),
                        DateTime.Parse(user.Updated.ToString()).ToString("yyyy-MM-dd"),
                        user.Device
                    };

                    rows.Add(row);
                }

                var valueRange = new ValueRange
                {
                    Values = rows
                };

                var valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                valueRange.Range = "Deactivated Users!A2";
                var appendRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
                appendRequest.ValueInputOption = valueInputOption;

                var appendResponse = appendRequest.Execute();

            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message.ToString());
            }
        }

        public async Task BackupTicketsAndUpdateLeaderboard()
        {
            await TicketProcedureRepository.BackupTicketsAndUpdateLeaderboard();
            return;
        }
    }
}
