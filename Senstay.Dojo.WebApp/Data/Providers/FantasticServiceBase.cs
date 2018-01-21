using System.Linq;
using OfficeOpenXml;
using Senstay.Dojo.Models;
using System.Collections.Generic;

namespace Senstay.Dojo.Data.Providers
{
    public class FantasticServiceBase
    {
        private readonly DojoDbContext _context;
        protected const int _dateCol = 1;

        public FantasticServiceBase(DojoDbContext dbContext)
        {
            _context = dbContext;
        }

        protected List<string> ParsePropertyRow(ExcelRange cells, int row, int totalCols)
        {
            var properties = new List<string>();

            if (cells[row, _dateCol].Text != string.Empty && cells[row, _dateCol + 1].Text != string.Empty)
            {
                for (int col = _dateCol + 1; col <= totalCols; col++) // excel column index starts from 1
                {
                    properties.Add(GetSafeCellString(cells[row, col].Value));
                }
            }
            return properties;
        }

        protected List<int> MapPropertyToIds(List<string> properties)
        {
            var ids = new List<int>();
            foreach(string property in properties)
            {
                var id = MapProperty(property);
                if (id != 0) ids.Add(id);
            }
            return ids;
        }

        protected int MapProperty(string property)
        {
            var map = _context.PropertyFantasticMaps.Where(x => x.PropertyCode == property).FirstOrDefault();
            int listingId = 0;
            if (map != null)
            {
                if (int.TryParse(map.ListingId, out listingId) == false) listingId = 0;
            }
            return listingId;
        }

        protected string GetSafeCellString(object cellValue, string defaultValue = "")
        {
            return cellValue == null ? defaultValue : cellValue.ToString();
        }
    }
}
