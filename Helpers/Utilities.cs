using System;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using EPiServer.Commerce.Order;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Orders.Search;

namespace Foundation.Commerce.Payment.Payoo
{
    public class Utilities
    {
        public static string EncryptSHA512(string hashString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(hashString);
            using (var hash = SHA512.Create())
            {
                byte[] hashedInputBytes = hash.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
                StringBuilder hashedInputStringBuilder = new StringBuilder(128);
                foreach (byte b in hashedInputBytes)
                {
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                }
                return hashedInputStringBuilder.ToString();
            }
        }

        public static IPurchaseOrder GetPurchaseOrderByOrderNumber(string orderNumber)
        {
            var parameters = new OrderSearchParameters
            {
                SqlMetaWhereClause = $"Meta.TrackingNumber = '{orderNumber}'"
            };

            var options = new OrderSearchOptions
            {
                CacheResults = false,
                RecordsToRetrieve = 1,
                StartingRecord = 0,
                Classes = new StringCollection { "PurchaseOrder" },
                Namespace = "Mediachase.Commerce.Orders"
            };

            var po = OrderContext.Current.Search<PurchaseOrder>(parameters, options).FirstOrDefault();
            return po;
        }

        public static PayooConnectionPackage GetPayooConnectionPackage(string packageData)
        {
            try
            {
                var objPackage = new PayooConnectionPackage();
                var doc = new XmlDocument();
                doc.LoadXml(packageData);
                objPackage.Data = ReadNodeValue(doc, "Data");
                objPackage.Signature = ReadNodeValue(doc, "Signature");
                return objPackage;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static PaymentNotification GetPaymentNotify(string notifyData)
        {
            try
            {
                var data = Encoding.UTF8.GetString(Convert.FromBase64String(notifyData));
                var invoice = new PaymentNotification();
                var doc = new XmlDocument();
                doc.LoadXml(data);
                if (!string.IsNullOrEmpty(ReadNodeValue(doc, "BillingCode")))
                {
                    // Pay at store
                    if (!string.IsNullOrEmpty(ReadNodeValue(doc, "ShopId")))
                    {
                        invoice.ShopID = long.Parse(ReadNodeValue(doc, "ShopId"));
                    }
                    invoice.OrderNo = ReadNodeValue(doc, "OrderNo");
                    if (!string.IsNullOrEmpty(ReadNodeValue(doc, "OrderCashAmount")))
                    {
                        invoice.OrderCashAmount = long.Parse(ReadNodeValue(doc, "OrderCashAmount"));
                    }
                    invoice.State = ReadNodeValue(doc, "State");
                    invoice.PaymentMethod = ReadNodeValue(doc, "PaymentMethod");
                    invoice.BillingCode = ReadNodeValue(doc, "BillingCode");
                    invoice.PaymentExpireDate = ReadNodeValue(doc, "PaymentExpireDate");
                }
                else
                {
                    invoice.Session = ReadNodeValue(doc, "session");
                    invoice.BusinessUsername = ReadNodeValue(doc, "username");
                    invoice.ShopID = long.Parse(ReadNodeValue(doc, "shop_id"));
                    invoice.ShopTitle = ReadNodeValue(doc, "shop_title");
                    invoice.ShopDomain = ReadNodeValue(doc, "shop_domain");
                    invoice.ShopBackUrl = ReadNodeValue(doc, "shop_back_url");
                    invoice.OrderNo = ReadNodeValue(doc, "order_no");
                    invoice.OrderCashAmount = long.Parse(ReadNodeValue(doc, "order_cash_amount"));
                    invoice.StartShippingDate = ReadNodeValue(doc, "order_ship_date");
                    invoice.ShippingDays = short.Parse(ReadNodeValue(doc, "order_ship_days"));
                    invoice.OrderDescription = System.Web.HttpUtility.UrlDecode((ReadNodeValue(doc, "order_description")));
                    invoice.NotifyUrl = ReadNodeValue(doc, "notify_url");
                    invoice.State = ReadNodeValue(doc, "State");
                    invoice.PaymentMethod = ReadNodeValue(doc, "PaymentMethod");
                    invoice.PaymentExpireDate = ReadNodeValue(doc, "validity_time");
                }
                return invoice;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static string ReadNodeValue(XmlDocument doc, string tagName)
        {
            XmlNodeList nodeList = doc.GetElementsByTagName(tagName);
            string nodeValue = null;
            if (nodeList.Count > 0)
            {
                XmlNode node = nodeList.Item(0);
                nodeValue = (node == null) ? string.Empty : node.InnerText;
            }
            return nodeValue ?? string.Empty;
        }
    }
}
