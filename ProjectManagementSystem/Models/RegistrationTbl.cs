//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProjectManagementSystem.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class RegistrationTbl
    {
        public int registration_id { get; set; }
        public string project_name { get; set; }
        public Nullable<System.DateTime> date_registered { get; set; }
        public string division { get; set; }
        public string registered_by { get; set; }
        public Nullable<int> year { get; set; }
        public Nullable<bool> is_completed { get; set; }
        public Nullable<bool> unregistered { get; set; }
        public string unregister_reason { get; set; }
        public Nullable<System.DateTime> date_unregistered { get; set; }
        public Nullable<bool> is_file_uploaded { get; set; }
        public Nullable<int> main_id { get; set; }
    }
}
