using System;
using System.Linq;
using System.Text;
using EPiServer.Commerce.Order;
using EPiServer.ServiceLocation;
using Mediachase.Commerce;

namespace Foundation.Commerce.Payment.Payoo
{
    public class OrderDescriptionHTMLFactory
    {
        private static Injected<IOrderGroupCalculator> _orderGroupCalculator = default(Injected<IOrderGroupCalculator>);

        private static string _orderDescHtml = @"<table width='100%' border='1' cellspacing='0'>
	                                                <thead>
		                                                <tr>
			                                                <td width='35%' align='center'>
				                                                <b>Tên hàng</b>
			                                                </td>
                                                            <td width='10%' align='center'>
				                                                <b>Số lượng</b>
			                                                </td>
			                                                <td width='20%' align='center'>
				                                                <b>Đơn giá</b>
			                                                </td>
                                                            <td width='15%' align='center'>
				                                                <b>Giảm giá</b>
			                                                </td>
			                                                <td width='20%' align='center'>
				                                                <b>Thành tiền</b>
			                                                </td>
		                                                </tr>
	                                                </thead>
	                                                <tbody>
                                                        {0}
                                                        <tr>
                                                            <td></td>
                                                            <td></td>
                                                            <td></td>
			                                                <td align='right'>
				                                                <b>Số tiền:</b>
			                                                </td>
			                                                <td align='right'>{1}</td>
		                                                </tr>
                                                        <tr>
                                                            <td></td>
                                                            <td></td>
                                                            <td></td>
			                                                <td align='right'>
				                                                <b>Phí giao hàng:</b>
			                                                </td>
			                                                <td align='right'>{2}</td>
                                                        </tr>
                                                        <tr>
                                                            <td></td>
                                                            <td></td>
                                                            <td></td>
			                                                <td align='right'>
				                                                <b>Khuyến mãi:</b>
			                                                </td>
			                                                <td align='right'>{3}</td>
		                                                </tr>
                                                        <tr>
                                                            <td></td>
                                                            <td></td>
                                                            <td></td>
			                                                <td align='right'>
				                                                <b>Thuế VAT:</b>
			                                                </td>
			                                                <td align='right'>{4}</td>
		                                                </tr>
                                                        <tr>
                                                            <td></td>
                                                            <td></td>
                                                            <td></td>
			                                                <td align='right'>
				                                                <b>Tổng tiền:</b>
			                                                </td>
			                                                <td align='right'>{5}</td>
		                                                </tr>
	                                                </tbody>
                                                </table>";


        private static string _orderLineHtml = @"<tr>
			                                        <td align='left'>{0}</td>
			                                        <td align='right'>{1}</td>
			                                        <td align='center'>{2}</td>
			                                        <td align='right'>{3}</td>
                                                    <td align='right'>{4}</td>
		                                        </tr>";

        public static string CreateOrderDescription(IOrderGroup orderGroup)
        {
            if (orderGroup == null) throw new ArgumentNullException(nameof(orderGroup));

            var totals = _orderGroupCalculator.Service.GetOrderGroupTotals(orderGroup);
            return string.Format(_orderDescHtml,
                CreateOrderLines(orderGroup),
                orderGroup.GetSubTotal(),
                orderGroup.GetShippingSubTotal() - orderGroup.GetShippingDiscountTotal(),
                -orderGroup.GetOrderDiscountTotal(),
                totals.TaxTotal,
                totals.Total);
        }

        private static string CreateOrderLines(IOrderGroup orderGroup)
        {
            var lineItems = orderGroup.GetFirstForm().Shipments.SelectMany(x => x.LineItems);
            var orderLinesHtml = new StringBuilder();
            var currency = orderGroup.Currency;
            foreach (var lineItem in lineItems)
            {
                orderLinesHtml.Append(string.Format(_orderLineHtml,
                    lineItem.GetEntryContent().DisplayName,
                    lineItem.Quantity.ToString("0"),
                    new Money(lineItem.PlacedPrice, currency),
                    new Money(lineItem.GetEntryDiscount(), currency),
                    lineItem.GetDiscountedPrice(currency)));
            }
            return orderLinesHtml.ToString();
        }
    }
}
