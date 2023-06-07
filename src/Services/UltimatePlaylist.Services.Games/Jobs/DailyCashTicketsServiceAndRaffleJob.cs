#region Usings

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

        public static DailyCashDrawingEntity Game;

        private IWinningsInfoService WinningsInfoService => WinningsInfoServiceProvider.Value;

        protected IEmailService EmailServices => EmailService;

        #endregion

        #region Public methods

        public async Task<object> RunDailyCashGame()
        {
            var result = new LotteryWinnersReadServiceModel();
            var obj = new {first = 0 , last = 0};
            try
            {
                result = await GetTicketsAndWinners();
                if (result.Counter > 0 )
                {
                    var jobId = BackgroundJob.Schedule(() => CreateGame(), TimeSpan.FromMinutes(2));
                    BackgroundJob.ContinueJobWith(jobId, () => AddWinnersAndFilterTickets(result), JobContinuationOptions.OnlyOnSucceededState);
                    obj = new { first = result.LotteryWinnersReadServiceModels.Last().Id, last = result.LotteryWinnersReadServiceModels.First().Id };
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

        public async Task<LotteryWinnersReadServiceModel> GetTicketsAndWinners()
        {
            List<RaffleUserTicketReadServiceModel> dailyCashTickets = default;

            try
            {
                var tickets = await DailyCashTicketsService.GetTicketsForDailyCashAsync();

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

                    return response;
                }
                else
                {
                    throw new Exception("Error in GetTicketsAndWinners");
                }
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
            int lastRow;            
            string date = DateTime.Now.ToString("M/dd/yyyy");
            try
            {

                UserCountView totalUsers = await TicketProcedureRepository.GetTotalUsers();               
                var credential = GoogleCredential.FromFile((@"C:\home\site\wwwroot\ultimate-play-list-sn-b736279e45c3.json"));

                var service = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "ultimate-play-list-sn"
                });

                string spreadsheetId = PlaylistConfig.ExcelSheetId;
                var sheetName = "Sheet1";

                var range = $"{sheetName}!A1:A";
                var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

                var response = request.Execute();
                var values = response.Values;

                lastRow = values?.Count ?? 0;
                int getPreviousValues = GetPreviousValues(service, $"Sheet2!A{lastRow}:Z{lastRow}", spreadsheetId);
                lastRow++;

                int newUsers = totalUsers.UserCount - getPreviousValues;
                int percentage = totalUsers.ActiveUsers / totalUsers.UserCount;
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>>
                    {
                        new List<object> { date, newUsers, totalUsers.ActiveUsers, totalUsers.UserCount, percentage }
                    }
                };

                var valueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                var updateRequest = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, $"Sheet2!A{lastRow}:Z{lastRow}");
                updateRequest.ValueInputOption = valueInputOption;
                
                var updateResponse = updateRequest.Execute();

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
    }
}
