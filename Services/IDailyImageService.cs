using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace MyProgram.Services
{
    public interface IDailyImageService
    {
        BitmapImage GetTodaysBackground();
    }
}
