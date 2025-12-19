namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class ProductoCat
    {
        public List<Producto> Destacados { get; set; } = new List<Producto>();
        public Dictionary<string, List<Producto>> ProductosPorCategoria { get; set; } = new Dictionary<string, List<Producto>>();
    }
}
