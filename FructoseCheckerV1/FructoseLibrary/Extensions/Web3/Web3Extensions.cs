using Nethereum.Contracts.Constants;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Web3;

namespace FructoseLibrary.Extensions.Web3
{
    public class TokenTotalSupply
    {
        public string ContractAddress { get; set; }
        public BigInteger TotalSupply { get; set; }
    }

    public class TokenDecimals
    {
        public string ContractAddress { get; set; }
        public byte Decimals { get; set; }
    }

    public static  class Web3Extensions
    {
        public static async Task<List<TokenDecimals>> GetAllTokenDecimalsUsingMultiCallAsync(
            this Nethereum.Web3.Web3 Web3,
            IEnumerable<string> ContractAddresses, BlockParameter Block,
            int NumberOfCallsPerRequest = MultiQueryHandler.DEFAULT_CALLS_PER_REQUEST,
            string MultiCallAddress = CommonAddresses.MULTICALL_ADDRESS)
        {
            try
            {
                var TotalSupplyCalls = new List<MulticallInputOutput<DecimalsFunction, DecimalsOutputDTO>>();

                foreach (var ContractAddress in ContractAddresses)
                {
                    var BalanceCall = new DecimalsFunction() { };
                    TotalSupplyCalls.Add(new MulticallInputOutput<DecimalsFunction, DecimalsOutputDTO>(BalanceCall,
                        ContractAddress));
                }

                var MultiqueryHandler = Web3.Eth.GetMultiQueryHandler(MultiCallAddress);
                var Results = await MultiqueryHandler.MultiCallAsync(NumberOfCallsPerRequest, TotalSupplyCalls.ToArray()).ConfigureAwait(false);

                return TotalSupplyCalls.Select(Call => new TokenDecimals()
                {
                    Decimals = Call.Output.Decimals,
                    ContractAddress = Call.Target,
                }).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<List<TokenTotalSupply>> GetAllTokenSupplyUsingMultiCallAsync(
            this Nethereum.Web3.Web3 Web3,
            IEnumerable<string> ContractAddresses, BlockParameter Block,
            int NumberOfCallsPerRequest = MultiQueryHandler.DEFAULT_CALLS_PER_REQUEST,
            string MultiCallAddress = CommonAddresses.MULTICALL_ADDRESS)
        {
            try
            {
                var TotalSupplyCalls = new List<MulticallInputOutput<TotalSupplyFunction, TotalSupplyOutputDTO>>();

                foreach (var ContractAddress in ContractAddresses)
                {
                    var BalanceCall = new TotalSupplyFunction() { };
                    TotalSupplyCalls.Add(new MulticallInputOutput<TotalSupplyFunction, TotalSupplyOutputDTO>(BalanceCall,
                        ContractAddress));
                }

                var MultiqueryHandler = Web3.Eth.GetMultiQueryHandler(MultiCallAddress);
                var Results = await MultiqueryHandler.MultiCallAsync(NumberOfCallsPerRequest, TotalSupplyCalls.ToArray()).ConfigureAwait(false);

                return TotalSupplyCalls.Select(Call => new TokenTotalSupply()
                {
                    TotalSupply = Call.Output.TotalSupply,
                    ContractAddress = Call.Target,
                }).ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
