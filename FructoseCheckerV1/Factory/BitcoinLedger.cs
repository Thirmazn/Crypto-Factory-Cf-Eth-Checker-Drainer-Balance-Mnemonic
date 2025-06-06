﻿using System;
using System.Globalization;
using System.Threading.Tasks;

using FructoseCheckerV1.Models;

using Newtonsoft.Json.Linq;

using Amount = FructoseCheckerV1.Models.Pair<double, double>;

namespace FructoseCheckerV1.Factory
{

    class BitcoinLedger : WalletCheckerModelHttp
    {
        private static DateTime PriceUpdatedAt = DateTime.MinValue;
        private double LastPrice = 1.0d;
        public BitcoinLedger(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.BTC_LEDGER, SelfCheck)
        {
            Url = "https://blockstream.info/api/address/{0}";
            SelfCheckAddress = "1KFHE7w8BhaENAswwryaoccDb6qcT6DbYY";
            PriceUrl = "https://www.binance.com/api/v3/ticker/price?symbol=BTCUSDT";
            //https://blockstream.info/api/address/1KFHE7w8BhaENAswwryaoccDb6qcT6DbYY
            //tx_count
        }

        protected override async Task<Amount> DeserializeCoinResponce(Wallet Wallet)
        {
            try
            {
                JObject Object = await GetResponse(GetUrl(Wallet));
                double Balance = (Convert.ToDouble(Object.GetValue("chain_stats")["funded_txo_sum"], new CultureInfo("ru-RU")) - Convert.ToDouble(Object.GetValue("chain_stats")["spent_txo_sum"], new CultureInfo("ru-RU"))) / 100000000;
                return new Amount(Balance * await DeserializePriceResponce(), Balance);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override async Task<double> DeserializePriceResponce()
        {
            if (PriceUpdatedAt.AddMinutes(5) < DateTime.Now)
            {
                try
                {
                    JObject Object = await GetResponse(PriceUrl);
                    PriceUpdatedAt = DateTime.Now;
                    return LastPrice = Convert.ToDouble(Object.GetValue("price"), new CultureInfo("ru-RU"));
                }
                catch (Exception)
                {

                    throw;
                }
            }
            else
            {
                return LastPrice;
            }

        }
    }
}
