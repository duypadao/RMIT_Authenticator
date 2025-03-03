using Google.Protobuf;
using Googleauth;
using OtpNet;
using QRCoder;
using RMIT_Authenticator.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Web;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace RMIT_Authenticator.Services
{
    public class QrCodeService
    {
        // Đọc QR Code từ file ảnh
        public string ReadQrCodeFromFile(string filePath)
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(filePath);
                return ReadQrCodeFromImageBytes(imageBytes);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading QR code", ex);
            }
        }

        // Đọc QR Code từ mảng byte ảnh
        public string ReadQrCodeFromImageBytes(byte[] imageBytes)
        {
            using (var image = Image.Load<Rgba32>(imageBytes))
            {
                byte[] rgbData = new byte[image.Width * image.Height * 3];
                int index = 0;
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            var pixel = row[x];
                            rgbData[index++] = pixel.R;
                            rgbData[index++] = pixel.G;
                            rgbData[index++] = pixel.B;
                        }
                    }
                });

                var luminanceSource = new RGBLuminanceSource(rgbData, image.Width, image.Height);
                var barcodeReader = new BarcodeReader();
                var result = barcodeReader.Decode(luminanceSource);
                return result?.Text;
            }
        }

        // Trích xuất Issuer, AccountName, Secret từ QR Code migration
        public (string issuer, string accountName, string secret) ExtractSecretFromMigration(string qrText)
        {
            try
            {
                var uri = new Uri(qrText);
                var query = HttpUtility.ParseQueryString(uri.Query);
                var data = query["data"];
                if (string.IsNullOrEmpty(data)) return (null, null, null);

                byte[] protoBytes = Convert.FromBase64String(data);
                var payload = MigrationPayload.Parser.ParseFrom(protoBytes);

                if (payload.OtpParameters.Count == 1)
                {
                    var otp = payload.OtpParameters[0];
                    return (otp.Issuer, otp.Name, Base32Encoding.ToString(otp.Secret.ToByteArray()));
                }
                return ("Multiple Issuers", "Multiple Accounts", null);
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing migration data", ex);
            }
        }

        // Trích xuất Secret Key từ chuỗi otpauth://totp/
        public string ExtractSecretFromQr(string qrText)
        {
            try
            {
                var uri = new Uri(qrText);
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query["secret"];
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Parse Issuer và AccountName từ chuỗi otpauth://totp/
        public (string issuer, string accountName) ParseTotpUri(string uriString)
        {
            try
            {
                var uri = new Uri(uriString);
                string path = Uri.UnescapeDataString(uri.Authority + uri.AbsolutePath);
                if (path.StartsWith("totp/"))
                {
                    path = path.Substring(5); // Loại bỏ "totp/"
                }
                var parts = path.Split(new[] { ':' }, 2);
                return parts.Length == 2 ? (parts[0], parts[1]) : (path, string.Empty);
            }
            catch (Exception)
            {
                return (string.Empty, string.Empty);
            }
        }

        // Tạo QR Code từ TotpItem và trả về mảng byte PNG
        public byte[] GenerateQrCode(TotpItem item)
        {
            string qrString = $"otpauth://totp/{Uri.EscapeDataString(item.Issuer)}:{Uri.EscapeDataString(item.Name)}?secret={item.Secret}&issuer={Uri.EscapeDataString(item.Issuer)}";
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            return qrCode.GetGraphic(20);
        }
    }
}