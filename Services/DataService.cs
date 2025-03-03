using RMIT_Authenticator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RMIT_Authenticator.Services
{
    public class DataService
    {
        private const string DataFilePath = "totpData.json";

        // Đọc dữ liệu từ file JSON
        public List<TotpItem> LoadData()
        {
            var items = new List<TotpItem>();
            try
            {
                if (!File.Exists(DataFilePath)) return items;

                string json = File.ReadAllText(DataFilePath);
                var deserializedItems = JsonSerializer.Deserialize<List<TotpItem>>(json);
                if (deserializedItems != null)
                {
                    foreach (var item in deserializedItems)
                    {
                        if (!string.IsNullOrEmpty(item.Secret))
                        {
                            items.Add(new TotpItem(item.Issuer, item.Name, item.Secret));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading data", ex);
            }
            return items;
        }

        // Lưu dữ liệu vào file JSON
        public void SaveData(List<TotpItem> items)
        {
            try
            {
                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(DataFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving data", ex);
            }
        }
    }
}