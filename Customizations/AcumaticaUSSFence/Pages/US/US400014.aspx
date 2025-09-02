<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormDetail.master" AutoEventWireup="true" ValidateRequest="false" CodeFile="US400014.aspx.cs" Inherits="Page_US400014" Title="Update SOLine Tax Category" %>

<%@ MasterType VirtualPath="~/MasterPages/FormDetail.master" %>

<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
    <px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Orders"
        TypeName="AcumaticaUSSFenceCustomizations.UpdateSOLineTaxCategory">
    </px:PXDataSource>
</asp:Content>

<asp:Content ID="cont2" ContentPlaceHolderID="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" Height="400px" Width="100%" Style="z-index: 100"
        AllowPaging="True" AllowSearch="True" AdjustPageSize="Auto" DataSourceID="ds" SkinID="PrimaryInquire" SyncPosition="true" TabIndex="100"
        TemporaryFilterCaption="Filter Applied" FilesIndicator="False">
        <Levels>
            <px:PXGridLevel DataKeyNames="OrderType,OrderNbr" DataMember="Orders">
                <RowTemplate>
                    <px:PXSelector Size="xxs" ID="edOrderType" runat="server" AllowNull="False" DataField="OrderType" Enabled="False" />
                    <px:PXSelector ID="edOrderNbr" runat="server" DataField="OrderNbr" Enabled="False" AllowEdit="True" />
                    <px:PXSelector ID="edInventoryCD" runat="server" DataField="InventoryItem__InventoryCD" Enabled="False" AllowEdit="True" />
                </RowTemplate>
                <Columns>
                    <px:PXGridColumn AllowCheckAll="true" DataField="Selected" TextAlign="Center" Type="CheckBox" Width="60px" />
                    <px:PXGridColumn DataField="OrderType" />
                    <px:PXGridColumn DataField="OrderNbr" />
                    <px:PXGridColumn DataField="Status" />
                    <px:PXGridColumn DataField="InventoryItem__TaxCategoryID" />
                    <px:PXGridColumn DataField="InventoryItem__InventoryCD" />
                    <px:PXGridColumn DisplayMode="Value" DataField="SOLine__TaxCategoryID" ></px:PXGridColumn>
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="200" />
    </px:PXGrid>   
</asp:Content>

