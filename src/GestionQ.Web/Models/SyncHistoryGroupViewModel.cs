using System;
using System.Collections.Generic;
using GestionQ.Domain.Entities;

namespace GestionQ.Web.Models
{
    public class SyncHistoryGroupViewModel
    {
        public DateTime SyncDate { get; set; }
        public List<ProductChangeLog> Logs { get; set; }
    }
}
