using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Navigation;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System;

namespace AsyncWPFDemoWorkshop {
    public partial class MainWindow : Window {
        private Stopwatch _stopwatch = new();

        public MainWindow() {
            InitializeComponent();
        }

        CancellationTokenSource? cancellationTokenSource;
        private async void Search_ClickAsync(object sender, RoutedEventArgs e) {
            if (cancellationTokenSource != null) {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;
                Search.Content = "Search";
                return;
            }
            try {
                cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.Token.Register(() => {
                    Notes.Text = "Cancellation requested";
                });
                Search.Content = "Cancel";
                BeforeLoadingStockData();
                var service = new StockService();
                var identifiers = StockIdentifier.Text.Split(',', ' ');

                //GetStocksSyncronously(service, identifiers);

                //await GetStocksAsync(service, identifiers);

                //GetStocksConcurrently(service, identifiers);

                //await GetStocksConcurrentlyAsync(service, identifiers);

                await GetStocksConcurrentlyThreadedAsync(service, identifiers);
            }
            catch (Exception ex) {
                Notes.Text = ex.Message;
            }
            finally {
                AfterLoadingStockData();
            }
        }

        private void GetStocksSyncronously(StockService service, string[] identifiers) {
            // THIS LOCKS UP THE UI WHILE THE DATA IS LOADED
            var stocks = new List<StockPrice>();
            foreach (var identifier in identifiers) {
                var data = service.GetStockPricesFor(identifier);
                stocks.AddRange(data);
            }
            Stocks.ItemsSource = stocks.ToArray();
        }
        private async Task GetStocksAsync(StockService service, string[] identifiers) {
            // THIS DOES NOT LOCK UP THE UI
            var stocks = new List<StockPrice>();
            foreach (var identifier in identifiers) {
                var response = await service.GetStockPricesForAsync(identifier, cancellationTokenSource.Token);
                stocks.AddRange(response);
                Debug.WriteLine($"Finished {identifier}");
            }
            Stocks.ItemsSource = stocks.ToArray();
        }
        private void GetStocksConcurrently(StockService service, string[] identifiers) {
            // THIS IS WHAT HAPPENS WHEN YOU DON'T AWAIT A CALL
            var stocks = new ConcurrentBag<StockPrice>();

            foreach (var identifier in identifiers) {

                var loadTask = service.GetStockPricesForAsync(identifier, cancellationTokenSource.Token);

                loadTask = loadTask.ContinueWith(t => {
                    var aFewStocks = t.Result;

                    foreach (var stock in aFewStocks) {
                        stocks.Add(stock);
                    }

                    // UPDATING THE UI AS TASKS ARE COMPLETED
                    Dispatcher.Invoke(() => {
                        Stocks.ItemsSource = stocks.ToArray();
                    });

                    return aFewStocks;
                });
                Debug.WriteLine($"Finished {identifier}");
            }
        }
        private async Task GetStocksConcurrentlyAsync(StockService service, string[] identifiers) {
            var stocks = new ConcurrentBag<StockPrice>();

            foreach (var identifier in identifiers) {

                var loadTask = service.GetStockPricesForAsync(identifier, cancellationTokenSource.Token);

                await (loadTask = loadTask.ContinueWith(t => {
                    var aFewStocks = t.Result;

                    foreach (var stock in aFewStocks) {
                        stocks.Add(stock);
                    }

                    // UPDATING THE UI AS TASKS ARE COMPLETED
                    Dispatcher.Invoke(() => {
                        Stocks.ItemsSource = stocks.ToArray();
                    });

                    return aFewStocks;
                }));
                Debug.WriteLine($"Finished {identifier}");
            }
        }
        private async Task GetStocksConcurrentlyThreadedAsync(StockService service, string[] identifiers) {
            var stocks = new ConcurrentBag<StockPrice>();
            await Parallel.ForEachAsync(identifiers, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (identifier, _) => {

                var loadTask = service.GetStockPricesForAsync(identifier, cancellationTokenSource.Token);

                await (loadTask = loadTask.ContinueWith(t => {
                    var aFewStocks = t.Result;

                    foreach (var stock in aFewStocks) {
                        stocks.Add(stock);
                    }

                    // UPDATING THE UI AS TASKS ARE COMPLETED
                    Dispatcher.Invoke(() => {
                        Stocks.ItemsSource = stocks.ToArray();
                    });

                    return aFewStocks;
                }));
                Debug.WriteLine($"Finished {identifier}");
            });
        }

        private void BeforeLoadingStockData() {
            _stopwatch.Restart();
            StockProgress.Visibility = Visibility.Visible;
            StockProgress.IsIndeterminate = true;
        }

        private void AfterLoadingStockData() {
            StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {_stopwatch.ElapsedMilliseconds}ms";
            StockProgress.Visibility = Visibility.Hidden;
            cancellationTokenSource = null;
            Search.Content = "Search";
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));

            e.Handled = true;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }
    }
}