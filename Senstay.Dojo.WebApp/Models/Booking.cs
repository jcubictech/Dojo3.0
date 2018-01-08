using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("Booking")]
    public partial class Booking
    {
        [Key]
        public string Lease_ID { get; set; }
        public Nullable<System.DateTime> Lease_Date { get; set; }
        public System.DateTime Arrive { get; set; }
        public System.DateTime Depart { get; set; }
        public int Nights { get; set; }
        public Nullable<int> Occupants { get; set; }
        public Nullable<int> Occupants_under_12 { get; set; }
        public string Status { get; set; }
        public string Location_Name { get; set; }
        public string Room_Type_Name { get; set; }
        public string Unit_Name { get; set; }
        public int Bedrooms { get; set; }
        public string Tenant_Last_Name { get; set; }
        public string Tenant_First_Name { get; set; }
        public string Tenant_Home__ { get; set; }
        public string Tenant_Cell__ { get; set; }
        public string Tenant_Email { get; set; }
        public Nullable<int> Tenant_Zip { get; set; }
        public string Tenant_City { get; set; }
        public string Tenant_State { get; set; }
        public string Tenant_Address { get; set; }
        public string Tenant_Country { get; set; }
        public int Gross_Total { get; set; }
        public Nullable<int> Taxable_Revenue { get; set; }
        public Nullable<int> MGMT_Fee { get; set; }
        public Nullable<int> TA_Commission { get; set; }
        public Nullable<int> Net_Revenue { get; set; }
        public Nullable<int> Cancell_Fee { get; set; }
        public Nullable<int> Empt1 { get; set; }
        public Nullable<int> Empt2 { get; set; }
    }
}
