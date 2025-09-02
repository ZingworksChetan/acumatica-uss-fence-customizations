using PX.Data;
using System.Collections;
using PX.Objects.SO;
using System.Collections.Generic;
using DAC;
using NAWUnitedSiteServices.Descr;
using NAWUnitedSiteServices.Prototype.Extensions.DACExtensions;
using NV.Rental360;
using System;
using PX.Common;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.SM;
using System.Threading;
using System.Linq;
using static NAWUnitedSiteServices.Prototype.Extensions.DACExtensions.NAWSOOrderUSSExt;
using static NAWUnitedSiteServices.Extensions.DAC.NAWSOOrderUSSExt;
using static NV.Rental360.NVRTSOOrderDacExtension;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

public class SOOrderEntry_Extension : PXGraphExtension<SOOrderEntry>
{
    public static bool IsActive() => true;

    public override void Initialize()
    {
        base.Initialize();
        Base.Actions["Delete"].SetVisible(false);
    }

    #region View

    #region Story: FOX-737 | Engineer: [Divya Kurumkar] | Date: [2025-03-24] | Display newly created RO from RQ (vice versa) in side panels.
    public PXSelectJoin<SOLine,
       InnerJoin<SOOrder,
           On<SOLine.orderType, Equal<SOOrder.orderType>,
           And<SOLine.orderNbr, Equal<SOOrder.orderNbr>>>>,
       Where<SOLine.origOrderNbr, Equal<SOOrder.orderNbr.FromCurrent>,
           And<SOLine.origOrderType, Equal<SOOrder.orderType.FromCurrent>>>> RelatedROLines;
    #endregion

    #endregion

    #region Events
    protected void _(Events.RowSelected<SOOrder> e)
    {
        PXCache cache = e.Cache;
        var currentSalesOrder = (SOOrder)e.Row;
        if (currentSalesOrder == null) return;
        List<string> userRolesList = UserRoleHelper.GetCurrentUserRolesList();
        bool isSystemAdmin = userRolesList.Contains("Administrator");
        HandleDeleteButtonVisibility(cache, currentSalesOrder, isSystemAdmin);// Story: FOX-577        
        HandleInvoiceTypeField(cache, currentSalesOrder);// Story: FOX-446        
        HandleCreateWorkOrderButton(currentSalesOrder);// Story: FOX-599        
        HandleRefOrderFields(e);// Story: FOX-737        
        HandleCopyOrderButton(currentSalesOrder, isSystemAdmin);// Story: FOX-756
        EnableCreateReturnButton(cache, currentSalesOrder);// Story: FOX-655
        SetDocumentDiscountVisibility(cache, currentSalesOrder);//Story: FOX-900
        DisableEventDatesOnOrderIfReleased(cache, currentSalesOrder);// Story: FOX-946
    }

    protected void _(Events.RowSelected<SOLine> e)
    {
        SOLine sOLineDetails = (SOLine)e.Row;
        if (sOLineDetails == null) return;
        List<string> userRolesList = UserRoleHelper.GetCurrentUserRolesList();
        bool isSystemAdmin = userRolesList.Contains("Administrator");
        HandleTranDescField(e.Cache, sOLineDetails, isSystemAdmin);// Story: FOX-181
        HandleCuryUnitPriceField(e.Cache, sOLineDetails);// Story: FOX-595
        HandleRelatedROLineFields(e.Cache, sOLineDetails);// Story: FOX-737
    }

    protected void _(Events.RowSelected<SOOrderShipment> e)
    {
        SOOrderShipment SOOrderShipmentDetails = (SOOrderShipment)e.Row;
        if (SOOrderShipmentDetails == null) return;
        List<string> userRolesList = UserRoleHelper.GetCurrentUserRolesList();
        bool isSystemAdmin = userRolesList.Contains("Administrator");
        HandleSoShipmentFields(e.Cache, SOOrderShipmentDetails);// story: FOX-810
        SetShipmentReleaseDateFromInvoice(SOOrderShipmentDetails);// Story: FOX-901
    }

