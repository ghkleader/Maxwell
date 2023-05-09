using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Models
{
    public class Equipment
    {
        [SugarColumn(IsPrimaryKey = true,IsIdentity = true)]
        public int id { get; set; }

        public string eqpname { get; set; }

        public string mesip { get; set; }

        public string workline { get; set; }

        public string status { get; set; }

        public string author { get; set; }

        [SugarColumn(InsertServerTime = true)]
        public DateTime datetime { get; set; }

        public string actionstatus { get; set; }

        public string opcxmlnode { get; set; }
    }
}