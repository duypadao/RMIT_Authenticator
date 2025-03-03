using System.Windows.Controls;

namespace RMIT_Authenticator.Utilities
{
    public static class UiHelper
    {
        // Xóa toàn bộ thông tin trong các TextBox
        public static void ClearInputFields(TextBox issuerBox, TextBox nameBox, TextBox secretBox, TextBox contentBox)
        {
            issuerBox.Text = string.Empty;
            nameBox.Text = string.Empty;
            secretBox.Text = string.Empty;
            contentBox.Text = string.Empty;
        }

        // Hiển thị thông báo trong TextBox
        public static void DisplayMessage(TextBox contentBox, string message)
        {
            contentBox.Text = message;
        }

        // Hiển thị thông báo lỗi với chi tiết
        public static void ShowError(string context, System.Exception ex)
        {
            System.Windows.MessageBox.Show($"{context}: {ex.Message}");
        }
    }
}