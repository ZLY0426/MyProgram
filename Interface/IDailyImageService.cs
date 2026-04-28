using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace MyProgram.Interface
{
    public interface IDailyImageService
    {
        Task<BitmapImage> GetTodaysBackgroundAsync(CancellationToken token = default);
    }
}