    #region Story: FOX-740  | Engineer: [Satej Ambekar] | Date: [2025-03-20] | Overriding Acumatica default MinValue of -100 with a very high number to allow entry of higher pricing
    [PXMergeAttributes(Method = MergeMethod.Replace)]
    [PXDBDecimal(MinValue = -1000.00, MaxValue = 100.00)]
    [PXDefault(TypeCode.Decimal, "0.0")]
    [PXUIField(DisplayName = "Discount Percent")]
    protected virtual void _(Events.CacheAttached<SOLine.discPct> e) { }
    #endregion
    protected virtual void _(Events.RowPersisting<SOLine> e)
    {
        ValidateOrderQty(e.Row as SOLine);// Story: FOX-638
    }
    #endregion

    #region Private Methods

    #region Story: FOX-577 | Engineer: [Divya Kurumkar] | Date: [2025-02-04] | Remove ability to delete RQs and ROs from role other than Admin
    private void HandleDeleteButtonVisibility(PXCache cache, SOOrder currentSalesOrder, bool isSystemAdmin)
    {
        bool isROorRQ = currentSalesOrder.OrderType == "RO" || currentSalesOrder.OrderType == "RQ";
        Base.Delete.SetVisible(isSystemAdmin || !isROorRQ);
    }
    #endregion

    #region Story: FOX-446 | Engineer: [Divya Kurumkar] | Date: [2025-02-25] | Disable UsrNVInvoiceType if Order Type is RO/EX and Status is not Initial Term Pending/Invoice or Rental Status is not New.
    private void HandleInvoiceTypeField(PXCache cache, SOOrder currentSalesOrder)
    {
        int? rentalStatus = cache.GetValue<usrNVRTRentalStatus>(currentSalesOrder) as int?;
        int? invoiceStage = cache.GetValue<usrNAWInvoiceStage>(currentSalesOrder) as int?;
        bool isInitialTermPending = invoiceStage == 0 || invoiceStage == 10;
        bool disableInvoiceType = ((currentSalesOrder.OrderType == "RO" || currentSalesOrder.OrderType == "EX") && !isInitialTermPending) && rentalStatus != 0;
        PXUIFieldAttribute.SetEnabled<usrNVInvoiceType>(cache, currentSalesOrder, !disableInvoiceType);
    }
    #endregion

    #region Story: Fox-857 | Engineer: [Satej Ambekar] | Date: [2025-06-03] | Reinstate conditions where a WO should be created.
    private bool AdditionalWorkOrderButtonConditions(SOOrder order)
    {
        var ussExt = order.GetExtension<NAWSOOrderUSSExt>();
        if (ussExt == null)
            return false;

        switch (ussExt.UsrNAWInvoiceStage)
        {
            case NAWUSSInvoiceStage.InitialTermPending:
                return order.Hold == false && order.Behavior == SOBehavior.IN;
            case NAWUSSInvoiceStage.InitialTermPendingInvoice:
                return true;
            case NAWUSSInvoiceStage.InitialTermInvoiced:
                return order.Status != SOOrderStatus.Invoiced;
            case NAWUSSInvoiceStage.ContractExtendedNotInvoiced:
                return true;
            case NAWUSSInvoiceStage.RenewalInCycleBilling:
                return order.Status != SOOrderStatus.Invoiced;
            default:
                return false;
        }
    }
    #endregion

    #region Story: FOX-599  | Engineer: [Abhishek Sonone] | Date: [2025-02-28] | If the rental order status is Canceled, disable the Create Work Order button.
    private void HandleCreateWorkOrderButton(SOOrder currentSalesOrder)
    {
        bool isCanceled = currentSalesOrder.Status == "L";
        bool isROorEX = currentSalesOrder.OrderType == "RO" || currentSalesOrder.OrderType == "EX";
        var nvrtExtension = Base.GetExtension<NVRTSOOrderEntryGraphExtension>();
        if (nvrtExtension == null || !isROorEX)
            return;
        bool enableButton = !isCanceled && AdditionalWorkOrderButtonConditions(currentSalesOrder);// Story : FOX-857

        nvrtExtension.NVRTxCreateWorkOrder.SetEnabled(enableButton);
    }
    #endregion

