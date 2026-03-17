namespace WebApiRbac.Application.DTOs
{
    public class PagedResponseDto<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();

        // informasi untuk frontend agar bisa mmebuat tombol 1, 2, 3... next
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        // otomatis menghitung total halaman
        // Jika tidak ada data (TotalCount = 0), minimal total halaman adalah 1
        public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling((double)TotalCount / PageSize);
        // Apakah ada halaman sebelumnya? (Ada, jika halaman saat ini lebih dari 1)
        public bool HasPreviousPage => CurrentPage > 1;
        // Apakah ada halaman selanjutnya? (Ada, jika halaman saat ini lebih kecil dari total halaman)
        public bool HasNextPage => CurrentPage < TotalPages;

        // Angka halamannya (Menggunakan int? agar bisa bernilai null jika mentok)
        public int? PrevPage => HasPreviousPage ? CurrentPage - 1 : null;
        public int? NextPage => HasNextPage ? CurrentPage + 1 : null;


    }
}
