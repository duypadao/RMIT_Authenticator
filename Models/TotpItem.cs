using System.ComponentModel;
using OtpNet;

namespace RMIT_Authenticator.Models
{
    public class TotpItem : INotifyPropertyChanged
    {
        private Totp totp;
        private string currentOtp;

        public string Issuer { get; set; }
        public string Name { get; set; }
        public string Secret { get; set; }

        public string CurrentOtp
        {
            get => currentOtp;
            set
            {
                currentOtp = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentOtp)));
            }
        }

        public TotpItem() { }

        public TotpItem(string issuer, string name, string secret)
        {
            Issuer = issuer;
            Name = name;
            Secret = secret;
            InitializeTotp(secret);
            UpdateOtp();
        }

        private void InitializeTotp(string secret)
        {
            try
            {
                if (string.IsNullOrEmpty(secret))
                {
                    throw new ArgumentException("Secret cannot be null or empty.");
                }
                totp = new Totp(Base32Encoding.ToBytes(secret));
            }
            catch (Exception)
            {
                CurrentOtp = "Invalid Secret";
                totp = null;
            }
        }

        public void UpdateOtp()
        {
            if (totp == null)
            {
                CurrentOtp = "Invalid Secret";
            }
            else
            {
                CurrentOtp = totp.ComputeTotp();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}