using PX.Data;
using System;

namespace NV.Rental360.ChangeOrders
{
    public sealed class NVRTChangeOrderExt : PXCacheExtension<NVRTChangeOrder>
    {
        public static bool IsActive() => true;

        #region MaxDiscPct
        [PXDecimal(6)]
        [PXUIField(DisplayName = "Max Discount Percent", Enabled = false, IsReadOnly = true)]
        public decimal? MaxDiscPct { get; set; }
        public abstract class maxDiscPct : PX.Data.BQL.BqlDecimal.Field<maxDiscPct> { }
        #endregion
    }
}