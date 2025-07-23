using Microsoft.EntityFrameworkCore;
namespace Repositories.Helpers
{
    /// <summary>
    /// Represents a paginated list of items along with pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    public class PagedList<T> : List<T>
    {
        /// <summary>
        /// Gets the pagination metadata.
        /// </summary>
        public MetaData MetaData { get; }

        /// <summary>
        /// Gets the current page number (1-based).
        /// </summary>
        public int CurrentPage => MetaData.CurrentPage;

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages => MetaData.TotalPages;

        /// <summary>
        /// Gets the page size (number of items per page).
        /// </summary>
        public int PageSize => MetaData.PageSize;

        /// <summary>
        /// Gets the total count of items across all pages.
        /// </summary>
        public int TotalCount => MetaData.TotalCount;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Gets the items for the current page.
        /// </summary>
        public List<T> Items => this.ToList();

        /// <summary>
        /// Initializes a new instance of the <see cref="PagedList{T}"/> class.
        /// </summary>
        /// <param name="items">The list of items for the current page.</param>
        /// <param name="count">The total number of items.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");

            MetaData = new MetaData
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = count,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            };

            AddRange(items);
        }

        /// <summary>
        /// Creates a paginated list asynchronously from an IQueryable source.
        /// </summary>
        /// <param name="query">The source query.</param>
        /// <param name="pageNumber">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the paginated list.</returns>
        public static async Task<PagedList<T>> ToPagedListAsync(IQueryable<T> query, int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");

            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }

    public class MetaData
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
    /// <summary>
    /// Lớp chứa các tham số cho việc phân trang.
    /// Được sử dụng để nhận thông tin từ query string của API request.
    /// </summary>
    public class PaginationParams
    {
        // Hằng số xác định kích thước trang tối đa để tránh request quá lớn làm ảnh hưởng hệ thống.
        private const int MaxPageSize = 50;

        // Số trang hiện tại, mặc định là trang 1.
        public int PageNumber { get; set; } = 1;

        // Kích thước trang (số lượng mục trên một trang).
        private int _pageSize = 10; // Giá trị mặc định là 10.

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}