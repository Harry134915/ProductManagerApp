using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductManagerApp.DTO
{
    public class ProductUpdateDto
    {

        public int Id { get; set; }

        public string Name { get; set; } = "";

        public decimal Price { get; set; }

        public int Stock {  get; set; }

        public string Description { get; set; } = "";
        
    }
}
