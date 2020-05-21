namespace Foundation.Commerce.Payment.Payoo
{
    public class PaymentNotification : PayooOrder
    {
        public string PaymentMethod { get; set; }
        public string State { get; set; }
    }
}
