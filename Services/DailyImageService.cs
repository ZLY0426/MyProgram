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

        public BitmapImage GetTodaysBackground()
        {
            try
            {
                string todayFile = Path.Combine(_cacheDir, $"bg_{DateTime.Today:yyyyMMdd}.jpg");

                // 1. 如果今天已经缓存过，直接读取
                if (File.Exists(todayFile))
                {
                    return LoadBitmapFromFile(todayFile);
                }

                // 2. 清理旧缓存
                CleanOldCache();

                // 3. 调用 API 获取图片 URL
                string imageUrl = FetchBingImageUrl();
                if (string.IsNullOrEmpty(imageUrl)) return null;

                // 4. 下载并保存
                Directory.CreateDirectory(_cacheDir);
                using (var httpClient = new HttpClient())
                {
                    byte[] data = httpClient.GetByteArrayAsync(imageUrl).Result;
                    File.WriteAllBytes(todayFile, data);
                }

                return LoadBitmapFromFile(todayFile);
            }
            catch
            {
                return null; // 网络失败时不显示背景
            }
        }

        private string FetchBingImageUrl()
        {
            using (var client = new HttpClient())
            {
                string json = client.GetStringAsync(BingApi).Result;
                JObject data = JObject.Parse(json);
                string? urlPart = data["images"]?[0]?["url"]?.ToString();
                return "https://www.bing.com" + urlPart;
            }
        }

        private BitmapImage LoadBitmapFromFile(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // 加载后释放文件锁
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze(); // 允许跨线程访问
            return bitmap;
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
