using System;
using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Core;
using Mediachase.Commerce.Orders.Dto;
using Mediachase.Commerce.Orders.Managers;

namespace Foundation.Commerce.Payment.Payoo
{
    /// <summary>
    /// Represents Payoo configuration data.
    /// </summary>
    public class PayooConfiguration
    {
        private PaymentMethodDto _paymentMethodDto;
        private IDictionary<string, string> _settings;

        public const string PayooSystemName = "Payoo";

        public const string ApiPayooCheckoutParameter = "ApiPayooCheckout";
        public const string BusinessUsernameParameter = "BusinessUsername";
        public const string ShopIDParameter = "ShopID";
        public const string ShopTitleParameter = "ShopTitle";
        public const string ChecksumKeyParameter = "ChecksumKey";
        public const string APIUsernameParameter = "APIUsername";
        public const string APIPasswordParameter = "APIPassword";
        public const string APISignatureParameter = "APISignature";

        public string ApiPayooCheckout { get; protected set; }
        public string BusinessUsername { get; protected set; }
        public string ShopID { get; protected set; }
        public string ShopTitle { get; protected set; }
        public string ChecksumKey { get; protected set; }
        public string APIUsername { get; protected set; }
        public string APIPassword { get; protected set; }
        public string APISignature { get; protected set; }

        public Guid PaymentMethodId { get; protected set; }

        /// <summary>
        /// Initializes a new instance of <see cref="PayPalConfiguration"/>.
        /// </summary>
        public PayooConfiguration() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PayPalConfiguration"/> with specific settings.
        /// </summary>
        /// <param name="settings">The specific settings.</param>
        public PayooConfiguration(IDictionary<string, string> settings)
        {
            Initialize(settings);
        }

        /// <summary>
        /// Gets the PaymentMethodDto's parameter (setting in CommerceManager of Payoo) by name.
        /// </summary>
        /// <param name="paymentMethodDto">The payment method dto.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter row.</returns>
        public static PaymentMethodDto.PaymentMethodParameterRow GetParameterByName(PaymentMethodDto paymentMethodDto, string parameterName)
        {
            var rowArray = (PaymentMethodDto.PaymentMethodParameterRow[])paymentMethodDto.PaymentMethodParameter.Select($"Parameter = '{parameterName}'");
            return rowArray.Length > 0 ? rowArray[0] : null;
        }

        protected virtual void Initialize(IDictionary<string, string> settings)
        {
            _paymentMethodDto = GetPayooPaymentMethod();
            PaymentMethodId = GetPaymentMethodId();

            _settings = settings ?? GetSettings();
            GetParametersValues();
        }

        public static PaymentMethodDto GetPayooPaymentMethod()
        {
            return PaymentManager.GetPaymentMethodBySystemName(PayooSystemName, SiteContext.Current.LanguageName);
        }

        private Guid GetPaymentMethodId()
        {
            var paymentMethodRow = _paymentMethodDto.PaymentMethod.Rows[0] as PaymentMethodDto.PaymentMethodRow;
            return paymentMethodRow?.PaymentMethodId ?? Guid.Empty;
        }

        private IDictionary<string, string> GetSettings()
        {
            return _paymentMethodDto.PaymentMethod
                .FirstOrDefault()
                ?.GetPaymentMethodParameterRows()
                ?.ToDictionary(row => row.Parameter, row => row.Value);
        }

        private void GetParametersValues()
        {
            if (_settings != null)
            {
                ApiPayooCheckout = GetParameterValue(ApiPayooCheckoutParameter);
                BusinessUsername = GetParameterValue(BusinessUsernameParameter);
                ShopID = GetParameterValue(ShopIDParameter);
                ShopTitle = GetParameterValue(ShopTitleParameter);
                ChecksumKey = GetParameterValue(ChecksumKeyParameter);
                APIUsername = GetParameterValue(APIUsernameParameter);
                APIPassword = GetParameterValue(APIPasswordParameter);
                APISignature = GetParameterValue(APISignatureParameter);
            }
        }

        private string GetParameterValue(string parameterName)
        {
            string parameterValue;
            return _settings.TryGetValue(parameterName, out parameterValue) ? parameterValue : string.Empty;
        }

    }
}
