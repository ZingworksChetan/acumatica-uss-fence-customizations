using System;
using DAC;
using NV.Rental360.WorkOrders;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;

namespace NV.Rental360
{
    public class NVRTWorkOrderMaint_Extension : PXGraphExtension<NVRTWorkOrderMaint>
    {
        public static bool IsActive() => true;

        #region Story: FOX-612 | Engineer: [Satej Ambekar] | Date: [2025-03-07] | Set time to 4AM for WorkOrder

        #region Events
        protected virtual void _(Events.FieldDefaulting<NVRTWorkOrder, NVRTWorkOrder.scheduleStartDate> e)
        {
            e.NewValue = SetTimeTo4AM(e.NewValue);
        }

        protected virtual void _(Events.FieldDefaulting<NVRTWorkOrder, NVRTWorkOrder.scheduleEndDate> e)
        {
            e.NewValue = SetTimeTo4AM(e.NewValue);
        }

        protected virtual void _(Events.FieldDefaulting<NVRTWorkOrder, NVRTWorkOrder.actualStartDate> e)
        {
            e.NewValue = SetTimeTo4AM(e.NewValue);
        }

        protected virtual void _(Events.FieldDefaulting<NVRTWorkOrder, NVRTWorkOrder.actualEndDate> e)
        {
            e.NewValue = SetTimeTo4AM(e.NewValue);
        }

        protected virtual void _(Events.FieldDefaulting<NVRTWorkOrder, NVRTWorkOrder.arrivalTimeEst> e)
        {
            e.NewValue = SetTimeTo4AM(e.NewValue);
        }

        protected virtual void _(Events.FieldDefaulting<NVRTWorkOrder, NVRTWorkOrder.arrivalTimeAct> e)
        {
            e.NewValue = SetTimeTo4AM(e.NewValue);
        }

        protected virtual void _(Events.RowPersisting<NVRTWorkOrder> e)
        {
            if (e.Row is NVRTWorkOrder row)
            {
                AdjustAndSetFieldValues(e.Cache, row);
            }
        }

        protected virtual void _(Events.RowSelected<NVRTWorkOrder> e)
        {
            if (e.Row is NVRTWorkOrder row)
            {
                AdjustRowFieldValues(row);
            }
        }
        
        #endregion

        #region Helper Methods
        private void AdjustAndSetFieldValues(PXCache cache, NVRTWorkOrder row)
        {
            cache.SetValueExt<NVRTWorkOrder.scheduleStartDate>(row, AdjustTime(row.ScheduleStartDate));
            cache.SetValueExt<NVRTWorkOrder.scheduleEndDate>(row, AdjustTime(row.ScheduleEndDate));
            cache.SetValueExt<NVRTWorkOrder.actualStartDate>(row, AdjustTime(row.ActualStartDate));
            cache.SetValueExt<NVRTWorkOrder.actualEndDate>(row, AdjustTime(row.ActualEndDate));
            cache.SetValueExt<NVRTWorkOrder.arrivalTimeEst>(row, AdjustTime(row.ArrivalTimeEst));
            cache.SetValueExt<NVRTWorkOrder.arrivalTimeAct>(row, AdjustTime(row.ArrivalTimeAct));
        }

        private void AdjustRowFieldValues(NVRTWorkOrder row)
        {
            row.ScheduleStartDate = AdjustTime(row.ScheduleStartDate);
            row.ScheduleEndDate = AdjustTime(row.ScheduleEndDate);
            row.ActualStartDate = AdjustTime(row.ActualStartDate);
            row.ActualEndDate = AdjustTime(row.ActualEndDate);
            row.ArrivalTimeEst = AdjustTime(row.ScheduleStartDate);
            row.ArrivalTimeAct = AdjustTime(row.ScheduleStartDate);
        }

        private DateTime? AdjustTime(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                DateTime dt = dateTime.Value;
                DateTime fourAM = dt.Date.AddHours(4);
                return dt < fourAM ? fourAM : dt;
            }
            return null;
        }

        private DateTime? SetTimeTo4AM(object date)
        {
            DateTime? inputDate = date as DateTime?;
            return inputDate?.Date.AddHours(4) ?? PXTimeZoneInfo.Now.Date.AddHours(4);
        }

        #endregion

        #endregion

    }
}
