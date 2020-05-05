namespace Foundation.Commerce.Payment.Payoo
{
    public class PayooPaymentResult
    {
        public string Session { get; set; }
        /// <summary>
        /// Order number from shopping website that sent to Payoo before in Order Information
        /// </summary>
        public string OrderNo { get; set; }
        /// <summary>
        /// Payment result |
        /// 1: Success |
        /// 0: Payment failed |
        /// -1: Cancelled
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Error code in case payment failure (Status = 0)
        /// </summary>
        public string ErrorCode { get; set; }
        /// <summary>
        /// Error description in case payment failure.
        /// </summary>
        public string ErrorMsg { get; set; }
        /// <summary>
        /// Payment method: Ewallet, ATM card, International card, pay at store
        /// </summary>
        public string PaymentMethod { get; set; }
        /// <summary>
        /// The Bank of user’s chosen
        /// </summary>
        public string Bank { get; set; }
        /// <summary>
        /// The hash string that will be used to verify payment result from Payoo
        /// Format: checksum = SHA512(SecretKey+session+'.'+order_no+'.'+status)
        /// Ex: SHA512(1029998308894403SS7981.ORD77823.1
        /// </summary>
        public string Checksum { get; set; }
    }
}
