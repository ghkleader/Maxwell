using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Models
{
    public class Productivity
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string eqpname { get; set; }

        public Int32 pro { get; set; }

        [SugarColumn(InsertServerTime = true)]
        public DateTime datetime { get; set; }
    }
}