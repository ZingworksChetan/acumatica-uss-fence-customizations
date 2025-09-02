using System;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.SO;

namespace DAC
{
    public class SOOrderShipmentExtensions : PXCacheExtension<PX.Objects.SO.SOOrderShipment>
    {
        public static bool IsActive() => true;

        #region Created Date
        public abstract class usrcreatedDate : PX.Data.BQL.BqlDateTime.Field<usrcreatedDate> { }

        protected DateTime? _UsrcreatedDate;
        [PXDate(UseTimeZone = false)]
        [PXUIField(DisplayName = "Billing Document Created Date", Enabled = false)]
        public virtual DateTime? UsrcreatedDate
        {
            get
            {
                return _UsrcreatedDate;
            }
            set
            {
                _UsrcreatedDate = value;
            }
        }
        #endregion

        #region Story: FOX-901 | Engineer: [Satej Ambekar] | Date: [2025-07-03] | Add Release Date to Billing Document 
        #region Release Date
        [PXDateAndTime]
        [PXUIField(DisplayName = "Release Date")]
        public virtual DateTime? UsrReleaseDate { get; set; }
        public abstract class usrReleaseDate : PX.Data.BQL.BqlDateTime.Field<usrReleaseDate> { }
        #endregion
        #endregion
    }
}
