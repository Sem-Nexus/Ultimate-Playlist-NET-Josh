#region Usings

using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UltimatePlaylist.Games.Const;
using UltimatePlaylist.Games.Interfaces;
using UltimatePlaylist.Games.Models.Lottery;

#endregion

namespace UltimatePlaylist.Games.Services
{
    public class LotteryService : ILotteryService
    {
        public Result<LotteryWinningNumbersReadServiceModel> GetLotteryWinningNumbers()
        {
            var result = new LotteryWinningNumbersReadServiceModel()
            {
                LotteryId = Guid.NewGuid(),
                DateTime = DateTime.UtcNow
            };

            List<int> numbers = new List<int>();
            while (numbers.Count <= 4)
            {
                int randomNumber = RandomNumberGenerator.GetInt32(1, LotteryRanges.OneToFiveNumbersRangeExclusive);

                if (!numbers.Contains(randomNumber))
                {
                    numbers.Add(randomNumber);
                }
                else
                {
                    while (numbers.Contains(randomNumber))
                    {
                        randomNumber = RandomNumberGenerator.GetInt32(1, LotteryRanges.OneToFiveNumbersRangeExclusive);
                    }
                    numbers.Add(randomNumber);
                }
            }

            result.FirstNumber = numbers[0];
            result.SecondNumber = numbers[1];
            result.ThirdNumber = numbers[2];
            result.FourthNumber = numbers[3];
            result.FifthNumber = numbers[4];
            result.SixthNumber = RandomNumberGenerator.GetInt32(1, LotteryRanges.SixthNumberRangeExclusive);

            return Result.Success(result);
        }
    }
}
