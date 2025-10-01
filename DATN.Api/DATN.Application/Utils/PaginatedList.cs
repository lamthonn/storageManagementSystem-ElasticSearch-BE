using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace DATN.Application.Utils
{
    public class PaginatedList<T>
    {
        public List<T> Items { get; set; }
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecord { get; set; }
        public int PageSize { get; set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            TotalRecord = count;
            PageSize = pageSize;
            PageIndex = Math.Max(pageIndex, 1); // Đảm bảo pageIndex >= 1
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            Items = items;
        }

        public static async Task<PaginatedList<T>> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = await source.CountAsync();
            pageNumber = Math.Max(pageNumber, 1); // Đảm bảo pageNumber >= 1
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageNumber, pageSize);
        }
    }

}
