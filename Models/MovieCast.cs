using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models
{
    public class MovieCast
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Movie")]
        public int MovieId { get; set; }

        [ForeignKey("Actor")]
        public int ActorId { get; set; }

        public string Role { get; set; }

        public Movie Movie { get; set; }
        public Actor Actor { get; set; }
    }
}