using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MyProgram.CustomControl
{
    public class FillDataGrid : DataGrid
    {
        private bool _isAdjusting; // 防止无限递归

        public FillDataGrid()
        {
            // 可选：设置默认列头高度，方便计算
            this.ColumnHeaderHeight = 50;
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            // 数据变化时，主动请求重新测量，保证翻页后行高更新
            InvalidateMeasure();
        }
        
        protected override Size MeasureOverride(Size availableSize)
        {
            // 跳过递归调用、无数据、无穷约束（例如在 ScrollViewer 中）的情况
            if (_isAdjusting || Items.Count == 0 || double.IsInfinity(availableSize.Height) || availableSize.Height <= 0)
                return base.MeasureOverride(availableSize);

            _isAdjusting = true;
            try
            {
                double headerHeight = ColumnHeaderHeight;
                if (double.IsNaN(headerHeight))
                    headerHeight = 30; // 默认备用值

                double totalRowsHeight = availableSize.Height - headerHeight;
                if (totalRowsHeight > 0)
                {
                    double newRowHeight = totalRowsHeight / Items.Count;
                    // 仅在值变化时设置，减少不必要布局更新
                    if (Math.Abs(RowHeight - newRowHeight) > 0.1)
                        RowHeight = newRowHeight - 1;
                }
            }
            finally
            {
                _isAdjusting = false;
            }

            // 用调整后的 RowHeight 继续默认测量流程
            return base.MeasureOverride(availableSize);
        }
    }
}
