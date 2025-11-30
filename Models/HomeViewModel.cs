using System.Collections.Generic;

namespace HoangLongTH.Models
{
    public class HomeViewModel
    {
        // Danh sách sản phẩm mới nhất
        public List<Product> NewProducts { get; set; }

        // Danh sách sản phẩm nổi bật / bán chạy
        public List<Product> BestSellerProducts { get; set; }
    }
}
