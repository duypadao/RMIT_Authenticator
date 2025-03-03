using RMIT_Authenticator.Models;
using RMIT_Authenticator.Services;
using RMIT_Authenticator.Utilities;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Timer = System.Timers.Timer;
using System.Windows.Input;

namespace RMIT_Authenticator
{
    public partial class MainWindow : Window
    {
        private List<TotpItem> totpItems = new List<TotpItem>();
        private readonly DataService dataService = new DataService();
        private readonly QrCodeService qrCodeService = new QrCodeService();
        private readonly TotpService totpService;


        public MainWindow()
        {
            InitializeComponent();
            totpService = new TotpService(Dispatcher);

            LoadInitialData();
            SetupTimer();
        }
        private void TextBox_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //if (sender is TextBox textBox)
            //{
            //    textBox.SelectAll();
            //    textBox.Focus(); // Đảm bảo TextBlock nhận focus để Ctrl+C hoạt động
            //}
        }
        private void LoadInitialData()
        {
            try
            {
                totpItems = dataService.LoadData();
                TotpList.ItemsSource = totpItems;
            }
            catch (Exception ex)
            {
                UiHelper.ShowError("Failed to load initial data", ex);
            }
        }

        private void SetupTimer()
        {
            var timer = new Timer(1000);
            timer.Elapsed += (s, e) => totpService.UpdateOtps(totpItems, TotpList);
            timer.Start();
        }

        private void AddTotp_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NameTextBox.Text) || string.IsNullOrEmpty(SecretTextBox.Text)) return;

            var newItem = new TotpItem(IssuerTextBox.Text, NameTextBox.Text, SecretTextBox.Text);
            totpItems.Add(newItem);

            if (newItem.CurrentOtp == "Invalid Secret")
            {
                MessageBox.Show($"Failed to add {newItem.Name}: Invalid secret key.");
            }
            else
            {
                UiHelper.ClearInputFields(IssuerTextBox, NameTextBox, SecretTextBox, QrCodeContentTextBox);
                TotpList.Items.Refresh();
                dataService.SaveData(totpItems);
            }
        }

        private void DeleteTotp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TotpItem item)
            {
                totpItems.Remove(item);
                TotpList.Items.Refresh();
                dataService.SaveData(totpItems);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*",
                Title = "Select QR Code Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string qrResult = qrCodeService.ReadQrCodeFromFile(openFileDialog.FileName);
                    ProcessQrCodeResult(qrResult);
                }
                catch (Exception ex)
                {
                    UiHelper.DisplayMessage(QrCodeContentTextBox, "Failed to read QR Code");
                    UiHelper.ClearInputFields(IssuerTextBox, NameTextBox, SecretTextBox, QrCodeContentTextBox);
                    UiHelper.ShowError("Error browsing QR code", ex);
                }
            }
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                ProcessQrCodeResult(Clipboard.GetText());
            }
            else if (Clipboard.ContainsImage())
            {
                try
                {
                    var clipboardImage = Clipboard.GetImage();
                    using (var stream = new MemoryStream())
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(clipboardImage));
                        encoder.Save(stream);
                        byte[] imageBytes = stream.ToArray();

                        string qrResult = qrCodeService.ReadQrCodeFromImageBytes(imageBytes);
                        ProcessQrCodeResult(qrResult);
                    }
                }
                catch (Exception ex)
                {
                    UiHelper.DisplayMessage(QrCodeContentTextBox, $"Error processing image from clipboard: {ex.Message}");
                    UiHelper.ClearInputFields(IssuerTextBox, NameTextBox, SecretTextBox, QrCodeContentTextBox);
                }
            }
            else
            {
                UiHelper.DisplayMessage(QrCodeContentTextBox, "No text or image found in clipboard");
                UiHelper.ClearInputFields(IssuerTextBox, NameTextBox, SecretTextBox, QrCodeContentTextBox);
            }
        }

        private void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TotpItem item)
            {
                byte[] qrImageBytes = qrCodeService.GenerateQrCode(item);
                using (var stream = new MemoryStream(qrImageBytes))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    QrCodeImage.Source = bitmapImage;
                    QrCodePopup.Tag = qrImageBytes;
                    QrCodePopup.IsOpen = true;
                }
            }
        }

        private void CopyImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (QrCodePopup.Tag is byte[] imageBytes)
            {
                try
                {
                    using (var stream = new MemoryStream(imageBytes))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        Clipboard.SetImage(bitmap);
                    }
                    QrCodePopup.IsOpen = false;
                }
                catch (Exception ex)
                {
                    UiHelper.ShowError("Error copying QR code image", ex);
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            UiHelper.ClearInputFields(IssuerTextBox, NameTextBox, SecretTextBox, QrCodeContentTextBox);
        }

        private void ProcessQrCodeResult(string qrResult)
        {
            if (string.IsNullOrEmpty(qrResult))
            {
                UiHelper.DisplayMessage(QrCodeContentTextBox, "Empty QR Code result");
                UiHelper.ClearInputFields(IssuerTextBox, NameTextBox, SecretTextBox, QrCodeContentTextBox);
                return;
            }

            QrCodeContentTextBox.Text = qrResult;
            if (qrResult.StartsWith("otpauth-migration://"))
            {
                var (issuer, accountName, secret) = qrCodeService.ExtractSecretFromMigration(qrResult);
                IssuerTextBox.Text = issuer;
                NameTextBox.Text = accountName;
                SecretTextBox.Text = secret ?? "Multiple TOTP entries detected";
            }
            else if (qrResult.StartsWith("otpauth://totp/"))
            {
                var (issuer, accountName) = qrCodeService.ParseTotpUri(qrResult);
                IssuerTextBox.Text = issuer;
                NameTextBox.Text = accountName;
                SecretTextBox.Text = qrCodeService.ExtractSecretFromQr(qrResult);
            }
            else
            {
                UiHelper.DisplayMessage(QrCodeContentTextBox, "Invalid QR Code format");
                UiHelper.ClearInputFields(IssuerTextBox, NameTextBox, SecretTextBox, QrCodeContentTextBox);
            }
        }
    }
}