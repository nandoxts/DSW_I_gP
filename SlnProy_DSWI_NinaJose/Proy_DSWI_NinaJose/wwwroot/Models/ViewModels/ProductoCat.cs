using Proy_DSWI_NinaJose.Models;
using System.Collections.Generic;

namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class ProductoCat
    {
        public List<Producto> Destacados { get; set; } = new();
        public Dictionary<string, List<Producto>> ProductosPorCategoria { get; set; } = new();
    }
}
