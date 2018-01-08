﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senstay.Dojo.Models
{
    [Table("InputError")]
    public class InputError
    {
        [Key]
        public int InputErrorId { get; set; }

        public string InputSource { get; set; }

        public int Row { get; set; }

        public string Section { get; set; }

        public string OriginalText { get; set; }

        public string Message { get; set; }

        public DateTime CreatedTime { get; set; }
    }
}