    #region Story: FOX-655  | Engineer: [Satej Ambekar] | Date: [2025-05-06] | If the invoice is Event, Enable the Create Returns Action.
    private void EnableCreateReturnButton(PXCache cache, SOOrder currentSalesOrder)
    {
        int? isInvoiceType = cache.GetValue<usrNVInvoiceType>(currentSalesOrder) as int?;
        int? invoiceStage = cache.GetValue<usrNAWInvoiceStage>(currentSalesOrder) as int?;
        bool isROorEX = currentSalesOrder.OrderType == "RO" || currentSalesOrder.OrderType == "EX";
        var nvrtExtension = Base.GetExtension<NVRTSOOrderEntryGraphExtension>();
        if (nvrtExtension != null && isInvoiceType == 1 && isROorEX)
        {
            nvrtExtension.NVRTxReturnProcessing.SetEnabled(true);
        }
    }
    #endregion

    #region Story: FOX-737 | Engineer: [Divya Kurumkar] | Date: [2025-03-24] | Display newly created RO from RQ (vice versa) in side panels. 
    private void HandleRefOrderFields(Events.RowSelected<SOOrder> e)
    {
        SOOrder order = e.Row;
        SOOrderExt orderExt = PXCache<SOOrder>.GetExtension<SOOrderExt>(order);
        bool isRQOrder = order.OrderType == "RQ";
        bool isROOrder = order.OrderType == "RO";
        SOLine relatedLine = null;

        if (isRQOrder)
        {
            relatedLine = PXSelect<SOLine,
                            Where<SOLine.origOrderNbr, Equal<Required<SOOrder.orderNbr>>,
                                  And<SOLine.origOrderType, Equal<Required<SOOrder.orderType>>>>,
                            OrderBy<Desc<SOLine.orderNbr>>>
                            .SelectWindowed(Base, 0, 1, e.Row.OrderNbr, e.Row.OrderType);
        }
        else if (isROOrder)
        {
            relatedLine = PXSelect<SOLine,
                            Where<SOLine.orderNbr, Equal<Required<SOOrder.origOrderNbr>>,
                                  And<SOLine.orderType, Equal<Required<SOOrder.origOrderType>>>>,
                            OrderBy<Desc<SOLine.orderNbr>>>
                            .SelectWindowed(Base, 0, 1, order.OrigOrderNbr, order.OrigOrderType);
        }

        if (relatedLine != null)
        {
            orderExt.RefOrderNbr = relatedLine.OrderNbr;
            orderExt.RefOrderType = relatedLine.OrderType;
        }
    }
    private void HandleRelatedROLineFields(PXCache cache, SOLine sOLineDetails)
    {
        SOLine relatedROLine = RelatedROLines.SelectWindowed(0, 1);
        if (relatedROLine != null)
        {
            cache.SetValueExt<SOLine.origOrderNbr>(sOLineDetails, relatedROLine.OrderNbr);
            cache.SetValueExt<SOLine.origOrderType>(sOLineDetails, relatedROLine.OrderType);
        }
        PXUIFieldAttribute.SetEnabled<SOLine.origOrderNbr>(cache, sOLineDetails, true);
        PXUIFieldAttribute.SetEnabled<SOLine.origOrderType>(cache, sOLineDetails, true);
    }
    #endregion

    #region Story: FOX-756 | Engineer: [Abhishek Sonone] | Date: [2025-03-31] | If Order Type = RO, then disable the Copy Order button for non-Admin users and enable it for Admin users.
    private void HandleCopyOrderButton(SOOrder currentSalesOrder, bool isSystemAdmin)
    {
        bool isROOrder = currentSalesOrder.OrderType == "RO";
        Base.copyOrder.SetEnabled(isSystemAdmin || !isROOrder);
    }
    #endregion

    #region Story: FOX-181 | Engineer: [Abhishek Sonone] | Date: [2025-02-04] | Allow editing of the TranDesc field on SOLine only for users with the Administrator role.
    private void HandleTranDescField(PXCache cache, SOLine sOLineDetails, bool isSystemAdmin)
    {
        PXUIFieldAttribute.SetEnabled<SOLine.tranDesc>(cache, sOLineDetails, isSystemAdmin);
    }
    #endregion

