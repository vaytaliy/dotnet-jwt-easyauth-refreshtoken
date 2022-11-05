using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Recipes.Models
{
    public class Recipe
    {
        [Key]
        public int Id { get; set; }
        public string RecipeName { get; set; }
    }
}
