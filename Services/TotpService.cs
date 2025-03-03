using RMIT_Authenticator.Models;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RMIT_Authenticator.Services
{
    public class TotpService
    {
        private readonly Dispatcher dispatcher;

        public TotpService(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        // Cập nhật mã OTP cho tất cả các mục
        public void UpdateOtps(List<TotpItem> items, ItemsControl listControl)
        {
            dispatcher.Invoke(() =>
            {
                foreach (var item in items)
                {
                    item.UpdateOtp();
                }
            });
        }
    }
}