    #region Story: FOX-595 | Engineer: [Satej Ambekar] | Date: [2025-04-02] | Access for "Cycle Billing" and "Release" functions, and "E&C Add-on charges" and the "billing hold flag".
    private void HandleCuryUnitPriceField(PXCache cache, SOLine sOLineDetails)
    {
        List<string> userRolesList = UserRoleHelper.GetCurrentUserRolesList();
        bool hasCycleBillingAccess = userRolesList.Contains("Administrator") ||
                                     userRolesList.Contains("AR Admin") ||
                                     userRolesList.Contains("CR Sales Representative");
        PXUIFieldAttribute.SetEnabled<SOLine.curyUnitPrice>(cache, sOLineDetails, hasCycleBillingAccess);
    }
    #endregion

    #region Story: FOX-638 | Engineer: [Divya Kurumkar] | Date: [2025-03-31] | Show popup warning if rental or sale line is added with quantity 0, preventing save until quantity > 1.
    private void ValidateOrderQty(SOLine sOLineDetails)
    {
        if (sOLineDetails != null && sOLineDetails.OrderQty == 0)
        {
            throw new PXRowPersistingException(nameof(SOLine.orderQty), sOLineDetails.OrderQty, Descriptor.Messages.QuantityMustBeGreaterThanZero);
        }
    }
    #endregion

    #region Story: FOX-810 | Engineer: [Abhishek] | Date: [2025-05-29] | Display a "Created Date" column on the Shipments tab showing the billing document generation date when a billing document is present.
    private void HandleSoShipmentFields(PXCache cache, SOOrderShipment currentSOOrderShipment)
    {
        if (currentSOOrderShipment != null)
        {
            ARRegister aRRegister = PXSelect<ARRegister,
                                    Where<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>.Select(Base, currentSOOrderShipment.InvoiceNbr);

            currentSOOrderShipment.GetExtension<SOOrderShipmentExtensions>().UsrcreatedDate = aRRegister?.CreatedDateTime;
        }
    }
    #endregion

    #region Story: FOX-900  | Engineer: [Satej Ambekar] | Date: [2025-05-30] | Hide Order Level Discount field from SOOrder & ARInvoice
    private void SetDocumentDiscountVisibility(PXCache cache, SOOrder row)
    {
        List<string> userRolesList = UserRoleHelper.GetCurrentUserRolesList();
        bool isSystemAdmin = userRolesList.Contains("Administrator");
        PXUIFieldAttribute.SetVisible<SOOrder.curyDiscTot>(cache, row, isSystemAdmin);
    }
    #endregion

    #region Story: FOX-901 | Engineer: [Satej Ambekar] | Date: [2025-07-03] | Add Release Date to Billing Document 
    private void SetShipmentReleaseDateFromInvoice(SOOrderShipment shipment)
    {
        if (shipment == null) return;

        var ext = shipment.GetExtension<SOOrderShipmentExtensions>();
        ext.UsrReleaseDate = null;

        if (!string.IsNullOrEmpty(shipment.InvoiceNbr))
        {
            var arInvoice = PXSelect<ARInvoice,
                Where<ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>
                .Select(Base, shipment.InvoiceNbr)
                .FirstOrDefault();

            if (arInvoice != null)
            {
                var arExt = PXCache<ARInvoice>.GetExtension<ARInvoiceExtension>(arInvoice);
                ext.UsrReleaseDate = arExt?.UsrReleaseDate;
            }
        }
    }
    #endregion
    #region Story: FOX-946 | Engineer: [Satej Ambekar] | Date: [2025-06-02] | Event Start/End Date - dates should not be editable after the invoice has been released
    private void DisableEventDatesOnOrderIfReleased(PXCache cache, SOOrder order)
    {
        if (order == null) return;
        var arTrans = SelectFrom<ARTran>
            .Where<ARTran.sOOrderType.IsEqual<@P.AsString>
                .And<ARTran.sOOrderNbr.IsEqual<@P.AsString>>>
            .OrderBy<ARTran.refNbr.Asc>
            .View.Select(Base, order.OrderType, order.OrderNbr)
            .FirstTableItems
            .ToList();
        bool anyReleased = false;
        foreach (var tran in arTrans)
        {
            var arInvoice = SelectFrom<ARInvoice>
                .Where<ARInvoice.docType.IsEqual<@P.AsString>
                    .And<ARInvoice.refNbr.IsEqual<@P.AsString>>>
                .View.Select(Base, tran.TranType, tran.RefNbr)
                .FirstTableItems
                .FirstOrDefault();
            if (arInvoice != null)
            {
                var arExt = PXCache<ARInvoice>.GetExtension<ARInvoiceExtension>(arInvoice);
                if (arExt?.UsrReleaseDate != null)
                {
                    anyReleased = true;
                    break;
                }
            }

        }
        if (anyReleased)
        {
            bool isStartDateEnabled = cache.GetAttributesOfType<PXUIFieldAttribute>(null, "UsrNAWEventStartDate")
                .FirstOrDefault()?.Enabled ?? true;
            bool isEndDateEnabled = cache.GetAttributesOfType<PXUIFieldAttribute>(null, "UsrNAWEventEndDate")
                .FirstOrDefault()?.Enabled ?? true;

            if (isStartDateEnabled)
                PXUIFieldAttribute.SetEnabled(cache, order, "UsrNAWEventStartDate", false);

            if (isEndDateEnabled)
                PXUIFieldAttribute.SetEnabled(cache, order, "UsrNAWEventEndDate", false);
        }
    }

