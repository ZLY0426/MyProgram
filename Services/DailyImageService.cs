using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Media.Imaging;

namespace MyProgram.Services
{
    public class DailyImageService : IDailyImageService
    {
        // 必应每日一图 API (返回 JSON)
        private const string BingApi = "https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=zh-CN";
        // 本地缓存路径
        private readonly string _cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IndustrialIotCache");

        public async Task<BitmapImage> GetTodaysBackgroundAsync(CancellationToken token = default)
        {
            try
            {
                string todayFile = Path.Combine(_cacheDir, $"bg_{DateTime.Today:yyyyMMdd}.jpg");

                // 1. 读取缓存（异步）
                if (File.Exists(todayFile))
                {
                    return await LoadBitmapAsync(todayFile, token);
                }

                // 2. 网络请求（异步）
                string imageUrl = await FetchBingImageUrlAsync(token);
                if (string.IsNullOrEmpty(imageUrl)) return null;

                // 3. 下载图片（异步）
                using var http = new HttpClient();
                var data = await http.GetByteArrayAsync(imageUrl, token);

                // 4. 写入文件（异步）
                Directory.CreateDirectory(_cacheDir);
                await File.WriteAllBytesAsync(todayFile, data, token);

                return await LoadBitmapAsync(todayFile, token);
            }
            catch
            {
                return null;
            }
        }

        // 异步加载图片
        private async Task<BitmapImage> LoadBitmapAsync(string path, CancellationToken token)
        {
            byte[] data = await File.ReadAllBytesAsync(path, token);
            using var ms = new MemoryStream(data);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private async Task<string> FetchBingImageUrlAsync(CancellationToken token)
        {
            using (var client = new HttpClient())
            {
                string json = await client.GetStringAsync(BingApi, token);
                JObject data = JObject.Parse(json);
                string? urlPart = data["images"]?[0]?["url"]?.ToString();
                return "https://www.bing.com" + urlPart;
            }
        }

        

        private void CleanOldCache()
        {
            if (!Directory.Exists(_cacheDir)) return;
            foreach (var file in Directory.GetFiles(_cacheDir, "bg_*.jpg"))
            {
                File.Delete(file);
            }
        }
    }
}
