#region Usings

using System.Collections.Generic;
#endregion

namespace UltimatePlaylist.Common.Filters.Models
{
    public class FilterModel
    {
        #region Constructor(s)

        public FilterModel()
        {
            ValueFilters = new List<ValueFilter>();
            QuantityFilters = new List<QuantityFilter>();
            EnumFilters = new List<EnumFilter>();
            Filters = new UserManagementRequestModel();
        }

        #endregion

        #region Public Properties

        public IEnumerable<ValueFilter> ValueFilters { get; set; }

        public IEnumerable<QuantityFilter> QuantityFilters { get; set; }

        public IEnumerable<EnumFilter> EnumFilters { get; set; }

        public UserManagementRequestModel Filters { get; set; }

        public class UserManagementRequestModel
        {
            public List<string> Genders { get; set; }

            public string ZipCode { get; set; }

        }

        #endregion
    }
}
