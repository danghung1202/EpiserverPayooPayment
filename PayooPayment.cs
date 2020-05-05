using System;
using System.Runtime.Serialization;
using Mediachase.Commerce.Orders;
using Mediachase.MetaDataPlus.Configurator;

namespace Foundation.Commerce.Payment.Payoo
{
    /// <summary>
    /// Represents Payment class for Payoo.
    /// </summary>
    [Serializable]
    public class PayooPayment : Mediachase.Commerce.Orders.Payment
    {
        private static MetaClass _metaClass;

        public PayooPayment()
            : base(PayooPaymentMetaClass)
        {
            PaymentType = PaymentType.Other;
            ImplementationClass = GetType().AssemblyQualifiedName; // need to have assembly name in order to retrieve the correct type in ClassInfo
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="PayooPayment"/> class.
        ///// </summary>
        ///// <param name="info">The info.</param>
        ///// <param name="context">The context.</param>
        //public PayooPayment(SerializationInfo info, StreamingContext context) : base(info, context)
        //{
        //    PaymentType = PaymentType.Other;
        //    ImplementationClass = GetType().AssemblyQualifiedName; // need to have assembly name in order to retrieve the correct type in ClassInfo
        //}

        /// <summary>
        /// Gets the payoo payment meta class.
        /// </summary>
        /// <value>The credit card payment meta class.</value>
        public static MetaClass PayooPaymentMetaClass => _metaClass ?? (_metaClass = MetaClass.Load(OrderContext.MetaDataContext, "PayooPayment"));

        /// <summary>
        /// Payoo will response the OrderId to Merchant
        /// </summary>
        public string PayooOrderId
        {
            get { return GetString(Constant.PayooOrderIdPropertyName); }
            set { this[Constant.PayooOrderIdPropertyName] = value; }
        }

        /// <summary>
        /// Order number. It should be unique from your system and use for Data exchange processing
        /// </summary>
        public string PayooOrderNumber
        {
            get { return GetString(Constant.PayooOrderNumberPropertyName); }
            set { this[Constant.PayooOrderNumberPropertyName] = value; }
        }

        /// <summary>
        /// Total amount. It must be in Vietnam Dong.
        /// </summary>
        public string PayooAmount
        {
            get { return GetString(Constant.PayooAmountPropertyName); }
            set { this[Constant.PayooAmountPropertyName] = value; }
        }

        /// <summary>
        /// Payoo will response the OrderId to Merchant
        /// </summary>
        public string PayooExpiryDate
        {
            get { return GetString(Constant.PayooExpiryDatePropertyName); }
            set { this[Constant.PayooExpiryDatePropertyName] = value; }
        }

        /// <summary>
        /// If Merchant has use method of store payment, Payoo will response payment code to Merchant.
        /// </summary>
        public string PayooPaymentCode
        {
            get { return GetString(Constant.PayooPaymentCodePropertyName); }
            set { this[Constant.PayooPaymentCodePropertyName] = value; }
        }
    }
}
