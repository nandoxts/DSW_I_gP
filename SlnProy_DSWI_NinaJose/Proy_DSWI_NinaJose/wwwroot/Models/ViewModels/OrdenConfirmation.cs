using System.Collections.Generic;

namespace Proy_DSWI_NinaJose.Models.ViewModels
{
    public class OrderConfirmation
    {
        public string NombreCliente { get; set; } = "";
        public string EmailCliente { get; set; } = "";
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal MontoTotal { get; set; }
    }
}
