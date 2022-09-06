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
        private void Search_Click(object sender, RoutedEventArgs e) {
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

                GetStocksSyncronously(service, identifiers);
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