    #endregion
    #endregion

    #region Helper Methods

    #region Story: FOX-733  | Engineer: [Satej Ambekar] | Date: [2025-03-24] 
    [PXButton(CommitChanges = true)]
    [PXUIField(DisplayName = "Print Quote", MapEnableRights = PXCacheRights.Select)]
    public virtual IEnumerable PrintQuote(PXAdapter adapter, string reportID = null)
    {
        return Report(adapter.Apply(delegate (PXAdapter it)
        {
            it.Menu = "Print Quote";
        }), reportID ?? "SO641000");
    }

    [PXUIField(DisplayName = "Reports", MapEnableRights = PXCacheRights.Select)]
    [PXButton(SpecialType = PXSpecialButtonType.ReportsFolder, MenuAutoOpen = true)]
    public virtual IEnumerable Report(PXAdapter adapter, [PXString(8, InputMask = "CC.CC.CC.CC")] string reportID)
    {
        List<SOOrder> list = adapter.Get<SOOrder>().ToList();
        if (!string.IsNullOrEmpty(reportID))
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string text = null;
            PXReportRequiredException ex = null;
            Dictionary<PrintSettings, PXReportRequiredException> reportsToPrint = new Dictionary<PrintSettings, PXReportRequiredException>();
            foreach (SOOrder item in list)
            {
                dictionary = new Dictionary<string, string>();
                dictionary["SOOrder.OrderType"] = item.OrderType;
                dictionary["SOOrder.OrderNbr"] = item.OrderNbr;
                string quoteType = item.GetExtension<NAWSOOrderUSSExt>()?.UsrNAWDefaultFenceQuoteType;
                if (!string.IsNullOrEmpty(quoteType))
                {
                    string format = quoteType == "S" ? "S" : "D";
                    dictionary["Format"] = format;
                }
                text = new NotificationUtility(Base).SearchCustomerReport(reportID, item.CustomerID, item.BranchID);
                ex = PXReportRequiredException.CombineReport(ex, text, dictionary, OrganizationLocalizationHelper.GetCurrentLocalization(Base));
                ex.Mode = PXBaseRedirectException.WindowMode.New;
                reportsToPrint = SMPrintJobMaint.AssignPrintJobToPrinter(reportsToPrint, dictionary, adapter, new NotificationUtility(Base).SearchPrinter, "Customer", reportID, text, item.BranchID, OrganizationLocalizationHelper.GetCurrentLocalization(Base));
            }
            if (ex != null)
            {
                Base.LongOperationManager.StartOperation(async delegate (CancellationToken ct)
                {
                    await SMPrintJobMaint.CreatePrintJobGroups(reportsToPrint, ct);
                    throw ex;
                });
            }
        }
        return list;
    }
    #endregion

    #endregion
}