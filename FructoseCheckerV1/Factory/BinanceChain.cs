using System;
using System.Globalization;
using System.Threading.Tasks;

using FructoseCheckerV1.Models;

using HtmlAgilityPack;

using Amount = FructoseCheckerV1.Models.Pair<double, double>;
using Tokens = System.Collections.Generic.List<FructoseCheckerV1.Models.TokenCheckResult>;

namespace FructoseCheckerV1.Factory
{
    public class BinanceChain : WalletChekerModelXpath
    {
        public BinanceChain(ref Python Python, bool SelfCheck = false)
            : base(ref Python, CoinType.BNB, SelfCheck)
        {
            Url = "https://explorer.bnbchain.org/address/{0}";
            SelfCheckAddress = "bnb1zmnv0hh0a8j4k59suel89nnmyfcfnytrzgqu56";
            XpathToken = "//tr[contains(@class,\"bnc-table-row bnc-table-row-level-0\")]";
            XpathTokenName = ".//a[contains(@href,\"/asset/\")]";
            XpathTokenPrice = ".//td[3]";
            XpathTokenBalance = ".//td[2]";
            //https://explorer.binance.org/txs?address=bnb1zmnv0hh0a8j4k59suel89nnmyfcfnytrzgqu56
            ////div[@class = "total"]/strong
        }

        protected override async Task<Amount> ParseCoinHtml(Wallet Wallet)
        {
            try
            {
                HtmlDocument Document = await GetHtml(GetUrl(Wallet));

                if (Document.Text.Contains("No Data") || Document.DocumentNode.SelectNodes(XpathToken) == null)
                {
                    return new(0.0, 0.0);
                }

                HtmlNode Node = Document.DocumentNode.SelectNodes(XpathToken)[0];

                string Name = Node.SelectSingleNode(XpathTokenName).InnerText;

                if (!Name.Contains("BNB"))
                {
                    return new(0.0, 0.0);
                }

                string PriceString = Node.SelectSingleNode(XpathTokenPrice).InnerText.Replace("\n", string.Empty).Replace("$", string.Empty).Replace(",", string.Empty).Replace(".", ",");
                double Price;
                double.TryParse(PriceString, NumberStyles.Currency, new CultureInfo("ru-RU"), out Price);

                string BalanceString = Node.SelectSingleNode(XpathTokenBalance).InnerText.Replace("\n", string.Empty).Replace(",", string.Empty).Replace(".", ",");
                double Balance;
                double.TryParse(BalanceString, NumberStyles.Currency, new CultureInfo("ru-RU"), out Balance);
                return new(Price, Balance);
            }
            catch (Exception)
            {
                throw;
            }
        }

        protected override async Task<Tokens> ParseTokenHtml(Wallet Wallet)
        {
            try
            {
                HtmlDocument Document = await GetHtml(GetUrl(Wallet));
                Tokens Tokens = new();

                if (Document.Text.Contains("No Data") || Document.DocumentNode.SelectNodes(XpathToken) == null)
                {
                    return new();
                }

                HtmlNodeCollection HtmlNodeCollection = Document.DocumentNode.SelectNodes(XpathToken); HtmlNodeCollection.RemoveAt(0);

                foreach (HtmlNode Node in HtmlNodeCollection)
                {
                    string Name = Node.SelectSingleNode(XpathTokenName).InnerText;

                    string PriceString = Node.SelectSingleNode(XpathTokenPrice).InnerText.Replace("\n", string.Empty).Replace("$", string.Empty).Replace(",", string.Empty).Replace(".", ",");
                    double Price;
                    double.TryParse(PriceString, NumberStyles.Currency, new CultureInfo("ru-RU"), out Price);

                    string BalanceString = Node.SelectSingleNode(XpathTokenBalance).InnerText.Replace("\n", string.Empty).Replace(",", string.Empty).Replace(".", ",");
                    double Balance;
                    double.TryParse(BalanceString, NumberStyles.Currency, new CultureInfo("ru-RU"), out Balance);

                    Tokens.Add(new TokenCheckResult(Name, Price, Balance, "null", TokenType.BNB));
                }

                return Tokens;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
