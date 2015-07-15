using System;

namespace CoreSprint.Models
{
    public class CardWorkDto
    {
        public string Professional { get; set; }
        public string CardName { get; set; }
        public string CardLink { get; set; }
        public DateTime CommentAt { get; set; }
        public DateTime WorkAt { get; set; }
        public double Worked { get; set; }
        public string Comment { get; set; }
    }
}
