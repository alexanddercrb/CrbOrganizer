using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace MediaOrganizerV2
{
    public partial class MainWindow : Window
    {
        private string _sourceFolderPath = string.Empty;
        private string _destinationFolderPath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SourceFolderSelectClick(object sender, RoutedEventArgs args)
        {
            Task.Run(() => SelectSourceFolderAsync());
        }

        public async Task SelectSourceFolderAsync() {
            var dialog = Dispatcher.UIThread.InvokeAsync(() => this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()));
            var path = dialog.Result.FirstOrDefault()?.Path?.LocalPath?.ToString();
            Dispatcher.UIThread.Post(() => {
                sourcePath.Text = path;
                _sourceFolderPath = sourcePath.Text;
            });
        }

        public void DestinationFolderSelectClick(object sender, RoutedEventArgs args)
        {
            Task.Run(() => SelectDestinationFolderAsync());
        }

        public async Task SelectDestinationFolderAsync() {
            var dialog = Dispatcher.UIThread.InvokeAsync(() => this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()));
            var path = dialog.Result.FirstOrDefault()?.Path?.LocalPath?.ToString();
            Dispatcher.UIThread.Post(() => {
                destinationPath.Text = path;
                _destinationFolderPath = destinationPath.Text;
            });
        }

        public async void ExecuteButtonClick(object sender, RoutedEventArgs args)
        {
            logsText.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(_sourceFolderPath))
            {
                AppendLogs($"Error: Please select a source folder (with unsorted files) \n", false, 2);
                return;
            }
            if (string.IsNullOrWhiteSpace(_destinationFolderPath))
            {
                AppendLogs($"Error: Please select a destination folder (for unsorted files) \n", false, 2);
                return;
            }

            bool includeSubfoldersCheck = includeSubfolders.IsChecked.Value;

            if (includeSubfoldersCheck && destinationPath.Text.Contains(sourcePath.Text))
            {
                AppendLogs($"\nError: destination folder cannot be the same or a subfolder whn 'Include Subfolders' is checked", false, 2);
                return;
            }

            sourceFolderButton.IsEnabled = false;
            destinationFolderButton.IsEnabled = false;
            controlsGrid.IsEnabled = false;

            var categorizationType = CategorizationTypeEnum.ByDay;
            if (monthlyRadio.IsChecked.Value)
            {
                categorizationType = CategorizationTypeEnum.ByMonth;
            }
            else if (yearlyRadio.IsChecked.Value)
            {
                categorizationType = CategorizationTypeEnum.ByYear;
            }

            // run external class
            var execution = Task.Run(() => ProgramRunner.Runner(_sourceFolderPath, _destinationFolderPath, categorizationType, includeSubfoldersCheck, this));
        }

        public void AppendLogs(string message = "", bool bold = false, int color = 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Run run = new Run($"{message} \n");
                if (bold)
                {
                    run.FontWeight = Avalonia.Media.FontWeight.Bold;
                }

                if (color > 0)
                {
                    var foregroundColor = Colors.Black;

                    switch (color)
                    {
                        case 1:
                            foregroundColor = Colors.Green;
                            break;
                        case 2:
                            foregroundColor = Colors.Red;
                            break;
                        default:
                            foregroundColor = Colors.Yellow;
                            break;
                    }

                    run.Foreground = new SolidColorBrush(foregroundColor);
                }
                    

                logsText.Inlines.Add(run);
            });

        }

        public void Clear()
        {
            Dispatcher.UIThread.Post(() => logsText.Inlines?.Clear());
        }

        public void EnableControls()
        {
            Dispatcher.UIThread.Post(() => sourceFolderButton.IsEnabled = true);
            Dispatcher.UIThread.Post(() => destinationFolderButton.IsEnabled = true);
            Dispatcher.UIThread.Post(() => controlsGrid.IsEnabled = true);
        }
    }
}