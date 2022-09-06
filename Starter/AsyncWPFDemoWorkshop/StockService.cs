using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncWPFDemoWorkshop {
    public interface IStockService
    {
        Task<IEnumerable<StockPrice>> GetStockPricesForAsync(string stockIdentifier,
            CancellationToken cancellationToken);
    }

    public class StockService : IStockService
    {
        private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
        private int i = 0;

        public async Task<IEnumerable<StockPrice>> GetStockPricesForAsync(string stockIdentifier, CancellationToken cancellationToken) {
            // THIS IS TO SIMULATE API CALLS THAT TAKE LONGER
            // DO NOT DO THIS IN PRODUCTION!!!
            await Task.Delay((i++) * 1000);

            using (var client = new HttpClient()) {
                var result = await client.GetAsync($"{API_URL}/{stockIdentifier}", cancellationToken);

                result.EnsureSuccessStatusCode();

                var content = await result.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
            }
        }
        public IEnumerable<StockPrice> GetStockPricesFor(string stockIdentifier) {
            using (var client = new WebClient()) {
                var content = client.DownloadString($"{API_URL}/{stockIdentifier}");

                return JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);
            }
        }
    }
    public class MockStockService : IStockService {
        public Task<IEnumerable<StockPrice>> GetStockPricesForAsync(string stockIdentifier, CancellationToken cancellationToken) {
            var stocks = new List<StockPrice> {
                new StockPrice {
                    Identifier = "MSFT",
                    Change = 0.5m,
                    ChangePercent = 0.75m
                },
                new StockPrice {
                    Identifier = "GOOGL",
                    Change = 0.5m,
                    ChangePercent = 0.75m
                },
                new StockPrice {
                    Identifier = "GOOGL",
                    Change = 0.5m,
                    ChangePercent = 0.75m
                },
                new StockPrice {
                    Identifier = "GOOGL",
                    Change = 0.5m,
                    ChangePercent = 0.75m
                }
            };
            var task = Task.FromResult(stocks.Where(stock => stock.Identifier == stockIdentifier));
            return task;
        }
    }
}
