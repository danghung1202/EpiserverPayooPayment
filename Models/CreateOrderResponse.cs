using System;

namespace Foundation.Commerce.Payment.Payoo.Models
{
    public class CreateOrderResponse
    {
        public string result;
        public Order order;
        public bool IsSuccess => result.Equals("success", StringComparison.OrdinalIgnoreCase);
    }
    public class Order
    {
        public string order_id;
        public string order_no;
        public string amount;
        public string payment_code;
        public string expiry_date;
        public string token;
        public string payment_url;
    }
}
