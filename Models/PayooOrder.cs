namespace Foundation.Commerce.Payment.Payoo
{
    public class PayooOrder
    {
        public string Session
        {
            set;
            get;
        }

        public string BusinessUsername
        {
            set;
            get;
        }

        public long ShopID
        {
            set;
            get;
        }

        public string ShopTitle
        {
            set;
            get;
        }

        public string ShopDomain
        {
            set;
            get;
        }

        public string ShopBackUrl
        {
            set;
            get;
        }

        public string OrderNo
        {
            set;
            get;
        }

        public long OrderCashAmount
        {
            set;
            get;
        }

        public string StartShippingDate
        {
            set;
            get;
        }

        public short ShippingDays
        {
            set;
            get;
        }

        public string OrderDescription
        {
            set;
            get;
        }

        public string NotifyUrl
        {
            set;
            get;
        } = "";

        public string ValidityTime
        {
            get;
            set;
        }

        public string CustomerName
        {
            get;
            set;
        }

        public string CustomerPhone
        {
            get;
            set;
        }

        public string CustomerAddress
        {
            get;
            set;
        }

        public string CustomerEmail
        {
            get;
            set;
        }

        public string CustomerCity
        {
            get;
            set;
        }

        public string BillingCode
        {
            get;
            set;
        }

        public string PaymentExpireDate
        {
            get;
            set;
        }

        public string Xml { get; set; }
    }
}
