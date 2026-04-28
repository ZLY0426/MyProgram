using System.Collections.Generic;

namespace MyProgram.Dtos
{
    // 用于包装分页结果，替代 out 参数
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}