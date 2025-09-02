using System;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.SO;
using PX.Objects.IN;
using System.Collections;
using PX.Web.UI;
using NAWUnitedSiteServices.Prototype.Extensions.DACExtensions;
using NAWUnitedSiteServices.Descr;
using PX.Common;
using PX.Data.BQL;

namespace AcumaticaUSSFenceCustomizations
{
    #region Story: FOX-782 | Engineer: [Satej Ambekar] | Date: [2025-04-22] | Processing screen updates SHPDELPICK to SHIPDELPICK for eligible RQ/EX/CH/RO orders.
    [Serializable]
    public class UpdateSOLineTaxCategory : PXGraph<UpdateSOLineTaxCategory>
    {
        public PXCancel<SOOrder> Cancel;

        [PXFilterable]
        public PXProcessingJoin<SOOrder,
            InnerJoin<SOLine, On<SOLine.orderType, Equal<SOOrder.orderType>,
                And<SOLine.orderNbr, Equal<SOOrder.orderNbr>>>,
            InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<SOLine.inventoryID>>>>,
            Where2<
            Where<SOLine.taxCategoryID, In3<SHPDELPICK, SHIPDELPICK>>,
            And<SOOrder.status, In3<SOOrderStatus.open, SOOrderStatus.hold>,
                And<NAWSOOrderUSSExt.usrNAWInvoiceStage, In3<NAWUSSInvoiceStage.initialTermPending, NAWUSSInvoiceStage.initialTermPendingInvoice>>>>>
            Orders;

        public UpdateSOLineTaxCategory()
        {
            Orders.SetProcessDelegate(ProcessOrders);
        }

        public static void ProcessOrders(List<SOOrder> orders)
        {
            foreach (SOOrder order in orders)
            {
                try
                {
                    ProcessOrder(order);
                    PXProcessing<SOOrder>.SetInfo(orders.IndexOf(order), "Processed successfully.");
                }
                catch (PXException ex)
                {
                    PXProcessing<SOOrder>.SetError(orders.IndexOf(order), ex.Message);
                }
                catch (Exception ex)
                {
                    PXProcessing<SOOrder>.SetError(orders.IndexOf(order), $"Unexpected error: {ex.Message}");
                }
            }
        }

        public static void ProcessOrder(SOOrder order)
        {
            var graph = PXGraph.CreateInstance<SOOrderEntry>();
            bool isUpdated = false;

            try
            {
                graph.Document.Current = graph.Document.Search<SOOrder.orderNbr>(order.OrderNbr, order.OrderType);

                // Process each line in the order
                foreach (SOLine line in graph.Transactions.Select())
                {
                    if (line.TaxCategoryID == "SHPDELPICK")
                    {
                        line.TaxCategoryID = "SHIPDELPICK";
                        graph.Transactions.Update(line);
                        isUpdated = true;
                    }
                }

                // Save changes if any updates were made
                if (isUpdated)
                {
                    graph.Actions.PressSave();
                }

                // Validation step: Check if any lines still have SHPDELPICK
                foreach (SOLine line in graph.Transactions.Select())
                {
                    if (line.TaxCategoryID == "SHPDELPICK")
                    {
                        // Acuminator disable once PX1050 HardcodedStringInLocalizationMethod [Justification]
                        throw new PXException("Some lines still have the SHPDELPICK tax category.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Acuminator disable once PX1050 HardcodedStringInLocalizationMethod [Justification]
                throw new PXException($"Error processing order {order.OrderType}-{order.OrderNbr}: {ex.Message}");
            }
        }
    }
    public class SHPDELPICK : BqlString.Constant<SHPDELPICK>
    {
        public SHPDELPICK() : base("SHPDELPICK") { }
    }
    public class SHIPDELPICK : BqlString.Constant<SHIPDELPICK>
    {
        public SHIPDELPICK() : base("SHIPDELPICK") { }
    }
    #endregion
}
