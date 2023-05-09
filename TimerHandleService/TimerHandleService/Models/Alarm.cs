using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Models
{
    public class Alarm
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string eqpname { get; set; }

        public Int32 id { get; set; }

        public string message { get; set; }

        [SugarColumn(InsertServerTime = true)]
        public DateTime datetime { get; set; }
    }
}