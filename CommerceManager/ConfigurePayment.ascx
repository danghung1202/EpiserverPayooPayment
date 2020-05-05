<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ConfigurePayment.ascx.cs" Inherits="Foundation.Commerce.Payment.Payoo.ConfigurePayment" %>
<div id="DataForm">
    <table cellpadding="0" cellspacing="2">
	    <tr>
		    <td class="FormLabelCell" colspan="2"><b><asp:Literal ID="Literal1" runat="server" Text="Configure Payoo Gateway" /></b></td>
	    </tr>
    </table>
    <br />
    <table class="DataForm">
        <tr>
            <td class="FormLabelCell"><asp:Literal ID="Literal8" runat="server" Text="Api Payoo Checkout Url" />:</td>
            <td class="FormFieldCell">
                <asp:TextBox Runat="server" ID="ApiPayooCheckout" Width="300px" MaxLength="250"></asp:TextBox><br/>
                <asp:RequiredFieldValidator ControlToValidate="ApiPayooCheckout" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
                                            ErrorMessage="Api Payoo Checkout Url required" runat="server" id="Requiredfieldvalidator8"></asp:RequiredFieldValidator>
            </td>
        </tr>
         <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal4" runat="server" Text="Business Username" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="BusinessUsername" Width="300px" MaxLength="250"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="BusinessUsername" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Business username required" runat="server" id="Requiredfieldvalidator2"></asp:RequiredFieldValidator>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal5" runat="server" Text="Shop ID" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="ShopID" Width="300px" MaxLength="250"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="ShopID" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Shop ID required" runat="server" id="Requiredfieldvalidator4"></asp:RequiredFieldValidator>
	          </td>
        </tr>
	     <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal3" runat="server" Text="Shop Title" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="ShopTitle" Width="300px" MaxLength="250"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="ShopTitle" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Shop Title Required" runat="server" id="Requiredfieldvalidator3"></asp:RequiredFieldValidator>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal2" runat="server" Text="Checksum Key" />:</td>
	          <td class="FormFieldCell">
		            <asp:TextBox Runat="server" ID="ChecksumKey" Width="300px" MaxLength="250"></asp:TextBox><br/>
		            <asp:RequiredFieldValidator ControlToValidate="ChecksumKey" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="Checksum Key required" runat="server" id="Requiredfieldvalidator1"></asp:RequiredFieldValidator>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal6" runat="server" Text="API Username" />:</td>
	          <td class="FormFieldCell">
                  <asp:TextBox Runat="server" ID="APIUsername" Width="300px" MaxLength="250"></asp:TextBox><br/>
                  <asp:RequiredFieldValidator ControlToValidate="APIUsername" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
                                              ErrorMessage="API Username required" runat="server" id="Requiredfieldvalidator6"></asp:RequiredFieldValidator>
	          </td>
        </tr>
         <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal7" runat="server" Text="API Password" />:</td>
	          <td class="FormFieldCell">
	                <asp:TextBox Runat="server" ID="APIPassword" Width="300px" MaxLength="250"></asp:TextBox><br/>
                    <asp:RequiredFieldValidator ControlToValidate="APIPassword" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
			                ErrorMessage="APIPassword required" runat="server" id="Requiredfieldvalidator5"></asp:RequiredFieldValidator>
	          </td>
        </tr>
        <tr>
            <td colspan="2" class="FormSpacerCell"></td>
        </tr>
        <tr>
              <td class="FormLabelCell"><asp:Literal ID="Literal12" runat="server" Text="API Signature" />:</td>
	          <td class="FormFieldCell">
                  <asp:TextBox Runat="server" ID="APISignature" Width="300px" MaxLength="250"></asp:TextBox><br/>
                  <asp:RequiredFieldValidator ControlToValidate="APISignature" Display="dynamic" Font-Name="verdana" Font-Size="9pt"
                                              ErrorMessage="API Signature required" runat="server" id="Requiredfieldvalidator7"></asp:RequiredFieldValidator>
	          </td>
        </tr>
    </table>
</div>