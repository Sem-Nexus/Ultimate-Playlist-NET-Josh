#region Usings

using System;
using System.Collections.Generic;
using UltimatePlaylist.Games.Models.Raffle;

#endregion

namespace UltimatePlaylist.Games.Models.Lottery
{
    public class LotteryWinnersReadServiceModel
    {
        public List<RaffleUserTicketReadServiceModel> LotteryWinnersReadServiceModels { get; set; }

        public IEnumerable<RaffleUserTicketReadServiceModel> RaffleUserTicketReadServiceModel { get; set; }

        public int Counter { get; set; }
    }
}
